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

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Reference {
        #region Public Instance Constructors

        public Reference(Solution solution, ProjectSettings ps, XmlElement elemReference, SolutionTask solutionTask, string outputDir) {
            _projectSettings = ps;
            _solutionTask = solutionTask;
            _referenceTimeStamp = DateTime.MinValue;
            _isSystem = false;

            _isCreated = false;
            DirectoryInfo diGAC = new DirectoryInfo(_solutionTask.Project.CurrentFramework.FrameworkDirectory.FullName);
            _strName = (string) elemReference.Attributes["Name"].Value;

            if (elemReference.Attributes["Project"] != null) {
                if (solution == null) {
                    throw new Exception("External reference found, but no solution specified: " + _strName);
                }

                Project project = new Project(_solutionTask, ps.TemporaryFiles, outputDir);
                string projectFile = solution.GetProjectFileFromGUID(elemReference.GetAttribute("Project"));
                if (projectFile == null) {
                    throw new Exception("External reference found, but project was not loaded: " + _strName);
                }

                project.Load(solution, projectFile);
                // We don't know what the timestamp of the project is going to be, because we don't know what configuration
                // we will be building
                _referenceTimeStamp = DateTime.MinValue;

                _project = project;
                _copyLocal = _privateSpecified ? _isPrivate : true;
                return;
            }

            if (elemReference.Attributes["WrapperTool"] != null) {
                _importTool = elemReference.Attributes["WrapperTool"].Value;
            }
                
            // Read the private flag
            _privateSpecified = (elemReference.Attributes["Private"] != null);
            if (_privateSpecified) {
                _isPrivate = (elemReference.Attributes["Private"].Value == "True");
            } else {
                _isPrivate = false;
            }

            if (_importTool == "tlbimp" || _importTool == "primary" || _importTool == "aximp") {
                HandleWrapperImport(elemReference);
            } else {
                _strReferenceFile = elemReference.Attributes[ "AssemblyName" ].Value + ".dll";
                
                string gacFile = Path.Combine(diGAC.FullName, _strReferenceFile);
                if (File.Exists(gacFile)) {
                    // This file is in the GAC
                    _baseDirectory = diGAC.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _strReferenceFile = gacFile;
                    _isSystem = true;
                } else {
                    FileInfo fiRef = new FileInfo(Path.Combine(ps.RootDirectory, elemReference.Attributes["HintPath"].Value));
                    // We may be loading a project whose references are not compiled yet
                    //if ( !fiRef.Exists )
                    //    throw new Exception( "Couldn't find referenced assembly: " + _strReferenceFile );

                    _baseDirectory = fiRef.DirectoryName;
                    _strReferenceFile = fiRef.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : true;
                }

                _referenceTimeStamp = GetTimestamp(_strReferenceFile);
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public bool CopyLocal {
            get { return _copyLocal; }
        }

        public bool IsCreated {
            get { return _isCreated; }
        }

        public string Setting {
            get { return String.Format(@"/r:""{0}""", _strReferenceFile); }
        }

        public string Filename {
            get { return _strReferenceFile; }
            set { 
                _strReferenceFile = value; 
                _baseDirectory = new FileInfo(_strReferenceFile).DirectoryName;
                _referenceTimeStamp = GetTimestamp(_strReferenceFile);
            }
        }

        public ConfigurationSettings ConfigurationSettings {
            get { return _configurationSettings; }
            set { _configurationSettings = value; }
        }

        public string Name {
            get { return _strName; }
        }

        public bool IsSystem {
            get { return _isSystem; }
        }
        
        public Project Project {
            get { return _project; }
        }

        public bool IsProjectReference {
            get { return _project != null; }
        }
        
        public DateTime Timestamp {
            get { 
                if (Project != null) {
                    return GetTimestamp(Project.GetConfigurationSettings(ConfigurationSettings.Name).FullOutputFile);
                }

                return _referenceTimeStamp; 
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void GetCreationCommand(ConfigurationSettings cs, out string program, out string commandLine) {
            _strReferenceFile = new FileInfo(Path.Combine(cs.OutputPath, _strInteropFile)).FullName;

            commandLine = @"""" + _strTypeLib + @""" /silent /out:""" + _strReferenceFile + @"""";
            if (_importTool == "tlbimp") {
                commandLine += " /namespace:" + _strNamespace;
            }
            program = _importTool + ".exe";
        }

        public string GetBaseDirectory(ConfigurationSettings configurationSettings) {
            if (Project != null) {
                return Project.GetConfigurationSettings(configurationSettings.Name).OutputPath;
            }

            return _baseDirectory;
        }

        public StringCollection GetReferenceFiles(ConfigurationSettings configurationSettings) {
            StringCollection referencedFiles = new StringCollection();

            if (Project != null) {
                _strReferenceFile = Project.GetConfigurationSettings(configurationSettings.Name).FullOutputFile; 
            }

            FileInfo fi = new FileInfo(_strReferenceFile);
            if (!fi.Exists) {
                if (Project == null) {
                    throw new Exception("Couldn't find referenced assembly: " + _strReferenceFile);
                } else {
                    throw new Exception("Couldn't find referenced project's output: " + _strReferenceFile);
                }
            }

            // Get a list of the references in the output directory
            foreach (string referenceFile in Directory.GetFiles(fi.DirectoryName, "*.dll")) {
                // Now for each reference, get the related files (.xml, .pdf, etc...)
                string relatedFiles = Path.GetFileName(Path.ChangeExtension(referenceFile, ".*"));

                foreach (string relatedFile in Directory.GetFiles(fi.DirectoryName, relatedFiles)) {
                    // Ignore any other the garbage files created
                    string fileExtension = Path.GetExtension(relatedFile).ToLower();
                    if (fileExtension != ".dll" && fileExtension != ".xml" && fileExtension != ".pdb") {
                        continue;
                    }

                    referencedFiles.Add(new FileInfo(relatedFile).Name);
                }
            }

            return referencedFiles;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private DateTime GetTimestamp(string fileName) {
            if (!File.Exists(fileName)) {
                return DateTime.MaxValue;
            }

            return File.GetLastWriteTime(fileName);
        }

        private void HandleWrapperImport(XmlElement elemReference) {
            string tlbVersionKey = string.Format(@"TYPELIB\{0}\{1}.{2}", 
                elemReference.Attributes["Guid"].Value,
                elemReference.Attributes["VersionMajor"].Value,
                elemReference.Attributes["VersionMinor"].Value
                );

            string tlbRegistryKey = string.Format(@"TYPELIB\{0}\{1}.{2}\{3}\win32", 
                elemReference.Attributes["Guid"].Value,
                elemReference.Attributes["VersionMajor"].Value,
                elemReference.Attributes["VersionMinor"].Value,
                elemReference.Attributes["Lcid"].Value
                );

            // First, look for a primary interop assembly
            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbVersionKey)) {
                if (registryKey.GetValue("PrimaryInteropAssemblyName") != null) {
                    _strReferenceFile = (string) registryKey.GetValue("PrimaryInteropAssemblyName");
                    // Assembly.Load does its own checking
                    //if ( !File.Exists( _strReferenceFile ) )
                    //    throw new Exception( "Couldn't find referenced primary interop assembly: " + _strReferenceFile );
                    Assembly asmRef = Assembly.Load(_strReferenceFile);
                    _strReferenceFile = new Uri(asmRef.CodeBase).LocalPath;
                    _baseDirectory = Path.GetDirectoryName(_strReferenceFile);
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _referenceTimeStamp = GetTimestamp(_strReferenceFile);

                    return;
                }
            }

            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbRegistryKey)) {
                if (registryKey == null)
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find reference to type library {0} ({1}).", 
                        elemReference.Attributes["Name"].Value, tlbRegistryKey));

                _strTypeLib = (string) registryKey.GetValue(null);
                if (!File.Exists(_strTypeLib)) {
                    throw new Exception("Couldn't find referenced type library: " + _strTypeLib);
                }

                _referenceTimeStamp = GetTimestamp(_strTypeLib);
                _strInteropFile = "Interop." + elemReference.Attributes["Name"].Value + ".dll";
                _strReferenceFile = _strInteropFile;
                _strNamespace = elemReference.Attributes["Name"].Value;
                _copyLocal = true;
                _isCreated = true;
            }

        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _strName;
        private string _strReferenceFile;
        private string _strInteropFile;
        private string _strTypeLib;
        private string _strNamespace;
        private string _baseDirectory;
        private bool _copyLocal;
        private bool _isCreated;
        private bool _isSystem;
        private string _importTool;
        private DateTime _referenceTimeStamp;    
        private bool _privateSpecified;
        private bool _isPrivate;
        private ProjectSettings _projectSettings;
        private ConfigurationSettings _configurationSettings;
        private Project _project;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
