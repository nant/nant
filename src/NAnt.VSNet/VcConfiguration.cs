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
// Dmitry Jemerov <yole@yole.ru>
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core.Util;

namespace NAnt.VSNet {
    /// <summary>
    /// A single build configuration for a Visual C++ project or for a specific
    /// file in the project.
    /// </summary>
    public class VcConfiguration : ConfigurationBase {
        #region Internal Instance Constructors

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, DirectoryInfo outputDir) : this(elem, parentProject, parentSln, null, outputDir) {
        }

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, VcConfiguration parent, DirectoryInfo outputDir) {
            DirectoryInfo projectDir = new DirectoryInfo(Path.GetDirectoryName(
                parentSln.GetProjectFileFromGuid(parentProject.Guid)));

            _parent = parent;

            // get name of configuration (also contains the targeted platform)
            _name = elem.GetAttribute("Name");

            // initialize variables for usage in macros
            _htMacros = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htMacros["ProjectName"] = parentProject.Name;
            _htMacros["ConfigurationName"] = Name;
            _htMacros["PlatformName"] = PlatformName;
            _htMacros["SolutionDir"] = parentSln.File.DirectoryName + Path.DirectorySeparatorChar;
            _htMacros["SolutionPath"] = parentSln.File.FullName;
            _htMacros["SolutionExt"] = parentSln.File.Extension;
            _htMacros["ProjectDir"] = projectDir.FullName + Path.DirectorySeparatorChar;

            // determine output directory
            if (outputDir == null) {
                _outputDir = new DirectoryInfo(Path.Combine(projectDir.FullName, 
                    ExpandMacros(elem.GetAttribute("OutputDirectory"))));
            } else {
                _outputDir = outputDir;
            }
            // make output directory available in macros
            _htMacros["OutDir"] = OutputDir.FullName;

            string managedExtentions = elem.GetAttribute("ManagedExtensions");
            if (managedExtentions != null) {
                _managedExtensions = managedExtentions.Trim().ToUpper(CultureInfo.InvariantCulture) == "TRUE";
            }

            if (String.Compare(elem.GetAttribute("WholeProgramOptimization"), "TRUE", true, CultureInfo.InvariantCulture) == 0) {
                _wholeProgramOptimization = true;
            } 

            // get intermediate directory and expand macros 
            _intermediateDir = ExpandMacros(elem.GetAttribute("IntermediateDirectory"));
            // make intermediate directory available in macros
            _htMacros["IntDir"] = _intermediateDir;

            _htTools = CollectionsUtil.CreateCaseInsensitiveHashtable();

            XmlNodeList tools = elem.GetElementsByTagName("Tool");
            foreach(XmlElement toolElem in tools) {
                string toolName = toolElem.GetAttribute("Name");
                Hashtable htToolSettings = CollectionsUtil.CreateCaseInsensitiveHashtable();
                foreach(XmlAttribute attr in toolElem.Attributes) {                    if (attr.Name != "Name") {                        htToolSettings[attr.Name] = attr.Value;                    }                }                _htTools[toolName] = htToolSettings;            }            _targetPath = ExpandMacros(GetToolSetting("VCLinkerTool", "OutputFile"));            _htMacros ["TargetPath"] = _targetPath;            _htMacros ["TargetName"] = Path.GetFileNameWithoutExtension(Path.GetFileName(_targetPath));
            _htMacros ["TargetExt"] = Path.GetExtension(_targetPath);

            // create the output path if it doesn't already exist
            OutputDir.Create();
        }

        #endregion Internal Instance Constructors
        #region Override implementation of ConfigurationBase        public override DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        public override string OutputPath {
            get { 
                string linkOutput = GetToolSetting("VCLinkerTool", "OutputFile");
                if (linkOutput != null) {
                    return Path.Combine(OutputDir.FullName, linkOutput);
                }
            
                return Path.Combine(OutputDir.FullName, GetToolSetting("VCLibrarianTool", "OutputFile"));
            }
        }
        #endregion Override implementation of ConfigurationBase        #region Public Instance Properties        public DirectoryInfo ProjectDir {
            get { return new DirectoryInfo((string) _htMacros["ProjectDir"]); }
        }
        /// <summary>
        /// Gets a value indicating whether Managed Extensions for C++ are 
        /// enabled.
        /// </summary>        public bool ManagedExtensions {            get { return _managedExtensions; }        }        #endregion Public Instance Properties        #region Internal Instance Properties        /// <summary>
        /// Gets the name of the configuration.
        /// </summary>        /// <value>        /// The name of the configuration.        /// </value>        internal string Name {            get {                int index = _name.IndexOf("|");                if (index >= 0) {                    return _name.Substring(0, index);
                }
                else {
                    return _name;
                }
            }
        }

        /// <summary>
        /// Gets the platform that the configuration targets.
        /// </summary>
        /// <value>
        /// The platform targeted by the configuration.
        /// </value>
        internal string PlatformName {
            get {
                int index = _name.IndexOf("|");
                if (index >= 0) {
                    if (index < _name.Length) {
                        return _name.Substring(index + 1, _name.Length - 1 - index);
                    } else {
                        return "";
                    }
                } else {
                    return "";
                }
            }
        }
        internal string FullName {            get { return _name; }        }
        /// <summary>
        /// Intermediate directory, specified relative to project directory.
        /// </summary>        internal string IntermediateDir {            get { return _intermediateDir; }        }
        internal bool WholeProgramOptimization {            get { return _wholeProgramOptimization; }        }
        #endregion Internal Instance Properties
        #region Internal Instance Methods
        internal string GetToolSetting(string toolName, string settingName) {            Hashtable toolSettings = (Hashtable) _htTools [toolName];            if (toolSettings != null) {                string setting = (string) toolSettings [settingName];                if (setting != null) {                    return ExpandMacros(setting);
                }            }            if (_parent != null) {                return _parent.GetToolSetting(toolName, settingName);            }            return null;        }
        internal string[] GetToolArguments(string toolName, VcArgumentMap argMap) {            ArrayList args = new ArrayList();            Hashtable toolSettings = (Hashtable) _htTools [toolName];            if (toolSettings != null) {                foreach(DictionaryEntry de in toolSettings) {                    string arg = argMap.GetArgument((string) de.Key, ExpandMacros((string) de.Value));                    if (arg != null) {                        args.Add(arg);
                    }                }            }            return (string[]) args.ToArray(typeof(string));        }
        internal string ExpandMacros(string s) {            if (s == null) {                return s;            }            return _rxMacro.Replace(s, new MatchEvaluator(EvaluateMacro));        }

        #endregion Internal Instance Methods

        #region Private Instance Methods
        private string EvaluateMacro(Match m) {            string macroValue = (string) _htMacros [m.Groups [1].Value];            if (macroValue != null) {                return macroValue;
            }            return m.Value;        }

        #endregion Private Instance Methods
        #region Private Instance Fields
        private readonly string _name;        private VcConfiguration _parent;        private Hashtable _htTools;        private readonly DirectoryInfo _outputDir;        private readonly string _intermediateDir;        private readonly string _targetPath;        private Hashtable _htMacros;        private readonly Regex _rxMacro = new Regex(@"\$\((\w+)\)");        private readonly bool _wholeProgramOptimization = false;
        private readonly bool _managedExtensions = false;
        #endregion Private Instance Fields
    }
}
