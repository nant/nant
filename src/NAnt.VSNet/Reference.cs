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
            _name = (string) elemReference.Attributes["Name"].Value;
            _isCreated = false;

            if (elemReference.Attributes["Project"] != null) {
                if (solution == null) {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "External reference '{0}' found, but no solution was specified.",
                        _name));
                }

                Project project = new Project(_solutionTask, ps.TemporaryFiles, outputDir);
                string projectFile = solution.GetProjectFileFromGUID(elemReference.GetAttribute("Project"));
                if (projectFile == null) {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, 
                        "External reference '{0}' found, but project was not loaded.",
                        _name));
                }

                project.Load(solution, projectFile);

                // we don't know what the timestamp of the project is going to be, 
                // because we don't know what configuration we will be building
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
                _referenceFile = elemReference.Attributes["AssemblyName"].Value + ".dll";

                DirectoryInfo diGAC = new DirectoryInfo(_solutionTask.Project.CurrentFramework.FrameworkDirectory.FullName);
                string gacFile = Path.Combine(diGAC.FullName, _referenceFile);
                if (File.Exists(gacFile)) {
                    // This file is in the GAC
                    _baseDirectory = diGAC.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _referenceFile = gacFile;
                    _isSystem = true;
                } else {
                    FileInfo fiRef = new FileInfo(Path.Combine(ps.RootDirectory, elemReference.Attributes["HintPath"].Value));
                    // We may be loading a project whose references are not compiled yet
                    //if ( !fiRef.Exists )
                    //    throw new Exception( "Couldn't find referenced assembly: " + _strReferenceFile );

                    _baseDirectory = fiRef.DirectoryName;
                    _referenceFile = fiRef.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : true;
                }

                _referenceTimeStamp = GetTimestamp(_referenceFile);
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
            get { return string.Format(CultureInfo.InvariantCulture, @"/r:""{0}""", _referenceFile); }
        }

        public string Filename {
            get { return _referenceFile; }
            set { 
                _referenceFile = value; 
                _baseDirectory = new FileInfo(_referenceFile).DirectoryName;
                _referenceTimeStamp = GetTimestamp(_referenceFile);
            }
        }

        public ConfigurationSettings ConfigurationSettings {
            get { return _configurationSettings; }
            set { _configurationSettings = value; }
        }

        public string Name {
            get { return _name; }
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
            _referenceFile = new FileInfo(Path.Combine(cs.OutputPath, _interopFile)).FullName;

            commandLine = @"""" + _typelibFile + @""" /silent /out:""" + _referenceFile + @"""";
            if (_importTool == "tlbimp") {
                commandLine += " /namespace:" + _namespace;
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
                _referenceFile = Project.GetConfigurationSettings(configurationSettings.Name).FullOutputFile; 
            }

            FileInfo fi = new FileInfo(_referenceFile);
            if (!fi.Exists) {
                if (Project == null) {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced assembly '{0}'.", _referenceFile));
                } else {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced project '{0}' output file, '{1}'.",
                        Project.Name, _referenceFile));
                }
            }

            // Get a list of the references in the output directory
            foreach (string referenceFile in Directory.GetFiles(fi.DirectoryName, "*.dll")) {
                // Now for each reference, get the related files (.xml, .pdf, etc...)
                string relatedFiles = Path.GetFileName(Path.ChangeExtension(referenceFile, ".*"));

                foreach (string relatedFile in Directory.GetFiles(fi.DirectoryName, relatedFiles)) {
                    // Ignore any other the garbage files created
                    string fileExtension = Path.GetExtension(relatedFile).ToLower(CultureInfo.InvariantCulture);
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

            // look for a primary interop assembly
            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbVersionKey)) {
                if (registryKey.GetValue("PrimaryInteropAssemblyName") != null) {
                    _referenceFile = (string) registryKey.GetValue("PrimaryInteropAssemblyName");
                    Assembly asmRef = Assembly.Load(_referenceFile);
                    _referenceFile = new Uri(asmRef.CodeBase).LocalPath;
                    _baseDirectory = Path.GetDirectoryName(_referenceFile);
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _referenceTimeStamp = GetTimestamp(_referenceFile);

                    return;
                }
            }

            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbRegistryKey)) {
                if (registryKey == null)
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find reference to type library '{0}' ({1}).", 
                        elemReference.Attributes["Name"].Value, tlbRegistryKey));

                // check if the typelib actually exists
                _typelibFile = (string) registryKey.GetValue(null);
                if (!File.Exists(_typelibFile)) {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find referenced type library '{0}'.", _typelibFile));
                }

                _referenceTimeStamp = GetTimestamp(_typelibFile);
                _interopFile = "Interop." + elemReference.Attributes["Name"].Value + ".dll";
                _referenceFile = _interopFile;
                _namespace = elemReference.Attributes["Name"].Value;
                _copyLocal = true;
                _isCreated = true;
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _name;
        private string _referenceFile;
        private string _baseDirectory;
        private string _namespace;
        private string _interopFile;
        private string _typelibFile;
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
