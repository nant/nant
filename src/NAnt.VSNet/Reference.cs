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
using System.Collections;
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

        public Reference(Solution solution, ProjectSettings ps, XmlElement elemReference, SolutionTask solutionTask, DirectoryInfo outputDir) {
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

            // used by Visual C++ projects
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

                DirectoryInfo frameworkAssemblyDirectory = new DirectoryInfo(_solutionTask.Project.TargetFramework.FrameworkAssemblyDirectory.FullName);
                string systemAssembly = Path.Combine(frameworkAssemblyDirectory.FullName, _referenceFile);
                if (File.Exists(systemAssembly)) {
                    // this file is a system assembly
                    _baseDirectory = frameworkAssemblyDirectory;
                    _copyLocal = _privateSpecified ? _isPrivate : false;
                    _referenceFile = systemAssembly;
                    _isSystem = true;
                } else {
                    FileInfo fiRef = new FileInfo(Path.Combine(ps.RootDirectory, elemReference.Attributes["HintPath"].Value));
                    _baseDirectory = fiRef.Directory;
                    _referenceFile = fiRef.FullName;
                    _copyLocal = _privateSpecified ? _isPrivate : true;
                }

                _referenceTimeStamp = GetTimestamp(_referenceFile);
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets a value indicating whether the output file(s) of this reference 
        /// should be copied locally.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output file(s) of this reference 
        /// should be copied locally; otherwise, <see langword="false" />.
        /// </value>
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
                _baseDirectory = new FileInfo(_referenceFile).Directory;
                _referenceTimeStamp = GetTimestamp(_referenceFile);
            }
        }

        public ConfigurationSettings ConfigurationSettings {
            get { return _configurationSettings; }
            set { _configurationSettings = value; }
        }

        /// <summary>
        /// Gets the name of the reference.
        /// </summary>
        /// <value>
        /// The name of the reference.
        /// </value>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets a value indicating whether this reference represents a system 
        /// assembly.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this reference represents a system 
        /// assembly; otherwise, <see langword="false" />.
        /// </value>
        public bool IsSystem {
            get { return _isSystem; }
        }
        
        public ProjectBase Project {
            get { return _project; }
        }

        /// <summary>
        /// Gets a value indicating whether this is a reference to another
        /// project.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this is a reference to another project;
        /// otherwise, <see langword="false" />.
        /// </value>
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
            _referenceFile = new FileInfo(Path.Combine(cs.OutputDir.FullName, 
                _interopFile)).FullName;

            commandLine = @"""" + _typelibFile + @""" /silent /out:""" + _referenceFile + @"""";
            if (_importTool == "aximp" && _runtimeCallableWrapper != null) {
                string rcwFile = Path.Combine(cs.OutputDir.FullName, _runtimeCallableWrapper);
                if (File.Exists(rcwFile)) {
                    commandLine += " /rcw:\"" + rcwFile + "\"";
                }
            }
            if (_importTool == "tlbimp") {
                commandLine += " /namespace:" + _namespace;
            }

            if (_importTool == "tlbimp" || _importTool == "aximp") {
                // check if wrapper assembly should be strongly signed
                if (_projectSettings.AssemblyOriginatorKeyFile != null) {
                    commandLine += @" /keyfile:""" + Path.Combine(_projectSettings.RootDirectory, _projectSettings.AssemblyOriginatorKeyFile) + @"""";
                }
            }
            program = _importTool + ".exe";
        }

        public DirectoryInfo GetBaseDirectory(ConfigurationSettings configurationSettings) {
            if (Project != null) {
                return Project.GetConfiguration(configurationSettings.Name).OutputDir;
            }
            return _baseDirectory;
        }

        public StringCollection GetReferenceFiles(ConfigurationSettings configurationSettings) {
            StringCollection referencedFiles = new StringCollection();

            // check if we're dealing with a project reference
            if (IsProjectReference) {
                // get output file of project
                _referenceFile = Project.GetConfiguration(
                    configurationSettings.Name).OutputPath; 
            }

            FileInfo fi = new FileInfo(_referenceFile);
            if (!fi.Exists) {
                if (!IsProjectReference) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced assembly '{0}'.", _referenceFile), 
                        Location.UnknownLocation);
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find referenced project '{0}' output file, '{1}'.",
                        Project.Name, _referenceFile), Location.UnknownLocation);
                }
            }

            // get a list of the references in the output directory
            foreach (string referenceFile in GetAllReferencedModules(_referenceFile)) {
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
                // do not resolve system assemblies or project references
                return;
            }

            FileInfo fiRef = new FileInfo(_referenceFile);
            if (fiRef.Exists) {
                // referenced file found - no other tasks required
                return;
            }

            if (ResolveFolderFromList(_solutionTask.AssemblyFolders.DirectoryNames, fiRef.Name)) {
                return;
            }

            ResolveFolderFromList(_solutionTask.DefaultAssemblyFolders.DirectoryNames, fiRef.Name);
        }


        #endregion Public Instance Methods

        #region Private Instance Methods

        private string[] GetAllReferencedModules(string module) {
            string fullPathToModule = Path.GetFullPath(module);
            string moduleDirectory = Path.GetDirectoryName(fullPathToModule);
            AppDomain temporaryDomain = AppDomain.CreateDomain("temporaryDomain");

            Hashtable allReferences = new Hashtable();
            Hashtable unresolvedReferences = new Hashtable();

            try {
                ReferencesResolver referencesResolver =
                    ((ReferencesResolver) temporaryDomain.CreateInstanceFrom(Assembly.GetExecutingAssembly().Location,
                        typeof(ReferencesResolver).FullName).Unwrap());

                allReferences.Add(fullPathToModule, null);
                unresolvedReferences.Add(fullPathToModule, null);

                while (unresolvedReferences.Count > 0) {
                    IDictionaryEnumerator unresolvedEnumerator = unresolvedReferences.GetEnumerator();
                    unresolvedEnumerator.MoveNext();

                    string referenceToResolve = (string) unresolvedEnumerator.Key;

                    unresolvedReferences.Remove(referenceToResolve);

                    referencesResolver.AppendReferencedModulesLocatedInGivenDirectory(moduleDirectory,
                        referenceToResolve, ref allReferences, ref unresolvedReferences);

                }
            } finally {
                AppDomain.Unload(temporaryDomain);
            }

            string[] result = new string[allReferences.Keys.Count];
            allReferences.Keys.CopyTo(result, 0);
            return result;
        }

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
                    _baseDirectory = fiRef.Directory;
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
            string majorVersion = (int.Parse(elemReference.Attributes["VersionMajor"].Value, 
                CultureInfo.InvariantCulture)).ToString("x", CultureInfo.InvariantCulture);
            string minorVersion = (int.Parse(elemReference.Attributes["VersionMinor"].Value, 
                CultureInfo.InvariantCulture)).ToString("x", CultureInfo.InvariantCulture);
            string referenceName = elemReference.Attributes["Name"].Value;

            string tlbVersionKey = string.Format(@"TYPELIB\{0}\{1}.{2}", 
                elemReference.Attributes["Guid"].Value, majorVersion, minorVersion);

            string tlbRegistryKey = string.Format(@"TYPELIB\{0}\{1}.{2}\{3}\win32", 
                elemReference.Attributes["Guid"].Value, majorVersion, minorVersion,
                elemReference.Attributes["Lcid"].Value);

            // look for a primary interop assembly
            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbVersionKey)) {
                if (registryKey != null && registryKey.GetValue("PrimaryInteropAssemblyName") != null) {
                    string primaryInteropAssemblyName = (string) registryKey.GetValue("PrimaryInteropAssemblyName");
                    try {
                        // TO-DO : assembly should be loaded into separate appdomain
                        Assembly asmRef = Assembly.Load(primaryInteropAssemblyName);
                        _referenceFile = new Uri(asmRef.CodeBase).LocalPath;
                        _baseDirectory = new DirectoryInfo(Path.GetDirectoryName(_referenceFile));
                        _copyLocal = _privateSpecified ? _isPrivate : false;
                        _referenceTimeStamp = GetTimestamp(_referenceFile);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Primary Interop Assembly '{0}' could not be loaded.", 
                            primaryInteropAssemblyName), Location.UnknownLocation, 
                            ex);
                    }

                    // if the import tool is aximp, we'll use the primary interop 
                    // assembly as runtime callable wrapper
                    if (_importTool == "aximp") {
                        _runtimeCallableWrapper = _referenceFile;
                    } else {
                        return;
                    }
                }

                if (_importTool == "primary") {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find primary interop assembly '{0}' ({1}).", 
                        referenceName, tlbRegistryKey), Location.UnknownLocation);
                }
            }

            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(tlbRegistryKey)) {
                if (registryKey == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find reference to type library '{0}' ({1}).", 
                        referenceName, tlbRegistryKey), Location.UnknownLocation);
                }

                // check if the typelib actually exists
                _typelibFile = (string) registryKey.GetValue(null);
                if (!File.Exists(_typelibFile)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Couldn't find referenced type library '{0}'.", _typelibFile),
                        Location.UnknownLocation);
                }

                _referenceTimeStamp = GetTimestamp(_typelibFile);

                if (_importTool == "aximp") {
                    if (_runtimeCallableWrapper == null) {
                        // if no primary interop assembly is provided for ActiveX control,
                        // trust the fact that VS.NET uses Interop.<name of the tlbimp reference>.dll
                        // for the imported typelibrary
                        _runtimeCallableWrapper = "Interop." + referenceName.Substring(2, referenceName.Length - 2) + ".dll";
                    }
                    // eg. AxVisOcx will become AxInterop.VisOcx.dll 
                    _interopFile = "AxInterop." + referenceName.Substring(2, referenceName.Length - 2) + ".dll";
                } else {
                    _interopFile = "Interop." + referenceName + ".dll";
                }
                _referenceFile = _interopFile;
                _namespace = referenceName;
                _copyLocal = true;
                _isCreated = true;
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _name;
        private string _referenceFile;
        private string _runtimeCallableWrapper;
        private DirectoryInfo _baseDirectory;
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

    // Placed here because you cannot instantiate type nested in another type by using AppDomain methods
    public class ReferencesResolver : MarshalByRefObject {
        public void AppendReferencedModulesLocatedInGivenDirectory(string moduleDirectory, string moduleName, ref Hashtable allReferences, ref Hashtable unresolvedReferences) {
            Assembly module = Assembly.LoadFrom(moduleName);
            AssemblyName[] referencedAssemblies = module.GetReferencedAssemblies();

            foreach (AssemblyName referencedAssemblyName in referencedAssemblies) {
                string fullPathToReferencedAssembly = Path.Combine(moduleDirectory, referencedAssemblyName.Name + ".dll");

                // we only add referenced assemblies which are located in given directory
                if (File.Exists(fullPathToReferencedAssembly) && !allReferences.ContainsKey(fullPathToReferencedAssembly)) {
                    allReferences.Add(fullPathToReferencedAssembly, null);
                    unresolvedReferences.Add(fullPathToReferencedAssembly, null);
                }
            }
        }
    }
}
