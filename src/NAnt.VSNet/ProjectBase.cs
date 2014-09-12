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

using System;
using System.Collections;
using System.Collections.Specialized;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Base class for all project classes.
    /// </summary>
    public abstract class ProjectBase {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBase" /> class.
        /// </summary>
        protected ProjectBase(XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection temporaryFiles, GacCache gacCache, ReferencesResolver referencesResolver, DirectoryInfo outputDir) {
            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }
            if (solutionTask == null) {
                throw new ArgumentNullException("solutionTask");
            }
            if (temporaryFiles == null) {
                throw new ArgumentNullException("temporaryFiles");
            }
            if (gacCache == null) {
                throw new ArgumentNullException("gacCache");
            }
            if (referencesResolver == null) {
                throw new ArgumentNullException("referencesResolver");
            }

            _projectConfigurations = new ConfigurationDictionary();
            _buildConfigurations = new ConfigurationDictionary();
            _extraOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();

            // ensure the specified project is actually supported by this project
            VerifyProjectXml(xmlDefinition);

            _solutionTask = solutionTask;
            _temporaryFiles = temporaryFiles;
            _outputDir = outputDir;
            _gacCache = gacCache;
            _refResolver = referencesResolver;
            _productVersion = DetermineProductVersion(xmlDefinition);
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the Visual Studio product version of the project.
        /// </summary>
        /// <value>
        /// The Visual Studio product version of the project.
        /// </value>
        public ProductVersion ProductVersion {
            get { return _productVersion; }
        }

        /// <summary>
        /// Gets the name of the VS.NET project.
        /// </summary>
        public abstract string Name {
            get;
        }

        /// <summary>
        /// Gets the type of the project.
        /// </summary>
        /// <value>
        /// The type of the project.
        /// </value>
        public abstract ProjectType Type {
            get;
        }

        /// <summary>
        /// Gets the path of the VS.NET project.
        /// </summary>
        public abstract string ProjectPath {
            get;
        }

        /// <summary>
        /// Gets the directory containing the VS.NET project.
        /// </summary>
        public abstract DirectoryInfo ProjectDirectory {
            get;
        }

        /// <summary>
        /// Get the location of the project.
        /// </summary>
        public abstract ProjectLocation ProjectLocation {
            get;
        }

        /// <summary>
        /// Get the directory in which intermediate build output that is not 
        /// specific to the build configuration will be stored.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   For <see cref="T:NAnt.VSNet.ProjectLocation.Local" /> projects, this is defined
        ///   as <c>&lt;Project Directory&lt;\obj</c>.
        ///   </para>
        ///   <para>
        ///   For <see cref="T:NAnt.VSNet.ProjectLocation.Web" /> projects, this is defined
        ///   as <c>%HOMEPATH%\VSWebCache\&lt;Machine Name&gt;\&lt;Project Directory&gt;\obj</c>.
        ///   </para>
        /// </remarks>
        public virtual DirectoryInfo ObjectDir {
            get {
                switch (ProjectLocation) {
                    case ProjectLocation.Web:
                        string webCachePath = FileUtils.CombinePaths(
                            FileUtils.GetHomeDirectory(), "VSWebCache");
                        string machinePath= FileUtils.CombinePaths(webCachePath,
                            Environment.MachineName);
                        string projectPath = FileUtils.CombinePaths(machinePath,
                            Name);
                        return new DirectoryInfo(FileUtils.CombinePaths(projectPath,
                            "obj"));
                    case ProjectLocation.Local:
                        return new DirectoryInfo(
                            FileUtils.CombinePaths(ProjectDirectory.FullName, "obj"));
                    default:
                        throw new NotSupportedException(ProjectLocation.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the VS.NET project.
        /// </summary>
        public abstract string Guid {
            get; 
            set;
        }

        /// <summary>
        /// Gets a list of all configurations defined in the project.
        /// </summary>
        public ConfigurationDictionary ProjectConfigurations {
            get { return _projectConfigurations; }
        }

        /// <summary>
        /// Gets a list of project configurations that can be build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Project configurations that are not in this list do not need to be
        /// compiled.
        /// </para>
        /// </remarks>
        public ConfigurationDictionary BuildConfigurations {
            get { return _buildConfigurations; }
        }

        public abstract ArrayList References {
            get;
        }

        public SolutionTask SolutionTask {
            get { return _solutionTask; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _temporaryFiles; }
        }

        /// <summary>
        /// Gets the extra set of output files for the project.
        /// </summary>
        /// <value>
        /// The extra set of output files for the project.
        /// </value>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public Hashtable ExtraOutputFiles {
            get { return _extraOutputFiles; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        protected DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        protected GacCache GacCache {
            get { return _gacCache; }
        }
        
        public ReferencesResolver ReferencesResolver {
            get { return _refResolver; }
        }

        /// <summary>
        /// Gets the set of projects that the project depends on.
        /// </summary>
        /// <value>
        /// The set of projects that the project depends on.
        /// </value>
        public ProjectBaseCollection ProjectDependencies {
            get { return _projectDependencies; }
        }

        protected virtual string DevEnvDir {
            get {
                string vs7CommonDirKeyName = @"SOFTWARE\Microsoft\VisualStudio\" 
                    + ProductVersionNumber + @"\Setup\VS";
                RegistryKey vs7CommonDirKey = Registry.LocalMachine.OpenSubKey(
                    vs7CommonDirKeyName);
                if (vs7CommonDirKey == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Registry key \"{0}\" could not be found.", vs7CommonDirKeyName),
                        Location.UnknownLocation);
                }

                string vs7CommonDir = vs7CommonDirKey.GetValue("VS7CommonDir") as string;
                if (vs7CommonDir == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Value \"VS7CommonDir\" does not exist in registry key"
                        + " \"{0}\".", vs7CommonDirKeyName), Location.UnknownLocation);
                }
                return FileUtils.CombinePaths(vs7CommonDir, @"IDE\");
            }
        }

        #endregion Protected Instance Properties

        #region Private Instance Properties

        /// <summary>
        /// TODO: refactor this !!!
        /// </summary>
        private Version ProductVersionNumber {
            get {
                switch (ProductVersion) {
                    case ProductVersion.Rainier:
                        return new Version(7, 0);
                    case ProductVersion.Everett:
                        return new Version(7, 1);
                    default:
                        throw new Exception("Invalid product version \"" 
                            + ProductVersion + "\".");
                }
            }
        }

        #endregion Private Instance Properties

        #region Public Instance Methods

        public abstract ProjectReferenceBase CreateProjectReference(
            ProjectBase project, bool isPrivateSpecified, bool isPrivate);

        public bool Compile(Configuration solutionConfiguration) {
            ConfigurationBase projectConfig = BuildConfigurations[solutionConfiguration];
            if (projectConfig == null) {
                Log(Level.Info, "Skipping '{0}' [{1}] ...", Name, solutionConfiguration);
                return true;
            }

            Log(Level.Info, "Building '{0}' [{1}] ...", Name, projectConfig.Name);

            // ensure project-level object directory exists, no need to do this
            // for the project-level output directory as we already this before
            if (!ObjectDir.Exists) {
                ObjectDir.Create();
                ObjectDir.Refresh();
            }

            // build the project
            BuildResult result = Build(solutionConfiguration);
            return (result != BuildResult.Failed);
        }

        public string GetOutputPath(Configuration solutionConfiguration) {
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = BuildConfigurations[solutionConfiguration];
            if (config == null) {
                return null;
            }

            return config.OutputPath;
        }

        public ConfigurationBase GetConfiguration(Configuration solutionConfiguration) {
            // obtain project configuration (corresponding with solution configuration)
            return BuildConfigurations[solutionConfiguration];
        }

        public StringCollection GetAssemblyReferences(Configuration solutionConfiguration) {
            Hashtable uniqueReferences = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (ReferenceBase reference in References) {
                StringCollection references = reference.GetAssemblyReferences(solutionConfiguration);

                // avoid ambiguous references when the same assembly is 
                // referenced multiple times
                //
                // this should only be possible for VB.NET project, as they
                // also include the assemblies referenced by referenced projects
                //
                // Project A references
                //      Assembly 1
                //      Assembly 2
                // Project B references
                //      Assembly 3
                //      Project A
                //
                // then to compile Project B, VB.NET will use the following 
                // assembly references:
                //
                //      Assembly 1
                //      Assembly 2
                //      Assembly 3
                //      Project Output of Project A
                //
                // see bug #1178862
                foreach (string assemblyFile in references) {
                    try {
                        // try to obtain AssemblyName of referenced assembly
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);

                        if (!uniqueReferences.ContainsKey(assemblyName.FullName)) {
                            uniqueReferences.Add(assemblyName.FullName, assemblyFile);
                        }
                    } catch (Exception ex) {
                        // ignore assemblies that cannot be found or loaded
                        Log(Level.Warning, "Referenced assembly \"{0}\" could not"
                            + " be loaded: {1}", assemblyFile, ex.Message);
                    }
                }
            }

            StringCollection assemblyReferences = new StringCollection();
            foreach (DictionaryEntry entry in uniqueReferences) {
                assemblyReferences.Add((string) entry.Value);
            }
            return assemblyReferences;
        }

        /// <summary>
        /// Gets the complete set of output files for the project configuration
        /// matching the specified solution configuration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <param name="outputFiles">The set of output files to be updated.</param>
        /// <remarks>
        ///   <para>
        ///   The key of the case-insensitive <see cref="Hashtable" /> is the 
        ///   full path of the output file and the value is the path relative to
        ///   the output directory.
        ///   </para>
        ///   <para>
        ///   If the project is not configured to be built for the specified
        ///   solution configuration, then no output files are added.
        ///   </para>
        /// </remarks>
        public virtual void GetOutputFiles(Configuration solutionConfiguration, Hashtable outputFiles) {
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = BuildConfigurations[solutionConfiguration];
            if (config == null) {
                // the project is not configured to be built for the specified
                // solution configuration
                return;
            }

            foreach (ReferenceBase reference in References) {
                if (!reference.CopyLocal) {
                    continue;
                }

                // only add output files of reference if we did not add them
                // already
                if (!outputFiles.ContainsKey(config.BuildPath)) {
                    reference.GetOutputFiles(solutionConfiguration, outputFiles);
                }
            }

            // determine output file of project
            string projectOutputFile = config.BuildPath;

            // check if project has output file (eg. NMake project does not 
            // necessarily have an output file)
            if (projectOutputFile != null && File.Exists(projectOutputFile)) {
                // get list of files related to project output file (eg. debug symbols,
                // xml doc, ...), this will include the project output file itself
                ReferenceBase.GetRelatedFiles(projectOutputFile, outputFiles);

                // NOTE:
                //
                // for now we assume that the extra output files do not need
                // to be copied/deployed if there's no main output file or
                // the main output file does not exist (eg. because of a compile
                // error)

                // add extra project-level output files
                foreach (DictionaryEntry de in ExtraOutputFiles) {
                    outputFiles[(string) de.Key] = (string) de.Value;
                }

                // add extra configuration-level output files
                foreach (DictionaryEntry de in config.ExtraOutputFiles) {
                    outputFiles[(string) de.Key] = (string) de.Value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether building the project for the specified
        /// build configuration results in managed output.
        /// </summary>
        /// <param name="configuration">The build configuration.</param>
        /// <returns>
        /// <see langword="true" /> if the project output for the given build
        /// configuration is managed; otherwise, <see langword="false" />.
        /// </returns>
        public abstract bool IsManaged(Configuration configuration);

        #endregion Public Instance Methods

        #region Protected Internal Instance Methods

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro or <see langword="null" /> if the macro is not
        /// supported.
        /// </returns>
        protected internal virtual string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "projectname": // E.g. WindowsApplication1
                    return Name;
                case "projectpath": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\WindowsApplications1.csproj
                    return ProjectPath;
                case "projectfilename": // E.g. WindowsApplication1.csproj
                    return Path.GetFileName(ProjectPath);
                case "projectext": // .csproj
                    return Path.GetExtension(ProjectPath);
                case "projectdir": // ProjectPath without ProjectFileName at the end
                    return Path.GetDirectoryName(ProjectPath) 
                        + Path.DirectorySeparatorChar;
                case "devenvdir":
                    return DevEnvDir;
                default:
                    return null;
            }
        }

        #endregion Protected Internal Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// file.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        protected abstract ProductVersion DetermineProductVersion(XmlElement docElement);

        /// <summary>
        /// Verifies whether the specified XML fragment represents a valid project
        /// that is supported by this <see cref="ProjectBase" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <exception cref="BuildException">
        ///   <para>The XML fragment is not supported by this <see cref="ProjectBase" />.</para>
        ///   <para>-or-</para>
        ///   <para>The XML fragment does not represent a valid project (for this <see cref="ProjectBase" />).</para>
        /// </exception>
        protected abstract void VerifyProjectXml(XmlElement docElement);

        /// <summary>
        /// Prepares the project for being built.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <remarks>
        /// The default implementation will ensure that none of the output files 
        /// are marked read-only.
        /// </remarks>
        protected virtual void Prepare(Configuration solutionConfiguration) {
            // determine the output files of the project
            Hashtable outputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            GetOutputFiles(solutionConfiguration, outputFiles);

            // use the <attrib> task to ensure none of the output files are
            // marked read-only
            AttribTask attribTask = new AttribTask();

            // parent is solution task
            attribTask.Parent = SolutionTask;

            // inherit project from solution task
            attribTask.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            attribTask.NamespaceManager = SolutionTask.NamespaceManager;

            // inherit verbose setting from solution task
            attribTask.Verbose = SolutionTask.Verbose;

            // only output warning messages or higher, unless 
            // we're running in verbose mode
            if (!attribTask.Verbose) {
                attribTask.Threshold = Level.Warning;
            }

            // make sure framework specific information is set
            attribTask.InitializeTaskConfiguration();

            // set parent of child elements
            attribTask.AttribFileSet.Parent = attribTask;

            // inherit project for child elements from containing task
            attribTask.AttribFileSet.Project = attribTask.Project;

            // inherit namespace manager from containing task
            attribTask.AttribFileSet.NamespaceManager = attribTask.NamespaceManager;

            // we want to reset the read-only attribute of all output files
            attribTask.ReadOnlyAttrib = false;

            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = BuildConfigurations[solutionConfiguration];

            // add all output files to the <attrib> fileset
            foreach (DictionaryEntry de in outputFiles) {
                attribTask.AttribFileSet.Includes.Add(FileUtils.CombinePaths(
                    config.OutputDir.FullName, (string) de.Value));
            }

            // increment indentation level
            attribTask.Project.Indent();

            try {
                // execute task
                attribTask.Execute();
            } finally {
                // restore indentation level
                attribTask.Project.Unindent();
            }
        }

        protected abstract BuildResult Build(Configuration solutionConfiguration);

        /// <summary>
        /// Copies the specified file if the destination file does not exist, or
        /// the source file has been modified since it was previously copied.
        /// </summary>
        /// <param name="srcFile">The file to copy.</param>
        /// <param name="destFile">The destination file.</param>
        /// <param name="parent">The <see cref="Task" /> in which context the operation will be performed.</param>
        protected void CopyFile(FileInfo srcFile, FileInfo destFile, Task parent) {
            // create instance of Copy task
            CopyTask ct = new CopyTask();

            // parent is solution task
            ct.Parent = parent;

            // inherit project from parent task
            ct.Project = parent.Project;

            // inherit namespace manager from parent task
            ct.NamespaceManager = parent.NamespaceManager;

            // inherit verbose setting from parent task
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

        protected bool ExecuteBuildEvent(string buildEvent, string buildCommandLine, string batchFile, string workingDirectory, ConfigurationBase config) {
            // create the batch file
            using (StreamWriter sw = new StreamWriter(batchFile)) {
                sw.WriteLine("@echo off");
                // replace any VS macros in the command line with real values
                buildCommandLine = config.ExpandMacros(buildCommandLine);
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

            // execute the batch file
            ProcessStartInfo psi = new ProcessStartInfo(batchFile);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true; // For logging
            psi.WorkingDirectory = workingDirectory;
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

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        protected void Log(Level messageLevel, string message) {
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
        protected void Log(Level messageLevel, string message, params object[] args) {
            if (SolutionTask != null) {
                SolutionTask.Log(messageLevel, message, args);
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Fields

        private readonly ProductVersion _productVersion;
        private readonly SolutionTask _solutionTask;
        private readonly TempFileCollection _temporaryFiles;
        private readonly DirectoryInfo _outputDir;
        private readonly ConfigurationDictionary _projectConfigurations;
        private readonly ConfigurationDictionary _buildConfigurations;
        private readonly GacCache _gacCache;
        private readonly ReferencesResolver _refResolver;
        private readonly Hashtable _extraOutputFiles;
        private readonly ProjectBaseCollection _projectDependencies = new ProjectBaseCollection();

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Specifies the type of the project.
    /// </summary>
    public enum ProjectType {
        /// <summary>
        /// A Visual Basic.NET project.
        /// </summary>
        VB = 0,

        /// <summary>
        /// A Visual C# project.
        /// </summary>
        CSharp = 1,

        /// <summary>
        /// A Visual C++ project.
        /// </summary>
        VisualC = 2,

        /// <summary>
        /// A Visual J# project.
        /// </summary>
        JSharp = 3,

        /// <summary>
        /// MSBuild project.
        /// </summary>
        MSBuild = 4
    }

    /// <summary>
    /// Specifies the result of the build.
    /// </summary>
    public enum BuildResult 
    {
        /// <summary>
        /// The build failed.
        /// </summary>
        Failed = 0,
        /// <summary>
        /// The build succeeded.
        /// </summary>
        Success = 1,
        /// <summary>
        /// The build succeeded and the output was updated.
        /// </summary>
        SuccessOutputUpdated = 2,
    }

    public enum ProductVersion {
        /// <summary>
        /// Visual Studio.NET 2002
        /// </summary>
        Rainier = 70,

        /// <summary>
        /// Visual Studio.NET 2003
        /// </summary>
        Everett = 71,

        /// <summary>
        /// Visual Studio 2005
        /// </summary>
        Whidbey = 80,

        /// <summary>
        /// Visual Studio 2008
        /// </summary>
        Orcas = 90,

        /// <summary>
        /// Visual Studio 2010
        /// </summary>
        Rosario = 100,
    }

    /// <summary>
    /// Indentifies the physical location of a managed project.
    /// </summary>
    public enum ProjectLocation {
        /// <summary>
        /// A local project.
        /// </summary>
        Local = 1,

        /// <summary>
        /// A web project.
        /// </summary>
        Web = 2,
    }

    /// <summary>
    /// Contains a collection of <see cref="ProjectBase" /> elements.
    /// </summary>
    [Serializable()]
    public class ProjectBaseCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBaseCollection"/> class.
        /// </summary>
        public ProjectBaseCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBaseCollection"/> class
        /// with the specified <see cref="ProjectBaseCollection"/> instance.
        /// </summary>
        public ProjectBaseCollection(ProjectBaseCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBaseCollection"/> class
        /// with the specified array of <see cref="ProjectBase"/> instances.
        /// </summary>
        public ProjectBaseCollection(ProjectBase[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public ProjectBase this[int index] {
            get { return (ProjectBase) base.List[index]; }
            set { base.List[index] = value; }
        }

        /// <summary>
        /// Gets the <see cref="ProjectBase"/> with the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the <see cref="ProjectBase"/> to get.</param>
        /// <remarks>
        /// Performs a case-insensitive lookup.
        /// </remarks>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public ProjectBase this[string guid] {
            get {
                if (guid == null) {
                    throw new ArgumentNullException("guid");
                }

                // try to locate instance by guid (case-insensitive)
                foreach (ProjectBase project in base.List) {
                    if (string.Compare(project.Guid, guid, true, CultureInfo.InvariantCulture) == 0) {
                        return project;
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="ProjectBase"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectBase"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(ProjectBase item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProjectBase"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="ProjectBase"/> elements to be added to the end of the collection.</param> 
        public void AddRange(ProjectBase[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProjectBaseCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="ProjectBaseCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(ProjectBaseCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="ProjectBase"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectBase"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(ProjectBase item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="ProjectBase"/> with the specified
        /// GUID is in the collection, using a case-insensitive lookup.
        /// </summary>
        /// <param name="value">The GUID to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="ProjectBase" /> with GUID 
        /// <paramref name="value"/> is found in the collection; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        public bool Contains(string value) {
            return this[value] != null;
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(ProjectBase[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="ProjectBase"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectBase"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="ProjectBase"/>. If the <see cref="ProjectBase"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(ProjectBase item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="ProjectBase"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="ProjectBase"/> to insert.</param>
        public void Insert(int index, ProjectBase item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ProjectBaseEnumerator"/> for the entire collection.
        /// </returns>
        public new ProjectBaseEnumerator GetEnumerator() {
            return new ProjectBaseEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectBase"/> to remove from the collection.</param>
        public void Remove(ProjectBase item) {
            base.List.Remove(item);
        }
        
        /// <summary>
        /// Remove items with the specified guid from the collection.
        /// </summary>
        /// <param name="guid">The guid of the project to remove from the collection.</param>
        public void Remove(string guid) {
            ProjectBase removeProject = null;

            // try to locate instance by guid (case-insensitive)
            foreach (ProjectBase project in base.List) {
                if (string.Compare(project.Guid, guid, true, CultureInfo.InvariantCulture) == 0) {
                    removeProject = project;
                    break;
                }
            }

            if (removeProject != null) {
                base.InnerList.Remove(removeProject);
            }
        }

        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="ProjectBase"/> elements of a <see cref="ProjectBaseCollection"/>.
    /// </summary>
    public class ProjectBaseEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBaseEnumerator"/> class
        /// with the specified <see cref="ProjectBaseCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal ProjectBaseEnumerator(ProjectBaseCollection arguments) {
            IEnumerable temp = (IEnumerable) (arguments);
            _baseEnumerator = temp.GetEnumerator();
        }

        #endregion Internal Instance Constructors

        #region Implementation of IEnumerator
            
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        public ProjectBase Current {
            get { return (ProjectBase) _baseEnumerator.Current; }
        }

        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced 
        /// to the next element; <see langword="false" /> if the enumerator has 
        /// passed the end of the collection.
        /// </returns>
        public bool MoveNext() {
            return _baseEnumerator.MoveNext();
        }

        bool IEnumerator.MoveNext() {
            return _baseEnumerator.MoveNext();
        }
            
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the 
        /// first element in the collection.
        /// </summary>
        public void Reset() {
            _baseEnumerator.Reset();
        }
            
        void IEnumerator.Reset() {
            _baseEnumerator.Reset();
        }

        #endregion Implementation of IEnumerator

        #region Private Instance Fields
    
        private IEnumerator _baseEnumerator;

        #endregion Private Instance Fields
    }
}
