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

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, string outputDir): this(elem, parentProject, parentSln, null, outputDir) {
        }

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, VcConfiguration parent, string outputDir) {
            string projectDir = Path.GetDirectoryName(parentSln.GetProjectFileFromGUID(parentProject.GUID));

            _parent = parent;
            _name = elem.GetAttribute("Name");

            if (StringUtils.IsNullOrEmpty(outputDir)) {
                _outputPath = elem.GetAttribute("OutputDirectory");
                _outputPath = new DirectoryInfo(Path.Combine(projectDir, elem.GetAttribute("OutputDirectory"))).FullName;
            } else {
                _outputPath = outputDir;
            }

            _intermediateDir = elem.GetAttribute("IntermediateDirectory");
            
            string managedExtentions = elem.GetAttribute("ManagedExtensions");
            if (managedExtentions != null) {
                _managedExtensions = managedExtentions.Trim().ToUpper(CultureInfo.InvariantCulture) == "TRUE";
            }

            if (String.Compare(elem.GetAttribute("WholeProgramOptimization"), "TRUE", true, CultureInfo.InvariantCulture) == 0) {
                _wholeProgramOptimization = true;
            } 

            _htMacros = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htMacros ["OutDir"] = OutputPath;
            _htMacros ["IntDir"] = _intermediateDir;
            _htMacros ["ConfigurationName"] = Name;
            _htMacros ["PlatformName"] = PlatformName;
            _htMacros ["SolutionDir"] = Path.GetDirectoryName(parentSln.FileName);
            _htMacros ["SolutionPath"] = parentSln.FileName;
            _htMacros ["SolutionExt"] = Path.GetExtension(parentSln.FileName);
            _htMacros ["ProjectDir"] = projectDir;
  
            _rxMacro = new Regex(@"\$\((\w+)\)");

            _htTools = CollectionsUtil.CreateCaseInsensitiveHashtable();

            XmlNodeList tools = elem.GetElementsByTagName("Tool");
            foreach(XmlElement toolElem in tools) {
                string toolName = toolElem.GetAttribute("Name");
                Hashtable htToolSettings = CollectionsUtil.CreateCaseInsensitiveHashtable();
                foreach(XmlAttribute attr in toolElem.Attributes) {                    if (attr.Name != "Name") {                        htToolSettings [attr.Name] = attr.Value;                    }                }                _htTools [toolName] = htToolSettings;            }            _targetPath = ExpandMacros(GetToolSetting("VCLinkerTool", "OutputFile"));            _htMacros ["TargetPath"] = _targetPath;            _htMacros ["TargetName"] = Path.GetFileNameWithoutExtension(Path.GetFileName(_targetPath));
            _htMacros ["TargetExt"] = Path.GetExtension(_targetPath);

            // create the output path if it doesn't already exist
            Directory.CreateDirectory(OutputPath);
        }

        #endregion Internal Instance Constructors
        #region Override implementation of ConfigurationBase        public override string OutputPath {
            get { return _outputPath; }
        }

        public override string OutputFile {
            get { 
                string linkOutput = GetToolSetting("VCLinkerTool", "OutputFile");
                if (linkOutput != null) {
                    return Path.Combine(OutputPath, linkOutput);
                }
            
                return Path.Combine(OutputPath, GetToolSetting("VCLibrarianTool", "OutputFile"));
            }
        }
        #endregion Override implementation of ConfigurationBase        #region Public Instance Properties        public string ProjectDir {
            get { return (string) _htMacros["ProjectDir"]; }
        }
        /// <summary>
        /// Gets a value indicating whether Managed Extensions for C++ are 
        /// enabled.
        /// </summary>        public bool ManagedExtensions {            get { return _managedExtensions; }        }        #endregion Public Instance Properties        #region Internal Instance Properties        internal string Name {            get {                int index = _name.IndexOf("|");                if (index >= 0) {                    return _name.Substring(0, index);
                }
                else {
                    return _name;
                }
            }
        }

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
        internal string IntermediateDir {            get { return _intermediateDir; }        }
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
        private string _name;        private VcConfiguration _parent;        private Hashtable _htTools;        private string _outputPath;        private string _intermediateDir;        private string _targetPath;        private Hashtable _htMacros;        private Regex _rxMacro;        private bool _wholeProgramOptimization = false;
        private bool _managedExtensions = false;
        #endregion Private Instance Fields
    }
}
