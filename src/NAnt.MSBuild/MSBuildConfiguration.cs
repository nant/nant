// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System;
using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet;

namespace NAnt.MSBuild {
    internal class MSBuildConfiguration : ConfigurationBase {
        private readonly string _name;
        private readonly string _relativeOutputDir;
        private readonly DirectoryInfo _outputDir;
        private readonly DirectoryInfo _objdir;
        private readonly ManagedOutputType _outputType;
        private readonly string _asmname;
        private readonly string _platform;

        public MSBuildConfiguration(MSBuildProject project, NAnt.MSBuild.BuildEngine.Project msproj, Configuration projectConfig)
            : base(project) {
            _name = projectConfig.Name;
            _platform = projectConfig.Platform;

            //explicit set. EvaluatedProperties will use those.
            //Its caller responsibility to set it back to original values, if needed
            msproj.GlobalProperties.SetProperty("Configuration", _name);

            if (!String.IsNullOrEmpty(_platform)) {
                msproj.GlobalProperties.SetProperty("Platform", _platform.Replace(" ", string.Empty));
            }

            _relativeOutputDir = msproj.GetEvaluatedProperty("OutputPath");
            if (!_relativeOutputDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                _relativeOutputDir = _relativeOutputDir + Path.DirectorySeparatorChar;
            }
            _outputDir = new DirectoryInfo(FileUtils.CombinePaths(
                project.ProjectDirectory.FullName,
                _relativeOutputDir));

            _objdir = new DirectoryInfo(msproj.GetEvaluatedProperty("IntermediateOutputPath"));

            _outputType = GetType(msproj.GetEvaluatedProperty("OutputType"));
            _asmname = msproj.GetEvaluatedProperty("AssemblyName");
        }

        private ManagedOutputType GetType(string p) {
            switch (p.ToLower(CultureInfo.InvariantCulture)) {
                case "library":
                    return ManagedOutputType.Library;
                case "exe":
                    return ManagedOutputType.Executable;
                case "winexe":
                    return ManagedOutputType.WindowsExecutable;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Output type \"{0}\" of project \"{1}\" is not supported.",
                        p, Project.Name), Location.UnknownLocation);
            }
        }

        public string OutputFileName {
            get { return string.Concat(AssemblyName, OutputExtension); }
        }

        public string OutputExtension {
            get {
                switch (_outputType) {
                    case ManagedOutputType.Library:
                        return ".dll";
                    case ManagedOutputType.Executable:
                    case ManagedOutputType.WindowsExecutable:
                    default:
                        return ".exe";
                }
            }
        }

        public override string Name {
            get { return _name; }
        }

        public string AssemblyName {
            get { return _asmname; }
        }

        public override DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        public override string OutputPath {
            get {
                return FileUtils.CombinePaths(OutputDir.FullName, OutputFileName);
            }
        }

        public override DirectoryInfo ObjectDir {
            get { return _objdir; }
        }

        public override string BuildPath {
            get { return _objdir.FullName; }
        }

        public override string RelativeOutputDir {
            get { return _relativeOutputDir; }
        }

        public override string PlatformName {
            get { return _platform; }
        }
    }
}
