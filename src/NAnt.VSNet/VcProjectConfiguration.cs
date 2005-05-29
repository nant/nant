// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gert Driesen
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core.Util;

using NAnt.VisualCpp.Types;

using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// Represents a Visual C++ project configuration.
    /// </summary>
    public class VcProjectConfiguration : VcConfigurationBase {
        #region Internal Instance Constructors

        internal VcProjectConfiguration(XmlElement elem, VcProject parentProject, DirectoryInfo outputDir) : base(elem, parentProject, outputDir) {
            // determine relative output directory (outdir)
            XmlAttribute outputDirAttribute = elem.Attributes["OutputDirectory"];
            if (outputDirAttribute != null) {
                _rawRelativeOutputDir = outputDirAttribute.Value;
            }

            // get intermediate directory and expand macros 
            XmlAttribute intermidiateDirAttribute = elem.Attributes["IntermediateDirectory"];
            if (intermidiateDirAttribute != null) {
                _rawIntermediateDir = intermidiateDirAttribute.Value;
            }

            // get referencespath directory and expand macros
            XmlAttribute referencesPathAttribute = elem.Attributes["ReferencesPath"];
            if (referencesPathAttribute != null) {
                _rawReferencesPath = StringUtils.ConvertEmptyToNull(referencesPathAttribute.Value);
            }

            string managedExtentions = GetXmlAttributeValue(elem, "ManagedExtensions");
            if (managedExtentions != null) {
                _managedExtensions = string.Compare(managedExtentions.Trim(), "true", true, CultureInfo.InvariantCulture) == 0;
            }

            // get configuration type
            string type = GetXmlAttributeValue(elem, "ConfigurationType");
            if (type != null) {
                _type = (ConfigurationType) Enum.ToObject(typeof(ConfigurationType), 
                    int.Parse(type, CultureInfo.InvariantCulture));
            }

            string wholeProgramOptimization = GetXmlAttributeValue(elem, "WholeProgramOptimization");
            if (wholeProgramOptimization != null) {
                _wholeProgramOptimization = string.Compare(wholeProgramOptimization.Trim(), "true", true, CultureInfo.InvariantCulture) == 0;
            }
            
            string characterSet = GetXmlAttributeValue(elem, "CharacterSet");
            if (characterSet != null) {
                _characterSet = (CharacterSet) Enum.ToObject(typeof(CharacterSet), 
                    int.Parse(characterSet, CultureInfo.InvariantCulture));
            }

            // get MFC settings
            string useOfMFC = GetXmlAttributeValue(elem, "UseOfMFC");
            if (useOfMFC != null) {
                _useOfMFC = (UseOfMFC) Enum.ToObject(typeof(UseOfMFC), 
                    int.Parse(useOfMFC, CultureInfo.InvariantCulture));
            }

            // get ATL settings
            string useOfATL = GetXmlAttributeValue(elem, "UseOfATL");
            if (useOfATL != null) {
                _useOfATL = (UseOfATL) Enum.ToObject(typeof(UseOfATL), 
                    int.Parse(useOfATL, CultureInfo.InvariantCulture));
            }

            _linkerConfiguration = new LinkerConfig(this);
        }

        #endregion Internal Instance Constructors

        #region Public Instance Properties

        public ConfigurationType Type {
            get { return _type; }
        }

        public bool WholeProgramOptimization {
            get { return _wholeProgramOptimization; }
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
        /// Gets a value indicating how MFC is used by the configuration.
        /// </summary>
        public UseOfMFC UseOfMFC {
            get { return _useOfMFC; }
        }

        /// <summary>
        /// Gets a value indicating how ATL is used by the configuration.
        /// </summary>
        public UseOfATL UseOfATL {
            get { return _useOfATL; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        internal string RawRelativeOutputDir {
            get { return _rawRelativeOutputDir;}
        }

        internal string RawIntermediateDir {
            get { return _rawIntermediateDir;}
        }

        internal string RawReferencesPath {
            get { return _rawReferencesPath; }
        }

        internal LinkerConfig LinkerConfiguration {
            get { return _linkerConfiguration; }
        }

        #endregion Internal Instance Properties

        #region Override implementation of ConfigurationBase

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
                return new DirectoryInfo(FileUtils.CombinePaths(Project.ProjectDirectory.FullName, 
                    IntermediateDir));
            }
        }

        /// <summary>
        /// Get the path of the output directory relative to the project
        /// directory.
        /// </summary>
        public override string RelativeOutputDir {
            get { return ExpandMacros(RawRelativeOutputDir) ; }
        }

        #endregion Override implementation of ConfigurationBase

        #region Override implementation of VcConfigurationBase

        /// <summary>
        /// Gets the intermediate directory, specified relative to project 
        /// directory.
        /// </summary>
        /// <value>
        /// The intermediate directory, specified relative to project directory.
        /// </value>
        public override string IntermediateDir {
            get { return ExpandMacros(RawIntermediateDir); }
        }

        /// <summary>
        /// Gets the path for the output file.
        /// </summary>
        /// <value>
        /// The path for the output file, or <see langword="null" /> if there's
        /// no output file for this configuration.
        /// </value>
        public override string OutputPath {
            get {
                // TODO : we might to move this to another property called TargetPath
                // as the current implementation will not match the real output file
                // of the project configuration
                //
                // Note: to determine the real output path we may even need access to
                // the sources of the project (if no output file for linker is 
                // explicitly set to an empty string)
                string outputPath = null;

                switch (Type) {
                    case ConfigurationType.Application:
                        string applicationOutput = GetToolSetting(VcConfigurationBase.LinkerTool, "OutputFile");
                        if (StringUtils.IsNullOrEmpty(applicationOutput)) {
                            applicationOutput = ExpandMacros("$(OutDir)/$(ProjectName).exe");
                        }
                        outputPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, applicationOutput);
                        break;
                    case ConfigurationType.DynamicLibrary:
                        string libraryOutput = GetToolSetting(VcConfigurationBase.LinkerTool, "OutputFile");
                        if (StringUtils.IsNullOrEmpty(libraryOutput)) {
                            libraryOutput = ExpandMacros("$(OutDir)/$(ProjectName).dll");
                        }
                        outputPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, libraryOutput);
                        break;
                    case ConfigurationType.StaticLibrary:
                        string librarianOutput = GetToolSetting("VCLibrarianTool", "OutputFile");
                        if (StringUtils.IsNullOrEmpty(librarianOutput)) {
                            librarianOutput = ExpandMacros("$(OutDir)/$(ProjectName).lib");
                        }
                        outputPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, librarianOutput);
                        break;
                    case ConfigurationType.Makefile:
                        string nmakeOutput = GetToolSetting("VCNMakeTool", "Output");
                        if (!StringUtils.IsNullOrEmpty(nmakeOutput)) {
                            outputPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, nmakeOutput);
                        }
                        break;
                    case ConfigurationType.Utility:
                        return null;
                }

                return outputPath;
            }
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
        public override string ReferencesPath {
            get { return ExpandMacros(RawReferencesPath); }
        }

        internal string GetToolSetting(string toolName, string settingName, ExpansionHandler expander) {
            return GetToolSetting(toolName, settingName, (string) null, expander);
        }

        public override string GetToolSetting(string toolName, string settingName, string defaultValue) {
            return GetToolSetting(toolName, settingName, defaultValue, 
                new ExpansionHandler(ExpandMacros));
        }

        internal string GetToolSetting(string toolName, string settingName, string defaultValue, ExpansionHandler expander) {
            string setting = null;

            Hashtable toolSettings = (Hashtable) Tools[toolName];
            if (toolSettings != null) {
                setting = (string) toolSettings[settingName];
                if (setting != null) {
                    // expand macros
                    return expander(setting);
                }
            }

            if (setting == null && defaultValue != null) {
                return expander(defaultValue);
            }

            return setting;
        }

        public override Hashtable GetToolArguments(string toolName, VcArgumentMap argMap, VcArgumentMap.ArgGroup ignoreGroup) {
            return GetToolArguments(toolName, argMap, ignoreGroup, new ExpansionHandler(ExpandMacros));
        }

        internal Hashtable GetToolArguments(string toolName, VcArgumentMap argMap, VcArgumentMap.ArgGroup ignoreGroup, ExpansionHandler expander) {
            Hashtable args = CollectionsUtil.CreateCaseInsensitiveHashtable();

            Hashtable toolSettings = (Hashtable) Tools[toolName];
            if (toolSettings != null) {
                foreach (DictionaryEntry de in toolSettings) {
                    string arg = argMap.GetArgument((string) de.Key, expander((string) de.Value), ignoreGroup);
                    if (arg != null) {
                        args[(string) de.Key] = arg;
                    }
                }
            }
            return args;
        }

        #endregion Override implementation of VcConfigurationBase

        #region Internal Instance Methods


        #endregion Internal Instance Methods

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

        #region Private Instance Fields

        private readonly string _rawRelativeOutputDir;
        private readonly string _rawIntermediateDir;
        private readonly string _rawReferencesPath;
        private readonly ConfigurationType _type;
        private readonly bool _wholeProgramOptimization;
        private readonly bool _managedExtensions;
        private readonly CharacterSet _characterSet = CharacterSet.NotSet;
        private readonly UseOfMFC _useOfMFC = UseOfMFC.NotUsing;
        private readonly UseOfATL _useOfATL = UseOfATL.NotUsing;
        private readonly LinkerConfig _linkerConfiguration;

        #endregion Private Instance Fields

        /// <summary>
        /// The type of output for a given configuration.
        /// </summary>
        public enum ConfigurationType {
            /// <summary>
            /// A Makefile.
            /// </summary>
            Makefile = 0,

            /// <summary>
            /// Application (.exe).
            /// </summary>
            Application = 1,

            /// <summary>
            /// Dynamic Library (.dll).
            /// </summary>
            DynamicLibrary = 2,

            /// <summary>
            /// Static Library (.lib).
            /// </summary>
            StaticLibrary = 4,

            /// <summary>
            /// Utility.
            /// </summary>
            Utility = 10
        }

        internal class LinkerConfig {
            #region Private Instance Constructor

            internal LinkerConfig(VcProjectConfiguration projectConfig) {
                _projectConfig = projectConfig;
            }

            #endregion Private Instance Constructor

            #region Public Instance Properties

            /// <summary>
            /// Gets the absolute path to the import library to generate.
            /// </summary>
            /// <value>
            /// The absolute path to the import library to generate, or
            /// <see langword="null" /> if no import library must be generated.
            /// </value>
            public string ImportLibrary {
                get {
                    string importLibrary = StringUtils.ConvertEmptyToNull(
                        _projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, 
                        "ImportLibrary"));
                    if (importLibrary != null) {
                        if (_projectConfig.OutputDir != null) {
                            importLibrary = FileUtils.CombinePaths(_projectConfig.OutputDir.FullName, Path.GetFileName(importLibrary));
                        } else {
                            importLibrary = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, importLibrary);
                        }
                    }
                    return importLibrary;
                }
            }

            #endregion Public Instance Properties

            #region Private Instance Properties

            private VcProject Project {
                get { return (VcProject) _projectConfig.Project; }
            }

            #endregion Private Instance Properties

            #region Private Instance Fields

            private readonly VcProjectConfiguration _projectConfig;

            #endregion Private Instance Fields
        }
    }
}
