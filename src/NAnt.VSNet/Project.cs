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
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Project: ProjectBase {
        #region Public Instance Constructors

        public Project(SolutionTask solutionTask, TempFileCollection tfc, string outputDir) {
            _solutionTask = solutionTask;
            _tfc = tfc;
            _outputDir = outputDir;
            _htConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferences = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htResources = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htAssemblies = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public override string Name {
            get { return _projectSettings.Name; }
        }

        public override string GUID {
            get { return _projectSettings.GUID; }
            set { throw new InvalidOperationException( "It is not allowed to change the GUID of a C#/VB.NET project" ); }
        }

        public override string[] Configurations {
            get { return (string[]) new ArrayList(_htConfigurations.Keys).ToArray(typeof(string)); }
        }

        public override Reference[] References {
            get { return (Reference[]) new ArrayList(_htReferences.Values).ToArray(typeof(Reference)); }
        }

        public Resource[] Resources {
            get { return (Resource[]) new ArrayList(_htResources.Values).ToArray(typeof(Resource)); }
        }

        public ProjectSettings ProjectSettings {
            get { return _projectSettings; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private string LogPrefix {
            get { 
                if (_solutionTask != null) {
                    return _solutionTask.LogPrefix;
                }

                return string.Empty;
            }
        }

        #endregion Private Instance Properties

        #region Public Static Methods

        public static bool IsEnterpriseTemplateProject(string fileName) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);
                return doc.DocumentElement.Name.ToString(CultureInfo.InvariantCulture) == "EFPROJECT";
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error checking whether '{0}' is an enterprise template project.",
                    fileName), Location.UnknownLocation, ex);
            }
        }

        public static string LoadGUID(string fileName, TempFileCollection tfc) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);

                ProjectSettings ps = new ProjectSettings(doc.DocumentElement, (XmlElement) doc.SelectSingleNode("//Build/Settings"), tfc);
                return ps.GUID;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading GUID of project '{0}'.", fileName), 
                    Location.UnknownLocation, ex);
            }
        }

        #endregion Public Static Methods

        #region Public Instance Methods

        public void Load(Solution sln, string fileName) {
            XmlDocument doc = LoadXmlDocument(fileName);

            _projectSettings = new ProjectSettings(doc.DocumentElement, (XmlElement) doc.SelectSingleNode("//Build/Settings"), _tfc);

            _isWebProject = IsURL(fileName);
            _webProjectBaseUrl = String.Empty;
            string webCacheDirectory = String.Empty;

            if (!_isWebProject) {
                _projectDirectory = new FileInfo(fileName).DirectoryName;
            } else {
                string projectDirectory = fileName.Replace(":", "_");
                projectDirectory = projectDirectory.Replace("/", "_");
                projectDirectory = projectDirectory.Replace("\\", "_");
                projectDirectory = Path.Combine(_projectSettings.TemporaryFiles.BasePath, projectDirectory);
                Directory.CreateDirectory(projectDirectory);

                webCacheDirectory = projectDirectory;
                _webProjectBaseUrl = fileName.Substring(0, fileName.LastIndexOf("/"));
                _projectDirectory = Path.GetDirectoryName(sln.FileName);
            }

            _projectSettings.RootDirectory = _projectDirectory;

            XmlNodeList nlConfigurations, nlReferences, nlFiles, nlImports;

            nlConfigurations = doc.SelectNodes("//Config");
            foreach (XmlElement elemConfig in nlConfigurations) {
                ConfigurationSettings cs = new ConfigurationSettings(_projectSettings, elemConfig, _solutionTask, _outputDir);
                _htConfigurations[elemConfig.Attributes["Name"].Value] = cs;
            }

            nlReferences = doc.SelectNodes("//References/Reference");
            foreach (XmlElement elemReference in nlReferences) {
                Reference reference = new Reference(sln, _projectSettings, elemReference, _solutionTask, _outputDir);
                _htReferences[elemReference.Attributes["Name"].Value] = reference;
            }

            if (_projectSettings.Type == ProjectType.VBNet) {
                nlImports = doc.SelectNodes("//Imports/Import");
                foreach (XmlElement elemReference in nlImports) {
                    _imports += elemReference.Attributes["Namespace"].Value.ToString(CultureInfo.InvariantCulture) + ",";
                }
                if (_imports.Length > 0) {
                    _imports = "/Imports:" + _imports;
                }
            }

            nlFiles = doc.SelectNodes("//Files/Include/File");
            foreach (XmlElement elemFile in nlFiles) {
                string buildAction = elemFile.Attributes["BuildAction"].Value;

                if (_isWebProject) {
                    WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                    string outputFile = Path.Combine(webCacheDirectory, elemFile.Attributes["RelPath"].Value);
                    wdc.DownloadFile(outputFile, elemFile.Attributes["RelPath"].Value);

                    FileInfo fi = new FileInfo(outputFile);
                    if (buildAction == "Compile") {
                        _htFiles[fi.FullName] = null;
                    } else if (buildAction == "EmbeddedResource") {
                        Resource r = new Resource(this, fi.FullName, elemFile.Attributes["RelPath"].Value, fi.DirectoryName + @"\" + elemFile.Attributes["DependentUpon"].Value, _solutionTask);
                        _htResources[r.InputFile] = r;
                    }
                } else {
                    string sourceFile;

                    if (!StringUtils.IsNullOrEmpty(elemFile.GetAttribute("Link"))) {
                        sourceFile = elemFile.GetAttribute("Link");
                    } else {
                        sourceFile = elemFile.GetAttribute("RelPath");
                    }

                    if (buildAction == "Compile") {
                        _htFiles[sourceFile] = null;
                    } else if (buildAction == "EmbeddedResource") {
                        string resourceFilename = Path.Combine(_projectSettings.RootDirectory, sourceFile);
                        string dependentOn = (elemFile.Attributes["DependentUpon"] != null) ? Path.Combine(new FileInfo(resourceFilename).DirectoryName, elemFile.Attributes["DependentUpon"].Value) : null;
                        Resource r = new Resource(this, resourceFilename, elemFile.Attributes["RelPath"].Value, dependentOn, _solutionTask);
                        _htResources[r.InputFile] = r;
                    }
                }
            }
        }

        public override bool Compile(string configuration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands) {
            bool bSuccess = true;

            ConfigurationSettings cs = (ConfigurationSettings) _htConfigurations[configuration];
            if (cs == null) {
                Log(Level.Info, LogPrefix + "Configuration {0} does not exist. Skipping.", configuration);
                return true;
            }

            Log(Level.Info, LogPrefix + "Building {0} [{1}]...", Name, configuration);
            Directory.CreateDirectory(cs.OutputPath);

            string tempResponseFile = Path.Combine(_tfc.BasePath, Project.CommandFile);

            using (StreamWriter sw = File.CreateText(tempResponseFile)) {
                if (CheckUpToDate(cs)) {
                    Log(Level.Verbose, LogPrefix + "Project is up-to-date.");
                    return true;
                }

                foreach (string setting in alCSCArguments) {
                    sw.WriteLine(setting);
                }

                foreach (string setting in ProjectSettings.Settings) {
                    sw.WriteLine(setting);
                }

                foreach (string setting in cs.Settings) {
                    sw.WriteLine(setting);
                }

                if (_projectSettings.Type == ProjectType.VBNet) {
                    sw.WriteLine(_imports);
                }

                Log(Level.Verbose, LogPrefix + "Copying references:");
                foreach (Reference reference in _htReferences.Values) {
                    Log(Level.Verbose, LogPrefix + " - " + reference.Name);

                    if (reference.CopyLocal) {
                        if (reference.IsCreated) {
                            string program, commandLine;
                            reference.GetCreationCommand(cs, out program, out commandLine);

                            Log(Level.Verbose, LogPrefix + program + " " + commandLine);
                            ProcessStartInfo psiRef = new ProcessStartInfo(program, commandLine);
                            psiRef.UseShellExecute = false;
                            psiRef.WorkingDirectory = cs.OutputPath;

                            try {
                                Process pRef = Process.Start(psiRef);
                                pRef.WaitForExit();
                            } catch (Win32Exception ex) {
                                throw new BuildException(string.Format("Unable to start process '{0}' with commandline '{1}'.", program, commandLine), ex);
                            }
                        } else {
                            StringCollection fromFilenames = reference.GetReferenceFiles(cs);

                            // create instance of Copy task
                            CopyTask ct = new CopyTask();

                            // inherit project from solution task
                            ct.Project = _solutionTask.Project;

                            // inherit parent from solution task
                            ct.Parent = _solutionTask.Parent;

                            // inherit verbose setting from solution task
                            ct.Verbose = _solutionTask.Verbose;

                            // make sure framework specific information is set
                            ct.InitializeTaskConfiguration();

                            // set task properties
                            foreach (string file in fromFilenames) {
                                ct.CopyFileSet.Includes.Add(file);
                            }
                            ct.CopyFileSet.BaseDirectory = reference.GetBaseDirectory(cs);
                            ct.ToDirectory = cs.OutputPath;

                            _solutionTask.Project.Indent();
                            ct.Execute();
                            _solutionTask.Project.Unindent();
                        }
                    }
                    sw.WriteLine(reference.Setting);
                }

                if (_htResources.Count > 0) {
                    Log(Level.Verbose, LogPrefix + "Compiling resources:");
                    foreach (Resource resource in _htResources.Values) {
                        Log(Level.Verbose, LogPrefix + " - {0}", resource.InputFile);
                        resource.Compile(cs, bShowCommands);
                        sw.WriteLine(resource.Setting);
                    }
                }

                // add the files to compile
                foreach (string file in _htFiles.Keys) {
                    sw.WriteLine(@"""" + file + @"""");
                }
            }

            if (bShowCommands) {
                using (StreamReader sr = new StreamReader(tempResponseFile)) {
                    Console.WriteLine("Commands:");
                    Console.WriteLine(sr.ReadToEnd());
                }
            }

            Log(Level.Verbose, LogPrefix + "Starting compiler...");
            ProcessStartInfo psi = null;
            if (_projectSettings.Type == ProjectType.CSharp) {
                psi = new ProcessStartInfo(Path.Combine(_solutionTask.Project.CurrentFramework.FrameworkDirectory.FullName, "csc.exe"), "@\"" + tempResponseFile + "\"");
            }

            if (_projectSettings.Type == ProjectType.VBNet) {
                psi = new ProcessStartInfo(Path.Combine(_solutionTask.Project.CurrentFramework.FrameworkDirectory.FullName, "vbc.exe"), "@\"" + tempResponseFile + "\"");
            }

            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.WorkingDirectory = _projectDirectory;

            Process p = Process.Start(psi);

            if (strLogFile != null) {
                using (StreamWriter sw = new StreamWriter(strLogFile, true)) {
                    sw.WriteLine("Configuration: {0}", configuration);
                    sw.WriteLine("");
                    while (true) {
                        string logContents = p.StandardOutput.ReadLine();
                        if (logContents == null) {
                            break;
                        }
                        sw.WriteLine(logContents);
                    }
                }
            } else {
                _solutionTask.Project.Indent();
                while (true) {
                    string logContents = p.StandardOutput.ReadLine();
                    if (logContents == null) {
                        break;
                    }
                    Log(Level.Info, "      [compile] " + logContents);
                }
                _solutionTask.Project.Unindent();
            }

            p.WaitForExit();

            int exitCode = p.ExitCode;
            Log(Level.Verbose, LogPrefix + "{0}! (exit code = {1})", (exitCode == 0) ? "Success" : "Failure", exitCode);

            if (exitCode > 0) {
                bSuccess = false;
            } else {
                if (_isWebProject) {
                    Log(Level.Verbose, LogPrefix + "Uploading output files...");
                    WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                    //wdc.DeleteFile( cs.FullOutputFile, cs.RelativeOutputPath.Replace(@"\", "/") + _ps.OutputFile );
                    wdc.UploadFile(cs.FullOutputFile, cs.RelativeOutputPath.Replace(@"\", "/") + _projectSettings.OutputFile);
                }

                // Copy any extra files over
                foreach (string extraOutputFile in cs.ExtraOutputFiles) {
                    FileInfo sourceFile = new FileInfo(extraOutputFile);
                    if (_isWebProject) {
                        WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                        wdc.UploadFile(extraOutputFile, cs.RelativeOutputPath.Replace(@"\", "/") + sourceFile.Name);
                    } else {
                        FileInfo destFile = new FileInfo(Path.Combine(cs.OutputPath, sourceFile.Name));

                        if (destFile.Exists) {
                            // only copy the file if the source file is more 
                            // recent than the destination file
                            if (FileSet.FindMoreRecentLastWriteTime(sourceFile.FullName, destFile.LastWriteTime) == null) {
                                continue; 
                            }

                            // make sure the destination file is writable
                            destFile.Attributes = FileAttributes.Normal;
                        }

                        // copy the file and overwrite the destination file
                        // if it already exists
                        sourceFile.CopyTo(destFile.FullName, true);
                    }
                }
            }

            if (!bSuccess ) {
                Log(Level.Error, LogPrefix + "Build failed.");
            }

            return bSuccess;
        }

        public override string GetOutputFile(string configuration) {
            ConfigurationSettings settings = GetConfigurationSettings(configuration);
            if (settings == null) {
                return null;
            }
            return settings.FullOutputFile;
        }

        public ConfigurationSettings GetConfigurationSettings(string configuration) {
            return (ConfigurationSettings) _htConfigurations[configuration];
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private bool CheckUpToDate(ConfigurationSettings cs) {
            DateTime dtOutputTimeStamp;
            if (File.Exists(cs.FullOutputFile)) {
                dtOutputTimeStamp = File.GetLastWriteTime(cs.FullOutputFile);
            } else {
                return false;
            }

            // Check all of the input files
            foreach (string file in _htFiles.Keys)
                if (dtOutputTimeStamp < File.GetLastWriteTime(Path.Combine(_projectDirectory, file))) {
                    return false;
                }

            // Check all of the input references
            foreach (Reference reference in _htReferences.Values) {
                reference.ConfigurationSettings = cs;
                if (dtOutputTimeStamp < reference.Timestamp) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        private void Log(Level messageLevel, string message) {
            if (_solutionTask != null) {
                _solutionTask.Log(messageLevel, message);
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        private void Log(Level messageLevel, string message, params object[] args) {
            if (_solutionTask != null) {
                _solutionTask.Log(messageLevel, message, args);
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private Hashtable _htConfigurations;
        private Hashtable _htReferences;
        private Hashtable _htFiles;
        private Hashtable _htResources;
        private Hashtable _htAssemblies;
        private string _imports;
        private bool _isWebProject;
        private string _projectDirectory;
        private string _webProjectBaseUrl;
        private ProjectSettings _projectSettings;
        private SolutionTask _solutionTask;
        private TempFileCollection _tfc;
        private string _outputDir;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CommandFile = "compile-commands.txt";

        #endregion Private Static Fields
    }
}
