// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Matthew Mastracci (mmastrac at users.sourceforge.net)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.IO;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Extensibility;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.VSNet.Extensibility;
using NAnt.VSNet.Types;

using System.Security;
using System.Security.Permissions;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Compiles VS.NET solutions (or sets of projects), automatically determining 
    /// project dependencies from inter-project references.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This task support the following projects:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Visual Basic .NET</description>
    ///     </item>
    ///     <item>
    ///         <description>Visual C# .NET</description>
    ///     </item>
    ///     <item>
    ///         <description>Visual J# .NET</description>
    ///     </item>
    ///     <item>
    ///         <description>Visual C++ .NET</description>
    ///     </item>
    /// </list>
    /// <note>
    /// Right now, only Microsoft Visual Studio .NET 2002 and 2003 solutions
    /// and projects are supported.  Support for .NET Compact Framework projects
    /// is also not available at this time.
    /// </note>
    /// <para>
    /// The <see cref="SolutionTask" /> also supports the model of referencing 
    /// projects by their output filenames, rather than referencing them inside 
    /// the solution.  It will automatically detect the existance of a file 
    /// reference and convert it to a project reference.  For example, if project
    /// &quot;A&quot; references the file in the release output directory of 
    /// project &quot;B&quot;, the <see cref="SolutionTask" /> will automatically 
    /// convert this to a project dependency on project &quot;B&quot; and will 
    /// reference the appropriate configuration output directory at the final 
    /// build time (ie: reference the debug version of &quot;B&quot; if the 
    /// solution is built as debug).
    /// </para>
    /// <note>
    /// The <see cref="SolutionTask" />  expects all project files to be valid
    /// XML files.
    /// </note>
    /// <h3>Resx Files</h3>
    /// <para>
    /// When building a project for a down-level target framework, special care
    /// should be given to resx files. Resx files (can) contain references to 
    /// a specific version of CLR types, and as such are only upward compatible.
    /// </para>
    /// <para>
    /// For example: if you want to be able to build a project both as a .NET 1.0 
    /// and .NET 1.1 assembly, the resx files should only contain references to 
    /// .NET 1.0 CLR types. Failure to do this may result in a <see cref="InvalidCastException" />
    /// failure at runtime on machines with only the .NET Framework 1.0 installed.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Compiles all of the projects in <c>test.sln</c>, in release mode, in 
    ///   the proper order.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <solution configuration="release" solutionfile="test.sln" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compiles all of the projects in <c>projects.txt</c>, in the proper 
    ///   order.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <solution configuration="release">
    ///     <projects>
    ///         <includesfile name="projects.txt" />
    ///    </projects>
    /// </solution>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compiles projects A, B and C, using the output of project X as a 
    ///   reference.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <solution configuration="release">
    ///     <projects>
    ///         <include name="A\A.csproj" />
    ///         <include name="B\b.vbproj" />
    ///         <include name="C\c.csproj" />
    ///     </projects>
    ///     <referenceprojects>
    ///         <include name="X\x.csproj" />
    ///     </referenceprojects>
    /// </solution>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compiles all of the projects in the solution except for project A.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <solution solutionfile="test.sln" configuration="release">
    ///     <excludeprojects>
    ///         <include name="A\A.csproj" />
    ///     </excludeprojects>
    /// </solution>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compiles all of the projects in the solution mapping the specific project at
    ///   http://localhost/A/A.csproj to c:\inetpub\wwwroot\A\A.csproj and any URLs under
    ///   http://localhost/B/[remainder] to c:\other\B\[remainder].  This allows the build 
    ///   to work without WebDAV.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <solution solutionfile="test.sln" configuration="release">
    ///     <webmap>
    ///         <map url="http://localhost/A/A.csproj" path="c:\inetpub\wwwroot\A\A.csproj" />
    ///         <map url="http://localhost/B" path="c:\other\B" />
    ///     </webmap>
    /// </solution>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compiles all of the projects in the solution placing compiled outputs 
    ///   in <c>c:\temp</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <solution solutionfile="test.sln" configuration="release" outputdir="c:\temp" />
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [TaskName("solution")]
    [PluginConsumer(typeof(IProjectBuildProvider))]
    [PluginConsumer(typeof(ISolutionBuildProvider))]
    public class SolutionTask : Task, IPluginConsumer {

        /// <summary>
        /// Private var containing custom properties.
        /// </summary>
        ArrayList _customproperties;

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionTask" /> class.
        /// </summary>
        public SolutionTask() {
            _customproperties = new ArrayList();
            _projects = new FileSet();
            _referenceProjects = new FileSet();
            _excludeProjects = new FileSet();
            _assemblyFolders = new FileSet();
            _webMaps = new WebMapCollection();
            _projectFactory = ProjectFactory.Create(this);
            _solutionFactory = SolutionFactory.Create();
            _configuration = new Configuration ();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The projects to build.
        /// </summary>
        [BuildElement("projects", Required=false)]
        public FileSet Projects {
            get { return _projects; }
            set { _projects = value; }
        }

        /// <summary>
        /// The projects to scan, but not build.
        /// </summary>
        /// <remarks>
        /// These projects are used to resolve project references and are 
        /// generally external to the solution being built. References to 
        /// these project's output files are converted to use the appropriate 
        /// solution configuration at build time.
        /// </remarks>
        [BuildElement("referenceprojects", Required=false)]
        public FileSet ReferenceProjects {
            get { return _referenceProjects; }
            set { _referenceProjects = value; }
        }

        /// <summary>
        /// The name of the VS.NET solution file to build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Projects" /> can be used instead to supply a list 
        /// of Visual Studio.NET projects that should be built.
        /// </para>
        /// </remarks>
        [TaskAttribute("solutionfile", Required=false)]
        public FileInfo SolutionFile {
            get { return _solutionFile; }
            set { _solutionFile = value; }
        }

        /// <summary>
        /// The name of the solution configuration to build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Generally <c>release</c> or <c>debug</c>.  Not case-sensitive.
        /// </para>
        /// </remarks>
        [TaskAttribute("configuration", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Configuration {
            get { return _configuration.Name; }
            set { _configuration.Name = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The name of platform to build the solution for.
        /// </summary>
        [TaskAttribute("platform", Required=false)]
        [StringValidator(AllowEmpty=true)]
        public string Platform {
            get { return _configuration.Platform; }
            set { _configuration.Platform = value; }
        }

        /// <summary>
        /// Gets the solution configuration to build.
        /// </summary>
        public Configuration SolutionConfig {
            get { return _configuration; }
        }

        /// <summary>
        /// The directory where compiled targets will be placed.  This
        /// overrides path settings contained in the solution/project.
        /// </summary>
        [TaskAttribute("outputdir", Required=false)]
        public DirectoryInfo OutputDir {
            get { return _outputDir; }
            set { _outputDir = value; }
        }

        /// <summary>
        /// WebMap of URL's to project references.
        /// </summary>
        [BuildElementCollection("webmap", "map")]
        public WebMapCollection WebMaps {
            get { return _webMaps; }
        }

        /// <summary>
        /// Fileset of projects to exclude.
        /// </summary>
        [BuildElement("excludeprojects", Required=false)]
        public FileSet ExcludeProjects {
            get { return _excludeProjects; }
            set { _excludeProjects = value; }
        }

        /// <summary>
        /// Set of folders where references are searched when not found in path 
        /// from project file (HintPath).
        /// </summary>
        [BuildElement("assemblyfolders", Required=false)]
        public FileSet AssemblyFolders {
            get { return _assemblyFolders; }
            set { _assemblyFolders = value; }
        }

        /// <summary>
        /// Includes Visual Studio search folders in reference search path.
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("includevsfolders")]
        [BooleanValidator()]
        public bool IncludeVSFolders {
            get { return _includeVSFolders; }
            set { _includeVSFolders = value; }
        }

        /// <summary>
        /// Allow the task to use WebDAV for retrieving/compiling the projects within solution.  Use of 
        /// <see cref="WebMap" /> is preferred over WebDAV.  The default is <see langword="false" />.
        /// </summary>
        /// <remarks>
        ///     <para>WebDAV support requires permission changes to be made on your project server.  These changes may affect          
        ///     the security of the server and should not be applied to a public installation.</para>
        ///     <para>Consult your web server or the NAnt Wiki documentation for more information.</para>
        /// </remarks>
        [TaskAttribute("enablewebdav", Required = false)]
        [BooleanValidator()]
        public bool EnableWebDav {
            get { return _enableWebDav; }
            set { _enableWebDav = value; }
        }

        /// <summary>
        /// Set of properties set at solution level. Builders for projects in solution may or may not use them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TODO: some documentataion which properties could be defined here.
        /// </para>
        /// </remarks>
        [BuildElementArray("property", ElementType = typeof(PropertyTask))]
        public ArrayList CustomProperties {
            get { return _customproperties; }
        }

        /// <summary>
        /// Gets the list of folders to scan for assembly references.
        /// </summary>
        /// <value>
        /// The list of folders to scan for assembly references.
        /// </value>
        public StringCollection AssemblyFolderList {
            get {
                if (_assemblyFolderList == null) {
                    _assemblyFolderList = new StringCollection();
                    foreach (string folder in AssemblyFolders.DirectoryNames) {
                        if (!_assemblyFolderList.Contains(folder)) {
                            _assemblyFolderList.Add(folder);
                            Log(Level.Debug, "Added \"{0}\" to AssemblyFolders.",
                                folder);
                        }
                    }

                    if (IncludeVSFolders) {
                        StringCollection vsAssemblyFolders = BuildAssemblyFolders();
                        foreach (string folder in vsAssemblyFolders) {
                            if (!_assemblyFolderList.Contains(folder)) {
                                _assemblyFolderList.Add(folder);
                                Log(Level.Debug, "Added \"{0}\" to AssemblyFolders.",
                                    folder);
                            }
                        }
                    }
                }

                return _assemblyFolderList;
            }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        internal ProjectFactory ProjectFactory {
            get { return _projectFactory; }
        }

        internal SolutionFactory SolutionFactory {
            get { return _solutionFactory; }
        }

        #endregion Internal Instance Properties

        #region Implementation of IPluginConsumer

        void IPluginConsumer.ConsumePlugin(IPlugin plugin) {
            if (plugin is IProjectBuildProvider)
                ProjectFactory.RegisterProvider((IProjectBuildProvider) plugin);
            if (plugin is ISolutionBuildProvider)
                SolutionFactory.RegisterProvider((ISolutionBuildProvider) plugin);
        }

        #endregion Implementation of IPluginConsumer

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">
        /// Project build failed.
        /// </exception>
        protected override void ExecuteTask() {
            Log(Level.Info, "Starting solution build.");

            if (SolutionFile != null) {
                if (!SolutionFile.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find solution file '{0}'.", SolutionFile.FullName), 
                        Location);
                }
            }

            if (Projects.FileNames.Count > 0) {
                Log(Level.Verbose, "Included projects:" );
                foreach (string projectFile in Projects.FileNames) {
                    Log(Level.Verbose, " - {0}", projectFile);
                }
            }

            if (ReferenceProjects.FileNames.Count > 0) {
                Log(Level.Verbose, "Reference projects:");
                foreach (string projectFile in ReferenceProjects.FileNames) {
                    Log(Level.Verbose, " - {0}", projectFile);
                }
            }
            
            string basePath = null;

            try {
                using (TempFileCollection tfc = new TempFileCollection()) {
                    // store the temp dir so we can clean it up later
                    basePath = tfc.BasePath;

                    // ensure temp directory exists
                    if (!Directory.Exists(tfc.BasePath)) {
                        Directory.CreateDirectory(tfc.BasePath);
                    }

                    // create temporary domain
                    PermissionSet tempDomainPermSet = new PermissionSet(PermissionState.Unrestricted);
                    
                    AppDomain temporaryDomain = AppDomain.CreateDomain("temporaryDomain", AppDomain.CurrentDomain.Evidence, 
                        AppDomain.CurrentDomain.SetupInformation, tempDomainPermSet);

                    try {
                        ReferencesResolver referencesResolver =
                            ((ReferencesResolver) temporaryDomain.CreateInstanceFrom(Assembly.GetExecutingAssembly().Location,
                            typeof(ReferencesResolver).FullName).Unwrap());

                        using (GacCache gacCache = new GacCache(this.Project)) {
                            SolutionBase sln = SolutionFactory.LoadSolution(this, 
                                tfc, gacCache, referencesResolver);
                            if (!sln.Compile(_configuration)) {
                                throw new BuildException("Project build failed.", Location);
                            }
                        }
                    } finally {
                        // unload temporary domain
                        AppDomain.Unload(temporaryDomain);
                    }
                }
            } finally {
                if (basePath != null && Directory.Exists(basePath)) {
                    Log(Level.Debug, "Cleaning up temp folder '{0}'.", basePath); 

                    // delete temporary directory and all files in it
                    DeleteTask deleteTask = new DeleteTask();
                    deleteTask.Project = Project;
                    deleteTask.Parent = this;
                    deleteTask.InitializeTaskConfiguration();
                    deleteTask.Directory = new DirectoryInfo(basePath);
                    deleteTask.Threshold = Level.None; // no output in build log
                    deleteTask.Execute();
                }
            }
        }

        #endregion Override implementation of Task

        #region Internal Instance Methods

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro or <see langword="null" /> if the macro is not
        /// supported.
        /// </returns>
        /// <exception cref="BuildException">The macro cannot be expanded.</exception>
        internal string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "solutionfilename": // E.g. WindowsApplication1.sln
                    if (SolutionFile != null) {
                        return Path.GetFileName(SolutionFile.FullName);
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded, no solution file specified.", 
                            macro), Location.UnknownLocation);
                    }
                case "solutionpath": // Absolute path for SolutionFileName
                    if (SolutionFile != null) {
                        return SolutionFile.FullName;
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded, no solution file specified.", 
                            macro), Location.UnknownLocation);
                    }
                case "solutiondir": // SolutionPath without SolutionFileName appended
                    if (SolutionFile != null) {
                        return Path.GetDirectoryName(SolutionFile.FullName) 
                            + Path.DirectorySeparatorChar;
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded, no solution file specified.", 
                            macro), Location.UnknownLocation);
                    }
                case "solutionname": // E.g. WindowsApplication1
                    if (SolutionFile != null) {
                        return Path.GetFileNameWithoutExtension(
                            SolutionFile.FullName);
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded, no solution file specified.", 
                            macro), Location.UnknownLocation);
                    }
                case "solutionext": // Is this ever anything but .sln?
                    if (SolutionFile != null) {
                        return Path.GetExtension(SolutionFile.FullName);
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded, no solution file specified.", 
                            macro), Location.UnknownLocation);
                    }
                default:
                    return null;
            }
        }

        #endregion Internal Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Builds the list of folders that should be scanned for assembly 
        /// references.
        /// </summary>
        /// <returns>
        /// The list of folders that should be scanned for assembly references.
        /// </returns>
        private StringCollection BuildAssemblyFolders() {
            StringCollection folderList = new StringCollection();

            // determine version of Visual Studio .NET corresponding with 
            // current target framework
            Version visualStudioVersion = Project.TargetFramework.VisualStudioVersion;

            // check HKCU
            BuildVisualStudioAssemblyFolders(folderList, Registry.CurrentUser, 
                visualStudioVersion.ToString(2));
            // check HKLM
            BuildVisualStudioAssemblyFolders(folderList, Registry.LocalMachine, 
                visualStudioVersion.ToString(2));

            // check HKCU for .NET Framework AssemblyFolders
            BuildDotNetAssemblyFolders(folderList, Registry.CurrentUser);
            // check HKLM for .NET Framework AssemblyFolders
            BuildDotNetAssemblyFolders(folderList, Registry.LocalMachine);

            return folderList;
        }

        private void BuildVisualStudioAssemblyFolders(StringCollection folderList, RegistryKey hive, string visualStudioVersion) {
            RegistryKey assemblyFolders = hive.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\" 
                + visualStudioVersion + @"\AssemblyFolders");
            if (assemblyFolders == null) {
                return;
            }
             
            string[] subKeyNames = assemblyFolders.GetSubKeyNames();
            foreach (string subKeyName in subKeyNames) {
                RegistryKey subKey = assemblyFolders.OpenSubKey(subKeyName);
                string folder = subKey.GetValue(string.Empty) as string;
                if (folder != null && !folderList.Contains(folder)) {
                    folderList.Add(folder);
                }
            }
        }

        private void BuildDotNetAssemblyFolders(StringCollection folderList, RegistryKey hive) {
            RegistryKey assemblyFolders = hive.OpenSubKey(@"SOFTWARE\Microsoft\"
                + @".NETFramework\AssemblyFolders");
            if (assemblyFolders == null) {
                return;
            }
             
            string[] subKeyNames = assemblyFolders.GetSubKeyNames();
            foreach (string subKeyName in subKeyNames) {
                RegistryKey subKey = assemblyFolders.OpenSubKey(subKeyName);
                string folder = subKey.GetValue(string.Empty) as string;
                if (folder != null && !folderList.Contains(folder)) {
                    folderList.Add(folder);
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private FileInfo _solutionFile;
        private Configuration _configuration;
        private DirectoryInfo _outputDir;
        private FileSet _projects;
        private FileSet _referenceProjects;
        private FileSet _excludeProjects;
        private FileSet _assemblyFolders;
        private StringCollection _assemblyFolderList;
        private WebMapCollection _webMaps;
        private bool _includeVSFolders = true;
        private bool _enableWebDav;
        private readonly SolutionFactory _solutionFactory;
        private readonly ProjectFactory _projectFactory;

        #endregion Private Instance Fields
    }
}
