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
using System.Globalization;
using System.IO;
using System.Collections;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class ConfigurationSettings : ConfigurationBase {
        #region Public Instance Constructors

        public ConfigurationSettings(Project project, XmlElement elemConfig, SolutionTask solutionTask, DirectoryInfo outputDir) {
            _project = project;
            _settings = new ArrayList();
            _solutionTask = solutionTask;
            if (outputDir == null) {
                _relativeOutputDir = elemConfig.Attributes["OutputPath"].Value;
                if (!_relativeOutputDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                    _relativeOutputDir = _relativeOutputDir + Path.DirectorySeparatorChar;
                }
                _outputDir = new DirectoryInfo(Path.Combine(Project.ProjectSettings.RootDirectory, _relativeOutputDir));
            } else {
                _relativeOutputDir = outputDir.FullName;
                if (!_relativeOutputDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                    _relativeOutputDir = _relativeOutputDir + Path.DirectorySeparatorChar;
                }
                _outputDir = outputDir;
            }

            _name = elemConfig.GetAttribute("Name").ToLower(CultureInfo.InvariantCulture);

            if (!StringUtils.IsNullOrEmpty(elemConfig.GetAttribute("DocumentationFile"))) {
                if (outputDir == null) {
                    // combine project root directory with (relative) path for 
                    // documentation file
                    _docFilename = Path.GetFullPath(Path.Combine(
                        Project.ProjectSettings.RootDirectory, elemConfig.GetAttribute("DocumentationFile")));
                } else {
                    // combine output directory and filename of document file (do not use path information)
                    _docFilename = Path.GetFullPath(Path.Combine(outputDir.FullName,
                        Path.GetFileName(elemConfig.GetAttribute("DocumentationFile"))));
                }
                _settings.Add(@"/doc:""" + _docFilename + @"""");

                // make sure the output directory for the doc file exists
                Directory.CreateDirectory(Path.GetDirectoryName(_docFilename));
            }

            _solutionTask.Log(Level.Debug, _solutionTask.LogPrefix + "Project: {0} Relative Output Path: {1} Output Path: {2} Documentation Path: {3}", 
                Project.Name, _relativeOutputDir, _outputDir.FullName, _docFilename);

            Hashtable htStringSettings = new Hashtable();
            Hashtable htBooleanSettings = new Hashtable();

            htStringSettings["BaseAddress"] = "/baseaddress:{0}";
            htStringSettings["FileAlignment"] = "/filealign:{0}";
            htStringSettings["DefineConstants"] = "/define:{0}";
            
            if (Project.ProjectSettings.Type == ProjectType.CSharp) {
                htStringSettings["WarningLevel"] = "/warn:{0}";
                htStringSettings["NoWarn"] = "/nowarn:{0}";
                htBooleanSettings["IncrementalBuild"] = "/incremental";
            }

            htBooleanSettings["AllowUnsafeBlocks"] = "/unsafe";
            htBooleanSettings["DebugSymbols"] = "/debug";
            htBooleanSettings["CheckForOverflowUnderflow"] = "/checked";
            htBooleanSettings["TreatWarningsAsErrors"] = "/warnaserror";
            htBooleanSettings["Optimize"] = "/optimize";

            foreach (DictionaryEntry de in htStringSettings) {
                string value = elemConfig.GetAttribute(de.Key.ToString());
                if (!StringUtils.IsNullOrEmpty(value)) {
                    _settings.Add(string.Format(CultureInfo.InvariantCulture, de.Value.ToString(), value));
                }
            }

            foreach (DictionaryEntry de in htBooleanSettings) {
                string value = elemConfig.GetAttribute(de.Key.ToString());
                if (!StringUtils.IsNullOrEmpty(value)) {
                    if (value == "true") {
                        _settings.Add(de.Value.ToString() + "+");
                    } else if (value == "false") {
                        _settings.Add(de.Value.ToString() + "-");
                    }
                }
            }

            _settings.Add(string.Format(CultureInfo.InvariantCulture, "/out:\"{0}\"", OutputPath));
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public Project Project {
            get { return _project; }
        }

        public string[] ExtraOutputFiles {
            get {
                if (_docFilename == null) {
                    return new string[0];
                }

                return new string[] {_docFilename};
            }
        }

        public string RelativeOutputDir {
            get { return _relativeOutputDir; }
        }

        public override DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        public override string OutputPath {
            get { return Path.Combine(OutputDir.FullName, Project.ProjectSettings.OutputFileName); }
        }

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        public string Name {
            get { return _name; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private Project _project;
        private ArrayList _settings;
        private string _docFilename;
        private string _relativeOutputDir;
        private DirectoryInfo _outputDir;
        private string _name;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
