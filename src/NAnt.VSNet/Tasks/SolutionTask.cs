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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.VSNet.Types;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Compiles VS.NET solutions (or sets of projects), automatically determining project dependencies from inter-project references.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This task will analyze each of the given .csproj or .vbproj files and
    /// build them in the proper order.  It supports reading solution files, as well
    /// as enterprise template projects.
    /// </para>
    /// <para>
    /// This task also supports the model of referencing projects by their
    /// output filenames, rather than referencing them inside the solution.  It will
    /// automatically detect the existance of a file reference and convert it to a 
    /// project reference.  For example, if project A references the file in the
    /// release output directory of project B, the solution task will automatically convert
    /// this to a project dependency on project B and will reference the appropriate configuration output
    /// directory at the final build time (ie: reference the debug version of B if the solution is built as debug).
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Compiles all of the projects in <c>test.sln</c>, in relase mode, in the proper order.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release" solutionfile="test.sln">
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles all of the projects in <c>projects.txt</c>, in the proper order.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release">
    ///        <projects>
    ///            <includesList name="projects.txt" />
    ///        </projects>
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles projects A, B and C, using the output of project X as a reference.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release">
    ///        <projects>
    ///            <includes name="A\A.csproj" />
    ///            <includes name="B\b.vbproj" />
    ///            <includes name="C\c.csproj" />
    ///        </projects>
    ///     <referenceprojects>
    ///         <includes name="X\x.csproj" />
    ///     </referenceprojects>
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles all of the projects in the solution except for project A.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution solutionfile="test.sln" configuration="release">
    ///        <excludeprojects>
    ///            <includes name="A\A.csproj" />
    ///        </excludeprojects>
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles all of the projects in the solution mapping the project at
    ///   http://localhost/A/A.csproj to c:\inetpub\wwwroot\A\A.csproj.  This allows
    ///   the build to work without WebDAV.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution solutionfile="test.sln" configuration="release">
    ///        <webmap>
    ///            <map url="http://localhost/A/A.csproj c:\inetpub\wwwroot\A\A.csproj" />
    ///        </webmap>
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles all of the projects in the solution placing compiled outputs in c:\temp.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution solutionfile="test.sln" configuration="release" outputdir="c:\temp"/>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("solution")]
    public class SolutionTask : Task {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionTask" /> class.
        /// </summary>
        public SolutionTask() {
            _projects = new FileSet();
            _referenceProjects = new FileSet();
            _excludeProjects = new FileSet();
            _assemblyFolders = new FileSet();
            _webMaps = new WebMapCollection();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The projects to build.
        /// </summary>
        [FileSet("projects", Required=false)]
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
        [FileSet("referenceprojects", Required=false)]
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
        public string SolutionFile {
            get { return (_solutionFile != null) ? Project.GetFullPath(_solutionFile) : null; }
            set { 
                if (!StringUtils.IsNullOrEmpty(value)) {
                    _solutionFile = value.ToLower();
                } else {
                    _solutionFile = null;
                }
            }
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
        public string Configuration {
            get { return _configuration; }
            set { _configuration = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The directory where compiled targets will be placed.  This
        /// overrides path settings contained in the solution/project.
        /// </summary>
        [TaskAttribute("outputdir", Required=false)]
        public string OutputDir {
            get { return _outputDir; }
            set { _outputDir = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// WebMap of URL's to project references.
        /// </summary>
        [BuildElementCollection("webmap", Required=false)]
        public WebMapCollection WebMaps {
            get { return _webMaps; }
        }

        /// <summary>
        /// Fileset of projects to exclude.
        /// </summary>
        [FileSet("excludeprojects", Required=false)]
        public FileSet ExcludeProjects {
            get { return _excludeProjects; }
            set { _excludeProjects = value; }
        }

        /// <summary>
        /// Set of folders where references are searched when not found in path 
        /// from project file (HintPath).
        /// </summary>
        [FileSet("assemblyfolders", Required = false)]
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
        /// Set of folders where references are searched when not found in path 
        /// from project file (HintPath) or <see cref="AssemblyFolders" />.
        /// </summary>
        public FileSet DefaultAssemlyFolders {
            get {
                if (IncludeVSFolders) {
                    return FindDefaultAssemblyFolders();
                } else {
                    return new FileSet();
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            Log(Level.Info, LogPrefix + "Starting solution build.");

            Solution sln;

            if (Projects.FileNames.Count > 0) {
                Log(Level.Verbose, LogPrefix + "Included projects:" );
                foreach (string projectFile in Projects.FileNames) {
                    Log(Level.Verbose, LogPrefix + " - " + projectFile);
                }
            }

            if (ReferenceProjects.FileNames.Count > 0) {
                Log(Level.Verbose, LogPrefix + "Reference projects:");
                foreach (string projectFile in ReferenceProjects.FileNames) {
                    Log(Level.Verbose, LogPrefix + " - " + projectFile);
                }
            }
            
            string basePath = null;

            try {
                using (TempFileCollection tfc = new TempFileCollection()) {
                    if (SolutionFile == null) {
                        sln = new Solution(new ArrayList(Projects.FileNames), new ArrayList(ReferenceProjects.FileNames), tfc, 
                            this, WebMaps, ExcludeProjects, OutputDir);
                    } else {
                        sln = new Solution(SolutionFile, new ArrayList(Projects.FileNames), 
                            new ArrayList(ReferenceProjects.FileNames), tfc, this, WebMaps, ExcludeProjects, OutputDir);
                    }

                    if (!sln.Compile(Configuration, new ArrayList(), null, Verbose, false)) {
                        throw new BuildException("Project build failed.", Location);
                    }
                
                    basePath = tfc.BasePath;
                }
            } finally {
                if (basePath != null) {
                    Log(Level.Debug, LogPrefix + "Cleaning up temp folder {0}.", basePath); 

                    // force all files to have normal attributes to allow deletion
                    DirectoryInfo di = new DirectoryInfo(basePath);
                    foreach (FileInfo info in di.GetFiles()) {
                        if (info.Attributes != FileAttributes.Normal) {
                            Log(Level.Debug, LogPrefix + "File {0} has other than normal attributes.  Fixing.", info.FullName);
                            File.SetAttributes(info.FullName, FileAttributes.Normal);
                        }
                    }

                    // delete directory recursively
                    Directory.Delete(basePath, true);
                }
            }
        }

        protected override void InitializeTask(XmlNode taskNode) {
            if (SolutionFile != null) {
                if (!File.Exists(SolutionFile)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Couldn't find solution file '{0}'.", SolutionFile), Location);
                }
            }

            base.InitializeTask (taskNode);
        }


        #endregion Override implementation of Task

        #region Private Instance Methods

        private void ScanRegistryForAssemblyFolders(RegistryKey mainKey, FileSet fsFolders) {
            if (mainKey == null) {
                return;
            }

            foreach (string subkey in mainKey.GetSubKeyNames()) {
                RegistryKey subKey = mainKey.OpenSubKey(subkey);
                string val = subKey.GetValue(string.Empty).ToString();

                fsFolders.Includes.Add(val);
            }
        }

        private FileSet FindDefaultAssemblyFolders() {
            FileSet fsFolders = new FileSet();

            try {
                ScanRegistryForAssemblyFolders(Registry.CurrentUser.OpenSubKey(VS71AssemblyFolders), fsFolders);
                ScanRegistryForAssemblyFolders(Registry.LocalMachine.OpenSubKey(VS71AssemblyFolders), fsFolders);
                ScanRegistryForAssemblyFolders(Registry.CurrentUser.OpenSubKey(VS70AssemblyFolders), fsFolders);
                ScanRegistryForAssemblyFolders(Registry.LocalMachine.OpenSubKey(VS70AssemblyFolders), fsFolders);
            } catch (NotImplementedException) {
                // ignore this exception, as Mono currently has no implementation 
                // for registry related classes

                // TO-DO : make sure we remove this in the future if possible
            }

            return fsFolders;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _solutionFile;
        private string _configuration;
        private string _outputDir;
        private FileSet _projects;
        private FileSet _referenceProjects;
        private FileSet _excludeProjects;
        private FileSet _assemblyFolders;
        private WebMapCollection _webMaps;
        private bool _includeVSFolders;


        #endregion Private Instance Fields

        #region Private Static Fields

        private const string VS71AssemblyFolders = @"SOFTWARE\Microsoft\VisualStudio\7.1\AssemblyFolders";
        private const string VS70AssemblyFolders = @"SOFTWARE\Microsoft\VisualStudio\7.0\AssemblyFolders";

        #endregion Private Static Fields
    }
}
