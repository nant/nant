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

        public ConfigurationSettings(ManagedProjectBase project, XmlElement elemConfig, DirectoryInfo outputDir) : base(project) {
            _settings = new ArrayList();
            if (outputDir == null) {
                _relativeOutputDir = elemConfig.Attributes["OutputPath"].Value;
                if (!_relativeOutputDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                    _relativeOutputDir = _relativeOutputDir + Path.DirectorySeparatorChar;
                }
                _outputDir = new DirectoryInfo(Path.Combine(
                    project.ProjectDirectory.FullName, 
                    _relativeOutputDir));
            } else {
                _relativeOutputDir = outputDir.FullName;
                if (!_relativeOutputDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                    _relativeOutputDir = _relativeOutputDir + Path.DirectorySeparatorChar;
                }
                _outputDir = outputDir;
            }

            _name = elemConfig.GetAttribute("Name");

            if (!StringUtils.IsNullOrEmpty(elemConfig.GetAttribute("DocumentationFile"))) {
                if (outputDir == null) {
                    // combine project root directory with (relative) path for 
                    // documentation file
                    _docFilename = Path.GetFullPath(Path.Combine(
                        project.ProjectDirectory.FullName, 
                        elemConfig.GetAttribute("DocumentationFile")));
                } else {
                    // combine output directory and filename of document file (do not use path information)
                    _docFilename = Path.GetFullPath(Path.Combine(outputDir.FullName,
                        Path.GetFileName(elemConfig.GetAttribute("DocumentationFile"))));
                }
                _settings.Add(@"/doc:""" + _docFilename + @"""");

                // make sure the output directory for the doc file exists
                Directory.CreateDirectory(Path.GetDirectoryName(_docFilename));

                // add documentation file as extra output file
                ExtraOutputFiles[_docFilename] = Path.GetFileName(_docFilename);
            }

            SolutionTask.Log(Level.Debug, "Project: {0} Relative Output Path: {1} Output Path: {2} Documentation Path: {3}", 
                Project.Name, _relativeOutputDir, _outputDir.FullName, _docFilename);

            Hashtable htStringSettings = new Hashtable();
            Hashtable htBooleanSettings = new Hashtable();

            htStringSettings["BaseAddress"] = "/baseaddress:{0}";

            // is only supported by csc (all versions) and vbc (2.0 or higher)
            htStringSettings["FileAlignment"] = "/filealign:{0}";

            htStringSettings["DefineConstants"] = "/define:{0}";

            switch (project.Type) {
                case ProjectType.CSharp:
                    htStringSettings["WarningLevel"] = "/warn:{0}";
                    htStringSettings["NoWarn"] = "/nowarn:{0}";
                    htBooleanSettings["IncrementalBuild"] = "/incremental";
                    htBooleanSettings["AllowUnsafeBlocks"] = "/unsafe";
                    htBooleanSettings["CheckForOverflowUnderflow"] = "/checked";
                    break;
                case ProjectType.JSharp:
                    htStringSettings["WarningLevel"] = "/warn:{0}";
                    htStringSettings["NoWarn"] = "/nowarn:{0}";
                    htBooleanSettings["IncrementalBuild"] = "/incremental";
                    break;
                case ProjectType.VB:
                    htStringSettings["DefineDebug"] = "/d:DEBUG={0}";
                    htStringSettings["DefineTrace"] = "/d:TRACE={0}";
                    htBooleanSettings["RemoveIntegerChecks"] = "/removeintchecks";
                    break;
            }

            htBooleanSettings["DebugSymbols"] = "/debug";
            htBooleanSettings["TreatWarningsAsErrors"] = "/warnaserror";
            htBooleanSettings["Optimize"] = "/optimize";

            foreach (DictionaryEntry de in htStringSettings) {
                string value = elemConfig.GetAttribute(de.Key.ToString());
                if (!StringUtils.IsNullOrEmpty(value)) {
                    switch (de.Key.ToString()) {
                        case "BaseAddress":
                            // vbc expects the base address to be specified as a
                            // hexadecimal number, csc supports decimal, hexadecimal, 
                            // or octal number
                            int intvalue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                            value = intvalue.ToString("x", CultureInfo.InvariantCulture);
                            break;
                        case "DefineConstants":
                            // vbc fails when the symbol contains spaces
                            value = value.Replace(" ", string.Empty);
                            break;
                    }
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

        /// <summary>
        /// Gets the platform that the configuration targets.
        /// </summary>
        /// <value>
        /// The platform targeted by the configuration.
        /// </value>
        public override string PlatformName {
            get { return ".NET"; }
        }

        public override string RelativeOutputDir {
            get { return _relativeOutputDir; }
        }

        public override DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        public override string OutputPath {
            get { 
                return Path.Combine(OutputDir.FullName, 
                    ((ManagedProjectBase) Project).ProjectSettings.OutputFileName);
            }
        }

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        public override string Name {
            get { return _name; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private readonly ArrayList _settings;
        private readonly string _docFilename;
        private readonly string _relativeOutputDir;
        private readonly DirectoryInfo _outputDir;
        private readonly string _name;

        #endregion Private Instance Fields
    }
}
