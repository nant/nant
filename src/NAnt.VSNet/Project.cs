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

using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Project : ProjectBase {
        #region Public Instance Constructors

        public Project(SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(solutionTask, tfc, gacCache, refResolver, outputDir) {
            _htReferences = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _sourceFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htResources = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htAssemblies = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public Resource[] Resources {
            get { return (Resource[]) new ArrayList(_htResources.Values).ToArray(typeof(Resource)); }
        }

        public ProjectSettings ProjectSettings {
            get { return _projectSettings; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        /// <summary>
        /// Gets a value indicating if this is a web project.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this is a web project; otherwise,
        /// <see langword="false" />.
        /// </value>
        private bool IsWebProject {
            get { return ProjectFactory.IsUrl(_projectPath); }
        }

        #endregion Private Instance Properties

        #region Override implementation of ProjectBase

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
        /// Gets the directory containing the VS.NET project.
        /// </summary>
        public override DirectoryInfo ProjectDirectory {
            get { return _projectDirectory; }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the VS.NET project.
        /// </summary>
        public override string Guid {
            get { return ProjectSettings.Guid; }
            set { throw new InvalidOperationException( "It is not allowed to change the GUID of a C#/VB.NET project" ); }
        }

        public override Reference[] References {
            get { return (Reference[]) new ArrayList(_htReferences.Values).ToArray(typeof(Reference)); }
        }

        public override void Load(SolutionBase sln, string projectPath) {
            XmlDocument doc = null;

            try {
                doc = LoadXmlDocument(projectPath);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading project '{0}'.", projectPath), 
                    Location.UnknownLocation, ex);
            }

            _projectPath = projectPath;

            if (!IsWebProject) {
                _projectDirectory = new FileInfo(projectPath).Directory;
            } else {
                string projectDirectory = projectPath.Replace(":", "_");
                projectDirectory = projectDirectory.Replace("/", "_");
                projectDirectory = projectDirectory.Replace("\\", "_");
                _projectDirectory = new DirectoryInfo(Path.Combine(
                    TemporaryFiles.BasePath, projectDirectory));

                // ensure project directory exists
                if (!_projectDirectory.Exists) {
                    _projectDirectory.Create();
                    _projectDirectory.Refresh();
                }

                _webProjectBaseUrl = projectPath.Substring(0, projectPath.LastIndexOf("/"));
            }

            _projectSettings = new ProjectSettings(doc.DocumentElement, (XmlElement) doc.SelectSingleNode("//Build/Settings"), this);

            XmlNodeList nlConfigurations = doc.SelectNodes("//Config");
            foreach (XmlElement elemConfig in nlConfigurations) {
                ConfigurationSettings cs = new ConfigurationSettings(this, elemConfig, OutputDir);
                ProjectConfigurations[elemConfig.Attributes["Name"].Value] = cs;
            }

            XmlNodeList nlReferences = doc.SelectNodes("//References/Reference");
            foreach (XmlElement elemReference in nlReferences) {
                Reference reference = new Reference(sln, _projectSettings, elemReference, GacCache, ReferencesResolver, this, OutputDir);
                _htReferences[elemReference.Attributes["Name"].Value] = reference;
            }

            if (_projectSettings.Type == ProjectType.VBNet) {
                XmlNodeList nlImports = doc.SelectNodes("//Imports/Import");
                foreach (XmlElement elemReference in nlImports) {
                    _imports += elemReference.Attributes["Namespace"].Value.ToString(CultureInfo.InvariantCulture) + ",";
                }
                if (!StringUtils.IsNullOrEmpty(_imports)) {
                    _imports = "/Imports:" + _imports;
                }
            }

            XmlNodeList nlFiles = doc.SelectNodes("//Files/Include/File");
            foreach (XmlElement elemFile in nlFiles) {
                string buildAction = elemFile.Attributes["BuildAction"].Value;
                string sourceFile;

                if (!StringUtils.IsNullOrEmpty(elemFile.GetAttribute("Link"))) {
                    sourceFile = Path.GetFullPath(Path.Combine(
                        ProjectDirectory.FullName, elemFile.GetAttribute("Link")));
                } else {
                    sourceFile = Path.GetFullPath(Path.Combine(
                        ProjectDirectory.FullName, elemFile.GetAttribute("RelPath")));
                }

                if (IsWebProject) {
                    WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                    wdc.DownloadFile(sourceFile, elemFile.Attributes["RelPath"].Value);

                    if (buildAction == "Compile") {
                        _sourceFiles[sourceFile] = null;
                    } else if (buildAction == "EmbeddedResource") {
                        FileInfo fi = new FileInfo(sourceFile);
                        if (fi.Exists && fi.Length == 0) {
                            Log(Level.Verbose, "Skipping zero-byte embedded resource '{0}'.", 
                                fi.FullName);
                        } else {
                            string dependentOn = (elemFile.Attributes["DependentUpon"] != null) ? Path.Combine(fi.DirectoryName, elemFile.Attributes["DependentUpon"].Value) : null;
                            Resource r = new Resource(this, fi, elemFile.Attributes["RelPath"].Value, Path.Combine(fi.DirectoryName, elemFile.Attributes["DependentUpon"].Value), SolutionTask, GacCache);
                            _htResources[r.InputFile] = r;
                        }
                    }
                } else {
                    switch (buildAction) {
                        case "Compile":
                            _sourceFiles[sourceFile] = null;
                            break;
                        case "EmbeddedResource":
                            FileInfo resourceFile = new FileInfo(sourceFile);
                            if (resourceFile.Exists && resourceFile.Extension.ToLower(CultureInfo.InvariantCulture) == ".resx" && resourceFile.Length == 0) {
                                Log(Level.Verbose, "Skipping zero-byte embedded resx '{0}'.", 
                                    resourceFile.FullName);
                            } else {
                                string dependentOn = (elemFile.Attributes["DependentUpon"] != null) ? Path.Combine(resourceFile.DirectoryName, elemFile.Attributes["DependentUpon"].Value) : null;
                                Resource r = new Resource(this, resourceFile, elemFile.Attributes["RelPath"].Value, dependentOn, SolutionTask, GacCache);
                                _htResources[r.InputFile] = r;
                            }
                            break;
                        case "None":
                            if (elemFile.GetAttribute("RelPath") == "App.config") {
                                ExtraOutputFiles[sourceFile] = ProjectSettings.OutputFileName
                                    + ".config";
                            }
                            break;
                    }
                }
            }
        }

        protected override bool Build(ConfigurationBase configurationSettings) {
            bool bSuccess = true;
            bool haveCultureSpecificResources = false;
            string tempFile = null;

            GacCache.RecreateDomain();
 
            try {
                ConfigurationSettings cs = (ConfigurationSettings) configurationSettings;

                // perform prebuild actions (for VS.NET 2003)
                if (ProjectSettings.RunPostBuildEvent != null) {
                    bSuccess = PreBuild(cs);
                }

                // ensure temp directory exists
                if (!Directory.Exists(TemporaryFiles.BasePath)) {
                    Directory.CreateDirectory(TemporaryFiles.BasePath);
                }

                string tempResponseFile = Path.Combine(TemporaryFiles.BasePath, 
                    Project.CommandFile);

                using (StreamWriter sw = File.CreateText(tempResponseFile)) {
                    if (CheckUpToDate(cs)) {
                        Log(Level.Verbose, "Project is up-to-date.");
                        if (ProjectSettings.RunPostBuildEvent != null) {
                            PostBuild(cs, true, false);
                        }
                        return true;
                    }

                    foreach (string setting in ProjectSettings.Settings) {
                        sw.WriteLine(setting);
                    }

                    foreach (string setting in cs.Settings) {
                        sw.WriteLine(setting);
                    }

                    if (ProjectSettings.ApplicationIcon != null) {
                        sw.WriteLine(@"/win32icon:""{0}""",
                            ProjectSettings.ApplicationIcon.FullName);
                    }

                    if (ProjectSettings.Type == ProjectType.VBNet) {
                        sw.WriteLine(_imports);
                    }

                    Log(Level.Verbose, "Copying references:");

                    foreach (Reference reference in _htReferences.Values) {
                        if (reference.CopyLocal) {
                            Log(Level.Verbose, " - " + reference.Name);

                            if (reference.IsCreated) {
                                string program, commandLine;
                                reference.GetCreationCommand(cs, out program, out commandLine);

                                // check that the SDK of the current target 
                                // framework is available
                                if (SolutionTask.Project.TargetFramework.SdkDirectory == null) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        "The SDK for the '{0}' framework is not installed or not configured correctly.", 
                                        SolutionTask.Project.TargetFramework.Name), Location.UnknownLocation);
                                }

                                // append the correct SDK directory to the program
                                program = Path.Combine(SolutionTask.Project.TargetFramework.SdkDirectory.FullName, program);
                                Log(Level.Verbose, program + " " + commandLine);

                                ProcessStartInfo psiRef = new ProcessStartInfo(program, commandLine);
                                psiRef.UseShellExecute = false;
                                psiRef.WorkingDirectory = cs.OutputDir.FullName;

                                try {
                                    Process pRef = Process.Start(psiRef);
                                    pRef.WaitForExit();
                                } catch (Win32Exception ex) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        "Unable to start process '{0}' with commandline '{1}'.", 
                                        program, commandLine), Location.UnknownLocation, ex);
                                }
                            } else {
                                Hashtable outputFiles = reference.GetOutputFiles(cs);

                                foreach (DictionaryEntry de in outputFiles) {
                                    // determine file to copy
                                    FileInfo srcFile = new FileInfo((string) de.Key);
                                    // determine destination file
                                    FileInfo destFile = new FileInfo(Path.Combine(
                                        cs.OutputDir.FullName, (string) de.Value));
                                    // perform actual copy
                                    CopyFile(srcFile, destFile, SolutionTask);
                                }
                            }
                        }
                    }

                    // wrtie assembly references to response file
                    foreach (string assemblyReference in GetAssemblyReferences(cs)) {
                        sw.WriteLine("/r:\"{0}\"", assemblyReference);
                    }

                    if (_htResources.Count > 0) {
                        Log(Level.Verbose, "Compiling resources:");
                        foreach (Resource resource in _htResources.Values) {
                            // ignore resource files associated with a culture
                            if (resource.Culture != null) {
                                haveCultureSpecificResources = true;
                                continue;
                            }

                            Log(Level.Verbose, " - {0}", resource.InputFile);
                            resource.Compile(cs);

                            sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                "/res:\"{0}\",\"{1}\"", resource.CompiledResourceFile, 
                                resource.ManifestResourceName)); 
                        }
                    }

                    // check if project does not contain any sources
                    if (_sourceFiles.Count == 0) {
                        // create temp file
                        tempFile = Path.GetTempFileName();

                        // add temp file to collection of sources to compile 
                        // as command line compilers require a least one source
                        // file to be specified, but VS.NET supports empty
                        // projects
                        _sourceFiles[tempFile] = null;
                    }

                    // add the files to compile
                    foreach (string file in _sourceFiles.Keys) {
                        sw.WriteLine(@"""" + file + @"""");
                    }
                }

                Log(Level.Verbose, "Starting compiler...");

                if (SolutionTask.Verbose) {
                    using (StreamReader sr = new StreamReader(tempResponseFile)) {
                        Log(Level.Verbose, "Commands:");

                        // increment indentation level
                        SolutionTask.Project.Indent();
                        try {
                            while (true) {
                                // read line
                                string line = sr.ReadLine();
                                if (line == null) {
                                    break;
                                }
                                // display line
                                Log(Level.Verbose, "    "  + line);
                            }
                        } finally {
                            // restore indentation level
                            SolutionTask.Project.Unindent();
                        }
                    }
                }

                ProcessStartInfo psi = null;

                switch (ProjectSettings.Type) {
                    case ProjectType.CSharp:
                        psi = new ProcessStartInfo(Path.Combine(SolutionTask.Project.TargetFramework.FrameworkDirectory.FullName, "csc.exe"), "@\"" + tempResponseFile + "\"");

                        // Visual C#.NET uses the <project dir>\obj\<configuration> 
                        // as working directory, so we should do the same to make 
                        // sure relative paths are resolved correctly 
                        // (eg. AssemblyKeyFile attribute)

                        // ensure object directory exists
                        if (!cs.ObjectDir.Exists) {
                            cs.ObjectDir.Create();
                        }
                        psi.WorkingDirectory = cs.ObjectDir.FullName;
                        break;
                    case ProjectType.VBNet:
                        psi = new ProcessStartInfo(Path.Combine(SolutionTask.Project.TargetFramework.FrameworkDirectory.FullName, "vbc.exe"), "@\"" + tempResponseFile + "\"");

                        // Visual Basic.NET uses the directory from which VS.NET 
                        // was launched as working directory, the closest match
                        // and best behaviour for us is to use the <solution dir>
                        // as working directory and fallback to the <project dir>
                        // if the project was explicitly specified
                        
                        if (SolutionTask.SolutionFile != null) {
                            psi.WorkingDirectory = SolutionTask.SolutionFile.DirectoryName;
                        } else {
                            psi.WorkingDirectory = ProjectDirectory.FullName;
                        }
                        break;
                }

                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;

                // start compiler
                Process p = Process.Start(psi);

                while (true) {
                    // read line
                    string line = p.StandardOutput.ReadLine();
                    if (line == null) {
                        break;
                    }
                    // display line
                    Log(Level.Info, line);
                }

                p.WaitForExit();

                int exitCode = p.ExitCode;
                Log(Level.Verbose, "{0}! (exit code = {1})", (exitCode == 0) ? "Success" : "Failure", exitCode);

                if (exitCode > 0) {
                    bSuccess = false;
                } else {
                    #region Process culture-specific resource files

                    if (haveCultureSpecificResources) {
                        Log(Level.Verbose, "Compiling satellite assemblies:");
                        Hashtable cultures = new Hashtable();
                        foreach (Resource resource in _htResources.Values) {
                            // ignore resource files NOT associated with a culture
                            if (resource.Culture == null) {
                                continue;
                            }
                            ArrayList resFiles = null;
                            if (!cultures.ContainsKey(resource.Culture)) {
                                resFiles = new ArrayList();
                                cultures.Add(resource.Culture, resFiles);
                            } else {
                                resFiles = (ArrayList) cultures[resource.Culture];
                            }
                            resFiles.Add(resource);
                        }

                        foreach (DictionaryEntry de in cultures) {
                            string culture = ((CultureInfo) de.Key).Name;
                            ArrayList resFiles = (ArrayList) de.Value;

                            AssemblyLinkerTask al = new AssemblyLinkerTask();
                            al.Project = SolutionTask.Project;
                            al.NamespaceManager = SolutionTask.NamespaceManager;
                            al.Parent = SolutionTask;
                            al.BaseDirectory = cs.OutputDir;
                            al.InitializeTaskConfiguration();

                            string satellitePath = cs.OutputDir.FullName;
                            satellitePath = Path.Combine (satellitePath, culture);
                            Directory.CreateDirectory(satellitePath);
                            satellitePath = Path.Combine(satellitePath, string.Format(
                                CultureInfo.InvariantCulture, "{0}.resources.dll", 
                                ProjectSettings.AssemblyName));
                            al.OutputFile = new FileInfo(satellitePath);
                            al.OutputTarget = "lib";
                            al.Culture = culture;
                            al.TemplateFile = new FileInfo(Path.Combine(cs.OutputDir.FullName, ProjectSettings.OutputFileName));
                            foreach (Resource resource in resFiles) {
                                resource.Compile(cs);
                                // add resources to embed 
                                Argument arg = new Argument();
                                arg.Value = string.Format(CultureInfo.InvariantCulture, "/embed:\"{0}\",\"{1}\"", resource.CompiledResourceFile, resource.ManifestResourceName);
                                al.Arguments.Add(arg);
                            }

                            // increment indentation level
                            SolutionTask.Project.Indent();
                            try {
                                Log(Level.Verbose, " - {0}", culture);
                                // run assembly linker
                                al.Execute();
                                // add satellite assembly to extra output files
                                ExtraOutputFiles[al.OutputFile.FullName] = Path.Combine(
                                    al.Culture, al.OutputFile.Name);
                            } finally {
                                // restore indentation level
                                SolutionTask.Project.Unindent();
                            }
                        }
                    }

                    #endregion Process culture-specific resource files

                    #region Deploy project-level output files

                    // copy primary project output (and related files)
                    if (IsWebProject) {
                        Hashtable primaryOutputFiles = Reference.GetRelatedFiles(
                            cs.OutputPath);

                        Log(Level.Verbose, "Uploading output files...");

                        // copy primary project output (and related files)
                        foreach (DictionaryEntry de in ExtraOutputFiles) {
                            WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                            wdc.UploadFile((string) de.Key, Path.Combine(cs.RelativeOutputDir,
                                (string) de.Value).Replace(@"\", "/"));
                        }
                    }

                    if (ExtraOutputFiles.Count > 0) {
                        Log(Level.Verbose, "Deploying extra project output files ...");
                    }

                    // copy any extra project-level output files
                    foreach (DictionaryEntry de in ExtraOutputFiles) {
                        FileInfo sourceFile = new FileInfo((string) de.Key);
                        if (IsWebProject) {
                            WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                            wdc.UploadFile(sourceFile.FullName, Path.Combine(cs.RelativeOutputDir, 
                                (string) de.Value).Replace(@"\", "/"));
                        } else {
                            // determine destination file
                            FileInfo destFile = new FileInfo(Path.Combine(cs.OutputDir.FullName, 
                                (string) de.Value));
                            // copy file using <copy> task
                            CopyFile(sourceFile, destFile, SolutionTask);
                        }
                    }

                    #endregion Deploy project-level output files

                    #region Deploy configuration-specific output files

                    if (cs.ExtraOutputFiles.Count > 0) {
                        Log(Level.Verbose, "Deploying extra config output files ...");
                    }

                    // copy any extra configuration-specific output files
                    foreach (DictionaryEntry de in cs.ExtraOutputFiles) {
                        FileInfo sourceFile = new FileInfo((string) de.Key);
                        if (IsWebProject) {
                            WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                            wdc.UploadFile(sourceFile.FullName, cs.RelativeOutputDir.Replace(@"\", "/") 
                                + (string) de.Value);
                        } else {
                            // determine destination file
                            FileInfo destFile = new FileInfo(Path.Combine(cs.OutputDir.FullName, 
                                (string) de.Value));
                            // copy file using <copy> task
                            CopyFile(sourceFile, destFile, SolutionTask);
                        }
                    }

                    #endregion Deploy configuration-specific output files
                }

                if (ProjectSettings.RunPostBuildEvent != null) {
                    if (!PostBuild(cs, (exitCode == 0) ? true : false, (exitCode == 0) ? true : false)) {
                        bSuccess = false;
                    }
                }

                if (!bSuccess ) {
                    Log(Level.Error, "Build failed.");
                }

                return bSuccess;
            } finally {
                // check if temporary file was created to support empty projects
                if (tempFile != null) {
                    // ensure temp file is deleted
                    File.Delete(tempFile);
                }
            }
        }

        #endregion Override implementation of ProjectBase

        #region Public Static Methods

        public static bool IsEnterpriseTemplateProject(string fileName) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);
                return doc.DocumentElement.Name.ToString(CultureInfo.InvariantCulture) == "EFPROJECT";
            } catch (XmlException) {
                // when the project isn't a valid XML document, it definitely
                // isn't an enterprise template project
                return false;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error checking whether '{0}' is an enterprise template project.",
                    fileName), Location.UnknownLocation, ex);
            }
        }

        public static string LoadGuid(string fileName) {
            try {
                XmlDocument doc = LoadXmlDocument(fileName);
                return ProjectSettings.GetProjectGuid(fileName, doc.DocumentElement);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error loading GUID of project '{0}'.", fileName), 
                    Location.UnknownLocation, ex);
            }
        }

        #endregion Public Static Methods

        #region Private Instance Methods

        private bool PreBuild(ConfigurationSettings cs) {
            string buildCommandLine = ProjectSettings.PreBuildEvent;
            Log(Level.Debug, "PreBuild commandline: {0}", buildCommandLine);
            // check if there are pre build commands to be run
            if (buildCommandLine != null) {
                // create a batch file for this, mirroring the behavior of VS.NET
                // the file is not even removed after a successful build by VS.NET,
                // so we don't either
                using (StreamWriter sw = new StreamWriter(Path.Combine(cs.OutputDir.FullName, "PreBuildEvent.bat"))) {
                    sw.WriteLine("@echo off");
                    // replace any VS macros in the command line with real values
                    buildCommandLine = cs.ExpandMacros(buildCommandLine);
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
                return ExecuteBuildEvent(Path.Combine(cs.OutputDir.FullName, "PreBuildEvent.bat"), "PreBuildEvent");
            }
            // nothing to do, signal success
            return true;
        }

        private bool PostBuild(ConfigurationSettings cs, bool bCompileSuccess, bool bOutputUpdated) {
            string buildCommandLine = ProjectSettings.PostBuildEvent;
            Log(Level.Debug, "PostBuild commandline: {0}", buildCommandLine);
            // check if there are post build commands to be run
            if (buildCommandLine != null) {
                // Create a batch file for this. This mirrors VS behavior. Also this
                // file is not removed even after a successful build by VS so we don't either.
                using (StreamWriter sw = new StreamWriter(Path.Combine(cs.OutputDir.FullName, "PostBuildEvent.bat"))) {
                    sw.WriteLine("@echo off");
                    // replace any VS macros in the command line with real values
                    buildCommandLine = cs.ExpandMacros(buildCommandLine);
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
                switch (ProjectSettings.RunPostBuildEvent) {
                    case "OnBuildSuccess":
                        // post-build event will run if the build succeeds. Thus, 
                        // the event will even run for a project that is up-to-date, 
                        // as long as the build succeeds
                        if (bCompileSuccess) {
                            Log(Level.Debug, "PostBuild+OnBuildSuccess+bCompileSuccess");
                            bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(
                                cs.OutputDir.FullName, "PostBuildEvent.bat"), "PostBuildEvent");
                        } else {
                            Log(Level.Debug, "PostBuild+OnBuildSuccess");
                            bBuildEventSuccess = true;
                        }
                        break;
                    case "Always":
                        // post-build event will run regardless of whether the 
                        // build succeeded
                        Log(Level.Debug, "PostBuild+Always");
                        bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(
                            cs.OutputDir.FullName, "PostBuildEvent.bat"), "PostBuildEvent");
                        break;
                    case "OnOutputUpdated":
                        // post-build event will only run when the compiler's 
                        // output file (.exe or .dll) is different than the 
                        // previous compiler output file. Thus, a post-build 
                        // event will not run if a project is up-to-date
                        if (bOutputUpdated) {
                            Log(Level.Debug, "PostBuild+OnOutputUpdated+bOutputUpdated");
                            bBuildEventSuccess = ExecuteBuildEvent(Path.Combine(
                                cs.OutputDir.FullName, "PostBuildEvent.bat"), "PostBuildEvent");
                        } else {
                            Log(Level.Debug, "PostBuild+OnOutputUpdated");
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
                Log(Level.Verbose, "{0} succeeded (exit code = 0)", buildEvent);
            } else {
                Log(Level.Error, "{0} failed with exit code = {1}", buildEvent, exitCode);
            }
            return (exitCode == 0) ? true : false;
        }

        private bool CheckUpToDate(ConfigurationSettings cs) {
            DateTime dtOutputTimeStamp;
            if (File.Exists(cs.OutputPath)) {
                dtOutputTimeStamp = File.GetLastWriteTime(cs.OutputPath);
            } else {
                return false;
            }

            // check all of the input files
            foreach (string file in _sourceFiles.Keys) {
                if (dtOutputTimeStamp < File.GetLastWriteTime(file)) {
                    return false;
                }
            }

            // check all resources
            foreach (Resource resource in _htResources.Values) {
                if (dtOutputTimeStamp < resource.InputFile.LastWriteTime) {
                    return false;
                }
            }

            // check all of the input references
            foreach (Reference reference in _htReferences.Values) {
                reference.ConfigurationSettings = cs;
                if (dtOutputTimeStamp < reference.Timestamp) {
                    return false;
                }
            }

            return true;
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Copies the specified file if the destination file does not exist, or
        /// the source file has been modified since it was previously copied.
        /// </summary>
        /// <param name="srcFile">The file to copy.</param>
        /// <param name="destFile">The destination file.</param>
        /// <param name="parent">The <see cref="Task" /> in which context the operation will be performed.</param>
        private static void CopyFile(FileInfo srcFile, FileInfo destFile, Task parent) {
            // create instance of Copy task
            CopyTask ct = new CopyTask();

            // inherit project from solution task
            ct.Project = parent.Project;

            // inherit namespace manager from solution task
            ct.NamespaceManager = parent.NamespaceManager;

            // parent is solution task
            ct.Parent = parent;

            // inherit verbose setting from solution task
            ct.Verbose = parent.Verbose;

            // only output warning messages or higher, unless 
            // we're running in verbose mode
            if (!ct.Verbose) {
                ct.Threshold = Level.Warning;
            }

            // make sure framework specific information is set
            ct.InitializeTaskConfiguration();

            // set parent of child elements
            ct.CopyFileSet.Parent = ct;

            // inherit project for child elements from containing task
            ct.CopyFileSet.Project = ct.Project;

            // inherit namespace manager from containing task
            ct.CopyFileSet.NamespaceManager = ct.NamespaceManager;

            // set file to copy
            ct.SourceFile = srcFile;

            // set file
            ct.ToFile = destFile;

            // increment indentation level
            ct.Project.Indent();

            try {
                // execute task
                ct.Execute();
            } finally {
                // restore indentation level
                ct.Project.Unindent();
            }
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private Hashtable _htReferences;

        /// <summary>
        /// Holds a case-insensitive list of source files.
        /// </summary>
        /// <remarks>
        /// The key of the <see cref="Hashtable" /> is the full path of the 
        /// source file and the value is <see langword="null" />.
        /// </remarks>
        private Hashtable _sourceFiles;

        private Hashtable _htResources;
        private Hashtable _htAssemblies;

        private string _imports;
        private string _projectPath;
        private DirectoryInfo _projectDirectory;
        private string _webProjectBaseUrl;
        private ProjectSettings _projectSettings;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CommandFile = "compile-commands.txt";

        #endregion Private Static Fields
    }
}
