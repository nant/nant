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
            _projectLocation = DetermineProjectLocation(xmlDefinition);

            if (!IsWebProject) {
                _projectDirectory = new FileInfo(projectPath).Directory;
            } else {
                string projectDirectory = projectPath.Replace(":", "_");
                projectDirectory = projectDirectory.Replace("/", "_");
                projectDirectory = projectDirectory.Replace("\\", "_");
                _projectDirectory = new DirectoryInfo(FileUtils.CombinePaths(
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
                    sourceFile = FileUtils.GetFullPath(FileUtils.CombinePaths(
                        ProjectDirectory.FullName, elemFile.GetAttribute("Link")));
                } else {
                    sourceFile = FileUtils.GetFullPath(FileUtils.CombinePaths(
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
                            string dependentOn = (elemFile.Attributes["DependentUpon"] != null) ? FileUtils.CombinePaths(fi.DirectoryName, elemFile.Attributes["DependentUpon"].Value) : null;
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
                                string dependentOn = (elemFile.Attributes["DependentUpon"] != null) ? FileUtils.CombinePaths(resourceFile.DirectoryName, elemFile.Attributes["DependentUpon"].Value) : null;
                                Resource r = new Resource(this, resourceFile, elemFile.Attributes["RelPath"].Value, dependentOn, SolutionTask, GacCache);
                                _resources.Add(r);
                            }
                            break;
                        case "None":
                            // check if file is "App.config" (using case-insensitive comparison)
                            if (string.Compare("App.config", elemFile.GetAttribute("RelPath"), true, CultureInfo.InvariantCulture) == 0) {
                                // App.config is only an output file for executable projects
                                if (ProjectSettings.OutputType == ManagedOutputType.Executable || ProjectSettings.OutputType == ManagedOutputType.WindowsExecutable) {
                                    ExtraOutputFiles[sourceFile] = ProjectSettings.OutputFileName
                                        + ".config";
                                }
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
        /// <remarks>
        /// If the url of a web project has been mapped to a local path
        /// (using the &lt;webmap&gt; element), then this property will return
        /// <see langword="false" /> for a <see cref="NAnt.VSNet.ProjectLocation.Web" />
        /// project.
        /// </remarks>
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
                    return FileUtils.GetFullPath(_projectPath);
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
        /// Get the location of the project.
        /// </summary>
        public override ProjectLocation ProjectLocation {
            get { return _projectLocation; }
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

        /// <summary>
        /// Gets a value indicating whether building the project for the specified
        /// build configuration results in managed output.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// <see langword="true" />.
        /// </returns>
        public override bool IsManaged(string solutionConfiguration) {
            return true;
        }

        /// <summary>
        /// Prepares the project for being built.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <remarks>
        /// Ensures the configuration-level object directory exists and ensures 
        /// that none of the output files are marked read-only.
        /// </remarks>
        protected override void Prepare(string solutionConfiguration) {
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = (ConfigurationBase) BuildConfigurations[solutionConfiguration]; 

            // ensure configuration-level object directory exists
            if (!config.ObjectDir.Exists) {
                config.ObjectDir.Create();
                config.ObjectDir.Refresh();
            }

            // ensure that none of the output files in the configuration-level 
            // object directory are marked read-only
            base.Prepare(solutionConfiguration);
        }

        protected override bool Build(string solutionConfiguration) {
            bool bSuccess = true;
            bool outputUpdated;
            string tempFile = null;

            GacCache.RecreateDomain();
 
            try {
                // obtain project configuration (corresponding with solution configuration)
                ConfigurationSettings cs = (ConfigurationSettings) BuildConfigurations[solutionConfiguration];

                // perform prebuild actions (for VS.NET 2003 and higher)
                if (!PreBuild(cs)) {
                    // no longer bother trying to build the project and do not
                    // execute any post-build events
                    return false;
                }

                // ensure temp directory exists
                if (!Directory.Exists(TemporaryFiles.BasePath)) {
                    Directory.CreateDirectory(TemporaryFiles.BasePath);
                }

                // check if project output needs to be rebuilt
                if (CheckUpToDate(solutionConfiguration)) {
                    Log(Level.Verbose, "Project is up-to-date.");

                    // project output is up-to-date
                    outputUpdated = false;
                } else {
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

                    string tempResponseFile = FileUtils.CombinePaths(TemporaryFiles.BasePath, 
                        CommandFile);

                    using (StreamWriter sw = File.CreateText(tempResponseFile)) {
                        // write compiler options
                        WriteCompilerOptions(sw, solutionConfiguration);
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
                    }

                    // project output has been updated
                    outputUpdated = true;
                }

                #region Process culture-specific resource files

                Log(Level.Verbose, "Building satellite assemblies...");

                Hashtable cultureResources = GetCultureSpecificResources();
                foreach (DictionaryEntry de in cultureResources) {
                    string culture = ((CultureInfo) de.Key).Name;
                    ArrayList resFiles = (ArrayList) de.Value;

                    AssemblyLinkerTask al = new AssemblyLinkerTask();
                    al.Project = SolutionTask.Project;
                    al.NamespaceManager = SolutionTask.NamespaceManager;
                    al.Parent = SolutionTask;
                    al.BaseDirectory = cs.OutputDir;
                    al.InitializeTaskConfiguration();

                    string satelliteBuildDir = FileUtils.CombinePaths(
                        cs.ObjectDir.FullName, culture);

                    // ensure satellite build directory exists
                    if (!Directory.Exists(satelliteBuildDir)) {
                        Directory.CreateDirectory(satelliteBuildDir);
                    }

                    string satelliteBuildFile = FileUtils.CombinePaths(satelliteBuildDir,
                        string.Format(CultureInfo.InvariantCulture, "{0}.resources.dll", 
                        ProjectSettings.AssemblyName));

                    al.OutputFile = new FileInfo(satelliteBuildFile);
                    al.OutputTarget = "lib";
                    al.Culture = culture;
                    al.TemplateFile = new FileInfo(cs.BuildPath);
                    foreach (Resource resource in resFiles) {
                        FileInfo compiledResourceFile = null;

                        if (resource.IsResX) {
                            // localized resx files have already been compiled
                            compiledResourceFile = resource.GetCompiledResourceFile(
                                solutionConfiguration);
                        } else {
                            // compile resource
                            compiledResourceFile = resource.Compile(solutionConfiguration);
                        }

                        // add resources to embed 
                        Argument arg = new Argument();
                        arg.Value = string.Format(CultureInfo.InvariantCulture, 
                            "/embed:\"{0}\",\"{1}\"", compiledResourceFile.FullName, 
                            resource.GetManifestResourceName(solutionConfiguration));
                        al.Arguments.Add(arg);
                    }

                    // increment indentation level
                    SolutionTask.Project.Indent();
                    try {
                        Log(Level.Verbose, " - {0}", culture);
                        // run assembly linker
                        al.Execute();
                        // add satellite assembly to extra output files
                        ExtraOutputFiles[al.OutputFile.FullName] = FileUtils.CombinePaths(
                            al.Culture, al.OutputFile.Name);
                    } finally {
                        // restore indentation level
                        SolutionTask.Project.Unindent();
                    }
                }

                #endregion Process culture-specific resource files

                #region Deploy project and configuration level output files

                // copy primary project output (and related files)
                Hashtable outputFiles = GetOutputFiles(solutionConfiguration);
                foreach (DictionaryEntry de in outputFiles) {
                    string srcPath = (string) de.Key;
                    string relativePath = (string) de.Value;

                    if (IsWebProject) {
                        WebDavClient wdc = new WebDavClient(new Uri(_webProjectBaseUrl));
                        wdc.UploadFile(srcPath, FileUtils.CombinePaths(cs.RelativeOutputDir,
                            relativePath).Replace(@"\", "/"));
                    } else {
                        // determine destination file
                        FileInfo destFile = new FileInfo(FileUtils.CombinePaths(cs.OutputDir.FullName, 
                            relativePath));
                        // copy file using <copy> task
                        CopyFile(new FileInfo(srcPath), destFile, SolutionTask);
                    }
                }

                #endregion Deploy project and configuration level output files

                if (ProjectSettings.RunPostBuildEvent != null) {
                    if (!PostBuild(cs, !outputUpdated || bSuccess, outputUpdated)) {
                        bSuccess = false;
                    }
                }

                if (!bSuccess) {
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

        protected virtual void WriteCompilerOptions(StreamWriter sw, string solutionConfiguration) {
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationSettings config = (ConfigurationSettings) BuildConfigurations[solutionConfiguration];

            // write project level options (eg. /target)
            foreach (string setting in ProjectSettings.Settings) {
                sw.WriteLine(setting);
            }

            // write configuration-level compiler options
            foreach (string setting in config.Settings) {
                sw.WriteLine(setting);
            }

            // write assembly references to response file
            foreach (string assemblyReference in GetAssemblyReferences(solutionConfiguration)) {
                sw.WriteLine("/r:\"{0}\"", assemblyReference);
            }

            if (ProjectSettings.ApplicationIcon != null) {
                sw.WriteLine(@"/win32icon:""{0}""",
                    ProjectSettings.ApplicationIcon.FullName);
            }

            if (_resources.Count > 0) {
                WriteResourceOptions(sw, solutionConfiguration);
            }

            // before writing files to response file, allow project specific
            // options to be written (eg. VB specific options)
            WriteProjectOptions(sw);

            // add the files to compile
            foreach (string file in _sourceFiles.Keys) {
                sw.WriteLine(@"""" + file + @"""");
            }
        }

        protected virtual void WriteProjectOptions(StreamWriter sw) {
        }

        /// <summary>
        /// Returns the project location from the specified project XML fragment.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <returns>
        /// The project location of the specified project XML file.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The project location could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The project location is invalid.</para>
        /// </exception>
        protected abstract ProjectLocation DetermineProjectLocation(XmlElement docElement);

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private void WriteResourceOptions(StreamWriter sw, string solutionConfiguration) {
            Log(Level.Verbose, "Compiling resources:");

            Hashtable resxResources = new Hashtable();

            foreach (Resource resource in _resources) {
                Log(Level.Verbose, " - {0}", resource.InputFile);

                if (resource.IsResX) {
                    // determine filename of output file
                    FileInfo compiledResxFile = resource.GetCompiledResourceFile(solutionConfiguration);
                    // add to list of resx files to compile
                    resxResources.Add(resource, compiledResxFile);
                } else if (resource.Culture == null) { // only compile non-localized non-resx files here
                    // compile resource
                    FileInfo compiledResourceFile = resource.Compile(
                        solutionConfiguration);
                    // write option to response file
                    sw.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "/res:\"{0}\",\"{1}\"", compiledResourceFile.FullName, 
                        resource.GetManifestResourceName(solutionConfiguration)));
                }
            }

            // no further processing required if there are no resx files to 
            // compile
            if (resxResources.Count == 0) {
                return;
            }

            // create instance of ResGen task
            ResGenTask rt = new ResGenTask();

            // inherit project from solution task
            rt.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            rt.NamespaceManager = SolutionTask.NamespaceManager;

            // parent is solution task
            rt.Parent = SolutionTask;

            // inherit verbose setting from solution task
            rt.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            rt.InitializeTaskConfiguration();

            // set parent of child elements
            rt.Assemblies.Parent = rt;

            // inherit project from solution task from parent task
            rt.Assemblies.Project = rt.Project;

            // inherit namespace manager from parent task
            rt.Assemblies.NamespaceManager = rt.NamespaceManager;

            // set base directory for filesets
            rt.Assemblies.BaseDirectory = ProjectDirectory;

            // set resx files to compile
            foreach (DictionaryEntry entry in resxResources) {
                Resource resource = (Resource) entry.Key;
                FileInfo outputFile = (FileInfo) entry.Value;

                QualifiedResource qualifiedResource = new QualifiedResource(
                    resource.InputFile, outputFile);

                rt.QualifiedResources.Add(qualifiedResource);
            }

            // inherit assembly references from project
            foreach (ReferenceBase reference in References) {
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    solutionConfiguration);
                foreach (string assemblyFile in assemblyReferences) {
                    rt.Assemblies.Includes.Add(assemblyFile);
                }
            }

            // increment indentation level
            rt.Project.Indent();
            try {
                // execute task
                rt.Execute();
            } finally {
                // restore indentation level
                rt.Project.Unindent();
            }

            // finally write the resx options to response file
            foreach (DictionaryEntry entry in resxResources) {
                Resource resource = (Resource) entry.Key;

                if (resource.Culture != null) {
                    // ignore resource files associated with a culture
                    continue;
                }

                FileInfo outputFile = (FileInfo) entry.Value;
                string manifestResourceName = resource.GetManifestResourceName(
                    solutionConfiguration);

                // write option to response file
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, 
                    "/res:\"{0}\",\"{1}\"", outputFile.FullName,
                    manifestResourceName));
            }
        }

        private bool PreBuild(ConfigurationSettings cs) {
            string buildCommandLine = ProjectSettings.PreBuildEvent;
            // check if there are pre build commands to be run
            if (buildCommandLine != null) {
                string batchFile = FileUtils.CombinePaths(cs.OutputDir.FullName, "PreBuildEvent.bat");
                string workingDirectory = cs.OutputDir.FullName;
                return ExecuteBuildEvent("PreBuildEvent", buildCommandLine, 
                    batchFile, workingDirectory, cs);
            }
            // nothing to do, signal success
            return true;
        }

        private bool PostBuild(ConfigurationSettings cs, bool bCompileSuccess, bool bOutputUpdated) {
            string buildCommandLine = ProjectSettings.PostBuildEvent;
            // check if there are post build commands to be run
            if (buildCommandLine != null) {
                Log(Level.Debug, "PostBuild commandline: {0}", buildCommandLine);

                string batchFile = FileUtils.CombinePaths(cs.OutputDir.FullName, "PostBuildEvent.bat");
                string workingDirectory = cs.OutputDir.FullName;

                bool bBuildEventSuccess;
                // there are three different settings for when the PostBuildEvent should be run
                switch (ProjectSettings.RunPostBuildEvent) {
                    case "OnBuildSuccess":
                        // post-build event will run if the build succeeds. Thus, 
                        // the event will even run for a project that is up-to-date, 
                        // as long as the build succeeds
                        if (bCompileSuccess) {
                            Log(Level.Debug, "PostBuild+OnBuildSuccess+bCompileSuccess");
                            bBuildEventSuccess = ExecuteBuildEvent("PostBuildEvent", 
                                buildCommandLine, batchFile, workingDirectory, cs);
                        } else {
                            Log(Level.Debug, "PostBuild+OnBuildSuccess");
                            bBuildEventSuccess = true;
                        }
                        break;
                    case "Always":
                        // post-build event will run regardless of whether the 
                        // build succeeded
                        Log(Level.Debug, "PostBuild+Always");
                        bBuildEventSuccess = ExecuteBuildEvent("PostBuildEvent", 
                            buildCommandLine, batchFile, workingDirectory, cs);
                        break;
                    case "OnOutputUpdated":
                        // post-build event will only run when the compiler's 
                        // output file (.exe or .dll) is different than the 
                        // previous compiler output file. Thus, a post-build 
                        // event will not run if a project is up-to-date
                        if (bOutputUpdated) {
                            Log(Level.Debug, "PostBuild+OnOutputUpdated+bOutputUpdated");
                            bBuildEventSuccess = ExecuteBuildEvent("PostBuildEvent", 
                                buildCommandLine, batchFile, workingDirectory, cs);
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

        private bool CheckUpToDate(string solutionConfiguration) {
            DateTime dtOutputTimeStamp;

            // obtain project configuration (corresponding with solution configuration)
            ConfigurationSettings cs = (ConfigurationSettings) BuildConfigurations[solutionConfiguration];

            // check if project build output exists
            if (File.Exists(cs.BuildPath)) {
                dtOutputTimeStamp = File.GetLastWriteTime(cs.BuildPath);
            } else {
                return false;
            }

            // check all of the input files
            foreach (string file in _sourceFiles.Keys) {
                if (dtOutputTimeStamp < File.GetLastWriteTime(file)) {
                    return false;
                }
            }

            // check all culture-neutral resources
            foreach (Resource resource in _resources) {
                // we're only interested in culture neutral resources, as these
                // are the only resources that are embedded in the output assembly
                if (resource.Culture != null) {
                    continue;
                }

                // check if input file was updated since last compile
                if (dtOutputTimeStamp < resource.InputFile.LastWriteTime) {
                    return false;
                }

                // check if compiled resource file exists
                FileInfo compiledResourceFile = resource.GetCompiledResourceFile(solutionConfiguration);
                if (!compiledResourceFile.Exists) {
                    return false;
                }

                // check if compiled resource file is up-to-date
                if (dtOutputTimeStamp < compiledResourceFile.LastWriteTime) {
                    return false;
                }
            }

            // check all of the input references
            foreach (ReferenceBase reference in _references) {
                if (dtOutputTimeStamp < reference.GetTimestamp(solutionConfiguration)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns <see cref="Hashtable" /> containing culture-specific resources.
        /// </summary>
        /// <returns>
        /// A <see cref="Hashtable" /> containing culture-specific resources.
        /// </returns>
        /// <remarks>
        /// The key of the <see cref="Hashtable" /> is <see cref="CultureInfo" />
        /// and the value is an <see cref="ArrayList" /> containing a list of
        /// <see cref="Resource" /> instances for that culture.
        /// </remarks>
        private Hashtable GetCultureSpecificResources() {
            Hashtable cultureResources = new Hashtable();
            foreach (Resource resource in _resources) {
                // ignore resource files NOT associated with a culture
                if (resource.Culture == null) {
                    continue;
                }
                ArrayList resFiles = null;
                if (!cultureResources.ContainsKey(resource.Culture)) {
                    resFiles = new ArrayList();
                    cultureResources.Add(resource.Culture, resFiles);
                } else {
                    resFiles = (ArrayList) cultureResources[resource.Culture];
                }
                resFiles.Add(resource);
            }
            return cultureResources;
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
                    + " missing from the project node.", Location.UnknownLocation);
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

        /// <summary>
        /// Returns the <see cref="ProjectLocation" /> of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="projectNode">XML fragment representing the project to check.</param>
        /// <returns>
        /// The <see cref="ProjectLocation" /> of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The project location could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The project location is invalid.</para>
        /// </exception>
        protected static ProjectLocation GetProjectLocation(XmlNode projectNode) {
            if (projectNode == null) {
                throw new ArgumentNullException("projectNode");
            }

            XmlAttribute projectTypeAttribute = projectNode.Attributes["ProjectType"];
            if (projectTypeAttribute == null) {
                throw new BuildException("The \"ProjectType\" attribute is"
                    + " missing from the project node.", Location.UnknownLocation);
            }

            try {
                return (ProjectLocation) Enum.Parse(typeof(ProjectLocation), 
                    projectTypeAttribute.Value, true);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The value of the \"ProjectType\" attribute ({0}) is not a valid"
                    + " location string.", projectTypeAttribute.Value),
                    Location.UnknownLocation, ex);
            }
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
        private readonly ProjectLocation _projectLocation;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CommandFile = "compile-commands.txt";

        #endregion Private Static Fields
    }

    /// <summary>
    /// Indentifies the different output types of a managed project.
    /// </summary>
    /// <remarks>
    /// Visual Studio .NET does not support modules.
    /// </remarks>
    public enum ManagedOutputType {
        /// <summary>
        /// A class library. 
        /// </summary>
        Library = 1,

        /// <summary>
        /// A console application.
        /// </summary>
        Executable = 2,

        /// <summary>
        /// A Windows program.
        /// </summary>
        WindowsExecutable = 3
    }
}
