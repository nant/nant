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
    public class Project : ProjectBase {
        #region Public Instance Constructors

        public Project(SolutionTask solutionTask, TempFileCollection tfc, string outputDir) : base(solutionTask, tfc, outputDir) {
            _htConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferences = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htResources = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htAssemblies = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the VS.NET project.
        /// </summary>
        public override string Name {
            get { 
                string projectPath;

                if (ProjectFactory.IsUrl(_projectPath)) {
                    // construct uri for project path
                    Uri projectUri = new Uri(_projectPath);

                    // get last segment of the uri (which should be the 
                    // project file itself)
                    projectPath = projectUri.LocalPath;
                } else {
                    projectPath = ProjectPath;
                }

                // return file part without extension
                return Path.GetFileNameWithoutExtension(projectPath); 
            }
        }

        /// <summary>
        /// Gets the path of the VS.NET project.
        /// </summary>
        public override string ProjectPath {
            get { 
                if (ProjectFactory.IsUrl(_projectPath)) {
                    return _projectPath;
                } else {
                    return Path.GetFullPath(_projectPath);
                }
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the VS.NET project.
        /// </summary>
        public override string Guid {
            get { return _projectSettings.Guid; }
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
                if (SolutionTask != null) {
                    return SolutionTask.LogPrefix;
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

        public static string LoadGuid(string fileName, TempFileCollection tfc) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);

                ProjectSettings ps = new ProjectSettings(doc.DocumentElement, (XmlElement) doc.SelectSingleNode("//Build/Settings"), tfc);
                return ps.Guid;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading GUID of project '{0}'.", fileName), 
                    Location.UnknownLocation, ex);
            }
        }

        #endregion Public Static Methods

        #region Public Instance Methods

        public override void Load(Solution sln, string projectPath) {
            XmlDocument doc = LoadXmlDocument(projectPath);

            _projectSettings = new ProjectSettings(doc.DocumentElement, (XmlElement) doc.SelectSingleNode("//Build/Settings"), TempFiles);
            _projectPath = projectPath;

            _isWebProject = ProjectFactory.IsUrl(projectPath);
            _webProjectBaseUrl = String.Empty;
            string webCacheDirectory = String.Empty;

            if (!_isWebProject) {
                _projectDirectory = new FileInfo(projectPath).DirectoryName;
            } else {
                string projectDirectory = projectPath.Replace(":", "_");
                projectDirectory = projectDirectory.Replace("/", "_");
                projectDirectory = projectDirectory.Replace("\\", "_");
                projectDirectory = Path.Combine(_projectSettings.TemporaryFiles.BasePath, projectDirectory);
                Directory.CreateDirectory(projectDirectory);

                webCacheDirectory = projectDirectory;
                _webProjectBaseUrl = projectPath.Substring(0, projectPath.LastIndexOf("/"));
                _projectDirectory = Path.GetDirectoryName(sln.FileName);
            }

            _projectSettings.RootDirectory = _projectDirectory;

            XmlNodeList nlConfigurations, nlReferences, nlFiles, nlImports;

            nlConfigurations = doc.SelectNodes("//Config");
            foreach (XmlElement elemConfig in nlConfigurations) {
                ConfigurationSettings cs = new ConfigurationSettings(this, elemConfig, SolutionTask, OutputDir);
                _htConfigurations[elemConfig.Attributes["Name"].Value] = cs;
            }

            nlReferences = doc.SelectNodes("//References/Reference");
            foreach (XmlElement elemReference in nlReferences) {
                Reference reference = new Reference(sln, _projectSettings, elemReference, SolutionTask, OutputDir);
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
                        Resource r = new Resource(this, fi.FullName, elemFile.Attributes["RelPath"].Value, fi.DirectoryName + @"\" + elemFile.Attributes["DependentUpon"].Value, SolutionTask);
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
                        Resource r = new Resource(this, resourceFilename, elemFile.Attributes["RelPath"].Value, dependentOn, SolutionTask);
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
            Directory.CreateDirectory(cs.OutputDir);

            // perform prebuild actions (for VS.NET 2003)
            if (ProjectSettings.RunPostBuildEvent != null) {
                bSuccess = PreBuild(cs);
            }

            // ensure the temp dir exists
            Directory.CreateDirectory(TempFiles.BasePath);

            string tempResponseFile = Path.Combine(TempFiles.BasePath, Project.CommandFile);

            using (StreamWriter sw = File.CreateText(tempResponseFile)) {
                if (CheckUpToDate(cs)) {
                    Log(Level.Verbose, LogPrefix + "Project is up-to-date.");
                    if (ProjectSettings.RunPostBuildEvent != null) {
                        PostBuild(cs, true, false);
                    }
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
                            psiRef.WorkingDirectory = cs.OutputDir;

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
                            ct.Project = SolutionTask.Project;

                            // inherit parent from solution task
                            ct.Parent = SolutionTask.Parent;

                            // inherit verbose setting from solution task
                            ct.Verbose = SolutionTask.Verbose;

                            // make sure framework specific information is set
                            ct.InitializeTaskConfiguration();

                            // set parent of child elements
                            ct.CopyFileSet.Parent = ct;

                            // inherit project from solution task for child elements
                            ct.CopyFileSet.Project = SolutionTask.Project;

                            // set task properties
                            foreach (string file in fromFilenames) {
                                ct.CopyFileSet.Includes.Add(file);
                            }
                            ct.CopyFileSet.BaseDirectory = reference.GetBaseDirectory(cs);
                            ct.ToDirectory = new DirectoryInfo(cs.OutputDir);

                            ct.Project.Indent();
                            ct.Execute();
                            ct.Project.Unindent();
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
                psi = new ProcessStartInfo(Path.Combine(SolutionTask.Project.CurrentFramework.FrameworkDirectory.FullName, "csc.exe"), "@\"" + tempResponseFile + "\"");
            }

            if (_projectSettings.Type == ProjectType.VBNet) {
                psi = new ProcessStartInfo(Path.Combine(SolutionTask.Project.CurrentFramework.FrameworkDirectory.FullName, "vbc.exe"), "@\"" + tempResponseFile + "\"");
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
                SolutionTask.Project.Indent();
                while (true) {
                    string logContents = p.StandardOutput.ReadLine();
                    if (logContents == null) {
                        break;
                    }
                    Log(Level.Info, "      [compile] " + logContents);
                }
                SolutionTask.Project.Unindent();
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
                    wdc.UploadFile(cs.OutputPath, cs.RelativeOutputDir.Replace(@"\", "/") + _projectSettings.OutputFileName);
                }

                // Copy any extra files over
                foreach (string extraOutputFile in cs.ExtraOutputFiles) {
                    FileInfo sourceFile = new FileInfo(extraOutputFile);
                    if (_isWebProject) {
                        WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                        wdc.UploadFile(extraOutputFile, cs.RelativeOutputDir.Replace(@"\", "/") + sourceFile.Name);
                    } else {
                        FileInfo destFile = new FileInfo(Path.Combine(cs.OutputDir, sourceFile.Name));

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

            if (ProjectSettings.RunPostBuildEvent != null) {
                if (!PostBuild(cs, (exitCode == 0) ? true : false, (exitCode == 0) ? true : false)) {
                    bSuccess = false;
                }
            }

            if (!bSuccess ) {
                Log(Level.Error, LogPrefix + "Build failed.");
            }

            return bSuccess;
        }

        public override string GetOutputPath(string configuration) {
            ConfigurationSettings settings = (ConfigurationSettings) GetConfiguration(configuration);
            if (settings == null) {
                return null;
            }
            return settings.OutputPath;
        }

        public override ConfigurationBase GetConfiguration(string configuration) {
            return (ConfigurationSettings) _htConfigurations[configuration];
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private bool PreBuild(ConfigurationSettings cs) {
            string buildCommandLine = _projectSettings.PreBuildEvent;
            Log(Level.Debug, LogPrefix + "PreBuild commandline: {0}", buildCommandLine);
            // check if there are pre build commands to be run
            if (buildCommandLine != null) {
                // create a batch file for this, mirroring the behavior of VS.NET
                // the file is not even removed after a successful build by VS.NET,
                // so we don't either
                using (StreamWriter sw = new StreamWriter(Path.Combine(cs.OutputDir, "PreBuildEvent.bat"))) {
                    sw.WriteLine("@echo off");
                    // replace any VS macros in the command line with real values
                    buildCommandLine = ReplaceMacros(_projectSettings, cs, buildCommandLine);
                    // handle linebreak charaters
                    buildCommandLine = buildCommandLine.Replace("&#xd;&#xa;", "\n");
                    sw.WriteLine(buildCommandLine);
                    sw.WriteLine("if errorlevel 1 goto EventReportError");
                    sw.WriteLine("goto EventEnd");
                    sw.WriteLine(":EventReportError");
                    sw.WriteLine("echo Project error: A tool returned an error code from the build event");
                    sw.WriteLine("exit 1");
                    sw.WriteLine(":EventEnd");
                }
                // now that we have to file on disk execute it
                return ExecuteBuildEvent(Path.Combine(cs.OutputDir, "PreBuildEvent.bat"), "PreBuildEvent");
            }
            // nothing to do, signal success
            return true;
        }

        private bool PostBuild(ConfigurationSettings cs, bool bCompileSuccess, bool bOutputUpdated) {
            string buildCommandLine = _projectSettings.PostBuildEvent;
            Log(Level.Debug, LogPrefix + "PostBuild commandline: {0}", buildCommandLine);
            // check if there are post build commands to be run
            if (buildCommandLine != null) {
                // Create a batch file for this. This mirrors VS behavior. Also this
                // file is not removed even after a successful build by VS so we don't either.
                using (StreamWriter sw = new StreamWriter(Path.Combine(cs.OutputDir, "PostBuildEvent.bat"))) {
                    sw.WriteLine("@echo off");
                    // replace any VS macros in the command line with real values
                    buildCommandLine = ReplaceMacros(_projectSettings, cs, buildCommandLine);
                    // handle linebreak charaters
                    buildCommandLine = buildCommandLine.Replace("&#xd;&#xa;", "\n");
                    sw.WriteLine(buildCommandLine);
                    sw.WriteLine("if errorlevel 1 goto EventReportError");
                    sw.WriteLine("goto EventEnd");
                    sw.WriteLine(":EventReportError");
                    sw.WriteLine("echo Project error: A tool returned an error code from the build event");
                    sw.WriteLine("exit 1");
                    sw.WriteLine(":EventEnd");
                }
                bool bBuildEventSuccess;
                // there are three different settings for when the PostBuildEvent should be run
                switch (_projectSettings.RunPostBuildEvent) {
                    case "OnBuildSuccess":
                        // post-build event will run if the build succeeds. Thus, 
                        // the event will even run for a project that is up-to-date, 
                        // as long as the build succeeds
                        if (bCompileSuccess) {
                            Log(Level.Debug, LogPrefix + "PostBuild+OnBuildSuccess+bCompileSuccess");
                            bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(cs.OutputDir, "PostBuildEvent.bat"), "PostBuildEvent");
                        } else {
                            Log(Level.Debug, LogPrefix + "PostBuild+OnBuildSuccess");
                            bBuildEventSuccess = true;
                        }
                        break;
                    case "Always":
                        // post-build event will run regardless of whether the 
                        // build succeeded
                        Log(Level.Debug, LogPrefix + "PostBuild+Always");
                        bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(cs.OutputDir, "PostBuildEvent.bat"), "PostBuildEvent");
                        break;
                    case "OnOutputUpdated":
                        // post-build event will only run when the compiler's 
                        // output file (.exe or .dll) is different than the 
                        // previous compiler output file. Thus, a post-build 
                        // event will not run if a project is up-to-date
                        if (bOutputUpdated) {
                            Log(Level.Debug, LogPrefix + "PostBuild+OnOutputUpdated+bOutputUpdated");
                            bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(cs.OutputDir, "PostBuildEvent.bat"), "PostBuildEvent");
                        } else {
                            Log(Level.Debug, LogPrefix + "PostBuild+OnOutputUpdated");
                            bBuildEventSuccess = true;
                        }
                        break;
                    default:
                        // getting here means unknown values in the RunPostBuildEvent 
                        // property
                        bBuildEventSuccess = false;
                        break;
                }
                return bBuildEventSuccess;
            }
            // nothing to do, signal success
            return true;
        }

        private bool ExecuteBuildEvent(string batchFile, string buildEvent) {
            // need to set some process info
            ProcessStartInfo psi = new ProcessStartInfo(batchFile);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true; // For logging
            psi.WorkingDirectory = Path.GetDirectoryName(batchFile);
            // start the process now
            Process batchEvent = Process.Start(psi);
            // keep logging output from the process for as long as it exists
            while (true) {
                string logContents = batchEvent.StandardOutput.ReadLine();
                if (logContents == null) {
                    break;
                }
                Log(Level.Verbose, "      [" + buildEvent.ToLower(CultureInfo.InvariantCulture) 
                    + "] " + logContents);
            }
            batchEvent.WaitForExit();
            // notify if there where problems running the batch file or it 
            // returned errors
            int exitCode = batchEvent.ExitCode;
            if (exitCode == 0) {
                Log(Level.Verbose, LogPrefix + "{0} succeeded (exit code = 0)", buildEvent);
            } else {
                Log(Level.Error, LogPrefix + "{0} failed with exit code = {1}", buildEvent, exitCode);
            }
            return (exitCode == 0) ? true : false;
        }

        private string ReplaceMacros(ProjectSettings ps, ConfigurationSettings cs, string commands) {
            // Replace all $(...} patterns with their expanded real values. Those are the
            // "macros" in the pre/post event builder in VS. This is potentionally a source
            // for user errors as expanded paths containing spaces needs to be enclosed in
            // quotation marks. Opted to follow VS behaviour where paths with spaces are left
            // to the user as a drill.
            string commandsExpanded, macro;
            int startPosition, stopPosition = 0;

            // Find the beginning of the first macro
            startPosition = commands.IndexOf("$(", stopPosition);
            commandsExpanded = commands;
            if (startPosition > -1) { // There is at least one macro to replace
                string targetPath = cs.OutputPath;
                string solutionPath = null;

                // The solution tag allows the specification of a bunch of projects with no
                // reference to a solution file. $(Solution... macros will not work then.
                if (SolutionTask.SolutionFile != null) {
                    solutionPath = Path.GetFullPath(SolutionTask.SolutionFile);
                }
                // As long there are new macros...
                while (startPosition > -1) {
                    stopPosition = commands.IndexOf(")", startPosition + 2);
                    macro = commands.Substring(startPosition, stopPosition - startPosition + 1);
                    switch (macro) { // Expand the appropriate macro
                        case "$(OutDir)": // E.g. bin\Debug\
                            commandsExpanded = commandsExpanded.Replace("$(OutDir)", cs.RelativeOutputDir);
                            break;
                        case "$(ConfigurationName)": // E.g. Debug
                            commandsExpanded = commandsExpanded.Replace("$(ConfigurationName)", cs.Name);
                            break;
                        case "$(ProjectName)": // E.g. WindowsApplication1
                            commandsExpanded = commandsExpanded.Replace("$(ProjectName)", Name);
                            break;
                        case "$(ProjectPath)": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\WindowsApplications1.csproj
                            commandsExpanded = commandsExpanded.Replace("$(ProjectPath)", ProjectPath);
                            break;
                        case "$(ProjectFileName)": // E.g. WindowsApplication1.csproj
                            commandsExpanded = commandsExpanded.Replace("$(ProjectFileName)", Path.GetFileName(ProjectPath));
                            break;
                        case "$(ProjectExt)": // .csproj
                            commandsExpanded = commandsExpanded.Replace("$(ProjectExt)", Path.GetExtension(ProjectPath));
                            break;
                        case "$(ProjectDir)": // ProjectPath without ProjectFileName at the end
                            commandsExpanded = commandsExpanded.Replace("$(ProjectDir)", Path.GetDirectoryName(ProjectPath) + Path.DirectorySeparatorChar);
                            break;
                        case "$(TargetName)": // E.g. WindowsApplication1
                            commandsExpanded = commandsExpanded.Replace("$(TargetName)", ps.AssemblyName);
                            break;
                        case "$(TargetPath)": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\bin\Debug\WindowsApplications1.exe
                            commandsExpanded = commandsExpanded.Replace("$(TargetPath)", cs.OutputPath);
                            break;
                        case "$(TargetExt)": // E.g. .exe
                            commandsExpanded = commandsExpanded.Replace("$(TargetExt)", Path.GetExtension(cs.OutputPath));
                            break;
                        case "$(TargetFileName)": // E.g. WindowsApplications1.exe
                            commandsExpanded = commandsExpanded.Replace("$(TargetFileName)", Path.GetFileName(cs.OutputPath));
                            break;
                        case "$(TargetDir)": // Absolute path to OutDir
                            commandsExpanded = commandsExpanded.Replace("$(TargetDir)", Path.GetDirectoryName(cs.OutputDir) + Path.DirectorySeparatorChar);
                            break;
                        case "$(SolutionFileName)": // E.g. WindowsApplication1.sln
                            if (solutionPath != null) {
                                commandsExpanded = commandsExpanded.Replace("$(SolutionFileName)", Path.GetFileName(solutionPath));
                            } else {
                                Log(Level.Error, LogPrefix + "Pre/post event macro {0} can not be set, no solution file specified.", macro);
                            }
                            break;
                        case "$(SolutionPath)": // Absolute path for SolutionFileName
                            if (solutionPath != null) {
                                commandsExpanded = commandsExpanded.Replace("$(SolutionPath)", solutionPath);
                            } else {
                               Log(Level.Error, LogPrefix + "Pre/post event macro {0} can not be set, no solution file specified.", macro);
                            }
                            break;
                        case "$(SolutionDir)": // SolutionPath without SolutionFileName appended
                            if (solutionPath != null) {
                                commandsExpanded = commandsExpanded.Replace("$(SolutionDir)", Path.GetDirectoryName(solutionPath) + Path.DirectorySeparatorChar);
                            } else {
                                Log(Level.Error, LogPrefix + "Pre/post event macro {0} can not be set, no solution file specified.", macro);
                            }
                            break;
                        case "$(SolutionName)": // E.g. WindowsApplication1
                            if (solutionPath != null) {
                                commandsExpanded = commandsExpanded.Replace("$(SolutionName)", Path.GetFileNameWithoutExtension(solutionPath));
                            } else {
                                Log(Level.Error, LogPrefix + "Pre/post event macro {0} can not be set, no solution file specified.", macro);
                            }
                            break;
                        case "$(SolutionExt)": // Is this ever anything but .sln?
                            if (solutionPath != null) {
                                commandsExpanded = commandsExpanded.Replace("$(SolutionExt)", Path.GetExtension(solutionPath));
                            } else {
                                Log(Level.Error, LogPrefix + "Pre/post event macro {0} can not be set, no solution file specified.", macro);
                            }
                            break;
                        case "$(PlatformName)": // .NET, does this value ever change?
                            commandsExpanded = commandsExpanded.Replace("$(PlatformName)", ".NET");
                            break;
                        // TO-DO
                        // DevEnvDir is avaliable from the key "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\7.1\InstallDir"
                        // But would require Microsoft.Win32 to be included. Don't want that for mono etc. ?
                        /* case "$(DevEnvDir)": // VS installation directory with \Common7\IDE appended
                           commandsExpanded = commandsExpanded.Replace("$(DevEnvDir)", "To be Implemented?");
                           break; */
                        default:
                            // Signal errors for macros that do not exist
                            Log(Level.Error, LogPrefix + "Pre/post event macro {0} not implemented.", macro);
                            break;
                    }
                    // Find the beginning of the next macro if any
                    startPosition = commands.IndexOf("$(", stopPosition);
                }
                Log(Level.Debug, LogPrefix + "Replaced command lines:{0} {1}.", Environment.NewLine, commandsExpanded);
                return commandsExpanded;
            } else { // No macro to replace
                Log(Level.Debug, LogPrefix + "Replaced command lines:{0} {1}.", Environment.NewLine, commandsExpanded);
                return commandsExpanded;
            }
        }

        private bool CheckUpToDate(ConfigurationSettings cs) {
            DateTime dtOutputTimeStamp;
            if (File.Exists(cs.OutputPath)) {
                dtOutputTimeStamp = File.GetLastWriteTime(cs.OutputPath);
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
            if (SolutionTask != null) {
                SolutionTask.Log(messageLevel, message);
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
            if (SolutionTask != null) {
                SolutionTask.Log(messageLevel, message, args);
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
        private string _projectPath;
        private string _projectDirectory;
        private string _webProjectBaseUrl;
        private ProjectSettings _projectSettings;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CommandFile = "compile-commands.txt";

        #endregion Private Static Fields
    }
}
