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

        public Resource(Project project, FileInfo resourceSourceFile, string resourceSourceFileRelativePath, string dependentFile, SolutionTask solutionTask) {
            _project = project;
            _resourceSourceFile = resourceSourceFile;
            _resourceSourceFileRelativePath = resourceSourceFileRelativePath;
            _dependentFile = dependentFile;
            _solutionTask = solutionTask;
            _culture = CompilerBase.GetResourceCulture(resourceSourceFile.FullName);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public CultureInfo Culture {
            get { return _culture; }
        }

        public string ManifestResourceName {
            get { return _manifestResourceName; }
        }

        public string CompiledResourceFile {
            get { return _compiledResourceFile; }
        }

        public FileInfo InputFile {
            get { return _resourceSourceFile; }
        }

        public Project Project {
            get { return _project; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void Compile(ConfigurationSettings configurationSettings) {
            switch (InputFile.Extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".resx":
                    _compiledResourceFile = CompileResx();
                    _manifestResourceName = GetManifestResourceName(configurationSettings);
                    break;
                case ".licx":
                    _compiledResourceFile = CompileLicx();
                    _manifestResourceName = GetManifestResourceName(configurationSettings);
                    break;
                default:
                    _compiledResourceFile = CompileResource();
                    _manifestResourceName = GetManifestResourceName(configurationSettings);
                    break;
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetManifestResourceName(ConfigurationSettings configSettings) {
            switch (Project.ProjectSettings.Type) {
                case ProjectType.CSharp:
                    return GetManifestResourceNameCSharp(configSettings, _dependentFile);
                case ProjectType.VBNet:
                    return GetManifestResourceNameVB(configSettings, _dependentFile);
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "Unsupported project type '{0}'.", Project.ProjectSettings.Type));
            }
        }

        private string GetManifestResourceNameCSharp(ConfigurationSettings configSetting, string dependentFile) {
            // defer to the resource management code in CscTask
            CscTask csc = new CscTask();      
            csc.Project = _solutionTask.Project;
            csc.OutputFile = new FileInfo(Path.Combine(configSetting.OutputDir.FullName, 
                Project.ProjectSettings.OutputFileName));

            // set-up resource fileset
            ResourceFileSet resources = new ResourceFileSet();
            resources.Project = _solutionTask.Project;
            resources.Parent = csc;
            resources.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));
            resources.Prefix = Project.ProjectSettings.RootNamespace;
            resources.DynamicPrefix = true;

            return csc.GetManifestResourceName(resources, InputFile.FullName, 
                dependentFile);
        }

        private string GetManifestResourceNameVB(ConfigurationSettings configSetting, string dependentFile) {
            // defer to the resource management code in VbcTask
            VbcTask vbc = new VbcTask();
            vbc.Project = _solutionTask.Project;
            vbc.OutputFile = new FileInfo(Path.Combine(configSetting.OutputDir.FullName, 
                Project.ProjectSettings.OutputFileName));
            vbc.RootNamespace = Project.ProjectSettings.RootNamespace;
            
            // set-up resource fileset
            ResourceFileSet resources = new ResourceFileSet();
            resources.Project = _solutionTask.Project;
            resources.Parent = vbc;
            resources.BaseDirectory = new DirectoryInfo(Path.GetDirectoryName(Project.ProjectPath));
            resources.Prefix = Project.ProjectSettings.RootNamespace;
            resources.DynamicPrefix = true;

            return vbc.GetManifestResourceName(resources, InputFile.FullName, 
                dependentFile);
        }

        private string CompileResource() {
            return InputFile.FullName;
        }

        private string CompileLicx() {
            string outputFileName = Project.ProjectSettings.OutputFileName;

            // create instance of License task
            LicenseTask lt = new LicenseTask();

            // inherit project from solution task
            lt.Project = _solutionTask.Project;

            // inherit parent from solution task
            lt.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            lt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            lt.InitializeTaskConfiguration();

            // set parent of child elements
            lt.Assemblies.Parent = lt;

            // inherit project from solution task for child elements
            lt.Assemblies.Project = lt.Project;

            // set task properties
            lt.InputFile = InputFile;
            lt.OutputFile = new FileInfo(Project.ProjectSettings.GetTemporaryFilename(outputFileName + ".licenses"));
            lt.Target = Path.GetFileName(outputFileName);

            foreach (Reference reference in Project.References) {
                lt.Assemblies.Includes.Add(reference.Filename);
            }

            // execute task
            lt.Project.Indent();
            lt.Execute();
            lt.Project.Unindent();

            return lt.OutputFile.FullName;
        }

        private string CompileResx() {
            FileInfo outputFile = new FileInfo(Project.ProjectSettings.GetTemporaryFilename(
                Path.Combine(Path.GetDirectoryName(_resourceSourceFileRelativePath), Path.GetFileNameWithoutExtension(
                    InputFile.Name) + ".resources")));

            // create instance of ResGen task
            ResGenTask rt = new ResGenTask();

            // inherit project from solution task
            rt.Project = _solutionTask.Project;

            // inherit parent from solution task
            rt.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            rt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            rt.InitializeTaskConfiguration();

            // set task properties
            rt.InputFile = InputFile;
            rt.OutputFile = outputFile;

            // execute task
            rt.Project.Indent();
            rt.Execute();
            rt.Project.Unindent();

            return outputFile.FullName;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private CultureInfo _culture;
        private string _compiledResourceFile;
        private FileInfo _resourceSourceFile;
        private string _dependentFile;
        private string _resourceSourceFileRelativePath;
        private string _manifestResourceName;
        private Project _project;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
