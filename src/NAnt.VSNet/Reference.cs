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
// Matthew Mastracci (matt@aclaro.com)
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
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

                string projectFile = solution.GetProjectFileFromGuid(elemReference.GetAttribute("Project"));

                TempFileCollection temporaryFiles = solution.TemporaryFiles;
                if (ps != null) {
                    temporaryFiles = ps.TemporaryFiles;
                }

                ProjectBase project = ProjectFactory.LoadProject(solution, _solutionTask, temporaryFiles, outputDir, projectFile);

                // we don't know what the timestamp of the project is going to be, 
                // because we don't know what configuration we will be building
                _referenceTimeStamp = DateTime.MinValue;

                _project = project;
                _copyLocal = _privateSpecified ? _isPrivate : true;
                return;
            }

            // TO-DO : check with Scott Ford (sford at RJKTECH.com) if this is
            // really necessary
            /*
            if (elemReference.Attributes["ReferencedProjectIdentifier"] != null) {
                if (solution == null) {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "External reference '{0}' found, but no solution was specified.",
                        _name));
                }

                string projectFile = solution.GetProjectFileFromGuid(elemReference.GetAttribute("ReferencedProjectIdentifier"));

              

                TempFileCollection temporaryFiles = solution.TemporaryFiles;
                if (ps != null) {
                    temporaryFiles = ps.TemporaryFiles;
                }

                ProjectBase project = ProjectFactory.LoadProject(solution, _solutionTask, temporaryFiles, outputDir, projectFile);

                if (project is Project) {
                    ((Project) project).Load(solution, projectFile);
                } else if (project is VcProject) {
                    ((VcProject) project).Load(solution, projectFile);
                }



              // we don't know what the timestamp of the project is going to be, 
              // because we don't know what configuration we will be building
              _referenceTimeStamp = DateTime.MinValue;

              _project = project;
              _copyLocal = _privateSpecified ? _isPrivate : true;
              return;
            }
            */

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

                // TO-DO : implement the same search MS uses for VS.NET, which is :
                // 1.)  The project directory.
                // 2.)  The directories specified in the "ReferencePath" property, which is 
                //      stored in the .USER file. (NOT SURE WE SHOULD DO THIS ONE)
                // 3.)  The .NET Framework directory.
                // 4.)  The directories specified under the following registry keys:
                //          HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders
                //          HKEY_CURRENT_USER\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders
                //          HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\7.1\AssemblyFolders
                //          HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\7.1\AssemblyFolders
                //
                //      Future versions of VS.NET will also check in :
                //          HKCU\SOFTWARE\Microsoft\.NETFramework\AssemblyFoldersEx
                //          HKLM\SOFTWARE\Microsoft\.NETFramework\AssemblyFoldersEx
                // 5.)  The HintPath

                DirectoryInfo frameworkDirectory = new DirectoryInfo(_solutionTask.Project.CurrentFramework.FrameworkDirectory.FullName);
                string systemAssembly = Path.Combine(frameworkDirectory.FullName, _referenceFile);
                if (File.Exists(systemAssembly)) {
                    // this file is a system assembly
                    _baseDirectory = frameworkDirectory.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _referenceFile = systemAssembly;
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
        
        public ProjectBase Project {
            get { return _project; }
        }

        public bool IsProjectReference {
            get { return _project != null; }
        }
        
        public DateTime Timestamp {
            get { 
                if (Project != null) {
                    return GetTimestamp(Project.GetOutputPath(ConfigurationSettings.Name));
                }

                return _referenceTimeStamp; 
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void GetCreationCommand(ConfigurationSettings cs, out string program, out string commandLine) {
            _referenceFile = new FileInfo(Path.Combine(cs.OutputDir, _interopFile)).FullName;

            commandLine = @"""" + _typelibFile + @""" /silent /out:""" + _referenceFile + @"""";
            if (_importTool == "tlbimp") {
                commandLine += " /namespace:" + _namespace;
            }
            program = _importTool + ".exe";
        }

        public string GetBaseDirectory(ConfigurationSettings configurationSettings) {
            if (Project != null) {
                return Project.GetConfiguration(configurationSettings.Name).OutputDir;
            }
            return _baseDirectory;
        }

        public StringCollection GetReferenceFiles(ConfigurationSettings configurationSettings) {
            StringCollection referencedFiles = new StringCollection();

            if (Project != null) {
                _referenceFile = Project.GetConfiguration(
                    configurationSettings.Name).OutputPath; 
            }

            FileInfo fi = new FileInfo(_referenceFile);
            if (!fi.Exists) {
                if (Project == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced assembly '{0}'.", _referenceFile), Location.UnknownLocation);
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced project '{0}' output file, '{1}'.",
                        Project.Name, _referenceFile), Location.UnknownLocation);
                }
            }

            // get a list of the references in the output directory
            foreach (string referenceFile in Directory.GetFiles(fi.DirectoryName, "*.dll")) {
                // now for each reference, get the related files (.xml, .pdf, etc...)
                string relatedFiles = Path.GetFileName(Path.ChangeExtension(referenceFile, ".*"));

                foreach (string relatedFile in Directory.GetFiles(fi.DirectoryName, relatedFiles)) {
                    // ignore any other the garbage files created
                    string fileExtension = Path.GetExtension(relatedFile).ToLower(CultureInfo.InvariantCulture);
                    if (fileExtension != ".dll" && fileExtension != ".xml" && fileExtension != ".pdb") {
                        continue;
                    }

                    referencedFiles.Add(new FileInfo(relatedFile).Name);
                }
            }

            return referencedFiles;
        }


        /// <summary>
        /// Searches for the reference file.
        /// </summary>
        public void ResolveFolder() {
            if (IsSystem || IsProjectReference) {
                //do not resolve system assemblies or project references
                return;
            }

            FileInfo fiRef = new FileInfo(_referenceFile);
            if (fiRef.Exists) {
                //referenced file found - no other tasks required
                return;
            }

            if (ResolveFolderFromList(_solutionTask.AssemblyFolders.DirectoryNames, fiRef.Name)) {
                return;
            }

            ResolveFolderFromList(_solutionTask.DefaultAssemlyFolders.DirectoryNames, fiRef.Name);
        }


        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Searches for the given file in all paths in <paramref name="folderList" />.
        /// </summary>
        /// <param name="folderList">The folders to search.</param>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="fileName" /> was found
        /// in <paramref name="folderList" />.
        /// </returns>
        private bool ResolveFolderFromList(StringCollection folderList, string fileName) {
            foreach (string path in folderList) {
                FileInfo fiRef = new FileInfo(Path.Combine(path, fileName));

                if (fiRef.Exists) {
                    _referenceFile = fiRef.FullName;
                    _baseDirectory = fiRef.DirectoryName;
                    return true;
                }
            }

            return false;
        }

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
                if (registryKey != null && registryKey.GetValue("PrimaryInteropAssemblyName") != null) {
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
        private ProjectBase _project;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
