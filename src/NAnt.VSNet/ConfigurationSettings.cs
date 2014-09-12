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
                _outputDir = new DirectoryInfo(FileUtils.CombinePaths(
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

            string documentationFile = elemConfig.GetAttribute("DocumentationFile");
            if (!String.IsNullOrEmpty(documentationFile)) {
                // to match VS.NET, the XML Documentation file will be output 
                // in the project directory, and only later copied to the output
                // directory
                string xmlDocBuildFile = FileUtils.CombinePaths(project.ProjectDirectory.FullName,
                    documentationFile);

                // add compiler option to build XML Documentation file
                _settings.Add(@"/doc:""" + xmlDocBuildFile + @"""");

                // make sure the output directory for the doc file exists
                if (!Directory.Exists(Path.GetDirectoryName(xmlDocBuildFile))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(xmlDocBuildFile));
                }

                // add built documentation file as extra output file
                ExtraOutputFiles[xmlDocBuildFile] = Path.GetFileName(xmlDocBuildFile);
            }

            // determine whether we need to register project output for use with
            // COM components
            _registerForComInterop = string.Compare(elemConfig.GetAttribute("RegisterForComInterop"), 
                "true", true, CultureInfo.InvariantCulture) == 0;

            SolutionTask.Log(Level.Debug, "Project: {0} Relative Output Path: {1} Output Path: {2} Documentation Path: {3}", 
                Project.Name, _relativeOutputDir, _outputDir.FullName, documentationFile);

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
                string name = de.Key.ToString();
                string value = elemConfig.GetAttribute(de.Key.ToString());
                if (!String.IsNullOrEmpty(value)) {
                    switch (name) {
                        case "BaseAddress":
                            // vbc and vjs expect the base address to be specified
                            // as a hexadecimal number, csc supports decimal, 
                            // hexadecimal, or octal number
                            //
                            // so use hexadecimal as all compiler support this
                            uint intvalue = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                            value = "0x" + intvalue.ToString("x", CultureInfo.InvariantCulture);
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
                string name = de.Key.ToString();
                switch (name) {
                    case "IncrementalBuild":
                        // ignore if not supported
                        if (!IncrementalBuildSupported) {
                            continue;
                        }
                        break;
                }

                string value = elemConfig.GetAttribute(name);
                if (string.Compare(value, "true", true, CultureInfo.InvariantCulture) == 0) {
                    _settings.Add(de.Value.ToString() + "+");
                } else if (string.Compare(value, "false", true, CultureInfo.InvariantCulture) == 0) {
                    _settings.Add(de.Value.ToString() + "-");
                }
            }

            _settings.Add(string.Format(CultureInfo.InvariantCulture, "/out:\"{0}\"", BuildPath));
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
                return FileUtils.CombinePaths(OutputDir.FullName, 
                    ((ManagedProjectBase) Project).ProjectSettings.OutputFileName);
            }
        }

        /// <summary>
        /// Gets the path in which the output file will be created before its
        /// copied to the actual output path.
        /// </summary>
        public override string BuildPath {
            get { 
                return FileUtils.CombinePaths(ObjectDir.FullName, 
                    Path.GetFileName(OutputPath));
            }
        }

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        public override string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets a value indicating whether to register the project output for
        /// use with COM components.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the project output should be registered
        /// for use with COM components; otherwise, <see langword="false" />.
        /// </value>
        public bool RegisterForComInterop {
            get { return _registerForComInterop; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private bool IncrementalBuildSupported {
            get {
                FrameworkInfo tf = SolutionTask.Project.TargetFramework;
                // only supported up until .NET Framework 2.0
                return (tf.Family == "net" && tf.Version <= new Version (2, 0));
            }
        }

        #endregion Private Instance Properties

        #region Private Instance Fields

        private readonly ArrayList _settings;
        private readonly string _relativeOutputDir;
        private readonly DirectoryInfo _outputDir;
        private readonly string _name;
        private readonly bool _registerForComInterop;

        #endregion Private Instance Fields
    }
}
