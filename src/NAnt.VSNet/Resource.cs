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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using NAnt.Core.Util;
using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Resource {
        #region Public Instance Constructors

        public Resource(ManagedProjectBase project, FileInfo resourceSourceFile, string resourceSourceFileRelativePath, string dependentFile, SolutionTask solutionTask, GacCache gacCache) {
            _project = project;
            _resourceSourceFile = resourceSourceFile;
            _resourceSourceFileRelativePath = resourceSourceFileRelativePath;
            _dependentFile = dependentFile;
            _solutionTask = solutionTask;
            _culture = CompilerBase.GetResourceCulture(resourceSourceFile.FullName, 
                dependentFile);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public CultureInfo Culture {
            get { return _culture; }
        }

        /// <summary>
        /// Gets a <see cref="FileInfo" /> representing the physical location
        /// of the resource file.
        /// </summary>
        public FileInfo InputFile {
            get { return _resourceSourceFile; }
        }

        public ManagedProjectBase Project {
            get { return _project; }
        }

        /// <summary>
        /// Gets a <see cref="FileInfo" /> representing the logical location
        /// of the resource file in the project.
        /// </summary>
        /// <remarks>
        /// When the resource file is not linked, this matches the
        /// <see cref="InputFile" />.
        /// </remarks>
        public FileInfo LogicalFile {
            get {
                return new FileInfo(FileUtils.CombinePaths(Path.GetDirectoryName(
                    Project.ProjectPath), _resourceSourceFileRelativePath));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the resource is in fact a ResX file.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the resource is a ResX file; otherwise,
        /// <see langword="false" />.
        /// </value>
        public bool IsResX {
            get { 
                return InputFile.Extension.Equals(".resx", StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Compiles the resource file.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// A <see cref="FileInfo" /> representing the compiled resource file.
        /// </returns>
        public FileInfo Compile(Configuration solutionConfiguration) {
            FileInfo compiledResourceFile = null;

            switch (InputFile.Extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".resx":
                    compiledResourceFile = CompileResx(solutionConfiguration);
                    break;
                case ".licx":
                    compiledResourceFile = CompileLicx(solutionConfiguration);
                    break;
                default:
                    compiledResourceFile = CompileResource(solutionConfiguration);
                    break;
            }

            return compiledResourceFile;
        }

        /// <summary>
        /// Returns a <see cref="FileInfo" /> representing the compiled resource
        /// file.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// A <see cref="FileInfo" /> representing the compiled resource file.
        /// </returns>
        /// <remarks>
        /// Calling this method does not force compilation of the resource file.
        /// </remarks>
        public FileInfo GetCompiledResourceFile(Configuration solutionConfiguration) {
            string compiledResourceFile = null;

            // obtain project configuration (corresponding with solution configuration)
            ConfigurationSettings config = (ConfigurationSettings) Project.BuildConfigurations[solutionConfiguration];

            switch (InputFile.Extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".resx":
                    compiledResourceFile = FileUtils.CombinePaths(config.ObjectDir.FullName, 
                        GetManifestResourceName(solutionConfiguration));
                    break;
                case ".licx":
                    compiledResourceFile = FileUtils.CombinePaths(config.ObjectDir.FullName, 
                        Project.ProjectSettings.OutputFileName + ".licenses");
                    break;
                default:
                    compiledResourceFile = InputFile.FullName;
                    break;
            }

            return new FileInfo(compiledResourceFile);
        }

        public string GetManifestResourceName(Configuration solutionConfiguration) {
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationSettings projectConfig = (ConfigurationSettings) Project.BuildConfigurations[solutionConfiguration];

            switch (Project.Type) {
                case ProjectType.CSharp:
                    return GetManifestResourceNameCSharp(projectConfig, _dependentFile);
                case ProjectType.VB:
                    return GetManifestResourceNameVB(projectConfig, _dependentFile);
                case ProjectType.JSharp:
                    return GetManifestResourceNameJSharp(projectConfig, _dependentFile);
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "Unsupported project type '{0}'.", Project.Type));
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetManifestResourceNameCSharp(ConfigurationSettings configSetting, string dependentFile) {
            // defer to the resource management code in CscTask
            CscTask csc = new CscTask();
            csc.Project = _solutionTask.Project;
            csc.NamespaceManager = _solutionTask.NamespaceManager;
            csc.OutputFile = new FileInfo(FileUtils.CombinePaths(configSetting.OutputDir.FullName, 
                Project.ProjectSettings.OutputFileName));

            // set-up resource fileset
            ResourceFileSet resources = new ResourceFileSet();
            resources.Project = _solutionTask.Project;
            resources.NamespaceManager = _solutionTask.NamespaceManager;
            resources.Parent = csc;
            resources.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));
            resources.Prefix = Project.ProjectSettings.RootNamespace;
            resources.DynamicPrefix = true;

            // bug #1042917: use logical location of resource file to determine
            // manifest resource name
            return csc.GetManifestResourceName(resources, InputFile.FullName,
                LogicalFile.FullName, dependentFile);
        }

        private string GetManifestResourceNameVB(ConfigurationSettings configSetting, string dependentFile) {
            // defer to the resource management code in VbcTask
            VbcTask vbc = new VbcTask();
            vbc.Project = _solutionTask.Project;
            vbc.NamespaceManager = _solutionTask.NamespaceManager;
            vbc.OutputFile = new FileInfo(FileUtils.CombinePaths(configSetting.OutputDir.FullName, 
                Project.ProjectSettings.OutputFileName));
            vbc.RootNamespace = Project.ProjectSettings.RootNamespace;
            
            // set-up resource fileset
            ResourceFileSet resources = new ResourceFileSet();
            resources.Project = _solutionTask.Project;
            resources.NamespaceManager = _solutionTask.NamespaceManager;
            resources.Parent = vbc;
            resources.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));
            resources.Prefix = Project.ProjectSettings.RootNamespace;
            resources.DynamicPrefix = false;

            // bug #1042917: use logical location of resource file to determine
            // manifest resource name
            return vbc.GetManifestResourceName(resources, InputFile.FullName,
                LogicalFile.FullName, dependentFile);
        }

        private string GetManifestResourceNameJSharp(ConfigurationSettings configSetting, string dependentFile) {
            // defer to the resource management code in VjcTask
            VjcTask vjc = new VjcTask();
            vjc.Project = _solutionTask.Project;
            vjc.NamespaceManager = _solutionTask.NamespaceManager;
            vjc.OutputFile = new FileInfo(FileUtils.CombinePaths(configSetting.OutputDir.FullName,
                Project.ProjectSettings.OutputFileName));

            // set-up resource fileset
            ResourceFileSet resources = new ResourceFileSet();
            resources.Project = _solutionTask.Project;
            resources.NamespaceManager = _solutionTask.NamespaceManager;
            resources.Parent = vjc;
            resources.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(
                Project.ProjectPath));
            resources.Prefix = Project.ProjectSettings.RootNamespace;
            resources.DynamicPrefix = true;

            // bug #1042917: use logical location of resource file to determine
            // manifest resource name
            return vjc.GetManifestResourceName(resources, InputFile.FullName,
                LogicalFile.FullName, dependentFile);
        }

        private FileInfo CompileResource(Configuration solutionConfiguration) {
            return GetCompiledResourceFile(solutionConfiguration);
        }

        private FileInfo CompileLicx(Configuration solutionConfiguration) {
            // create instance of License task
            LicenseTask lt = new LicenseTask();

            // inherit project from solution task
            lt.Project = _solutionTask.Project;

            // inherit namespace manager from solution task
            lt.NamespaceManager = _solutionTask.NamespaceManager;

            // parent is solution task
            lt.Parent = _solutionTask;

            // inherit verbose setting from solution task
            lt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            lt.InitializeTaskConfiguration();

            // set parent of child elements
            lt.Assemblies.Parent = lt;

            // inherit project from solution task from parent task
            lt.Assemblies.Project = lt.Project;

            // inherit namespace manager from parent task
            lt.Assemblies.NamespaceManager = lt.NamespaceManager;

            // set base directory for filesets
            lt.Assemblies.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));

            // set task properties
            lt.InputFile = InputFile;
            lt.OutputFile = GetCompiledResourceFile(solutionConfiguration);
            // convert target to uppercase to match VS.NET
            lt.Target = Path.GetFileName(Project.ProjectSettings.OutputFileName).
                ToUpper(CultureInfo.InvariantCulture);

            // inherit assembly references from project
            foreach (ReferenceBase reference in Project.References) {
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    solutionConfiguration);
                foreach (string assemblyFile in assemblyReferences) {
                    lt.Assemblies.Includes.Add(assemblyFile);
                }
            }

            // increment indentation level
            lt.Project.Indent();
            try {
                // execute task
                lt.Execute();
            } finally {
                // restore indentation level
                lt.Project.Unindent();
            }

            return lt.OutputFile;
        }

        private FileInfo CompileResx(Configuration solutionConfiguration) {
            // for performance reasons, compilation of resx files is done in
            // batch using the ResGen task in ManagedProjectBase
            throw new InvalidOperationException();
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly CultureInfo _culture;
        private readonly FileInfo _resourceSourceFile;
        private readonly string _dependentFile;
        private readonly string _resourceSourceFileRelativePath;
        private readonly ManagedProjectBase _project;
        private readonly SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
