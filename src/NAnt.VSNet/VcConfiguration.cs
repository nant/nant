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

using NAnt.VisualCpp.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// A single build configuration for a Visual C++ project or for a specific
    /// file in the project.
    /// </summary>
    public class VcConfiguration : ConfigurationBase {
        #region Internal Instance Constructors

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, DirectoryInfo outputDir) : this(elem, parentProject, parentSln, null, outputDir) {
        }

        internal VcConfiguration(XmlElement elem, VcProject parentProject, Solution parentSln, VcConfiguration parentConfig, DirectoryInfo outputDir) {
            DirectoryInfo projectDir = new DirectoryInfo(Path.GetDirectoryName(
                parentSln.GetProjectFileFromGuid(parentProject.Guid)));

            _parentConfig = parentConfig;

            // get name of configuration (also contains the targeted platform)
            _name = elem.GetAttribute("Name");

            // initialize variables for usage in macros
            _htMacros["ProjectName"] = parentProject.Name;
            _htMacros["ConfigurationName"] = Name;
            _htMacros["PlatformName"] = PlatformName;
            if (parentSln.File != null) {
                _htMacros["SolutionDir"] = parentSln.File.DirectoryName + Path.DirectorySeparatorChar;
                _htMacros["SolutionPath"] = parentSln.File.FullName;
                _htMacros["SolutionExt"] = parentSln.File.Extension;
            }    
            _htMacros["ProjectDir"] = projectDir.FullName + Path.DirectorySeparatorChar;

            // determine output directory
            if (outputDir == null) {
                XmlAttribute outputDirAttribute = elem.Attributes["OutputDirectory"];
                if (outputDirAttribute != null) {
                    _outputDir = new DirectoryInfo(Path.Combine(projectDir.FullName, 
                        ExpandMacros(outputDirAttribute.Value)));
                } else if (_parentConfig != null) {
                    _outputDir = _parentConfig.OutputDir;
                } else {
                    // TO-DO : throw BuildException, as there's no output directory defined
                    // or do some configuration types no require this ?
                }
            } else {
                _outputDir = outputDir;
            }
            // make output directory available in macros
            _htMacros["OutDir"] = OutputDir.FullName;

            string managedExtentions = GetXmlAttributeValue(elem, "ManagedExtensions");
            if (managedExtentions != null) {
                _managedExtensions = managedExtentions.Trim().ToUpper(CultureInfo.InvariantCulture) == "TRUE";
            } else if (_parentConfig != null) {
                _managedExtensions = _parentConfig.ManagedExtensions;
            }

            string wholeProgramOptimization = GetXmlAttributeValue(elem, "WholeProgramOptimization");
            if (wholeProgramOptimization != null) {
                _wholeProgramOptimization = wholeProgramOptimization.Trim().ToUpper(CultureInfo.InvariantCulture) == "TRUE";
            } else if (_parentConfig != null) {
                _wholeProgramOptimization = _parentConfig.WholeProgramOptimization;
            }
            
            string excludeFromBuild = GetXmlAttributeValue(elem, "ExcludedFromBuild");
            if (excludeFromBuild != null) {
                _excludeFromBuild = excludeFromBuild.Trim().ToUpper(CultureInfo.InvariantCulture) == "TRUE";
            }
            
            string characterSet = GetXmlAttributeValue(elem, "CharacterSet");
            if (characterSet != null) {
                _characterSet = (CharacterSet) Enum.ToObject(typeof(CharacterSet), 
                    int.Parse(characterSet, CultureInfo.InvariantCulture));
            } else if (_parentConfig != null) {
                _characterSet = _parentConfig.CharacterSet;
            }

            // get intermediate directory and expand macros 
            XmlAttribute intermidiateDirAttribute = elem.Attributes["IntermediateDirectory"];
            if (intermidiateDirAttribute != null) {
                _intermediateDir = ExpandMacros(intermidiateDirAttribute.Value);
            } else if (_parentConfig != null) {
                _intermediateDir = _parentConfig.IntermediateDir;
            }

            // make intermediate directory available in macros
            _htMacros["IntDir"] = _intermediateDir;

            _htTools = CollectionsUtil.CreateCaseInsensitiveHashtable();

            XmlNodeList tools = elem.GetElementsByTagName("Tool");
            foreach(XmlElement toolElem in tools) {
                string toolName = toolElem.GetAttribute("Name");
                Hashtable htToolSettings = CollectionsUtil.CreateCaseInsensitiveHashtable();

                foreach(XmlAttribute attr in toolElem.Attributes) {
                    if (attr.Name != "Name") {
                        htToolSettings[attr.Name] = attr.Value;
                    }
                }

                _htTools[toolName] = htToolSettings;
            }

            _targetPath = ExpandMacros(GetToolSetting("VCLinkerTool", "OutputFile"));
            _htMacros ["TargetPath"] = _targetPath;
            _htMacros ["TargetName"] = Path.GetFileNameWithoutExtension(Path.GetFileName(_targetPath));
            _htMacros ["TargetExt"] = Path.GetExtension(_targetPath);

            // create the output path if it doesn't already exist
            OutputDir.Create();
        }

        #endregion Internal Instance Constructors

        #region Override implementation of ConfigurationBase

        public override DirectoryInfo OutputDir {
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

        #endregion Override implementation of ConfigurationBase

        #region Public Instance Properties

        public DirectoryInfo ProjectDir {
            get { return new DirectoryInfo((string) _htMacros["ProjectDir"]); }
        }

        /// <summary>
        /// Tells the compiler which character set to use.
        /// </summary>
        public CharacterSet CharacterSet {
            get { return _characterSet; }
        }

        /// <summary>
        /// Gets a value indicating whether Managed Extensions for C++ are 
        /// enabled.
        /// </summary>
        public bool ManagedExtensions {
            get { return _managedExtensions; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        /// <summary>
        /// Gets the name of the configuration.
        /// </summary>
        /// <value>
        /// The name of the configuration.
        /// </value>
        internal string Name {
            get {
                int index = _name.IndexOf("|");
                if (index >= 0) {
                    return _name.Substring(0, index);
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

        /// <summary>
        /// Gets the name of the configuration, including the platform it
        /// targets.
        /// </summary>
        /// <value>
        /// Tthe name of the configuration, including the platform it targets.
        /// </value>
        internal string FullName {
            get { return _name; }
        }

        /// <summary>
        /// Intermediate directory, specified relative to project directory.
        /// </summary>
        internal string IntermediateDir {
            get { return _intermediateDir; }
        }

        internal bool WholeProgramOptimization {
            get { return _wholeProgramOptimization; }
        }

        /// <summary>
        /// Gets a value indication whether the file should be excluded from 
        /// the build for this configuration.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the file should be excluded from the 
        /// build for this configuration; otherwise, <see langword="false" />.
        /// </value>
        internal bool ExcludeFromBuild {
            get { return _excludeFromBuild; }
        }

        /// <summary>
        /// Gets the collection of macros that can be expanded in configuration
        /// settings.
        /// </summary>
        internal Hashtable Macros {
            get { return _htMacros; }
        }

        #endregion Internal Instance Properties

        #region Private Static Methods

        /// <summary>
        /// Gets the value of the specified attribute from the specified node.
        /// </summary>
        /// <param name="xmlNode">The node of which the attribute value should be retrieved.</param>
        /// <param name="attributeName">The attribute of which the value should be returned.</param>
        /// <returns>
        /// The value of the attribute with the specified name or <see langword="null" />
        /// if the attribute does not exist or has no value.
        /// </returns>
        private static string GetXmlAttributeValue(XmlNode xmlNode, string attributeName) {
            string attributeValue = null;

            if (xmlNode != null) {
                XmlAttribute xmlAttribute = (XmlAttribute) xmlNode.Attributes.GetNamedItem(attributeName);

                if (xmlAttribute != null) {
                    attributeValue = StringUtils.ConvertEmptyToNull(xmlAttribute.Value);
                }
            }

            return attributeValue;
        }

        #endregion Private Static Methods

        #region Internal Instance Methods

        internal string GetToolSetting(string toolName, string settingName) {
            Hashtable toolSettings = (Hashtable) _htTools[toolName];
            if (toolSettings != null) {
                string setting = (string) toolSettings[settingName];
                if (setting != null) {
                    return ExpandMacros(setting);
                }
            }
            if (_parentConfig != null) {
                return _parentConfig.GetToolSetting(toolName, settingName);
            }

            return null;
        }

        internal Hashtable GetToolArguments(string toolName, VcArgumentMap argMap) {
            Hashtable args;
            if (_parentConfig != null) {
                args = _parentConfig.GetToolArguments(toolName, argMap);
            } else {
                args = CollectionsUtil.CreateCaseInsensitiveHashtable();
            }

            Hashtable toolSettings = (Hashtable) _htTools[toolName];
            if (toolSettings != null) {
                foreach (DictionaryEntry de in toolSettings) {
                    string arg = argMap.GetArgument((string) de.Key, ExpandMacros((string) de.Value));
                    if (arg != null) {
                        args[(string) de.Key] = arg;
                    }
                }
            }
            return args;
        }

        internal string ExpandMacros(string s) {
            if (s == null) {
                return s;
            }

            return _rxMacro.Replace(s, new MatchEvaluator(EvaluateMacro));
        }

        #endregion Internal Instance Methods

        #region Private Instance Methods

        private string EvaluateMacro(Match m) {
            string macroValue = (string) _htMacros[m.Groups[1].Value];
            if (macroValue != null) {
                return macroValue;
            }
            return m.Value;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly string _name;
        private VcConfiguration _parentConfig;
        private Hashtable _htTools;
        private readonly DirectoryInfo _outputDir;
        private readonly string _intermediateDir;
        private readonly string _targetPath;
        private Hashtable _htMacros = CollectionsUtil.CreateCaseInsensitiveHashtable();
        private readonly Regex _rxMacro = new Regex(@"\$\((\w+)\)");
        private readonly bool _wholeProgramOptimization;
        private readonly bool _managedExtensions;
        private readonly bool _excludeFromBuild;
        private readonly CharacterSet _characterSet = CharacterSet.NotSet;

        #endregion Private Instance Fields
    }
}
