// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Dmitry Jemerov <yole@yole.ru>
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (gert.driesen@ardatis.com)
// Hani Atassi (haniatassi@users.sourceforge.net)

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.VisualCpp.Tasks;

using NAnt.VSNet.Tasks;
using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// Visual C++ project.
    /// </summary>
    public class VcProject: ProjectBase {
        #region Public Instance Constructors
        
        public VcProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            if (projectPath == null) {
                throw new ArgumentNullException("projectPath");
            }

            _htPlatformConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _references = new ArrayList();
            _clArgMap = VcArgumentMap.CreateCLArgumentMap();
            _linkerArgMap = VcArgumentMap.CreateLinkerArgumentMap();
            _objFiles = new ArrayList();
            _projectPath = Path.GetFullPath(projectPath);

            _name = xmlDefinition.GetAttribute("Name");
            _guid = xmlDefinition.GetAttribute("ProjectGUID");
            _rootNamespace = xmlDefinition.GetAttribute("RootNamespace");

            XmlNodeList configurationNodes = xmlDefinition.SelectNodes("//Configurations/Configuration");
            foreach (XmlElement configElem in configurationNodes) {
                VcConfiguration config = new VcConfiguration(configElem, this, solution, OutputDir);
                ProjectConfigurations[config.Name] = config;
                _htPlatformConfigurations[config.FullName] = config;
            }

            XmlNodeList projectReferences = xmlDefinition.SelectNodes("//References/ProjectReference");
            foreach (XmlElement referenceElem in projectReferences) {
                ReferenceBase reference = CreateReference(solution, referenceElem);
                _references.Add(reference);
            }

            XmlNodeList assemblyReferences = xmlDefinition.SelectNodes("//References/AssemblyReference");
            foreach (XmlElement referenceElem in assemblyReferences) {
                ReferenceBase reference = CreateReference(solution, referenceElem);
                _references.Add(reference);
            }

            XmlNodeList fileNodes = xmlDefinition.SelectNodes("//File");
            foreach (XmlElement fileElem in fileNodes) {
                string relPath = fileElem.GetAttribute("RelativePath");
                
                Hashtable htFileConfigurations = null;
                XmlNodeList fileConfigList = fileElem.GetElementsByTagName("FileConfiguration");
                if (fileConfigList.Count > 0) {
                    htFileConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
                    foreach (XmlElement fileConfigElem in fileConfigList) {
                        string fileConfigName = fileConfigElem.GetAttribute("Name");
                        VcConfiguration baseConfig = (VcConfiguration) _htPlatformConfigurations[fileConfigName];
                        VcConfiguration fileConfig = new VcConfiguration(fileConfigElem, this, solution, baseConfig, OutputDir);
                        htFileConfigurations [fileConfig.Name] = fileConfig;
                    }
                }

                _htFiles [relPath] = htFileConfigurations;
            }
        }

        #endregion Public Instance Constructors

        #region Override implementation of ProjectBase

        /// <summary>
        /// Gets the name of the Visual C++ project.
        /// </summary>
        public override string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets the type of the project.
        /// </summary>
        /// <value>
        /// The type of the project.
        /// </value>
        public override ProjectType Type {
            get { return ProjectType.VisualC; }
        }

        /// <summary>
        /// Gets the path of the Visual C++ project.
        /// </summary>
        public override string ProjectPath {
            get { return _projectPath; }
        }

        /// <summary>
        /// Gets the directory containing the VS.NET project.
        /// </summary>
        public override DirectoryInfo ProjectDirectory {
            get { return new DirectoryInfo(Path.GetDirectoryName(_projectPath)); }
        }

        /// <summary>
        /// Get the directory in which intermediate build output that is not 
        /// specific to the build configuration will be stored.
        /// </summary>
        /// <remarks>
        /// This is a directory relative to the project directory, 
        /// named <c>temp\</c>.
        /// </remarks>
        public override DirectoryInfo ObjectDir {
            get {
                return new DirectoryInfo(
                    Path.Combine(ProjectDirectory.FullName, "temp"));
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the Visual C++ project.
        /// </summary>
        public override string Guid {
            get { return _guid; }
            set { _guid = value; }
        }

        public override ArrayList References {
            get { return _references; }
        }

        public override ProjectReferenceBase CreateProjectReference(ProjectBase project, bool isPrivateSpecified, bool isPrivate) {
            return new VcProjectReference(project, this, isPrivateSpecified, 
                isPrivate);
        }

        /// <summary>
        /// Verifies whether the specified XML fragment represents a valid project
        /// that is supported by this <see cref="ProjectBase" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <exception cref="BuildException">
        ///   <para>The XML fragment is not supported by this <see cref="ProjectBase" />.</para>
        ///   <para>-or-</para>
        ///   <para>The XML fragment does not represent a valid project (for this <see cref="ProjectBase" />).</para>
        /// </exception>
        protected override void VerifyProjectXml(XmlElement docElement) {
            if (!IsSupported(docElement)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid Visual C++ project.", 
                    ProjectPath), Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="docElement">The document element of the project.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        protected override ProductVersion DetermineProductVersion(XmlElement docElement) {
            return GetProductVersion(docElement);
        }

        protected override bool Build(ConfigurationBase config) {
            _objFiles.Clear();
            
            VcConfiguration baseConfig = (VcConfiguration) config;

            // initialize hashtable for holding the C++ sources for each build
            // configuration
            //
            // the key of the hashtable is a build configuration, and the
            // value is an ArrayList holding the C++ source files for that
            // build configuration
            Hashtable buildConfigs = new Hashtable();

            // initialize hashtable for holding the resources for each build
            // configuration
            //
            // the key of the hashtable is a build configuration, and the
            // value is an ArrayList holding the resources files for that
            // build configuration
            Hashtable buildRcConfigs = new Hashtable();

            foreach (DictionaryEntry de in _htFiles) {
                string fileName = (string) de.Key;
                string ext = Path.GetExtension(fileName).ToLower(CultureInfo.InvariantCulture);
                
                VcConfiguration fileConfig = null;
                if (de.Value != null) {
                    if (de.Value is Hashtable) {
                        Hashtable _htValue = (Hashtable) de.Value;
                        fileConfig = (VcConfiguration) _htValue[baseConfig.Name];
                    } else {
                        fileConfig = (VcConfiguration) de.Value;
                    }
                }

                if (fileConfig == null) {
                    fileConfig = baseConfig;
                }
                
                if (fileConfig.ExcludeFromBuild) {
                    continue;
                }

                if (ext == ".cpp" || ext == ".c") {
                    if (!buildConfigs.ContainsKey(fileConfig)) {
                        buildConfigs[fileConfig] = new ArrayList(1);
                    }

                    // add file to list of sources to build with this config
                    ((ArrayList) buildConfigs[fileConfig]).Add(fileName);
                } else if (ext == ".rc") {
                    if (!buildRcConfigs.ContainsKey(fileConfig)) {
                        buildRcConfigs[fileConfig] = new ArrayList(1);
                    }

                    // add file to list of resources to build with this config
                    ((ArrayList) buildRcConfigs[fileConfig]).Add(fileName);
                }
            }

            string nmakeCommand = baseConfig.GetToolSetting("VCNMakeTool", "BuildCommandLine");
            if (nmakeCommand != null) {
                RunNMake(nmakeCommand);
                return true;
            }

            VcConfiguration stdafxConfig = null;

            // If VC project uses precompiled headers then the precompiled header
            // output (.pch file) must be generated before compiling any other
            // source files.
            foreach (VcConfiguration vcConfig in buildConfigs.Keys) {
                const string compilerTool = "VCCLCompilerTool";
                Hashtable compilerArgs = vcConfig.GetToolArguments(compilerTool, _clArgMap);   

                object o = compilerArgs["UsePrecompiledHeader"];

                if (o != null) {
                    string arg = o as string;
                    if (arg.Equals("/Yc")) {
                        BuildCPPFiles((ArrayList) buildConfigs[vcConfig], baseConfig, vcConfig);
                        stdafxConfig = vcConfig;
                    }
                }
            }

            foreach (VcConfiguration vcConfig in buildConfigs.Keys) {
                if (vcConfig != stdafxConfig) {
                    BuildCPPFiles((ArrayList) buildConfigs[vcConfig], baseConfig,
                        vcConfig);
                }
            }

            // build resource files
            foreach (VcConfiguration rcConfig in buildRcConfigs.Keys) {
                BuildResourceFiles((ArrayList) buildRcConfigs[rcConfig],
                    rcConfig);
            }

            string libOutput = baseConfig.GetToolSetting("VCLibrarianTool", "OutputFile");
            if (libOutput != null) {
                RunLibrarian(baseConfig);
            } else {
                string linkOutput = baseConfig.GetToolSetting("VCLinkerTool", "OutputFile");
                if (linkOutput != null) {
                    RunLinker(baseConfig);
                }
            }

            Log(Level.Verbose, "Copying references:");

            foreach (ReferenceBase reference in _references) {
                if (reference.CopyLocal) {
                    Log(Level.Verbose, " - " + reference.Name);

                    Hashtable outputFiles = reference.GetOutputFiles(config);

                    foreach (DictionaryEntry de in outputFiles) {
                        // determine file to copy
                        FileInfo srcFile = new FileInfo((string) de.Key);
                        // determine destination file
                        FileInfo destFile = new FileInfo(Path.Combine(
                            config.OutputDir.FullName, (string) de.Value));
                        // perform actual copy
                        CopyFile(srcFile, destFile, SolutionTask);
                    }
                }
            }

            return true;
        }

        #endregion Override implementation of ProjectBase

        #region Protected Internal Instance Methods

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro or <see langword="null" /> if the macro is not
        /// supported.
        /// </returns>
        protected internal override string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "inputdir":
                    return Path.GetDirectoryName(ProjectPath)
                        + Path.DirectorySeparatorChar;
                case "inputname":
                    return Path.GetFileNameWithoutExtension(ProjectPath);
                case "inputpath":
                    return ProjectPath;
                case "inputfilename": // E.g. Inc.vcproj
                    return Path.GetFileName(ProjectPath);
                case "inputext": // E.g. .vcproj
                    return Path.GetExtension(ProjectPath);
                case "safeparentname":
                    return Name;
                case "safeinputname":
                    return Path.GetFileNameWithoutExtension(ProjectPath);
                default:
                    return base.ExpandMacro(macro);
            }
        }

        #endregion Protected Internal Instance Methods

        #region Protected Instance Methods

        protected virtual ReferenceBase CreateReference(SolutionBase solution, XmlElement xmlDefinition) {
            if (solution == null) {
                throw new ArgumentNullException("solution");
            }
            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }

            switch (xmlDefinition.Name) {
                case "ProjectReference":
                    // project reference
                    return new VcProjectReference(xmlDefinition, ReferencesResolver,
                        this, solution, solution.TemporaryFiles, GacCache, 
                        OutputDir);
                case "AssemblyReference":
                    // assembly reference
                    return new VcAssemblyReference(xmlDefinition, ReferencesResolver, 
                        this, GacCache);
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "\"{0}\" reference not supported.", xmlDefinition.Name),
                        Location.UnknownLocation);
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private void RunNMake(string nmakeCommand) {
            // store current directory
            string originalCurrentDirectory = Directory.GetCurrentDirectory();

            try {
                // change current directory to directory containing
                // project file
                Directory.SetCurrentDirectory(ProjectDirectory.FullName);

                // execute command
                ExecTask nmakeTask = new ExecTask();
                nmakeTask.Project = SolutionTask.Project;
                nmakeTask.Parent = SolutionTask;
                nmakeTask.Verbose = SolutionTask.Verbose;
                nmakeTask.CommandLineArguments = 
                    "/c \"" + nmakeCommand + "\"";
                nmakeTask.FileName = "cmd.exe";
                ExecuteInProjectDirectory(nmakeTask);
            } finally {
                // restore original current directory
                Directory.SetCurrentDirectory(originalCurrentDirectory);
            }
        }

        private void BuildCPPFiles(ArrayList fileNames, VcConfiguration baseConfig, VcConfiguration fileConfig) {
            const string compilerTool = "VCCLCompilerTool";

            string intermediateDir = Path.Combine(ProjectDirectory.FullName, 
                fileConfig.IntermediateDir);

            // create instance of Cl task
            ClTask clTask = new ClTask();

            // inherit project from solution task
            clTask.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            clTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            clTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            clTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            clTask.InitializeTaskConfiguration();

            // set parent of child elements
            clTask.IncludeDirs.Parent = clTask;
            clTask.Sources.Parent = clTask;
            clTask.MetaDataIncludeDirs.Parent = clTask;
            clTask.ForcedUsingFiles.Parent = clTask;

            // inherit project from solution task for child elements
            clTask.IncludeDirs.Project = clTask.Project;
            clTask.Sources.Project = clTask.Project;
            clTask.MetaDataIncludeDirs.Project = clTask.Project;
            clTask.ForcedUsingFiles.Project = clTask.Project;

            // set namespace manager of child elements
            clTask.IncludeDirs.NamespaceManager = clTask.NamespaceManager;
            clTask.Sources.NamespaceManager = clTask.NamespaceManager;
            clTask.MetaDataIncludeDirs.NamespaceManager = clTask.NamespaceManager;
            clTask.ForcedUsingFiles.NamespaceManager = clTask.NamespaceManager;

            // set base directories
            clTask.IncludeDirs.BaseDirectory = fileConfig.ProjectDir;
            clTask.Sources.BaseDirectory = fileConfig.ProjectDir;
            clTask.MetaDataIncludeDirs.BaseDirectory = fileConfig.ProjectDir;
            clTask.ForcedUsingFiles.BaseDirectory = fileConfig.ProjectDir;

            // set task properties
            clTask.OutputDir = new DirectoryInfo(intermediateDir);
            clTask.PchFile = fileConfig.GetToolSetting(compilerTool, "PrecompiledHeaderFile");
            clTask.CharacterSet = fileConfig.CharacterSet;
            
            // ensure output directory exists
            if (!clTask.OutputDir.Exists) {
                clTask.OutputDir.Create();
                clTask.OutputDir.Refresh();
            }

            string includeDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalIncludeDirectories");
            if (!StringUtils.IsNullOrEmpty(includeDirs)) {
                foreach (string includeDir in includeDirs.Split(',', ';')) {
                    if (includeDir.Length == 0) {
                        continue;
                    }
                    clTask.IncludeDirs.DirectoryNames.Add(
                        CleanPath(includeDir));
                }
            }

            string metadataDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalUsingDirectories");
            if (!StringUtils.IsNullOrEmpty(metadataDirs)) {
                foreach (string metadataDir in metadataDirs.Split(';')) {
                    if (metadataDir.Length == 0) {
                        continue;
                    }
                    clTask.MetaDataIncludeDirs.DirectoryNames.Add(
                        CleanPath(baseConfig.ExpandMacros(metadataDir)));
                }
            }

            string forcedUsingFiles = fileConfig.GetToolSetting(compilerTool, "ForcedUsingFiles");
            if (!StringUtils.IsNullOrEmpty(forcedUsingFiles)) {
                foreach (string forcedUsingFile in forcedUsingFiles.Split(';')) {
                    if (forcedUsingFile.Length == 0) {
                        continue;
                    }
                    clTask.ForcedUsingFiles.Includes.Add(
                        CleanPath(baseConfig.ExpandMacros(forcedUsingFile)));
                }
            }

            // add project and assembly references
            foreach (ReferenceBase reference in References) {
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    fileConfig);
                foreach (string assemblyFile in assemblyReferences) {
                    clTask.ForcedUsingFiles.Includes.Add(assemblyFile);
                }
            }

            string asmOutput = fileConfig.GetToolSetting(compilerTool, "AssemblerOutput");
            string asmListingLocation = fileConfig.GetToolSetting(compilerTool, "AssemblerListingLocation");
            if (asmOutput != null && asmOutput != "0" && asmListingLocation != null) {
                // parameter for AssemblerOutput itself will be handled by the map
                clTask.Arguments.Add(new Argument("/Fa\"" + asmListingLocation + "\""));
            }

            foreach (string fileName in fileNames) {
                clTask.Sources.FileNames.Add(fileName);
                _objFiles.Add(Path.Combine(intermediateDir, 
                    Path.GetFileNameWithoutExtension(fileName) + ".obj"));
            }

            string preprocessorDefs = MergeCompilerToolSetting(baseConfig, fileConfig, "PreprocessorDefinitions");
            if (!StringUtils.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string def in preprocessorDefs.Split(';', ',')) {
                    if (def.Length != 0) {
                        clTask.Arguments.Add(new Argument("/D"));
                        clTask.Arguments.Add(new Argument(def));
                    }
                }
            }

            string undefinePreprocessorDefs = fileConfig.GetToolSetting(compilerTool, "UndefinePreprocessorDefinitions");
            if (!StringUtils.IsNullOrEmpty(undefinePreprocessorDefs)) { 
                foreach (string def in undefinePreprocessorDefs.Split(';', ',')) { 
                    clTask.Arguments.Add(new Argument("/U"));
                    clTask.Arguments.Add(new Argument(def));
                }
            }


            string addOptions = baseConfig.GetToolSetting(compilerTool, "AdditionalOptions");
            if (addOptions != null) {
                using (StringReader reader = new StringReader(addOptions)) {
                    string addOptionsLine = reader.ReadLine();
                    while (addOptionsLine != null) {
                        foreach(string addOption in addOptionsLine.Split(' '))  {
                            clTask.Arguments.Add(new Argument(addOption));
                        }
                        addOptionsLine = reader.ReadLine();
                    }
                }
            }

            string exceptionHandling = baseConfig.GetToolSetting(compilerTool, "ExceptionHandling");
            if (exceptionHandling == null || exceptionHandling.ToUpper().Equals("TRUE")) {
                clTask.Arguments.Add(new Argument("/EHsc"));
            }

            if (IsOutputDll(fileConfig)) {
                clTask.Arguments.Add(new Argument("/D"));
                clTask.Arguments.Add(new Argument("_WINDLL"));
            }

            if (fileConfig.WholeProgramOptimization) {
                clTask.Arguments.Add(new Argument("/GL"));
            }
            
            Hashtable compilerArgs = fileConfig.GetToolArguments(compilerTool, _clArgMap);   
            foreach (string key in compilerArgs.Keys) {
                switch (key) {
                    case "PrecompiledHeaderThrough":
                    case "PrecompiledHeaderFile":
                        // skip these as they will only be used in combination 
                        // with the "UsePrecompiledHeader" argument
                        break;
                    case "UsePrecompiledHeader":
                        string arg = compilerArgs["UsePrecompiledHeader"] as string;

                        // if arg.length == 0 then this configuration explicitly turns
                        // OFF use of precompiled headers - even if parent config uses it.
                        if (arg == null || arg.Length > 0) {
                            string headerThrough = compilerArgs["PrecompiledHeaderThrough"] as string;
                            if (headerThrough == null) {
                                headerThrough = "StdAfx.h";
                            }
                            clTask.Arguments.Add(new Argument(((string) compilerArgs[key]) + "\"" + headerThrough + "\""));

                            string headerFile = compilerArgs["PrecompiledHeaderFile"] as string;
                            if (headerFile == null) {
                                headerFile = fileConfig.ExpandMacros("$(IntDir)/$(TargetName).pch");
                            }

                            clTask.Arguments.Add(new Argument("/Fp\"" + headerFile + "\""));
                        }
                        break;
                    default:
                        clTask.Arguments.Add(new Argument((string) compilerArgs[key]));
                        break;
                }
            }

            // check for shared MFC
            if (baseConfig.UseOfMFC == UseOfMFC.Shared) {
                clTask.Arguments.Add(new Argument("/D"));
                clTask.Arguments.Add(new Argument("_AFXDLL"));
            }

            // check for shared ATL
            switch (baseConfig.UseOfATL) {
                case UseOfATL.Shared:
                    clTask.Arguments.Add(new Argument("/D"));
                    clTask.Arguments.Add(new Argument("_ATL_DLL"));
                    break;
                case UseOfATL.Static:
                    clTask.Arguments.Add(new Argument("/D"));
                    clTask.Arguments.Add(new Argument("_ATL_STATIC_REGISTRY"));
                    break;
            }
                
            // enable/disable Managed Extensions for C++
            clTask.ManagedExtensions = fileConfig.ManagedExtensions;

            // execute the task
            ExecuteInProjectDirectory(clTask);
        }

        /// <summary>
        /// Build resource files for the given configuration.
        /// </summary>
        /// <param name="fileNames">The resource files to build.</param>
        /// <param name="fileConfig">The build configuration.</param>
        private void BuildResourceFiles(ArrayList fileNames, VcConfiguration fileConfig) {
            const string compilerTool = "VCResourceCompilerTool";

            string intermediateDir = Path.Combine(ProjectDirectory.FullName, 
                fileConfig.IntermediateDir);

            // create instance of RC task
            RcTask rcTask = new RcTask();

            // inherit project from solution task
            rcTask.Project = SolutionTask.Project;

            // Set the base directory
            rcTask.BaseDirectory = fileConfig.ProjectDir;

            // inherit namespace manager from solution task
            rcTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            rcTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            rcTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            rcTask.InitializeTaskConfiguration();

            // Store the options to pass to the resource compiler in the options variable
            StringBuilder options = new StringBuilder();

            // Collect options

            string ignoreStandardIncludePath = fileConfig.GetToolSetting(compilerTool, "IgnoreStandardIncludePath");
            if (ignoreStandardIncludePath != null && ignoreStandardIncludePath.ToUpper().Equals("TRUE")) {
                options.Append("/X ");
            }

            string culture = fileConfig.GetToolSetting(compilerTool, "Culture");
            if (culture != null) {
                int cultureId = Convert.ToInt32(culture);
                options.AppendFormat("/l 0x{0:X} ", cultureId);
            }

            string preprocessorDefs = fileConfig.GetToolSetting(compilerTool, "PreprocessorDefinitions");
            if (!StringUtils.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string preprocessorDef in preprocessorDefs.Split(';')) {
                    if (preprocessorDef.Length == 0) {
                        continue;
                    }
                    options.AppendFormat("/d \"{0}\" ", preprocessorDef);
                }
            }

            string showProgress = fileConfig.GetToolSetting(compilerTool, "ShowProgress");
            if (showProgress != null && showProgress.ToUpper().Equals("TRUE")) {
                options.Append("/v ");
            }

            string addIncludeDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalIncludeDirectories");
            if (!StringUtils.IsNullOrEmpty(addIncludeDirs)) {
                foreach (string addIncludeDir in addIncludeDirs.Split(';')) {
                    if (addIncludeDir.Length == 0) {
                        continue;
                    }
                    options.AppendFormat("/I \"{0}\" ", addIncludeDir);
                }
            }

            // check for shared MFC
            if (fileConfig.UseOfMFC == UseOfMFC.Shared) {
                options.AppendFormat("/d \"_AFXDLL\"");
            }

            if (options.Length > 0)
                rcTask.Options = options.ToString();

            // Compile each resource file
            foreach (string rcFile in fileNames) {
                string outFile = Path.Combine(intermediateDir, 
                    Path.GetFileNameWithoutExtension(rcFile) + ".res");

                // add it to _objFiles to link later on
                _objFiles.Add(outFile);

                rcTask.OutputFile = new FileInfo(outFile);
                rcTask.RcFile = new FileInfo(Path.Combine(fileConfig.ProjectDir.FullName, rcFile));
                
                // execute the task
                ExecuteInProjectDirectory(rcTask);
            }
        }

        private void RunLibrarian(VcConfiguration baseConfig) {
            // create instance of Lib task
            LibTask libTask = new LibTask();

            // inherit project from solution task
            libTask.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            libTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            libTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            libTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            libTask.InitializeTaskConfiguration();

            // set parent of child elements
            libTask.Sources.Parent = libTask;

            // inherit project from solution task for child elements
            libTask.Sources.Project = libTask.Project;

            // inherit namespace manager from parent
            libTask.Sources.NamespaceManager = libTask.NamespaceManager;

            // set task properties
            string outFile = baseConfig.GetToolSetting("VCLibrarianTool", "OutputFile");
            libTask.OutputFile = new FileInfo(Path.Combine(
                ProjectDirectory.FullName, outFile));

            foreach (string objFile in _objFiles) {
                libTask.Sources.FileNames.Add(objFile);
            }
            
            // execute the task
            ExecuteInProjectDirectory(libTask);
        }

        private void RunLinker(VcConfiguration baseConfig) {
            const string linkerTool = "VCLinkerTool";

            // create instance of Link task
            LinkTask linkTask = new LinkTask();

            // inherit project from solution task
            linkTask.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            linkTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            linkTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            linkTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            linkTask.InitializeTaskConfiguration();

            // set parent of child elements
            linkTask.Sources.Parent = linkTask;
            linkTask.LibDirs.Parent = linkTask;

            // inherit project from solution task for child elements
            linkTask.Sources.Project = linkTask.Project;
            linkTask.LibDirs.Project = linkTask.Project;

            // inherit namespace manager from parent
            linkTask.Sources.NamespaceManager = linkTask.NamespaceManager;
            linkTask.LibDirs.NamespaceManager = linkTask.NamespaceManager;

            // set task properties
            string outFile = baseConfig.GetToolSetting(linkerTool, "OutputFile");
            string pdbFile = baseConfig.GetToolSetting(linkerTool, "ProgramDatabaseFile");
            if (OutputDir != null) {
                linkTask.OutputFile = new FileInfo(Path.Combine(OutputDir.FullName, 
                    Path.GetFileName(outFile)));
                if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                    pdbFile = Path.Combine(OutputDir.FullName, Path.GetFileName(pdbFile));
                }
            } else {
                linkTask.OutputFile = new FileInfo(Path.Combine(
                    ProjectDirectory.FullName, outFile));
                if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                    pdbFile = Path.Combine(ProjectDirectory.FullName, pdbFile);
                }
            }

            if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                linkTask.Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                    "/PDB:\"{0}\"", pdbFile)));
            }

            if (IsOutputDll(baseConfig)) {
                linkTask.Arguments.Add(new Argument("/DLL"));
            }

            foreach (string objFile in _objFiles) {
                linkTask.Sources.FileNames.Add(objFile);
            }

            string addDeps = baseConfig.ExpandMacros(baseConfig.GetToolSetting(linkerTool, "AdditionalDependencies"));
            if (addDeps != null) {
                foreach (string addDep in addDeps.Split(' ')) {
                    linkTask.Sources.FileNames.Add(addDep);
                }
            }

            foreach (string defaultLib in _defaultLibraries) {
                linkTask.Sources.FileNames.Add(defaultLib);
            }

            string addLibDirs = baseConfig.GetToolSetting(linkerTool, "AdditionalLibraryDirectories");
            if (addLibDirs != null) {
                foreach (string addLibDir in addLibDirs.Split(',', ';')) {
                    if (addLibDir.Length == 0) {
                        continue;
                    }
                    linkTask.LibDirs.DirectoryNames.Add(addLibDir);
                }
            }

            Hashtable linkerArgs = baseConfig.GetToolArguments(linkerTool, _linkerArgMap);
            foreach (string arg in linkerArgs.Values) {
                linkTask.Arguments.Add(new Argument(arg));
            }

            string addOptions = baseConfig.GetToolSetting(linkerTool, "AdditionalOptions");
            if (addOptions != null) {
                using (StringReader reader = new StringReader(addOptions)) {
                    string addOptionsLine = reader.ReadLine();
                    while (addOptionsLine != null) {
                        foreach (string addOption in addOptionsLine.Split(' ')) {
                            linkTask.Arguments.Add(new Argument(addOption));
                        }
                        addOptionsLine = reader.ReadLine();
                    }
                }
            }

            if (baseConfig.WholeProgramOptimization) {
                linkTask.Arguments.Add(new Argument("/LTCG"));
            }

            // execute the task
            ExecuteInProjectDirectory(linkTask);
        }

        private bool IsOutputDll(VcConfiguration config) {
            string outFile = config.GetToolSetting("VCLinkerTool", "OutputFile");
            if (outFile == null) {
                return false;
            }
            return Path.GetExtension(outFile).ToLower(CultureInfo.InvariantCulture) == ".dll";
        }

        private void ExecuteInProjectDirectory(NAnt.Core.Task task) {
            string oldBaseDir = SolutionTask.Project.BaseDirectory;
            SolutionTask.Project.BaseDirectory = ProjectDirectory.FullName;

            try {
                // increment indentation level
                task.Project.Indent();

                // execute task
                task.Execute();
            } finally {
                // restore original base directory
                SolutionTask.Project.BaseDirectory = oldBaseDir;

                // restore indentation level
                task.Project.Unindent();
            }
        }

        /// <summary>
        /// Merges the specified tool setting of <paramref name="baseConfig" /> 
        /// with <paramref name="fileConfig" />.
        /// </summary>
        /// <remarks>
        /// The merge is suppressed when the flag $(noinherit) is defined in
        /// <paramref name="fileConfig" />.
        /// </remarks>
        private string MergeCompilerToolSetting(VcConfiguration baseConfig, VcConfiguration fileConfig, string setting) {
            const string compilerTool = "VCCLCompilerTool";
            const string noinherit = "$(noinherit)";
            string settingValue = fileConfig.GetToolSetting(compilerTool, setting);

            if (settingValue == null) {
                return baseConfig.GetToolSetting(compilerTool, setting);
            }

            if (settingValue.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit) == -1) {
                settingValue += ";" + baseConfig.GetToolSetting(compilerTool, setting);
            } else {
                settingValue = settingValue.Remove(settingValue.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit), noinherit.Length);
            }
            return settingValue;
        }

        #endregion Private Instance Methods

        #region Public Static Methods

        public static string LoadGuid(string fileName) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);
                return doc.DocumentElement.GetAttribute("ProjectGUID");
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading GUID of project '{0}'.", fileName), 
                    Location.UnknownLocation, ex);
            }
        }

        /// <summary>
        /// Returns a value indicating whether the project represented by the
        /// specified XML fragment is supported by <see cref="VcProject" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project to check.</param>
        /// <returns>
        /// <see langword="true" /> if <see cref="VcProject" /> supports the 
        /// specified project; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A project is identified as as Visual C++ project, if the XML 
        /// fragment at least has the following information:
        /// </para>
        /// <code>
        ///   <![CDATA[
        /// <VisualStudioProject
        ///     ProjectType="Visual C++"
        ///     Version="..."
        ///     ...
        ///     >
        /// </VisualStudioProject>
        ///   ]]>
        /// </code>
        /// </remarks>
        public static bool IsSupported(XmlElement docElement) {
            if (docElement == null) {
                return false;
            }

            if (docElement.Name != "VisualStudioProject") {
                return false;
            }

            XmlAttribute projectTypeAttribute = docElement.Attributes["ProjectType"];
            if (projectTypeAttribute == null || projectTypeAttribute.Value != "Visual C++") {
                return false;
            }

            try {
                ProductVersion productVersion = GetProductVersion(docElement);
                // no need to perform version check here as this is done in 
                // GetProductVersion
            } catch {
                // product version could not be determined or is not supported
                return false;
            }

            return true;
        }

        #endregion Public Static Methods

        #region Private Static Methods

        /// <summary>
        /// Removes leading and trailing quotes from the specified path.
        /// </summary>
        /// <param name="path">The path to clean.</param>
        private static string CleanPath(string path) {
            string cleanedPath = path.TrimStart('\"');
            return cleanedPath.TrimEnd('\"');
        }

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project to check.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        private static ProductVersion GetProductVersion(XmlElement docElement) {
            if (docElement == null) {
                throw new ArgumentNullException("docElement");
            }

            XmlAttribute productVersionAttribute = docElement.Attributes["Version"];
            if (productVersionAttribute == null) {
                throw new BuildException("The \"Version\" attribute is"
                    + " missing from the <VisualStudioProject> node.",
                    Location.UnknownLocation);
            }

            // check if we're dealing with a valid version number
            Version productVersion = null;
            try {
                productVersion = new Version(productVersionAttribute.Value);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The value of the \"Version\" attribute ({0}) is not a valid"
                    + " version string.", productVersionAttribute.Value),
                    Location.UnknownLocation, ex);
            }

            if (productVersion.Major == 7) {
                switch (productVersion.Minor) {
                    case 0:
                        return ProductVersion.Rainier;
                    case 10:
                        return ProductVersion.Everett;
                }
            } 

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Visual Studio version \"{0\" is not supported.",
                productVersion.ToString()), Location.UnknownLocation);
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private readonly string _name;
        private readonly string _projectPath;
        private string _guid;
        private readonly string _rootNamespace;
        private readonly ArrayList _references;
        private readonly Hashtable _htPlatformConfigurations;
        private readonly Hashtable _htFiles;
        private readonly ArrayList _objFiles;
        private readonly VcArgumentMap _clArgMap;
        private readonly VcArgumentMap _linkerArgMap;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static string[] _defaultLibraries = new string[] { 
                                                                     "kernel32.lib", "user32.lib", "gdi32.lib", "winspool.lib", "comdlg32.lib",
                                                                     "advapi32.lib", "shell32.lib", "ole32.lib", "oleaut32.lib", "uuid.lib"
                                                                 };

        #endregion Private Static Fields
    }
}
