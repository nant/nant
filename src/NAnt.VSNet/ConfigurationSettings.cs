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
using System.Globalization;
using System.IO;
using System.Collections;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class ConfigurationSettings {
        #region Public Instance Constructors

        public ConfigurationSettings(ProjectSettings projectSettings, XmlElement elemConfig, SolutionTask solutionTask, string outputDir) {
            _settings = new ArrayList();
            _solutionTask = solutionTask;
            if (StringUtils.IsNullOrEmpty(outputDir)) {
                _relativeOutputPath = elemConfig.Attributes["OutputPath"].Value;
                if (!_relativeOutputPath.EndsWith(@"\")) {
                    _relativeOutputPath = _relativeOutputPath + @"\";
                }
                _outputPath = new DirectoryInfo(Path.Combine(projectSettings.RootDirectory, elemConfig.GetAttribute("OutputPath"))).FullName;
            } else {
                _relativeOutputPath = outputDir;
                if (!_relativeOutputPath.EndsWith(@"\")) {
                    _relativeOutputPath = _relativeOutputPath + @"\";
                }
                _outputPath = Path.GetFullPath(_relativeOutputPath);
            }

            _projectSettings = projectSettings;
            _name = elemConfig.GetAttribute("Name").ToLower(CultureInfo.InvariantCulture);
            _docFilename = null;

            if (!StringUtils.IsNullOrEmpty(elemConfig.GetAttribute("DocumentationFile"))) {
                if (StringUtils.IsNullOrEmpty(outputDir)) {
                    FileInfo fiDocumentation = new FileInfo(projectSettings.RootDirectory + @"/" + elemConfig.Attributes["DocumentationFile"].Value);
                    _docFilename = fiDocumentation.FullName;
                } else {
                    _docFilename = Path.GetFullPath(Path.Combine(outputDir, elemConfig.GetAttribute("DocumentationFile")));
                }
                _settings.Add(@"/doc:""" + _docFilename + @"""");
                Directory.CreateDirectory(Path.GetDirectoryName(_docFilename));
            }

            _solutionTask.Log(Level.Debug, _solutionTask.LogPrefix + "Project: {0} Relative Output Path: {1} Output Path: {2} Documentation Path: {3}", 
                projectSettings.Name, _relativeOutputPath, _outputPath, _docFilename);

            Hashtable htStringSettings = new Hashtable();
            Hashtable htBooleanSettings = new Hashtable();

            htStringSettings["BaseAddress"] = "/baseaddress:{0}";
            htStringSettings["FileAlignment"] = "/filealign:{0}";
            htStringSettings["DefineConstants"] = "/define:{0}";
            
            if (projectSettings.Type == ProjectType.CSharp) {
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

            _settings.Add(string.Format(CultureInfo.InvariantCulture, "/out:\"{0}\"", FullOutputFile));
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string[] ExtraOutputFiles {
            get {
                if (_docFilename == null) {
                    return new string[0];
                }

                return new string[] {_docFilename};
            }
        }

        public string RelativeOutputPath {
            get { return _relativeOutputPath; }
        }

        public string OutputPath {
            get { return _outputPath; }
        }

        public string FullOutputFile {
            get { return Path.Combine(OutputPath, _projectSettings.OutputFile); }
        }

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        public string Name {
            get { return _name; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private ArrayList _settings;
        private string _docFilename;
        private string _relativeOutputPath;
        private string _outputPath;
        private string _name;
        private ProjectSettings _projectSettings;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
