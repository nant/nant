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
    public abstract class ManagedProjectBase : ProjectBase {
        #region Public Instance Constructors

        protected ManagedProjectBase(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            if (projectPath == null) {
                throw new ArgumentNullException("projectPath");
            }

            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }

            _references = new ArrayList();
            _resources = new ArrayList();
            _sourceFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
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

            _projectSettings = new ProjectSettings(xmlDefinition, (XmlElement) 
                xmlDefinition.SelectSingleNode("//Build/Settings"), this);

            XmlNodeList nlConfigurations = xmlDefinition.SelectNodes("//Config");
            foreach (XmlElement elemConfig in nlConfigurations) {
                ConfigurationSettings cs = new ConfigurationSettings(this, elemConfig, OutputDir);
                ProjectConfigurations[elemConfig.Attributes["Name"].Value] = cs;
            }

            XmlNodeList nlReferences = xmlDefinition.SelectNodes("//References/Reference");
            foreach (XmlElement elemReference in nlReferences) {
                ReferenceBase reference = CreateReference(solution, elemReference);
                _references.Add(reference);
            }

            XmlNodeList nlFiles = xmlDefinition.SelectNodes("//Files/Include/File");
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
                            Resource r = new Resource(this, fi, elemFile.Attributes["RelPath"].Value, dependentOn, SolutionTask, GacCache);
                            _resources.Add(r);
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
                                _resources.Add(r);
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

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public Resource[] Resources {
            get { return (Resource[]) _resources.ToArray(typeof(Resource)); }
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

        /// <summary>
        /// Gets a value indicating if there are culture-specific resources
        /// in the project.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if there are culture-specific resources in
        /// the project; otherwise, <see langword="false" />.
        /// </value>
        private bool HasCultureSpecificResources {
            get {
                bool hasCultureSpecificResources = false;
                foreach (Resource resource in _resources) {
                    // ignore resource files associated with a culture
                    if (resource.Culture != null) {
                        hasCultureSpecificResources = true;
                        break;;
                    }
                }
                return hasCultureSpecificResources;
            }
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

        public override ArrayList References {
            get { return _references; }
        }

        protected override bool Build(ConfigurationBase config) {
            bool bSuccess = true;
            string tempFile = null;

            GacCache.RecreateDomain();
 
            try {
                ConfigurationSettings cs = (ConfigurationSettings) config;

                // perform prebuild actions (for VS.NET 2003 and higher)
                // TODO: should we stop build if prebuild event fails?
                bSuccess = PreBuild(cs);

                // ensure temp directory exists
                if (!Directory.Exists(TemporaryFiles.BasePath)) {
                    Directory.CreateDirectory(TemporaryFiles.BasePath);
                }

                // check if project needs to be rebuilt
                if (CheckUpToDate(cs)) {
                    Log(Level.Verbose, "Project is up-to-date.");
                    // check if postbuild event needs to be executed
                    if (ProjectSettings.RunPostBuildEvent != null) {
                        // TODO: should we stop build if postbuild event fails?
                        PostBuild(cs, true, false);
                    }
                    return true;
                }

                Log(Level.Verbose, "Copying references:");

                foreach (ReferenceBase reference in _references) {
                    if (reference.CopyLocal) {
                        Log(Level.Verbose, " - " + reference.Name);

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

                string tempResponseFile = Path.Combine(TemporaryFiles.BasePath, 
                    CommandFile);

                using (StreamWriter sw = File.CreateText(tempResponseFile)) {
                    // write compiler options
                    WriteCompilerOptions(sw, cs);
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

                ProcessStartInfo psi = GetProcessStartInfo(cs, tempResponseFile);
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

                    if (HasCultureSpecificResources) {
                        Log(Level.Verbose, "Compiling satellite assemblies:");
                        Hashtable cultures = new Hashtable();
                        foreach (Resource resource in _resources) {
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
                        Hashtable primaryOutputFiles = ReferenceBase.GetRelatedFiles(
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

        #region Protected Instance Methods

        /// <summary>
        /// Returns a <see cref="ProcessStartInfo" /> for launching the compiler
        /// for this project.
        /// </summary>
        /// <param name="config">The configuration to build.</param>
        /// <param name="responseFile">The response file for the compiler.</param>
        /// <returns>
        /// A <see cref="ProcessStartInfo" /> for launching the compiler for 
        /// this project.
        /// </returns>
        protected abstract ProcessStartInfo GetProcessStartInfo(ConfigurationBase config, string responseFile);

        protected virtual ReferenceBase CreateReference(SolutionBase solution, XmlElement xmlDefinition) {
            if (solution == null) {
                throw new ArgumentNullException("solution");
            }
            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }

            if (xmlDefinition.Attributes["Project"] != null) {
                return new ManagedProjectReference(xmlDefinition, ReferencesResolver, this, 
                    solution, ProjectSettings.TemporaryFiles, GacCache, OutputDir);
            } else if (xmlDefinition.Attributes["WrapperTool"] != null) {
                // wrapper
                return new WrapperReference(xmlDefinition, ReferencesResolver, 
                    this, GacCache, ProjectSettings);
            } else {
                // assembly reference
                return new ManagedAssemblyReference(xmlDefinition, ReferencesResolver, 
                    this, GacCache);
            }
        }

        public override ProjectReferenceBase CreateProjectReference(ProjectBase project, bool isPrivateSpecified, bool isPrivate) {
            return new ManagedProjectReference(project, this, isPrivateSpecified, 
                isPrivate);
        }

        protected virtual void WriteCompilerOptions(StreamWriter sw, ConfigurationSettings config) {
            // write project level options (eg. /target)
            foreach (string setting in ProjectSettings.Settings) {
                sw.WriteLine(setting);
            }

            // write configuration-level compiler options
            foreach (string setting in config.Settings) {
                sw.WriteLine(setting);
            }

            // write assembly references to response file
            foreach (string assemblyReference in GetAssemblyReferences(config)) {
                sw.WriteLine("/r:\"{0}\"", assemblyReference);
            }

            if (ProjectSettings.ApplicationIcon != null) {
                sw.WriteLine(@"/win32icon:""{0}""",
                    ProjectSettings.ApplicationIcon.FullName);
            }

            if (_resources.Count > 0) {
                Log(Level.Verbose, "Compiling resources:");
                foreach (Resource resource in _resources) {
                    if (resource.Culture != null) {
                        // ignore resource files associated with a culture
                        continue;
                    }

                    Log(Level.Verbose, " - {0}", resource.InputFile);
                    resource.Compile(config);

                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "/res:\"{0}\",\"{1}\"", resource.CompiledResourceFile, 
                        resource.ManifestResourceName)); 
                }
            }

            // before writing files to response file, allow project specific
            // options to be written (eg. VB specific options)
            WriteProjectOptions(sw, config);

            // add the files to compile
            foreach (string file in _sourceFiles.Keys) {
                sw.WriteLine(@"""" + file + @"""");
            }
        }
    
        protected virtual void WriteProjectOptions(StreamWriter sw, ConfigurationSettings config) {
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private bool PreBuild(ConfigurationSettings cs) {
            string buildCommandLine = ProjectSettings.PreBuildEvent;
            // check if there are pre build commands to be run
            if (buildCommandLine != null) {
                Log(Level.Debug, "PreBuild commandline: {0}", buildCommandLine);
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
            // check if there are post build commands to be run
            if (buildCommandLine != null) {
                Log(Level.Debug, "PostBuild commandline: {0}", buildCommandLine);
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
            foreach (Resource resource in _resources) {
                if (dtOutputTimeStamp < resource.InputFile.LastWriteTime) {
                    return false;
                }
            }

            // check all of the input references
            foreach (ReferenceBase reference in _references) {
                if (dtOutputTimeStamp < reference.GetTimestamp(cs)) {
                    return false;
                }
            }

            return true;
        }

        #endregion Private Instance Methods

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

        #region Protected Static Methods

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="projectNode">XML fragment representing the project to check.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        protected static ProductVersion GetProductVersion(XmlNode projectNode) {
            if (projectNode == null) {
                throw new ArgumentNullException("projectNode");
            }

            XmlAttribute productVersionAttribute = projectNode.Attributes["ProductVersion"];
            if (productVersionAttribute == null) {
                throw new BuildException("The \"ProductVersion\" attribute is"
                    + " missing from the <VisualStudioProject> node.",
                    Location.UnknownLocation);
            }

            // check if we're dealing with a valid version number
            Version productVersion = null;
            try {
                productVersion = new Version(productVersionAttribute.Value);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The value of the \"Version\" attribute ({0}) is not a valid"
                    + " version string.", productVersionAttribute.Value),
                    Location.UnknownLocation, ex);
            }

            if (productVersion.Major == 7) {
                switch (productVersion.Minor) {
                    case 0:
                        return ProductVersion.Rainier;
                    case 10:
                        return ProductVersion.Everett;
                }
            } 

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Visual Studio version \"{0\" is not supported.",
                productVersion.ToString()), Location.UnknownLocation);
        }

        #endregion Protected Static Methods

        #region Private Instance Fields

        private ArrayList _references;

        /// <summary>
        /// Holds a case-insensitive list of source files.
        /// </summary>
        /// <remarks>
        /// The key of the <see cref="Hashtable" /> is the full path of the 
        /// source file and the value is <see langword="null" />.
        /// </remarks>
        private readonly Hashtable _sourceFiles;

        private readonly ArrayList _resources;
        private readonly string _projectPath;
        private readonly DirectoryInfo _projectDirectory;
        private readonly string _webProjectBaseUrl;
        private readonly ProjectSettings _projectSettings;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CommandFile = "compile-commands.txt";

        #endregion Private Static Fields
    }
}
