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
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VisualCpp.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// A single build configuration for a Visual C++ project or for a specific
    /// file in the project.
    /// </summary>
    public class VcConfiguration : ConfigurationBase {
        #region Internal Instance Constructors

        internal VcConfiguration(XmlElement elem, VcProject parentProject, SolutionBase parentSln, DirectoryInfo outputDir) : this(elem, parentProject, parentSln, null, outputDir) {
        }

        internal VcConfiguration(XmlElement elem, VcProject parentProject, SolutionBase parentSln, VcConfiguration parentConfig, DirectoryInfo outputDir) : base(parentProject) {
            _parentConfig = parentConfig;

            // get name of configuration (also contains the targeted platform)
            _name = elem.GetAttribute("Name");

            // determine relative output directory (outdir)
            XmlAttribute outputDirAttribute = elem.Attributes["OutputDirectory"];
            if (outputDirAttribute != null) {
                _relativeOutputDir = ExpandMacros(outputDirAttribute.Value);
            }

            // set output directory (if specified)
            _outputDir = outputDir;

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

            // get referencespath directory and expand macros 
            XmlAttribute referencesPathAttribute = elem.Attributes["ReferencesPath"];
            if (referencesPathAttribute != null) {
                _referencesPath = StringUtils.ConvertEmptyToNull(
                    ExpandMacros(referencesPathAttribute.Value));
            } else if (_parentConfig != null) {
                _referencesPath = _parentConfig.ReferencesPath;
            }

            XmlNodeList tools = elem.GetElementsByTagName("Tool");
            foreach (XmlElement toolElem in tools) {
                string toolName = toolElem.GetAttribute("Name");
                Hashtable htToolSettings = CollectionsUtil.CreateCaseInsensitiveHashtable();

                foreach(XmlAttribute attr in toolElem.Attributes) {
                    if (attr.Name != "Name") {
                        htToolSettings[attr.Name] = attr.Value;
                    }
                }

                _htTools[toolName] = htToolSettings;
            }

            // create the output path if it doesn't already exist
            OutputDir.Create();
        }

        #endregion Internal Instance Constructors

        #region Override implementation of ConfigurationBase

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        public override DirectoryInfo OutputDir {
            get { 
                if (_outputDir == null) {
                    if (_relativeOutputDir != null) {
                        _outputDir = new DirectoryInfo(Path.Combine(ProjectDir.FullName, 
                            _relativeOutputDir));
                    } else if (_parentConfig != null) {
                        _outputDir = _parentConfig.OutputDir;
                    } else {
                        throw new BuildException("The output directory could not be"
                            + " determined.", Location.UnknownLocation);
                    }
                }

                return _outputDir;
            }
        }


        /// <summary>
        /// Gets the path for the output file.
        /// </summary>
        public override string OutputPath {
            get { 
                string linkOutput = GetToolSetting("VCLinkerTool", "OutputFile");
                if (linkOutput != null) {
                    return Path.Combine(ProjectDir.FullName, linkOutput);
                }

                string librarianOutput = GetToolSetting("VCLibrarianTool", "OutputFile");
                if (librarianOutput != null) {
                    return Path.Combine(ProjectDir.FullName, librarianOutput);
                }

                return OutputDir.Name;
            }
        }

        /// <summary>
        /// Gets the name of the configuration.
        /// </summary>
        /// <value>
        /// The name of the configuration.
        /// </value>
        public override string Name {
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
        /// Get the directory in which intermediate build output will be stored 
        /// for this configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a directory relative to the project directory named 
        /// <c>obj\&lt;configuration name&gt;</c>.
        /// </para>
        /// <para>
        /// <c>.resx</c> and <c>.licx</c> files will only be recompiled if the
        /// compiled resource files in the <see cref="ObjectDir" /> are not 
        /// uptodate.
        /// </para>
        /// </remarks>
        public override DirectoryInfo ObjectDir {
            get { 
                return new DirectoryInfo(Path.Combine(Project.ProjectDirectory.FullName, 
                    IntermediateDir));
            }
        }

        /// <summary>
        /// Gets the platform that the configuration targets.
        /// </summary>
        /// <value>
        /// The platform targeted by the configuration.
        /// </value>
        public override string PlatformName {
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
        /// Get the path of the output directory relative to the project
        /// directory.
        /// </summary>
        public override string RelativeOutputDir {
            get {
                if (_relativeOutputDir != null) {
                    return _relativeOutputDir;
                } else if (_parentConfig != null) {
                    return _parentConfig.RelativeOutputDir;
                }

                throw new BuildException("The relative output directory could"
                    + " not be determined.", Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The macro is not supported.</para>
        ///   <para>-or-</para>
        ///   <para>The macro is not implemented.</para>
        ///   <para>-or-</para>
        ///   <para>The macro cannot be expanded.</para>
        /// </exception>
        protected internal override string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "noinherit":
                    return "$(noinherit)";
                case "intdir":
                    return IntermediateDir;
                case "vcinstalldir":
                    throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture,
                        "\"{0}\" macro is not yet implemented.", macro));
                case "vsinstalldir":
                    throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture,
                        "\"{0}\" macro is not yet implemented.", macro));
                case "frameworkdir":
                    return SolutionTask.Project.TargetFramework.FrameworkDirectory.
                        Parent.FullName;
                case "frameworkversion":
                    return "v" + SolutionTask.Project.TargetFramework.ClrVersion;
                case "frameworksdkdir":
                    if (SolutionTask.Project.TargetFramework.SdkDirectory != null) {
                        return SolutionTask.Project.TargetFramework.SdkDirectory.FullName;
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Macro \"{0}\" cannot be expanded: the SDK for {0}"
                            + " is not installed.", SolutionTask.Project.TargetFramework.Description),
                            Location.UnknownLocation);
                    }
                default:
                    try {
                        return base.ExpandMacro(macro);
                    } catch (BuildException) {
                        // Visual C++ also supports environment variables
                        string envvar = Environment.GetEnvironmentVariable(macro);
                        if (envvar != null) {
                            return envvar;
                        } else {
                            // re-throw build exception
                            throw;
                        }
                    }
            }
        }

        #endregion Override implementation of ConfigurationBase

        #region Public Instance Properties

        public DirectoryInfo ProjectDir {
            get { 
                return new DirectoryInfo(Path.GetDirectoryName(
                    Project.ProjectPath));
            }
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

        /// <summary>
        /// Gets a comma-separated list of directories to scan for assembly
        /// references.
        /// </summary>
        /// <value>
        /// A comma-separated list of directories to scan for assembly
        /// references, or <see langword="null" /> if no additional directories
        /// should scanned.
        /// </value>
        public string ReferencesPath {
            get { return _referencesPath; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

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

        #endregion Internal Instance Methods

        #region Private Instance Fields

        private readonly string _name;
        private readonly VcConfiguration _parentConfig;
        private readonly Hashtable _htTools = CollectionsUtil.CreateCaseInsensitiveHashtable();
        private DirectoryInfo _outputDir;
        private readonly string _relativeOutputDir;
        private readonly string _intermediateDir;
        private readonly bool _wholeProgramOptimization;
        private readonly bool _managedExtensions;
        private readonly bool _excludeFromBuild;
        private readonly CharacterSet _characterSet = CharacterSet.NotSet;
        private readonly string _referencesPath;

        #endregion Private Instance Fields
    }
}
