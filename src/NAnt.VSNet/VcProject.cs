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

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;
using NAnt.VisualCpp.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Visual C++ project.
    /// </summary>
    public class VcProject: ProjectBase {
        #region Public Instance Constructors
        
        public VcProject(SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(solutionTask, tfc, gacCache, refResolver, outputDir) {
            _htPlatformConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _references = new ArrayList();
            _clArgMap = VcArgumentMap.CreateCLArgumentMap();
            _linkerArgMap = VcArgumentMap.CreateLinkerArgumentMap();
            _objFiles = new ArrayList();
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
        /// Gets or sets the unique identifier of the Visual C++ project.
        /// </summary>
        public override string Guid {
            get { return _guid; }
            set { _guid = value; }
        }

        public override ArrayList References {
            get { return _references; }
        }

        protected override bool Build(ConfigurationBase configurationSettings) {
            _objFiles.Clear();
            
            VcConfiguration baseConfig = (VcConfiguration) configurationSettings;

            // initialize hashtable for holding all build configuration
            Hashtable buildConfigs = new Hashtable();

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
                }
            }

            string nmakeCommand = baseConfig.GetToolSetting("VCNMakeTool", "BuildCommandLine");
            if (nmakeCommand != null) {
                RunNMake(nmakeCommand);
                return true;
            }

            foreach (VcConfiguration config in buildConfigs.Keys) {
                BuildCPPFiles((ArrayList) buildConfigs[config], baseConfig, config);
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

            return true;
        }

        #endregion Override implementation of ProjectBase

        #region Public Instance Methods

        public override void Load(SolutionBase sln, string projectPath) {
            _projectPath = Path.GetFullPath(projectPath);
            XmlDocument doc = null;

            try {
                doc = LoadXmlDocument(_projectPath);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading project '{0}'.", _projectPath), 
                    Location.UnknownLocation, ex);
            }

            XmlElement elem = doc.DocumentElement;
            _name = elem.GetAttribute("Name");
            _guid = elem.GetAttribute("ProjectGUID");
            _rootNamespace = elem.GetAttribute("RootNamespace");

            XmlNodeList configurationNodes = elem.SelectNodes("//Configurations/Configuration");
            foreach (XmlElement configElem in configurationNodes) {
                VcConfiguration config = new VcConfiguration(configElem, this, sln, OutputDir);
                ProjectConfigurations[config.Name] = config;
                _htPlatformConfigurations[config.FullName] = config;
            }

            XmlNodeList projectReferences = elem.SelectNodes("//References/ProjectReference");
            foreach (XmlElement referenceElem in projectReferences) {
                ReferenceBase reference = ReferenceFactory.CreateReference(sln, null, referenceElem, GacCache, ReferencesResolver, this, OutputDir);
                _references.Add(reference);
            }

            XmlNodeList assemblyReferences = elem.SelectNodes("//References/AssemblyReference");
            foreach (XmlElement referenceElem in assemblyReferences) {
                ReferenceBase reference = ReferenceFactory.CreateReference(sln, null, referenceElem, GacCache, ReferencesResolver, this, OutputDir);
                _references.Add(reference);
            }

            XmlNodeList fileNodes = elem.SelectNodes("//File");
            foreach (XmlElement fileElem in fileNodes) {
                string relPath = fileElem.GetAttribute("RelativePath");
                
                Hashtable htFileConfigurations = null;
                XmlNodeList fileConfigList = fileElem.GetElementsByTagName("FileConfiguration");
                if (fileConfigList.Count > 0) {
                    htFileConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
                    foreach (XmlElement fileConfigElem in fileConfigList) {
                        string fileConfigName = fileConfigElem.GetAttribute("Name");
                        VcConfiguration baseConfig = (VcConfiguration) _htPlatformConfigurations[fileConfigName];
                        VcConfiguration fileConfig = new VcConfiguration(fileConfigElem, this, sln, baseConfig, OutputDir);
                        htFileConfigurations [fileConfig.Name] = fileConfig;
                   }
                }

                _htFiles [relPath] = htFileConfigurations;
            }
        }

        #endregion Public Instance Methods

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
            clTask.OutputDir.Create();

            string includeDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalIncludeDirectories");
            if (!StringUtils.IsNullOrEmpty(includeDirs)) {
                foreach (string includeDir in includeDirs.Split(',', ';')) {
                    clTask.IncludeDirs.DirectoryNames.Add(
                        CleanPath(includeDir));
                }
            }

            string metadataDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalUsingDirectories");
            if (!StringUtils.IsNullOrEmpty(metadataDirs)) {
                foreach (string metadataDir in metadataDirs.Split(';')) {
                    clTask.MetaDataIncludeDirs.DirectoryNames.Add(
                        CleanPath(baseConfig.ExpandMacros(metadataDir)));
                }
            }

            string forcedUsingFiles = fileConfig.GetToolSetting(compilerTool, "ForcedUsingFiles");
            if (!StringUtils.IsNullOrEmpty(forcedUsingFiles)) {
                foreach (string forcedUsingFile in forcedUsingFiles.Split(';')) {
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

            string preprocessorDefs = fileConfig.GetToolSetting(compilerTool, "PreprocessorDefinitions");
            if (!StringUtils.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string def in preprocessorDefs.Split(';', ',')) {
                    clTask.Arguments.Add(new Argument("/D"));
                    clTask.Arguments.Add(new Argument(def));
                }
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
                        break;
                    default:
                        clTask.Arguments.Add(new Argument((string) compilerArgs[key]));
                        break;
                }
            }
                
            // enable/disable Managed Extensions for C++
            clTask.ManagedExtensions = fileConfig.ManagedExtensions;

            // execute the task
            ExecuteInProjectDirectory(clTask);
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
                    linkTask.LibDirs.DirectoryNames.Add(addLibDir);
                }
            }

            Hashtable linkerArgs = baseConfig.GetToolArguments(linkerTool, _linkerArgMap);
            foreach (string arg in linkerArgs.Values) {
                linkTask.Arguments.Add(new Argument(arg));
            }

            string addOptions = baseConfig.GetToolSetting(linkerTool, "AdditionalOptions");
            if (addOptions != null) {
                foreach(string addOption in addOptions.Split(' ')) {
                    linkTask.Arguments.Add(new Argument(addOption));
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

        #endregion Private Static Methods

        #region Private Instance Fields

        private string _name;
        private string _projectPath;
        private string _guid;
        private string _rootNamespace;
        private ArrayList _references;
        private Hashtable _htPlatformConfigurations;
        private Hashtable _htFiles;
        private ArrayList _objFiles;
        private VcArgumentMap _clArgMap;
        private VcArgumentMap _linkerArgMap;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static string[] _defaultLibraries = new string[] { 
            "kernel32.lib", "user32.lib", "gdi32.lib", "winspool.lib", "comdlg32.lib",
            "advapi32.lib", "shell32.lib", "ole32.lib", "oleaut32.lib", "uuid.lib"
        };

        #endregion Private Static Fields
    }
}
