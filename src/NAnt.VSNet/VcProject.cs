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
        
        public VcProject(SolutionTask solutionTask, TempFileCollection tfc, string outputDir) {
            _htConfigurations         = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htPlatformConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htFiles                  = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferences             = CollectionsUtil.CreateCaseInsensitiveHashtable();
            
            _solutionTask = solutionTask;
            _outputDir    = outputDir;

            _clArgMap     = VcArgumentMap.CreateCLArgumentMap();
            _linkerArgMap = VcArgumentMap.CreateLinkerArgumentMap();

            _objFiles = new ArrayList();
        }

        #endregion Public Instance Constructors

        #region Override implementation of ProjectBase

        public override string Name {
            get { return _name; }
        }

        public override string GUID {
            get { return _guid; }
            set { _guid = value; }
        }

        public override string[] Configurations {
            get {
                return (string[]) new ArrayList(_htConfigurations.Keys).ToArray(typeof(string));
            }
        }

        public override Reference[] References {
            get { return (Reference[]) new ArrayList(_htReferences.Values).ToArray(typeof(Reference)); }
        }

        public override bool Compile(string configuration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands) {
            _objFiles.Clear();
            
            VcConfiguration baseConfig = (VcConfiguration) _htConfigurations [configuration];

            ArrayList cppFilesToBuild = new ArrayList();
            VcConfiguration lastConfig = null;
            
            foreach (DictionaryEntry de in _htFiles) {
                string fileName = (string) de.Key;
                string ext = Path.GetExtension(fileName).ToLower(CultureInfo.InvariantCulture);
                
                VcConfiguration fileConfig = null;
                if (de.Value != null) {
                    if (de.Value is Hashtable) {
                        Hashtable _htValue = (Hashtable) de.Value;
                        fileConfig = (VcConfiguration) _htValue[configuration];
                    } else {
                        fileConfig = (VcConfiguration) de.Value;
                    }
                }

                if (fileConfig == null) {
                    fileConfig = baseConfig;
                }

                if (ext == ".cpp" || ext == ".c") {
                    if (fileConfig != lastConfig) {
                        if (cppFilesToBuild.Count > 0) {
                            BuildCPPFiles(cppFilesToBuild, baseConfig, lastConfig);
                        }

                        lastConfig = fileConfig;
                        cppFilesToBuild.Clear();
                    }
                    cppFilesToBuild.Add(fileName);
                }
            }

            if (cppFilesToBuild.Count > 0) {
                BuildCPPFiles(cppFilesToBuild, baseConfig, lastConfig);
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

        public override string GetOutputFile(string configuration) {
            VcConfiguration config = (VcConfiguration) _htConfigurations [configuration];
            if (config == null) {
                return null;
            }

            return config.OutputFile;
        }

        public string GetProjectDir(string configuration) {
            VcConfiguration config = (VcConfiguration) _htConfigurations[configuration];
            if (config == null) {
                return null;
            }

            return config.ProjectDir;
        }

        #endregion Override implementation of ProjectBase

        #region Public Instance Methods

        public override void Load(Solution sln, string fileName) {
            _projectDirectory = Path.GetFullPath(Path.GetDirectoryName(fileName));
            
            XmlDocument doc = LoadXmlDocument(fileName);

            XmlElement elem = doc.DocumentElement;
            _name = elem.GetAttribute("Name");
            _guid = elem.GetAttribute("ProjectGUID");

            XmlNodeList configurationNodes = elem.SelectNodes("//Configurations/Configuration");
            foreach (XmlElement configElem in configurationNodes) {
                VcConfiguration config = new VcConfiguration(configElem, this, sln, _outputDir);
                _htConfigurations [config.Name] = config;
                _htPlatformConfigurations [config.FullName] = config;
            }

            XmlNodeList referenceNodes = elem.SelectNodes("//References/ProjectReference");
            foreach (XmlElement referenceElem in referenceNodes) {
                Reference reference = new Reference(sln, null, referenceElem, _solutionTask, _outputDir);
                _htReferences[referenceElem.Attributes["Name"].Value] = reference;
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
                        VcConfiguration baseConfig = (VcConfiguration) _htPlatformConfigurations [fileConfigName];
                        VcConfiguration fileConfig = new VcConfiguration(fileConfigElem, this, sln, baseConfig, _outputDir);
                        htFileConfigurations [fileConfig.Name] = fileConfig;
                   }
                }

                _htFiles [relPath] = htFileConfigurations;
            }
        }

        public override ConfigurationBase GetConfiguration(string configuration) {
            return (VcConfiguration) _htConfigurations[configuration];
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        public static string LoadGUID(string fileName) {
            XmlDocument doc = LoadXmlDocument(fileName);
            return doc.DocumentElement.GetAttribute("ProjectGUID");
        }

        #endregion Public Static Methods

        #region Private Instance Methods

        private void BuildCPPFiles(ArrayList fileNames, VcConfiguration baseConfig, VcConfiguration fileConfig) {
            const string compilerTool = "VCCLCompilerTool";

            // create instance of Cl task
            ClTask clTask = new ClTask();

            // inherit project from solution task
            clTask.Project = _solutionTask.Project;

            // inherit parent from solution task
            clTask.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            clTask.Verbose = _solutionTask.Verbose;

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

            // set base directories
            clTask.IncludeDirs.BaseDirectory = fileConfig.ProjectDir;
            clTask.Sources.BaseDirectory = fileConfig.ProjectDir;
            clTask.MetaDataIncludeDirs.BaseDirectory = fileConfig.ProjectDir;
            clTask.ForcedUsingFiles.BaseDirectory = fileConfig.ProjectDir;

            // set task properties
            clTask.OutputDir = Path.Combine(_projectDirectory, baseConfig.IntermediateDir);
            clTask.PchFile = fileConfig.GetToolSetting(compilerTool, "PrecompiledHeaderFile");
            Directory.CreateDirectory(clTask.OutputDir);

            string includeDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalIncludeDirectories");
            if (!StringUtils.IsNullOrEmpty(includeDirs)) {
                foreach (string includeDir in includeDirs.Split(',')) {
                    clTask.IncludeDirs.DirectoryNames.Add(includeDir);
                }
            }

            string metadataDirs = fileConfig.GetToolSetting(compilerTool, "AdditionalUsingDirectories");
            if (!StringUtils.IsNullOrEmpty(metadataDirs)) {
                foreach (string metadataDir in metadataDirs.Split(';')) {
                    clTask.MetaDataIncludeDirs.DirectoryNames.Add(baseConfig.ExpandMacros(metadataDir));
                }
            }

            string forcedUsingFiles = fileConfig.GetToolSetting(compilerTool, "ForcedUsingFiles");
            if (!StringUtils.IsNullOrEmpty(forcedUsingFiles)) {
                foreach (string forcedUsingFile in forcedUsingFiles.Split(';')) {
                    clTask.ForcedUsingFiles.Includes.Add(baseConfig.ExpandMacros(forcedUsingFile));
                }
            }

            string asmOutput = fileConfig.GetToolSetting(compilerTool, "AssemblerOutput");
            string asmListingLocation = fileConfig.GetToolSetting(compilerTool, "AssemblerListingLocation");
            if (asmOutput != null && asmOutput != "0" && asmListingLocation != null) {
                // parameter for AssemblerOutput itself will be handled by the map
                clTask.Arguments.Add(new Argument("/Fa" + asmListingLocation));
            }

            string intermediateDir = Path.Combine(_projectDirectory, baseConfig.IntermediateDir);

            foreach (string fileName in fileNames) {
                clTask.Sources.FileNames.Add(fileName);
                _objFiles.Add(Path.Combine(intermediateDir, Path.GetFileNameWithoutExtension(fileName) + ".obj"));
            }

            string preprocessorDefs = baseConfig.GetToolSetting(compilerTool, "PreprocessorDefinitions");
            if (!StringUtils.IsNullOrEmpty(preprocessorDefs)) {
                foreach (string def in preprocessorDefs.Split(';', ',')) {
                    clTask.Arguments.Add(new Argument("/D"));
                    clTask.Arguments.Add(new Argument(def));
                }
            }

            if (IsOutputDLL(baseConfig)) {
                clTask.Arguments.Add(new Argument("/D"));
                clTask.Arguments.Add(new Argument("_WINDLL"));
            }

            if (baseConfig.WholeProgramOptimization) {
                clTask.Arguments.Add(new Argument("/GL"));
            }

            string[] args = baseConfig.GetToolArguments(compilerTool, _clArgMap);   
            foreach (string arg in args) {
                clTask.Arguments.Add(new Argument(arg));
            }

            if (baseConfig.ManagedExtensions) {
                // enable Managed Extensions for C++
                clTask.ManagedExtensions = true;
            }

            // execute the task
            ExecuteInProjectDirectory(clTask);
        }

        private void RunLibrarian(VcConfiguration baseConfig) {
            // create instance of Lib task
            LibTask libTask = new LibTask();

            // inherit project from solution task
            libTask.Project = _solutionTask.Project;

            // inherit parent from solution task
            libTask.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            libTask.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            libTask.InitializeTaskConfiguration();

            // set parent of child elements
            libTask.Sources.Parent = libTask;

            // inherit project from solution task for child elements
            libTask.Sources.Project = libTask.Project;

            // set task properties
            string outFile = baseConfig.GetToolSetting("VCLibrarianTool", "OutputFile");
            libTask.Output = Path.Combine(_projectDirectory, outFile);

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
            linkTask.Project = _solutionTask.Project;

            // inherit parent from solution task
            linkTask.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            linkTask.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            linkTask.InitializeTaskConfiguration();

            // set parent of child elements
            linkTask.Sources.Parent = linkTask;
            linkTask.LibDirs.Parent = linkTask;

            // inherit project from solution task for child elements
            linkTask.Sources.Project = linkTask.Project;
            linkTask.LibDirs.Project = linkTask.Project;

            // set task properties
            string outFile = baseConfig.GetToolSetting(linkerTool, "OutputFile");
            string pdbFile = baseConfig.GetToolSetting(linkerTool, "ProgramDatabaseFile");
            if (!StringUtils.IsNullOrEmpty(_outputDir)) {
                linkTask.Output = Path.Combine(_outputDir, Path.GetFileName(outFile));
                if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                    pdbFile = Path.Combine(_outputDir, Path.GetFileName(pdbFile));
                }
            }
            else {
                linkTask.Output = Path.Combine(_projectDirectory, outFile);
                if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                    pdbFile = Path.Combine(_projectDirectory, pdbFile);
                }
            }

            if (!StringUtils.IsNullOrEmpty(pdbFile)) {
                linkTask.Arguments.Add(new Argument("/PDB:" + pdbFile));
            }

            if (IsOutputDLL(baseConfig)) {
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
                foreach (string addLibDir in addLibDirs.Split(',')) {
                    linkTask.LibDirs.DirectoryNames.Add(addLibDir);
                }
            }

            string[] args = baseConfig.GetToolArguments(linkerTool, _linkerArgMap);
            foreach (string arg in args) {
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

        private bool IsOutputDLL(VcConfiguration config) {
            string outFile = config.GetToolSetting("VCLinkerTool", "OutputFile");
            if (outFile == null) {
                return false;
            }
            return Path.GetExtension(outFile).ToLower(CultureInfo.InvariantCulture) == ".dll";
        }

        private void ExecuteInProjectDirectory(NAnt.Core.Task task) {
            string oldBaseDir = _solutionTask.Project.BaseDirectory;
            _solutionTask.Project.BaseDirectory = _projectDirectory;

            try {
                task.Execute();
            } finally {
                _solutionTask.Project.BaseDirectory = oldBaseDir;
            }
        }

        #endregion

        #region Private Instance Fields

        private string _name;
        private string _guid;
        private string _projectDirectory;
        private string _outputDir;
        private Hashtable _htConfigurations;
        private Hashtable _htReferences;
        private Hashtable _htPlatformConfigurations;
        private Hashtable _htFiles;
        private SolutionTask _solutionTask;
        private ArrayList    _objFiles;
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
