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
// Gert Driesen (drieseng@users.sourceforge.net)
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
using NAnt.VisualCpp.Types;

using NAnt.VSNet.Tasks;
using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// Visual C++ project.
    /// </summary>
    public class VcProject: ProjectBase {
        #region Public Instance Constructors
        
        public VcProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, 
                SolutionTask solutionTask, 
                TempFileCollection tfc, 
                GacCache gacCache, 
                ReferencesResolver refResolver, 
                DirectoryInfo outputDir) 
                : base(xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            if (projectPath == null) {
                throw new ArgumentNullException("projectPath");
            }

            _references = new ArrayList();
            _clArgMap = VcArgumentMap.CreateCLArgumentMap();
            _linkerArgMap = VcArgumentMap.CreateLinkerArgumentMap();
            _midlArgMap = VcArgumentMap.CreateMidlArgumentMap();
            _projectPath = FileUtils.GetFullPath(projectPath);

            _name = xmlDefinition.GetAttribute("Name");
            _guid = xmlDefinition.GetAttribute("ProjectGUID");
            
            XmlNodeList configurationNodes = xmlDefinition.SelectNodes("//Configurations/Configuration");
            foreach (XmlElement configElem in configurationNodes) {
                VcProjectConfiguration config = new VcProjectConfiguration(configElem, this, OutputDir);
                ProjectConfigurations[new Configuration (config.Name, config.PlatformName)] = config;
            }

            XmlNodeList references = xmlDefinition.SelectNodes("//References/child::*");
            foreach (XmlElement referenceElem in references) {
                ReferenceBase reference = CreateReference(solution, referenceElem);
                _references.Add(reference);
            }

            XmlNodeList fileNodes = xmlDefinition.SelectNodes("//File");
            _projectFiles = new ArrayList(fileNodes.Count);
            foreach (XmlElement fileElem in fileNodes) {
                string parentName = Name;

                // determine name of parent
                if (fileElem.ParentNode != null && fileElem.ParentNode.Name == "Filter") {
                    XmlNode filterName = fileElem.ParentNode.Attributes.GetNamedItem("Name");
                    if (filterName != null) {
                        parentName = filterName.Value;
                    }
                }

                string relPath = fileElem.GetAttribute("RelativePath");

                Hashtable htFileConfigurations = null;

                XmlNodeList fileConfigList = fileElem.GetElementsByTagName("FileConfiguration");
                if (fileConfigList.Count > 0) {
                    htFileConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable(fileConfigList.Count);
                    foreach (XmlElement fileConfigElem in fileConfigList) {
                        Configuration fileConfig = Configuration.Parse (
                            fileConfigElem.GetAttribute("Name"));
                        VcProjectConfiguration projectConfig = (VcProjectConfiguration) ProjectConfigurations[fileConfig];
                        htFileConfigurations[projectConfig.Name] = new VcFileConfiguration(
                            relPath, parentName, fileConfigElem, projectConfig, outputDir);
                    }
                }

                // TODO: refactor this together with the Build methods

                // we'll always create a file configuration for IDL and res 
                // files as macro's in the configuration properties for these
                // files will always need to be expanded on the file level
                string ext = Path.GetExtension(relPath).ToLower(CultureInfo.InvariantCulture);
                switch (ext) {
                    case ".idl":
                    case ".odl":
                    case ".rc":
                        // ensure there's a file configuration for each project 
                        // configuration
                        foreach (VcProjectConfiguration projectConfig in ProjectConfigurations.Values) {
                            // if file configuration for project config existed 
                            // in project file, then skip this project config
                            if (htFileConfigurations != null && htFileConfigurations.ContainsKey(projectConfig.Name)) {
                                continue;
                            }

                            // lazy initialize hashtable
                            if (htFileConfigurations == null) {
                                htFileConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
                            }

                            // create the file configuration
                            htFileConfigurations[projectConfig.Name] = new 
                                VcFileConfiguration(relPath, parentName, projectConfig, outputDir);
                        }
                        break;
                }

                if (htFileConfigurations != null) {
                    _projectFiles.Add(htFileConfigurations);
                } else {
                    _projectFiles.Add(relPath);
                }
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
        /// Get the location of the project.
        /// </summary>
        /// <value>
        /// <see cref="T:NAnt.VSNet.ProjectLocation.Local" />.
        /// </value>
        /// <remarks>
        /// For now, we only support local Visual C++ projects.
        /// </remarks>
        public override ProjectLocation ProjectLocation {
            get { return ProjectLocation.Local; }
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
                    FileUtils.CombinePaths(ProjectDirectory.FullName, "temp"));
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
        /// Gets a value indicating whether building the project for the specified
        /// build configuration results in managed output.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// <see langword="true" /> if the project output for the specified build
        /// configuration is either a Dynamic Library (dll) or an Application
        /// (exe), and Managed Extensions are enabled; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        public override bool IsManaged(Configuration solutionConfiguration) {
            VcProjectConfiguration projectConfig = (VcProjectConfiguration)
                BuildConfigurations[solutionConfiguration];
            return (projectConfig.Type == VcProjectConfiguration.ConfigurationType.DynamicLibrary ||
                projectConfig.Type == VcProjectConfiguration.ConfigurationType.Application) &&
                projectConfig.ManagedExtensions;
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

        protected override BuildResult Build(Configuration solutionConfiguration) {
            // prepare the project for build
            Prepare(solutionConfiguration);

            // obtain project configuration (corresponding with solution configuration)
            VcProjectConfiguration projectConfig = (VcProjectConfiguration) BuildConfigurations[solutionConfiguration];

            // perform pre-build actions
            if (!PreBuild(projectConfig)) {
                return BuildResult.Failed;
            }

            string nmakeCommand = projectConfig.GetToolSetting(VcConfigurationBase.NMakeTool, "BuildCommandLine");
            if (!String.IsNullOrEmpty(nmakeCommand)) {
                RunNMake(nmakeCommand);
                return BuildResult.Success;
            }

            VcConfigurationBase stdafxConfig = null;

            // build idl files before everything else
            foreach (VcConfigurationBase idlConfig in projectConfig.IdlConfigs.Keys) {
                BuildIDLFiles((ArrayList) projectConfig.IdlConfigs[idlConfig], 
                    projectConfig, idlConfig);
            }

            // If VC project uses precompiled headers then the precompiled header
            // output (.pch file) must be generated before compiling any other
            // source files.
            foreach (VcConfigurationBase vcConfig in projectConfig.SourceConfigs.Keys) {
                if (vcConfig.UsePrecompiledHeader == UsePrecompiledHeader.Create) {
                    BuildCPPFiles((ArrayList) projectConfig.SourceConfigs[vcConfig], 
                        solutionConfiguration, vcConfig);
                    stdafxConfig = vcConfig;
                }
            }

            foreach (VcConfigurationBase vcConfig in projectConfig.SourceConfigs.Keys) {
                if (vcConfig != stdafxConfig) {
                    BuildCPPFiles((ArrayList) projectConfig.SourceConfigs[vcConfig], 
                        solutionConfiguration, vcConfig);
                }
            }

            // build resource files
            foreach (VcConfigurationBase rcConfig in projectConfig.RcConfigs.Keys) {
                BuildResourceFiles((ArrayList) projectConfig.RcConfigs[rcConfig], 
                    projectConfig, rcConfig);
            }

            switch (projectConfig.Type) {
                case VcProjectConfiguration.ConfigurationType.StaticLibrary:
                    RunLibrarian(projectConfig);
                    break;
                case VcProjectConfiguration.ConfigurationType.Application:
                case VcProjectConfiguration.ConfigurationType.DynamicLibrary:
                    // perform pre-link actions
                    if (!PreLink(projectConfig)) {
                        return BuildResult.Failed;
                    }
                    RunLinker(solutionConfiguration);
                    break;
            }

            Log(Level.Verbose, "Copying references:");

            foreach (ReferenceBase reference in _references) {
                if (reference.CopyLocal) {
                    Log(Level.Verbose, " - " + reference.Name);

                    Hashtable outputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
                    reference.GetOutputFiles(solutionConfiguration, outputFiles);

                    foreach (DictionaryEntry de in outputFiles) {
                        // determine file to copy
                        FileInfo srcFile = new FileInfo((string) de.Key);
                        // determine destination file
                        FileInfo destFile = new FileInfo(FileUtils.CombinePaths(
                            projectConfig.OutputDir.FullName, (string) de.Value));
                        // perform actual copy
                        CopyFile(srcFile, destFile, SolutionTask);
                    }
                }
            }

            // run custom build steps
            if (!RunCustomBuildStep(solutionConfiguration, projectConfig)) {
                return BuildResult.Failed;
            }

            // perform post-build actions
            if (!PostBuild(projectConfig)) {
                return BuildResult.Failed;
            }

            return BuildResult.Success;
        }

        #endregion Override implementation of ProjectBase

        #region Internal Instance Properties

        internal ArrayList ProjectFiles {
            get { return _projectFiles; }
        }

        #endregion Internal Instance Properties

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

        #region Internal Instance Methods

        internal string GetObjOutputFile(string fileName, VcConfigurationBase fileConfig, string intermediateDir) {
            string objectFile = GetObjectFile(fileConfig);
            if (objectFile == null) {
                objectFile = intermediateDir;
            }
            return ClTask.GetObjOutputFile(fileName, objectFile);
        }

        internal string GetResourceOutputFile(string fileName, VcConfigurationBase fileConfig) {
            return FileUtils.CombinePaths(ProjectDirectory.FullName, 
                fileConfig.GetToolSetting(VcConfigurationBase.ResourceCompilerTool,
                    "ResourceOutputFileName", "$(IntDir)/$(InputName).res"));
        }

        #endregion Internal Instance Methods

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
                case "ActiveXReference":
                    // ActiveX reference
                    return new VcWrapperReference(xmlDefinition, ReferencesResolver, 
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

        private void BuildCPPFiles(ArrayList fileNames, Configuration solutionConfiguration, VcConfigurationBase fileConfig) {
            // obtain project configuration (corresponding with solution configuration)
            VcProjectConfiguration projectConfig = (VcProjectConfiguration) BuildConfigurations[solutionConfiguration];

            string intermediateDir = FileUtils.CombinePaths(ProjectDirectory.FullName, 
                projectConfig.IntermediateDir);

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
            clTask.IncludeDirs.BaseDirectory = ProjectDirectory;
            clTask.Sources.BaseDirectory = ProjectDirectory;
            clTask.MetaDataIncludeDirs.BaseDirectory = ProjectDirectory;
            clTask.ForcedUsingFiles.BaseDirectory = ProjectDirectory;

            // set task properties
            clTask.OutputDir = new DirectoryInfo(intermediateDir);

            // TODO: add support for disabling specific warnings !!!

            // check if precompiled headers are used
            if (fileConfig.UsePrecompiledHeader != UsePrecompiledHeader.No && fileConfig.UsePrecompiledHeader != UsePrecompiledHeader.Unspecified) {
                // get location of precompiled header file
                string pchFile = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, 
                    "PrecompiledHeaderFile", "$(IntDir)/$(TargetName).pch");

                // we must set an absolute path for the PCH location file, 
                // otherwise <cl> assumes a location relative to the output 
                // directory - not the project directory.
                if (!String.IsNullOrEmpty(pchFile)) {
                    clTask.PchFile = FileUtils.CombinePaths(ProjectDirectory.FullName, pchFile);
                }

                // check if a header file is specified for the precompiled header 
                // file, use "StdAfx.h" as default value
                string headerThrough = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool,
                    "PrecompiledHeaderThrough", "StdAfx.h");
                if (!String.IsNullOrEmpty(headerThrough)) {
                    clTask.PchThroughFile = headerThrough;
                }

                switch (fileConfig.UsePrecompiledHeader) {
                    case UsePrecompiledHeader.Use:
                        clTask.PchMode = ClTask.PrecompiledHeaderMode.Use;
                        break;
                    case UsePrecompiledHeader.AutoCreate:
                        clTask.PchMode = ClTask.PrecompiledHeaderMode.AutoCreate;
                        break;
                    case UsePrecompiledHeader.Create:
                        clTask.PchMode = ClTask.PrecompiledHeaderMode.Create;
                        break;
                }
            }

            clTask.CharacterSet = projectConfig.CharacterSet;
            
            // ensure output directory exists
            if (!clTask.OutputDir.Exists) {
                clTask.OutputDir.Create();
                clTask.OutputDir.Refresh();
            }

            string includeDirs = MergeToolSetting(projectConfig, fileConfig, 
                VcConfigurationBase.CLCompilerTool, "AdditionalIncludeDirectories");
            if (!String.IsNullOrEmpty(includeDirs)) {
                foreach (string includeDir in includeDirs.Split(',', ';')) {
                    if (includeDir.Length == 0) {
                        continue;
                    }
                    clTask.IncludeDirs.DirectoryNames.Add(FileUtils.CombinePaths(
                        ProjectDirectory.FullName, CleanPath(includeDir)));
                }
            }

            string metadataDirs = MergeToolSetting(projectConfig, fileConfig, 
                VcConfigurationBase.CLCompilerTool, "AdditionalUsingDirectories");
            if (!String.IsNullOrEmpty(metadataDirs)) {
                foreach (string metadataDir in metadataDirs.Split(';')) {
                    if (metadataDir.Length == 0) {
                        continue;
                    }
                    clTask.MetaDataIncludeDirs.DirectoryNames.Add(
                        CleanPath(fileConfig.ExpandMacros(metadataDir)));
                }
            }

            string forcedUsingFiles = MergeToolSetting(projectConfig, fileConfig, 
                VcConfigurationBase.CLCompilerTool, "ForcedUsingFiles");
            if (!String.IsNullOrEmpty(forcedUsingFiles)) {
                foreach (string forcedUsingFile in forcedUsingFiles.Split(';')) {
                    if (forcedUsingFile.Length == 0) {
                        continue;
                    }
                    clTask.ForcedUsingFiles.Includes.Add(
                        CleanPath(fileConfig.ExpandMacros(forcedUsingFile)));
                }
            }

            // add project and assembly references
            foreach (ReferenceBase reference in References) {
                if (!reference.IsManaged(solutionConfiguration)) {
                    continue;
                }
                
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    solutionConfiguration);
                foreach (string assemblyFile in assemblyReferences) {
                    clTask.ForcedUsingFiles.Includes.Add(assemblyFile);
                }
            }

            // Since the name of the pdb file is based on the VS version, we need to see
            // which version we are targeting to make sure the right pdb file is used.
            string pdbTargetFileName;

            switch (ProductVersion) {
                case ProductVersion.Rosario:
                    pdbTargetFileName = "$(IntDir)/vc100.pdb";
                    break;
                case ProductVersion.Orcas:
                    pdbTargetFileName = "$(IntDir)/vc90.pdb";
                    break;
                case ProductVersion.Whidbey:
                    pdbTargetFileName = "$(IntDir)/vc80.pdb";
                    break;
                default:
                    pdbTargetFileName = "$(IntDir)/vc70.pdb";
                    break;
            }

            // set name of program database file
            //
            // we must set an absolute path for the program database file, 
            // otherwise <cl> assumes a location relative to the output 
            // directory - not the project directory.
            string pdbFile = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool,
                "ProgramDataBaseFileName", pdbTargetFileName);
            if (!String.IsNullOrEmpty(pdbFile)) {
                clTask.ProgramDatabaseFile = FileUtils.CombinePaths(ProjectDirectory.FullName, 
                    pdbFile);
            }

            // set path of object file or directory (can be null)
            clTask.ObjectFile = GetObjectFile(fileConfig);

            string asmOutput = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "AssemblerOutput");
            string asmListingLocation = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "AssemblerListingLocation");
            if (!String.IsNullOrEmpty(asmOutput) && asmOutput != "0" && !String.IsNullOrEmpty(asmListingLocation)) {
                // parameter for AssemblerOutput itself will be handled by the map
                clTask.Arguments.Add(new Argument("/Fa\"" + asmListingLocation + "\""));
            }

            foreach (string fileName in fileNames) {
                clTask.Sources.FileNames.Add(FileUtils.CombinePaths(
                    ProjectDirectory.FullName, fileName));
            }

            string preprocessorDefs = MergeToolSetting(projectConfig, fileConfig, 
                VcConfigurationBase.CLCompilerTool, "PreprocessorDefinitions");
            if (!String.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string def in preprocessorDefs.Split(';', ',')) {
                    if (def.Length != 0) {
                        Option op = new Option();
                        op.OptionName = def;
                        clTask.Defines.Add(op);
                    }
                }
            }

            string undefinePreprocessorDefs = MergeToolSetting(projectConfig, fileConfig,
                VcConfigurationBase.CLCompilerTool, "UndefinePreprocessorDefinitions");
            if (!String.IsNullOrEmpty(undefinePreprocessorDefs)) {
                foreach (string def in undefinePreprocessorDefs.Split(';', ',')) { 
                    Option op = new Option();
                    op.OptionName = def;
                    clTask.Undefines.Add(op);
                }
            }

            string addOptions = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "AdditionalOptions");
            if (!String.IsNullOrEmpty(addOptions)) {
                using (StringReader reader = new StringReader(addOptions)) {
                    string addOptionsLine = reader.ReadLine();
                    while (addOptionsLine != null) {
                        foreach (string addOption in addOptionsLine.Split(' '))  {
                            clTask.Arguments.Add(new Argument(addOption));
                        }
                        addOptionsLine = reader.ReadLine();
                    }
                }
            }

            //exception handling stuff
            string exceptionHandling = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "ExceptionHandling");
            if (exceptionHandling == null) {
                if (ProductVersion >= ProductVersion.Whidbey) {
                    exceptionHandling = "2";
                } else {
                    exceptionHandling = "false";
                }
            } else {
                exceptionHandling = exceptionHandling.ToLower();
            }
            switch(exceptionHandling) {
                case "0":
                case "false":
                    break;
                case "1":
                case "true":
                    clTask.Arguments.Add(new Argument("/EHsc"));
                    break;
                case "2":
                    clTask.Arguments.Add(new Argument("/EHa"));
                    break;
            }

            string browseInformation = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "BrowseInformation");
            if (!String.IsNullOrEmpty(browseInformation) && browseInformation != "0") {
                // determine file name of browse information file
                string browseInformationFile = fileConfig.GetToolSetting(
                    VcConfigurationBase.CLCompilerTool, "BrowseInformationFile", 
                    "$(IntDir)/");
                if (!String.IsNullOrEmpty(browseInformationFile)) {
                    switch (browseInformation) {
                        case "1": // Include All Browse Information
                            clTask.Arguments.Add(new Argument("/FR\"" 
                                + browseInformationFile + "\""));
                            break;
                        case "2": // No Local Symbols
                            clTask.Arguments.Add(new Argument("/Fr\"" 
                                + browseInformationFile + "\""));
                            break;
                    }
                } else {
                    switch (browseInformation) {
                        case "1": // Include All Browse Information
                            clTask.Arguments.Add(new Argument("/FR"));
                            break;
                        case "2": // No Local Symbols
                            clTask.Arguments.Add(new Argument("/Fr"));
                            break;
                    }
                }
            }

            if (projectConfig.Type == VcProjectConfiguration.ConfigurationType.DynamicLibrary) {
                clTask.Arguments.Add(new Argument("/D"));
                clTask.Arguments.Add(new Argument("_WINDLL"));
            }

            if (projectConfig.WholeProgramOptimization) {
                clTask.Arguments.Add(new Argument("/GL"));
            }

            // used to ignore some arguments
            VcArgumentMap.ArgGroup vcArgIgnoreGroup = VcArgumentMap.ArgGroup.Unassigned;

            // if optimzation level is Minimum Size (1) or Maximum size (2), we 
            // need to ignore all the arguments of the group "OptiIgnoreGroup"
            string optimization = fileConfig.GetToolSetting(VcConfigurationBase.CLCompilerTool, "Optimization");
            if (!String.IsNullOrEmpty(optimization)) {
                int optimizationLevel = int.Parse(optimization);
                if (optimizationLevel == 1 || optimizationLevel == 2) {
                    vcArgIgnoreGroup |= VcArgumentMap.ArgGroup.OptiIgnoreGroup;
                }
            }

            Hashtable compilerArgs = fileConfig.GetToolArguments(VcConfigurationBase.CLCompilerTool, 
                _clArgMap, vcArgIgnoreGroup);
            foreach (string arg in compilerArgs.Values) {
                Argument compilerArg = new Argument();
                compilerArg.Line = arg;
                clTask.Arguments.Add(compilerArg);
            }

            // check for shared MFC
            if (projectConfig.UseOfMFC == UseOfMFC.Shared) {
                clTask.Arguments.Add(new Argument("/D"));
                clTask.Arguments.Add(new Argument("_AFXDLL"));
            }

            // check for shared ATL
            switch (projectConfig.UseOfATL) {
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
            clTask.ManagedExtensions = projectConfig.ManagedExtensions;

            // execute the task
            ExecuteInProjectDirectory(clTask);
        }

        /// <summary>
        /// Build resource files for the given configuration.
        /// </summary>
        /// <param name="fileNames">The resource files to build.</param>
        /// <param name="projectConfig">The project configuration.</param>
        /// <param name="fileConfig">The build configuration.</param>
        /// <remarks>
        /// TODO: refactor this as we should always get only one element in the
        /// <paramref name="fileNames" /> list. Each res file should be built
        /// with its own file configuration.
        /// </remarks>
        private void BuildResourceFiles(ArrayList fileNames, VcProjectConfiguration projectConfig, VcConfigurationBase fileConfig) {
            // create instance of RC task
            RcTask rcTask = new RcTask();

            // inherit project from solution task
            rcTask.Project = SolutionTask.Project;

            // Set the base directory
            rcTask.BaseDirectory = ProjectDirectory;

            // inherit namespace manager from solution task
            rcTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            rcTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            rcTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            rcTask.InitializeTaskConfiguration();

            // set parent of child elements
            rcTask.IncludeDirs.Parent = rcTask;

            // inherit project from solution task for child elements
            rcTask.IncludeDirs.Project = rcTask.Project;

            // set namespace manager of child elements
            rcTask.IncludeDirs.NamespaceManager = rcTask.NamespaceManager;

            // set base directories
            rcTask.IncludeDirs.BaseDirectory = ProjectDirectory;

            // Store the options to pass to the resource compiler in the options variable
            StringBuilder options = new StringBuilder();

            // Collect options

            string ignoreStandardIncludePath = fileConfig.GetToolSetting(VcConfigurationBase.ResourceCompilerTool, "IgnoreStandardIncludePath");
            if (ignoreStandardIncludePath != null && string.Compare(ignoreStandardIncludePath, "true", true, CultureInfo.InvariantCulture) == 0) {
                options.Append("/X ");
            }

            string culture = fileConfig.GetToolSetting(VcConfigurationBase.ResourceCompilerTool, "Culture");
            if (!String.IsNullOrEmpty(culture)) {
                int cultureId = Convert.ToInt32(culture);
                rcTask.LangId = cultureId;
            }

            string preprocessorDefs = fileConfig.GetToolSetting(VcConfigurationBase.ResourceCompilerTool, "PreprocessorDefinitions");
            if (!String.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string preprocessorDef in preprocessorDefs.Split(';')) {
                    if (preprocessorDef.Length == 0) {
                        continue;
                    }
                    Option op = new Option();
                    op.OptionName = preprocessorDef;
                    rcTask.Defines.Add(op);
                }
            }

            string showProgress = fileConfig.GetToolSetting(VcConfigurationBase.ResourceCompilerTool, "ShowProgress");
            if (showProgress != null && string.Compare(showProgress, "true", true, CultureInfo.InvariantCulture) == 0) {
                rcTask.Verbose = true;
            }

            string addIncludeDirs = MergeToolSetting(projectConfig, fileConfig, VcConfigurationBase.ResourceCompilerTool, "AdditionalIncludeDirectories");
            if (!String.IsNullOrEmpty(addIncludeDirs)) {
                foreach (string includeDir in addIncludeDirs.Split(';')) {
                    if (includeDir.Length == 0) {
                        continue;
                    }
                    rcTask.IncludeDirs.DirectoryNames.Add(FileUtils.CombinePaths(
                        ProjectDirectory.FullName, CleanPath(includeDir)));
                }
            }

            // check for shared MFC
            if (projectConfig.UseOfMFC == UseOfMFC.Shared) {
                options.AppendFormat("/d \"_AFXDLL\"");
            }

            if (options.Length > 0) {
                rcTask.Options = options.ToString();
            }

            // Compile each resource file
            foreach (string rcFile in fileNames) {
                rcTask.OutputFile = new FileInfo(GetResourceOutputFile(rcFile, fileConfig));
                rcTask.RcFile = new FileInfo(FileUtils.CombinePaths(ProjectDirectory.FullName, rcFile));
                
                // execute the task
                ExecuteInProjectDirectory(rcTask);
            }
        }

        /// <summary>
        /// Build Interface Definition Language files for the given
        /// configuration.
        /// </summary>
        /// <param name="fileNames">The IDL files to build.</param>
        /// <param name="projectConfig">The project configuration.</param>
        /// <param name="fileConfig">The build configuration.</param>
        /// <remarks>
        /// TODO: refactor this as we should always get only one element in the
        /// <paramref name="fileNames" /> list. Each IDL file should be built
        /// with its own file configuration.
        /// </remarks>
        private void BuildIDLFiles(ArrayList fileNames, VcProjectConfiguration projectConfig, VcConfigurationBase fileConfig) {
            // create instance of MIDL task
            MidlTask midlTask = new MidlTask();

            // inherit project from solution task
            midlTask.Project = SolutionTask.Project;

            // Set the base directory
            midlTask.BaseDirectory = ProjectDirectory;

            // inherit namespace manager from solution task
            midlTask.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            midlTask.Parent = SolutionTask;

            // inherit verbose setting from solution task
            midlTask.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            midlTask.InitializeTaskConfiguration();

            // set parent of child elements
            midlTask.IncludeDirs.Parent = midlTask;

            // inherit project from solution task for child elements
            midlTask.IncludeDirs.Project = midlTask.Project;

            // set namespace manager of child elements
            midlTask.IncludeDirs.NamespaceManager = midlTask.NamespaceManager;

            // set base directories
            midlTask.IncludeDirs.BaseDirectory = ProjectDirectory;

            // If outputDirectory is not supplied in the configuration, assume 
            // it's the project directory
            string outputDirectory = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, "OutputDirectory");
            if (String.IsNullOrEmpty(outputDirectory)) {
                outputDirectory = ProjectDirectory.FullName;
            } else {
                outputDirectory = FileUtils.CombinePaths(ProjectDirectory.FullName, 
                    outputDirectory);
            }

            // ensure output directory exists
            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory);
            }

            midlTask.Arguments.Add(new Argument("/out"));
            midlTask.Arguments.Add(new Argument(outputDirectory));

            string typeLibraryName = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, 
                "TypeLibraryName", "$(IntDir)/$(ProjectName).tlb");
            if (!String.IsNullOrEmpty(typeLibraryName)) {
                midlTask.Tlb = new FileInfo(FileUtils.CombinePaths(outputDirectory, 
                    typeLibraryName));

                // ensure tlb directory exists
                if (!midlTask.Tlb.Directory.Exists) {
                    midlTask.Tlb.Directory.Create();
                    midlTask.Tlb.Directory.Refresh();
                }
            }

            string proxyFileName = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, "ProxyFileName");
            if (!String.IsNullOrEmpty(proxyFileName)) {
                midlTask.Proxy = new FileInfo(FileUtils.CombinePaths(outputDirectory, 
                    proxyFileName));

                // ensure proxy directory exists
                if (!midlTask.Proxy.Directory.Exists) {
                    midlTask.Proxy.Directory.Create();
                    midlTask.Proxy.Directory.Refresh();
                }
            }

            string interfaceIdentifierFileName = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, "InterfaceIdentifierFileName");
            if (!String.IsNullOrEmpty(interfaceIdentifierFileName)) {
                midlTask.Iid = new FileInfo(FileUtils.CombinePaths(outputDirectory, 
                    interfaceIdentifierFileName));

                // ensure IID directory exists
                if (!midlTask.Iid.Directory.Exists) {
                    midlTask.Iid.Directory.Create();
                    midlTask.Iid.Directory.Refresh();
                }
            }

            string dllDataFileName = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, "DLLDataFileName");
            if (!String.IsNullOrEmpty(dllDataFileName)) {
                midlTask.DllData = new FileInfo(FileUtils.CombinePaths(outputDirectory, 
                    dllDataFileName));

                // ensure DllData directory exists
                if (!midlTask.DllData.Directory.Exists) {
                    midlTask.DllData.Directory.Create();
                    midlTask.DllData.Directory.Refresh();
                }
            }

            string headerFileName = fileConfig.GetToolSetting(VcConfigurationBase.MIDLTool, "HeaderFileName");
            if (!String.IsNullOrEmpty(headerFileName)) {
                midlTask.Header = new FileInfo(FileUtils.CombinePaths(outputDirectory, 
                    headerFileName));

                // ensure Header directory exists
                if (!midlTask.Header.Directory.Exists) {
                    midlTask.Header.Directory.Create();
                    midlTask.Header.Directory.Refresh();
                }
            }

            string preprocessorDefs = MergeToolSetting(projectConfig, fileConfig,
                VcConfigurationBase.MIDLTool, "PreprocessorDefinitions");
            if (!String.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string preprocessorDef in preprocessorDefs.Split(';')) {
                    if (preprocessorDef.Length == 0) {
                        continue;
                    }
                    Option op = new Option();
                    op.OptionName = preprocessorDef;
                    midlTask.Defines.Add(op);
                }
            }

            string undefinePreprocessorDefs = MergeToolSetting(projectConfig, fileConfig,
                VcConfigurationBase.MIDLTool, "UndefinePreprocessorDefinitions");
            if (!String.IsNullOrEmpty(undefinePreprocessorDefs)) {
                foreach (string undefinePreprocessorDef in undefinePreprocessorDefs.Split(';')) {
                    if (undefinePreprocessorDef.Length == 0) {
                        continue;
                    }
                    Option op = new Option();
                    op.OptionName = undefinePreprocessorDef;
                    midlTask.Undefines.Add(op);
                }
            }

            string additionalIncludeDirs = MergeToolSetting(projectConfig, fileConfig,
                VcConfigurationBase.MIDLTool, "AdditionalIncludeDirectories");
            if (!String.IsNullOrEmpty(additionalIncludeDirs)) {
                foreach (string includeDir in additionalIncludeDirs.Split(';')) {
                    if (includeDir.Length == 0) {
                        continue;
                    }
                    midlTask.IncludeDirs.DirectoryNames.Add(FileUtils.CombinePaths(
                        ProjectDirectory.FullName, CleanPath(includeDir)));
                }
            }

            string cPreprocessOptions = MergeToolSetting(projectConfig, fileConfig,
                VcConfigurationBase.MIDLTool, "CPreprocessOptions");
            if (!String.IsNullOrEmpty(cPreprocessOptions)) {
                foreach (string cPreprocessOption in cPreprocessOptions.Split(';')) {
                    if (cPreprocessOption.Length == 0) {
                        continue;
                    }
                    midlTask.Arguments.Add(new Argument(string.Format("/cpp_opt\"{0}\"", cPreprocessOption)));
                }
            }

            Hashtable midlArgs = fileConfig.GetToolArguments(VcConfigurationBase.MIDLTool, _midlArgMap);
            foreach (string key in midlArgs.Keys) {
                switch (key) {
                    case "TargetEnvironment":
                        midlTask.Env = (string) midlArgs[key];
                        break;
                    case "DefaultCharType":
                        midlTask.Char = (string) midlArgs[key];
                        break;
                    default:
                        Argument midlArg = new Argument();
                        midlArg.Line = (string) midlArgs[key];
                        midlTask.Arguments.Add(midlArg);
                        break;
                }
            }

            // Compile each idl file
            foreach (string idlFile in fileNames) {
                midlTask.Filename = new FileInfo(FileUtils.CombinePaths(
                    ProjectDirectory.FullName, idlFile));
                
                // execute the task
                ExecuteInProjectDirectory(midlTask);
            }
        }

        private bool RunCustomBuildStep(Configuration solutionConfiguration, VcProjectConfiguration projectConfig) {
            // check if a custom build step is configured
            string commandLine = projectConfig.GetToolSetting(VcConfigurationBase.CustomBuildTool,
                "CommandLine");
            if (String.IsNullOrEmpty(commandLine)) {
                return true;
            }

            DateTime oldestOutputFile = DateTime.MinValue;

            string outputs = projectConfig.GetToolSetting(VcConfigurationBase.CustomBuildTool,
                "Outputs");
            if (!String.IsNullOrEmpty(outputs)) {
                foreach (string output in outputs.Split(';')) {
                    if (output.Length == 0) {
                        continue;
                    }

                    string outputFile = Path.Combine (ProjectDirectory.FullName,
                        output);
                    if (File.Exists(outputFile)) {
                        DateTime lastWriteTime = File.GetLastWriteTime(outputFile);
                        if (lastWriteTime < oldestOutputFile || oldestOutputFile == DateTime.MinValue) {
                            oldestOutputFile = lastWriteTime;
                        }
                    }
                }
            }

            bool runCustomBuildStep = false;

            // when at least one of the output files of the custom build step
            // does not exist or is older than the project output file, then
            // the custom build step must be executed
            string projectOutputFile = GetOutputPath(solutionConfiguration);
            if (projectOutputFile != null && File.Exists (projectOutputFile)) {
                DateTime lastWriteTime = File.GetLastWriteTime(projectOutputFile);
                if (lastWriteTime > oldestOutputFile) {
                    runCustomBuildStep = true;
                }
            }

            // if one of the additional dependencies was updated after the oldest
            // output file of the custom build step, then the custom build step
            // must also be executed
            if (!runCustomBuildStep) {
                string additionalDependencies = projectConfig.GetToolSetting(
                    VcConfigurationBase.CustomBuildTool, "AdditionalDependencies");
                if (!String.IsNullOrEmpty(additionalDependencies)) {
                    foreach (string dependency in additionalDependencies.Split(';')) {
                        if (dependency.Length == 0) {
                            continue;
                        }

                        string dependencyFile = Path.Combine (ProjectDirectory.FullName,
                            dependency);
                        if (File.Exists (dependencyFile)) {
                            DateTime lastWriteTime  = File.GetLastWriteTime(dependencyFile);
                            if (lastWriteTime > oldestOutputFile) {
                                runCustomBuildStep = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!runCustomBuildStep) {
                return true;
            }

            string description = projectConfig.GetToolSetting(VcConfigurationBase.CustomBuildTool,
                "Description", "Performing Custom Build Step");
            Log(Level.Info, description);
            return ExecuteBuildEvent("Custom-Build", commandLine, projectConfig);
        }

        private void RunLibrarian(VcProjectConfiguration projectConfig) {
            // check if there's anything to do
            if (projectConfig.ObjFiles.Count == 0) {
                Log(Level.Debug, "No files to compile.");
                return;
            }

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

            libTask.OutputFile = new FileInfo(projectConfig.OutputPath);

            // Additional Library Directory
            string addLibDirs = projectConfig.GetToolSetting(VcConfigurationBase.LibTool, "AdditionalLibraryDirectories");
            if (!String.IsNullOrEmpty(addLibDirs)) {
                foreach (string addLibDir in addLibDirs.Split(',', ';')) {
                    if (addLibDir.Length == 0) {
                        continue;
                    }
                    libTask.LibDirs.DirectoryNames.Add(addLibDir);
                }
            }

            // Additional Dependencies
            string addDeps = projectConfig.GetToolSetting(VcConfigurationBase.LibTool, "AdditionalDependencies");
            if (!String.IsNullOrEmpty(addDeps)) {
                int insertedDeps = 0;
                foreach (string addDep in addDeps.Split(' ')) {
                    if (Path.GetExtension(addDep) == ".obj") {
                        projectConfig.ObjFiles.Insert(insertedDeps++, addDep);
                    } else {
                        libTask.Sources.FileNames.Add(addDep);
                    }
                }
            }

            foreach (string objFile in projectConfig.ObjFiles) {
                libTask.Sources.FileNames.Add(objFile);
            }
            
            // Module Definition File Name
            string moduleDefinitionFile = projectConfig.GetToolSetting(VcConfigurationBase.LibTool, "ModuleDefinitionFile");
            if (!String.IsNullOrEmpty(moduleDefinitionFile)) {
                libTask.ModuleDefinitionFile = new FileInfo(FileUtils.CombinePaths(
                    ProjectDirectory.FullName, moduleDefinitionFile));
            }

            // Ignore All Default Libraries
            string ignoreAllDefaultLibraries = projectConfig.GetToolSetting(VcConfigurationBase.LibTool, "IgnoreAllDefaultLibraries");
            if (string.Compare(ignoreAllDefaultLibraries, "TRUE", true, CultureInfo.InvariantCulture) == 0) {
                libTask.Options = "/NODEFAULTLIB";
            }

            // Ignore Specific Libraries
            string ignoreDefaultLibraries = projectConfig.GetToolSetting(VcConfigurationBase.LibTool, "IgnoreDefaultLibraryNames");
            if (!String.IsNullOrEmpty(ignoreDefaultLibraries)) {
                foreach (string ignoreLibrary in ignoreDefaultLibraries.Split(';')) {
                    libTask.IgnoreLibraries.Add(new Library(ignoreLibrary));
                }
            }

            // Export Named Functions
            // TODO

            // Forced Symbol References
            string symbolReferences = projectConfig.GetToolSetting(VcConfigurationBase.LibTool,
                "ForceSymbolReferences");
            if (!String.IsNullOrEmpty(symbolReferences)) {
                foreach (string symbol in symbolReferences.Split(';')) {
                    libTask.Symbols.Add(new Symbol(symbol));
                }
            }

            // execute the task
            ExecuteInProjectDirectory(libTask);
        }

        private void RunLinker(Configuration solutionConfiguration) {
            const string noinherit = "$(noinherit)";

            // obtain project configuration (corresponding with solution configuration)
            VcProjectConfiguration projectConfig = (VcProjectConfiguration) BuildConfigurations[solutionConfiguration];

            // check if linking needs to be performed
            if (projectConfig.ObjFiles.Count == 0) {
                Log(Level.Debug, "No files to link.");
                return;
            }

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
            linkTask.Modules.Parent = linkTask;
            linkTask.EmbeddedResources.Project = linkTask.Project;

            // inherit project from solution task for child elements
            linkTask.Sources.Project = linkTask.Project;
            linkTask.LibDirs.Project = linkTask.Project;
            linkTask.Modules.Project = linkTask.Project;
            linkTask.EmbeddedResources.Project = linkTask.Project;

            // inherit namespace manager from parent
            linkTask.Sources.NamespaceManager = linkTask.NamespaceManager;
            linkTask.LibDirs.NamespaceManager = linkTask.NamespaceManager;
            linkTask.Modules.NamespaceManager = linkTask.NamespaceManager;
            linkTask.EmbeddedResources.NamespaceManager = linkTask.NamespaceManager;

            // set base directory of filesets
            linkTask.Sources.BaseDirectory = ProjectDirectory;
            linkTask.LibDirs.BaseDirectory = ProjectDirectory;
            linkTask.Modules.BaseDirectory = ProjectDirectory;
            linkTask.EmbeddedResources.BaseDirectory = ProjectDirectory;

            string addDeps = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "AdditionalDependencies");
            if (!String.IsNullOrEmpty(addDeps)) {
                // only include default libraries if noinherit is not set
                if (addDeps.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit) == -1) {
                    foreach (string defaultLib in _defaultLibraries) {
                        linkTask.Sources.FileNames.Add(defaultLib);
                    }
                } else {
                    addDeps = addDeps.Remove(addDeps.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit), noinherit.Length);
                }
                foreach (string addDep in addDeps.Split(' ')) {
                    if (Path.GetExtension(addDep) == ".obj") {
                        // skip obj files are these are handled in 
                        // VcProjectConfiguration
                        continue;
                    }
                    linkTask.Sources.FileNames.Add(addDep);
                }
            } else {
                // always include default libraries if no additional dependencies
                // are specified
                foreach (string defaultLib in _defaultLibraries) {
                    linkTask.Sources.FileNames.Add(defaultLib);
                }
            }

            string delayLoadedDlls = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "DelayLoadDLLs");
            if (!String.IsNullOrEmpty(delayLoadedDlls)) {
                foreach (string dll in delayLoadedDlls.Split(';')) {
                    linkTask.DelayLoadedDlls.FileNames.Add(dll);
                }
            }

            foreach (string objFile in projectConfig.ObjFiles) {
                linkTask.Sources.FileNames.Add(objFile);
            }

            // add output generated by referenced projects and explicit project
            // dependencies
            ProjectBaseCollection projectDependencies = GetVcProjectDependencies();
            foreach (VcProject vcProject in projectDependencies) {
                VcProjectConfiguration vcProjectConfig = vcProject.BuildConfigurations[
                    solutionConfiguration] as VcProjectConfiguration;

                switch (vcProjectConfig.Type) {
                    case VcProjectConfiguration.ConfigurationType.Application:
                    case VcProjectConfiguration.ConfigurationType.DynamicLibrary:
                        FileInfo dependencyImportLibrary = vcProjectConfig.LinkerConfiguration.ImportLibrary;
                        if (dependencyImportLibrary != null) {
                            linkTask.Sources.FileNames.Add(
                                dependencyImportLibrary.FullName);
                        }
                        break;
                    case VcProjectConfiguration.ConfigurationType.StaticLibrary:
                        linkTask.Sources.FileNames.Add(vcProjectConfig.OutputPath);
                        break;
                }
            }

            linkTask.OutputFile = new FileInfo(projectConfig.OutputPath);

            // ensure directory exists
            if (!linkTask.OutputFile.Directory.Exists) {
                linkTask.OutputFile.Directory.Create();
                linkTask.OutputFile.Directory.Refresh();
            }

            // generation of debug information
            linkTask.Debug = bool.Parse(projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "GenerateDebugInformation", "FALSE"));

            string pdbFile = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "ProgramDatabaseFile");
            // use default value if pdb was not explicitly disabled (by setting it to empty string)
            if (pdbFile == null && linkTask.Debug) {
                pdbFile = projectConfig.ExpandMacros("$(OutDir)/$(ProjectName).pdb");
            }
            if (!String.IsNullOrEmpty(pdbFile)) {
                if (OutputDir != null) {
                    pdbFile = FileUtils.CombinePaths(OutputDir.FullName, Path.GetFileName(pdbFile));
                } else {
                    pdbFile = FileUtils.CombinePaths(ProjectDirectory.FullName, pdbFile);
                }
                linkTask.ProgramDatabaseFile = new FileInfo(pdbFile);

                // ensure directory exists
                if (!linkTask.ProgramDatabaseFile.Directory.Exists) {
                    linkTask.ProgramDatabaseFile.Directory.Create();
                    linkTask.ProgramDatabaseFile.Directory.Refresh();
                }
            }

            // generation of import library
            FileInfo importLibrary = projectConfig.LinkerConfiguration.ImportLibrary;
            if (importLibrary != null) {
                Argument importLibraryArg = new Argument();
                importLibraryArg.Line = "/IMPLIB:" + LinkTask.QuoteArgumentValue(
                    importLibrary.FullName);
                linkTask.Arguments.Add(importLibraryArg);

                // ensure directory exists
                if (!importLibrary.Directory.Exists) {
                    importLibrary.Directory.Create();
                    importLibrary.Directory.Refresh();
                }
            }

            // Ignore Specific Libraries
            string ignoreDefaultLibraries = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, 
                "IgnoreDefaultLibraryNames");
            if (!String.IsNullOrEmpty(ignoreDefaultLibraries)) {
                foreach (string ignoreLibrary in ignoreDefaultLibraries.Split(';')) {
                    linkTask.IgnoreLibraries.Add(new Library(ignoreLibrary));
                }
            }

            // Forced Symbol References
            string symbolReferences = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool,
                "ForceSymbolReferences");
            if (!String.IsNullOrEmpty(symbolReferences)) {
                foreach (string symbol in symbolReferences.Split(';')) {
                    linkTask.Symbols.Add(new Symbol(symbol));
                }
            }

            // generation of map file during linking
            bool generateMapFile = bool.Parse(projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "GenerateMapFile", "FALSE"));
            if (generateMapFile) {
                Argument mapArg = new Argument();

                string mapFileName = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "MapFileName");
                if (!String.IsNullOrEmpty(mapFileName)) {
                    mapArg.Line = "/MAP:" + LinkTask.QuoteArgumentValue(mapFileName);
                } else {
                    mapArg.Line = "/MAP";
                }

                linkTask.Arguments.Add(mapArg);
            }

            // total heap allocation size
            string heapReserveSize = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "HeapReserveSize");
            if (!String.IsNullOrEmpty(heapReserveSize)) {
                Argument heapArg = new Argument();

                string heapCommitSize = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "HeapCommitSize");
                if (!String.IsNullOrEmpty(heapCommitSize)) {
                    heapArg.Line = string.Format(CultureInfo.InvariantCulture, 
                        "/HEAP:{0},{1}", heapReserveSize, heapCommitSize);
                } else {
                    heapArg.Line = "/HEAP:" + heapReserveSize;
                }

                linkTask.Arguments.Add(heapArg);
            }

            // total stack allocation size
            string stackReserveSize = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "StackReserveSize");
            if (!String.IsNullOrEmpty(stackReserveSize)) {
                Argument stackArg = new Argument();

                string stackCommitSize = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "StackCommitSize");
                if (!String.IsNullOrEmpty(stackCommitSize)) {
                    stackArg.Line = string.Format(CultureInfo.InvariantCulture, 
                        "/STACK:{0},{1}", stackReserveSize, stackCommitSize);
                } else {
                    stackArg.Line = "/STACK:" + stackReserveSize;
                }

                linkTask.Arguments.Add(stackArg);
            }

            if (projectConfig.Type == VcProjectConfiguration.ConfigurationType.DynamicLibrary) {
                linkTask.Arguments.Add(new Argument("/DLL"));
            }

            string addLibDirs = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "AdditionalLibraryDirectories");
            if (!String.IsNullOrEmpty(addLibDirs)) {
                foreach (string addLibDir in addLibDirs.Split(',', ';')) {
                    if (addLibDir.Length == 0) {
                        continue;
                    }
                    linkTask.LibDirs.DirectoryNames.Add(addLibDir);
                }
            }

            // links to modules
            string modules = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "AddModuleNamesToAssembly");
            if (!String.IsNullOrEmpty(modules)) {
                foreach (string module in modules.Split(';')) {
                    linkTask.Modules.FileNames.Add(module);
                }
            }

            // embedded resources
            string embeddedResources = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "EmbedManagedResourceFile");
            if (!String.IsNullOrEmpty(embeddedResources)) {
                foreach (string embeddedResource in embeddedResources.Split(';')) {
                    linkTask.EmbeddedResources.FileNames.Add(embeddedResource);
                }
            }

            Hashtable linkerArgs = projectConfig.GetToolArguments(VcConfigurationBase.LinkerTool, _linkerArgMap);
            foreach (string arg in linkerArgs.Values) {
                Argument linkArg = new Argument();
                linkArg.Line = (string) arg;
                linkTask.Arguments.Add(linkArg);
            }

            string addOptions = projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, "AdditionalOptions");
            if (!String.IsNullOrEmpty(addOptions)) {
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

            if (projectConfig.WholeProgramOptimization) {
                linkTask.Arguments.Add(new Argument("/LTCG"));
            }

            // execute the task
            ExecuteInProjectDirectory(linkTask);
        }

        private void ExecuteInProjectDirectory(Task task) {
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
        /// Merges the specified tool setting of <paramref name="projectConfig" /> 
        /// with <paramref name="fileConfig" />.
        /// </summary>
        /// <remarks>
        /// The merge is suppressed when the flag $(noinherit) is defined in
        /// <paramref name="fileConfig" />.
        /// </remarks>
        private string MergeToolSetting(VcProjectConfiguration projectConfig, VcConfigurationBase fileConfig, string tool, string setting) {
            const string noinherit = "$(noinherit)";

            // get tool setting from either the file configuration or project 
            // configuration (if setting is not defined on file configuration)
            string settingValue = fileConfig.GetToolSetting(tool, setting);
            if (settingValue != null) {
                if (settingValue.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit) == -1) {
                    // only add project-level setting to value if noherit if 
                    // "fileConfig" is not actually the project config
                    if (!object.ReferenceEquals(projectConfig, fileConfig)) {
                        string baseSettingValue = projectConfig.GetToolSetting(tool, setting);
                        if (!String.IsNullOrEmpty(baseSettingValue)) {
                            settingValue += ";" + baseSettingValue;
                        }
                    }
                } else {
                    settingValue = settingValue.Remove(settingValue.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit), noinherit.Length);
                }
            } else {
                // if settingValue is null, then its not defined in neither the 
                // file nor the project configuration
                return null;
            }

            // individual values are separated by ';'
            string[] values = settingValue.Split(';');

            // holds filtered setting value
            settingValue = string.Empty;

            // filter duplicate setting values
            Hashtable filteredValues = CollectionsUtil.CreateCaseInsensitiveHashtable(values.Length);
            foreach (string value in values) {
                if (!filteredValues.ContainsKey(value)) {
                    filteredValues.Add(value, null);

                    if (settingValue.Length != 0) {
                        settingValue += ';';
                    }
                    settingValue += value;
                }
            }

            return settingValue;
        }

        private bool PreBuild(VcProjectConfiguration projectConfig) {
            // check if the build event should be executed
            string excludedFromBuild = projectConfig.GetToolSetting(VcConfigurationBase.PreBuildEventTool,
                "ExcludedFromBuild");
            if (excludedFromBuild != null) {
                if (string.Compare(excludedFromBuild.Trim(), "true", true, CultureInfo.InvariantCulture) == 0) {
                    return true;
                }
            }

            string commandLine = projectConfig.GetToolSetting(VcConfigurationBase.PreBuildEventTool,
                "CommandLine");
            if (!String.IsNullOrEmpty(commandLine)) {
                Log(Level.Info, "Performing Pre-Build Event...");
                return ExecuteBuildEvent("Pre-Build", commandLine, projectConfig);
            }

            return true;
        }

        private bool PostBuild(VcProjectConfiguration projectConfig) {
            // check if the build event should be executed
            string excludedFromBuild = projectConfig.GetToolSetting(VcConfigurationBase.PostBuildEventTool,
                "ExcludedFromBuild");
            if (excludedFromBuild != null) {
                if (string.Compare(excludedFromBuild.Trim(), "true", true, CultureInfo.InvariantCulture) == 0) {
                    return true;
                }
            }

            string commandLine = projectConfig.GetToolSetting(VcConfigurationBase.PostBuildEventTool,
                "CommandLine");
            if (!String.IsNullOrEmpty(commandLine)) {
                Log(Level.Info, "Performing Post-Build Event...");
                return ExecuteBuildEvent("Post-Build", commandLine, projectConfig);
            }

            return true;
        }

        private bool PreLink(VcProjectConfiguration projectConfig) {
            // check if the build event should be executed
            string excludedFromBuild = projectConfig.GetToolSetting(VcConfigurationBase.PreLinkEventTool,
                "ExcludedFromBuild");
            if (excludedFromBuild != null) {
                if (string.Compare(excludedFromBuild.Trim(), "true", true, CultureInfo.InvariantCulture) == 0) {
                    return true;
                }
            }

            string commandLine = projectConfig.GetToolSetting(VcConfigurationBase.PreLinkEventTool,
                "CommandLine");
            if (!String.IsNullOrEmpty(commandLine)) {
                Log(Level.Info, "Performing Pre-Link Event...");
                return ExecuteBuildEvent("Pre-Link", commandLine, projectConfig);
            }

            return true;
        }

        private bool ExecuteBuildEvent(string buildEvent, string buildCommandLine, ConfigurationBase config) {
            string batchFile = null;

            try {
                // get unique temp file name to write command line of build event to
                batchFile = Path.GetTempFileName();

                // remove temp file
                File.Delete(batchFile);

                // change extension to .bat
                batchFile = Path.ChangeExtension(batchFile, ".bat");

                // execute the build event
                return base.ExecuteBuildEvent(buildEvent, buildCommandLine, batchFile, 
                    ProjectDirectory.FullName, config);
            } finally {
                if (batchFile != null && File.Exists(batchFile)) {
                    File.Delete(batchFile);
                }
            }
        }

        /// <summary>
        /// Gets the absolute path to the object file or directory.
        /// </summary>
        /// <param name="fileConfig">The build configuration</param>
        /// <returns>
        /// The absolute path to the object file or directory, or 
        /// </returns>
        /// <remarks>
        /// We use an absolute path for the object file, otherwise 
        /// <c>&lt;cl&gt;</c> assumes a location relative to the output 
        /// directory - not the project directory.
        /// </remarks>
        private string GetObjectFile(VcConfigurationBase fileConfig) {
            string objectFile = fileConfig.GetToolSetting("VCCLCompilerTool", 
                "ObjectFile", "$(IntDir)/");
            if (!String.IsNullOrEmpty(objectFile)) {
                return FileUtils.CombinePaths(ProjectDirectory.FullName, 
                    objectFile);
            }
            return null;
        }

        private ProjectBaseCollection GetVcProjectDependencies() {
            ProjectBaseCollection vcProjectDependencies = new ProjectBaseCollection();
            foreach (ProjectBase projectDependency in ProjectDependencies) {
                if (projectDependency is VcProject) {
                    vcProjectDependencies.Add(projectDependency);
                }
            }

            foreach (ReferenceBase reference in References) {
                // skip non-project reference
                ProjectReferenceBase projectReference = reference as ProjectReferenceBase;
                if (projectReference == null) {
                    continue;
                }

                // check if we're dealing with reference to VC++ project
                VcProject vcProject = projectReference.Project as VcProject;
                if (vcProject == null) {
                    continue;
                }

                // skip projects that have already been added to collection
                if (vcProjectDependencies.Contains(vcProject)) {
                    continue;
                }

                vcProjectDependencies.Add(vcProject);
            }

            return vcProjectDependencies;
        }

        #endregion Private Instance Methods

        #region Public Static Methods

        public static string LoadGuid(XmlElement xmlDefinition) {
            return xmlDefinition.GetAttribute("ProjectGUID");
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

            //MAL 2007/12/20
            //this is double check - really needed? In addition, when version check fails, it gives odd error.
            //2nd check is made in DetermineProductVersion (called from ProjectBase constructor)
            //try {
            //    GetProductVersion(docElement);
            //    // no need to perform version check here as this is done in 
            //    // GetProductVersion
            //} catch {
            //    // product version could not be determined or is not supported
            //    return false;
            //}

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
                string ver = productVersionAttribute.Value;
                ver = ver.Replace(',', '.'); //Whidbey (8,00) and Orcas (9,00) are using comma instead of point
                productVersion = new Version(ver);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The value of the \"Version\" attribute ({0}) is not a valid"
                    + " version string.", productVersionAttribute.Value),
                    Location.UnknownLocation, ex);
            }

            switch(productVersion.Major) {
                case 7:
                    switch (productVersion.Minor) {
                        case 0:
                            return ProductVersion.Rainier;
                        case 10:
                            return ProductVersion.Everett;
                    }
                    break;
                case 8:
                    return ProductVersion.Whidbey;
                case 9:
                    return ProductVersion.Orcas;
            } 

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Visual Studio version \"{0}\" is not supported.",
                productVersion.ToString()), Location.UnknownLocation);
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private readonly string _name;
        private readonly string _projectPath;
        private string _guid;
        private readonly ArrayList _references;

        private readonly VcArgumentMap _clArgMap;
        private readonly VcArgumentMap _linkerArgMap;
        private readonly VcArgumentMap _midlArgMap;

        /// <summary>
        /// Holds the files included in the project.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   For project files with no specific file configuration, the relative
        ///   path is added to the list.
        ///   </para>
        ///   <para>
        ///   For project files that have a specific file configuration, a
        ///   <see cref="Hashtable" /> containing the <see cref="VcFileConfiguration" />
        ///   instance representing the file configurations is added.
        ///   </para>
        /// </remarks>
        private readonly ArrayList _projectFiles;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static string[] _defaultLibraries = new string[] { 
                                                                     "kernel32.lib", "user32.lib", "gdi32.lib", "winspool.lib", "comdlg32.lib",
                                                                     "advapi32.lib", "shell32.lib", "ole32.lib", "oleaut32.lib", "uuid.lib", 
                                                                     "odbc32.lib", "odbccp32.lib"
                                                                 };

        #endregion Private Static Fields
    }
}
