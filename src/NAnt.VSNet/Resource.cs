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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
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
            _gacCache = gacCache;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public CultureInfo Culture {
            get { return _culture; }
        }

        public string ManifestResourceName {
            get { 
                if (_manifestResourceName == null) {
                    throw new InvalidOperationException("The resource must compiled first.");
                }
                return _manifestResourceName;
            }
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

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Compiles the resource file.
        /// </summary>
        /// <param name="config">A build configuration.</param>
        /// <returns>
        /// A <see cref="FileInfo" /> representing the compiled resource file.
        /// </returns>
        public FileInfo Compile(ConfigurationSettings config) {
            FileInfo compiledResourceFile = null;

            switch (InputFile.Extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".resx":
                    compiledResourceFile = CompileResx(config);
                    break;
                case ".licx":
                    compiledResourceFile = CompileLicx(config);
                    break;
                default:
                    compiledResourceFile = CompileResource(config);
                    break;
            }

            // determine manifest resource name
            _manifestResourceName = GetManifestResourceName(config);

            return compiledResourceFile;
        }

        /// <summary>
        /// Returns a <see cref="FileInfo" /> representing the compiled resource
        /// file.
        /// </summary>
        /// <param name="config">A build configuration.</param>
        /// <returns>
        /// A <see cref="FileInfo" /> representing the compiled resource file.
        /// </returns>
        /// <remarks>
        /// Calling this method does not force compilation of the resource file.
        /// </remarks>
        public FileInfo GetCompiledResourceFile(ConfigurationSettings config) {
            string compiledResourceFile = null;

            switch (InputFile.Extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".resx":
                    compiledResourceFile = FileUtils.CombinePaths(config.ObjectDir.FullName, 
                        GetManifestResourceName(config));
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

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetManifestResourceName(ConfigurationSettings configSettings) {
            switch (Project.Type) {
                case ProjectType.CSharp:
                    return GetManifestResourceNameCSharp(configSettings, _dependentFile);
                case ProjectType.VB:
                    return GetManifestResourceNameVB(configSettings, _dependentFile);
                case ProjectType.JSharp:
                    return GetManifestResourceNameJSharp(configSettings, _dependentFile);
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "Unsupported project type '{0}'.", Project.Type));
            }
        }

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

        private FileInfo CompileResource(ConfigurationSettings config) {
            return GetCompiledResourceFile(config);
        }

        private FileInfo CompileLicx(ConfigurationSettings config) {
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
            lt.OutputFile = GetCompiledResourceFile(config);
            // convert target to uppercase to match VS.NET
            lt.Target = Path.GetFileName(Project.ProjectSettings.OutputFileName).
                ToUpper(CultureInfo.InvariantCulture);

            // inherit non-GAC assembly references from project
            foreach (ReferenceBase reference in Project.References) {
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    config);
                foreach (string assemblyFile in assemblyReferences) {
                    if (!_gacCache.IsAssemblyInGac(assemblyFile)) {
                        lt.Assemblies.Includes.Add(assemblyFile);
                    }
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

        private FileInfo CompileResx(ConfigurationSettings config) {
            // create instance of ResGen task
            ResGenTask rt = new ResGenTask();

            // inherit project from solution task
            rt.Project = _solutionTask.Project;

            // inherit namespace manager from solution task
            rt.NamespaceManager = _solutionTask.NamespaceManager;

            // parent is solution task
            rt.Parent = _solutionTask;

            // inherit verbose setting from solution task
            rt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            rt.InitializeTaskConfiguration();

            // set parent of child elements
            rt.Assemblies.Parent = rt;

            // inherit project from solution task from parent task
            rt.Assemblies.Project = rt.Project;

            // inherit namespace manager from parent task
            rt.Assemblies.NamespaceManager = rt.NamespaceManager;

            // set base directory for filesets
            rt.Assemblies.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));

            // set task properties
            rt.InputFile = InputFile;
            rt.OutputFile = GetCompiledResourceFile(config);

            // inherit assembly references from project
            foreach (ReferenceBase reference in Project.References) {
                StringCollection assemblyReferences = reference.GetAssemblyReferences(
                    config);
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

            return rt.OutputFile;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private CultureInfo _culture;
        private FileInfo _resourceSourceFile;
        private string _dependentFile;
        private string _resourceSourceFileRelativePath;
        private string _manifestResourceName;
        private ManagedProjectBase _project;
        private SolutionTask _solutionTask;
        private GacCache _gacCache;

        #endregion Private Instance Fields
    }
}
