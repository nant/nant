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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
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
                switch(managedExtentions.ToLower()) {
                    case "false":
                    case "0":
                        _managedExtensions = false;
                        break;
                    case "true":
                    case "1":
                        _managedExtensions = true;
                        break;
                    default:
                        throw new BuildException(String.Format("ManagedExtensions '{0}' is not supported yet.",managedExtentions));
                }
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

        /// <summary>
        /// Gets the list of files to link in the order in which they are 
        /// defined in the project file.
        /// </summary>
        internal ArrayList ObjFiles {
            get {
                // lazy init
                if (!_initialized) {
                    Initialize();
                }
                return _objFiles;
            }
        }

        /// <summary>
        /// Holds the C++ sources for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the C++ source files for that
        /// build configuration.
        /// </remarks>
        internal Hashtable SourceConfigs {
            get {
                // lazy init
                if (!_initialized) {
                    Initialize();
                }
                return _sourceConfigs;
            }
        }

        /// <summary>
        /// Gets the resources for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the resources files for that
        /// build configuration.
        /// </remarks>
        internal Hashtable RcConfigs {
            get {
                // lazy init
                if (!_initialized) {
                    Initialize();
                }
                return _rcConfigs;
            }
        }

        /// <summary>
        /// Get the IDL files for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the IDL files for that build 
        /// configuration.
        /// </remarks>
        internal Hashtable IdlConfigs {
            get {
                // lazy init
                if (!_initialized) {
                    Initialize();
                }
                return _idlConfigs;
            }
        }

        #endregion Internal Instance Properties

        #region Private Instance Properties

        /// <summary>
        /// Gets the target path for usage in macro expansion.
        /// </summary>
        /// <value>
        /// The target path, or a zero-length string if there's no output file 
        /// for this configuration.
        /// </value>
        private string TargetPath {
            get {
                string targetPath = string.Empty;

                switch (Type) {
                    case ConfigurationType.Application:
                        string applicationOutput = GetToolSetting(VcConfigurationBase.LinkerTool, "OutputFile");
                        if (String.IsNullOrEmpty(applicationOutput)) {
                            applicationOutput = ExpandMacros("$(OutDir)/$(ProjectName).exe");
                        }
                        targetPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, applicationOutput);
                        break;
                    case ConfigurationType.DynamicLibrary:
                        string libraryOutput = GetToolSetting(VcConfigurationBase.LinkerTool, "OutputFile");
                        if (String.IsNullOrEmpty(libraryOutput)) {
                            libraryOutput = ExpandMacros("$(OutDir)/$(ProjectName).dll");
                        }
                        targetPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, libraryOutput);
                        break;
                    case ConfigurationType.StaticLibrary:
                        string librarianOutput = GetToolSetting(VcConfigurationBase.LibTool, "OutputFile");
                        if (String.IsNullOrEmpty(librarianOutput)) {
                            librarianOutput = ExpandMacros("$(OutDir)/$(ProjectName).lib");
                        }
                        targetPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, librarianOutput);
                        break;
                    case ConfigurationType.Makefile:
                        string nmakeOutput = GetToolSetting(VcConfigurationBase.NMakeTool, "Output");
                        if (!String.IsNullOrEmpty(nmakeOutput)) {
                            targetPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, nmakeOutput);
                        }
                        break;
                    case ConfigurationType.Utility:
                        break;
                }

                return targetPath;
            }
        }

        #endregion Private Instance Properties

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
            get { return ExpandMacros(RawRelativeOutputDir); }
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
        /// Gets the absolute path for the output file.
        /// </summary>
        /// <value>
        /// The absolute path for the output file, or <see langword="null" /> 
        /// if there's no output file for this configuration.
        /// </value>
        public override string OutputPath {
            get {
                // lazy init
                if (!_initialized) {
                    Initialize();
                }
                return _outputPath;
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
        /// <exception cref="NotImplementedException">
        ///   <para>Expansion of a given macro is not yet implemented.</para>
        /// </exception>
        protected internal override string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "targetname": // E.g. WindowsApplication1
                    return Path.GetFileNameWithoutExtension(Path.GetFileName(
                        TargetPath));
                case "targetpath": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\bin\Debug\WindowsApplications1.exe
                    return TargetPath;
                case "targetext": // E.g. .exe
                    return Path.GetExtension(TargetPath);
                case "targetfilename": // E.g. WindowsApplications1.exe
                    return Path.GetFileName(TargetPath);
                case "targetdir": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\bin\Debug
                    return Path.GetDirectoryName(TargetPath) + (TargetPath.EndsWith(
                        Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) 
                        ? string.Empty : Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture));
                default:
                    return base.ExpandMacro(macro);
            }
        }

        #endregion Override implementation of VcConfigurationBase

        #region Private Instance Methods

        private void Initialize() {
            VcProject vcProject = (VcProject) Project;

            // determine directory for storing intermediate build output for
            // current project build configuration
            string intermediateDir = FileUtils.CombinePaths(vcProject.ProjectDirectory.FullName, 
                IntermediateDir);

            foreach (object projectFile in vcProject.ProjectFiles) {
                string fileName = null;
                VcConfigurationBase fileConfig = null;

                // the array list contains either strings or hashtables
                if (projectFile is string) {
                    fileName = (string) projectFile;
                } else {
                    Hashtable fileConfigurations = (Hashtable) projectFile;
                    // obtain file configuration for current build configuration
                    VcFileConfiguration configuration = (VcFileConfiguration) 
                        fileConfigurations[Name];
                    if (configuration != null && configuration.ExcludeFromBuild) {
                        continue;
                    }
                    fileConfig = configuration;

                    // determine relative path
                    if (configuration == null) {
                        // obtain relative path for other build configuration
                        // as the relative is the same anyway
                        foreach (DictionaryEntry de in fileConfigurations) {
                            configuration = (VcFileConfiguration) de.Value;
                            break;
                        }
                    }
                    fileName = configuration.RelativePath;
                }

                string ext = Path.GetExtension(fileName).ToLower(CultureInfo.InvariantCulture);

                // if there's no specific file configuration (for the current
                // build configuration), then use the project configuration
                if (fileConfig == null) {
                    fileConfig = this;
                }

                switch (ext) {
                    case ".cpp":
                    case ".c":
                        if (!_sourceConfigs.ContainsKey(fileConfig)) {
                            _sourceConfigs[fileConfig] = new ArrayList(1);
                        }

                        // add file to list of sources to build with this config
                        ((ArrayList) _sourceConfigs[fileConfig]).Add(fileName);

                        // register output file for linking
                        _objFiles.Add(vcProject.GetObjOutputFile(fileName, 
                            fileConfig, intermediateDir));
                        break;
                    case ".rc":
                        if (!_rcConfigs.ContainsKey(fileConfig)) {
                            _rcConfigs[fileConfig] = new ArrayList(1);
                        }

                        // add file to list of resources to build with this config
                        ((ArrayList) _rcConfigs[fileConfig]).Add(fileName);

                        // register output file for linking
                        _objFiles.Add(vcProject.GetResourceOutputFile(fileName, 
                            fileConfig));
                        break;
                    case ".idl":
                    case ".odl": // ODL is used for old OLE objects
                        if (!_idlConfigs.ContainsKey(fileConfig)) {
                            _idlConfigs[fileConfig] = new ArrayList(1);
                        }

                        // add file to list of idl's to build with this config
                        ((ArrayList) _idlConfigs[fileConfig]).Add(fileName);
                        break;
                }
            }

            switch (Type) {
                case ConfigurationType.StaticLibrary:
                    _outputPath = GetLibrarianOutputFile(intermediateDir);
                    break;
                case ConfigurationType.Application:
                case ConfigurationType.DynamicLibrary:
                    _outputPath = GetLinkerOutputFile();
                    break;
                case ConfigurationType.Makefile:
                    string nmakeOutput = GetToolSetting(VcConfigurationBase.NMakeTool, "Output");
                    if (!String.IsNullOrEmpty(nmakeOutput)) {
                        _outputPath = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, nmakeOutput);
                    }
                    break;
            }

            // mark initialization complete
            _initialized = true;
        }

        private string GetLibrarianOutputFile(string intermediateDir) {
            if (_objFiles.Count == 0) {
                return null;
            }

            string outFile = GetToolSetting(VcConfigurationBase.LibTool, 
                "OutputFile", "$(OutDir)/$(ProjectName).lib");
            // if OutputFile is explicitly set to an empty string, VS.NET
            // uses file name of first obj file (in intermediate directory)
            if (String.IsNullOrEmpty(outFile)) {
                outFile = FileUtils.CombinePaths(intermediateDir,
                    Path.GetFileNameWithoutExtension((string) _objFiles[0]) 
                    + ".lib");
            } else {
                outFile = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, 
                    outFile);
            }
            return outFile;
        }

        private string GetLinkerOutputFile() {
            const string noinherit = "$(noinherit)";

            string addDeps = GetToolSetting(VcConfigurationBase.LinkerTool, "AdditionalDependencies");
            if (!String.IsNullOrEmpty(addDeps)) {
                // remove noherit macro from addDeps
                if (addDeps.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit) != -1) {
                    addDeps = addDeps.Remove(addDeps.ToLower(CultureInfo.InvariantCulture).IndexOf(noinherit), noinherit.Length);
                }

                string[] depParts = addDeps.Split(' ');
                for (int i = 0; i < depParts.Length; i++) {
                    string addDep = depParts[i];
                    if (Path.GetExtension(addDep) == ".obj") {
                        _objFiles.Insert(i, addDep);
                    }
                }
            }

            if (_objFiles.Count == 0) {
                return null;
            }

            string extension = string.Empty;

            switch (Type) {
                case ConfigurationType.Application:
                    extension = ".exe";
                    break;
                case ConfigurationType.DynamicLibrary:
                    extension = ".dll";
                    break;
            }

            // output file name
            string outFile = GetToolSetting(VcConfigurationBase.LinkerTool, 
                "OutputFile", "$(OutDir)/$(ProjectName)" + extension);
            // if OutputFile is explicitly set to an empty string, VS.NET
            // uses file name of first obj file (in the current directory) and 
            // extention based on configuration type 
            if (String.IsNullOrEmpty(outFile)) {
                outFile = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, 
                    Path.GetFileNameWithoutExtension((string) _objFiles[0]) +
                    extension);
            }
            if (SolutionTask.OutputDir != null) {
                outFile = FileUtils.CombinePaths(SolutionTask.OutputDir.FullName, 
                    Path.GetFileName(outFile));
            } else {
                outFile = FileUtils.CombinePaths(Project.ProjectDirectory.FullName, 
                    outFile);
            }

            return outFile;
        }

        #endregion Private Instance Methods

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
        private bool _initialized;

        /// <summary>
        /// Holds the output path for this build configuration.
        /// </summary>
        /// <remarks>
        /// Lazy initialized by <see cref="Initialize()" />.
        /// </remarks>
        private string _outputPath;

        /// <summary>
        /// Holds list of files to link in the order in which they are defined
        /// in the project file.
        /// </summary>
        private readonly ArrayList _objFiles = new ArrayList();

        /// <summary>
        /// Holds the C++ sources for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the C++ source files for that
        /// build configuration.
        /// </remarks>
        private readonly Hashtable _sourceConfigs = new Hashtable();

        /// <summary>
        /// Holds the resources for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the resources files for that
        /// build configuration.
        /// </remarks>
        private readonly Hashtable _rcConfigs = new Hashtable();

        /// <summary>
        /// Holds the IDL files for each build configuration.
        /// </summary>
        /// <remarks>
        /// The key of the hashtable is a build configuration, and the
        /// value is an ArrayList holding the IDL files for that build 
        /// configuration.
        /// </remarks>
        private readonly Hashtable _idlConfigs = new Hashtable();

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
            /// Gets a <see cref="FileInfo" /> instance representing the 
            /// absolute path to the import library to generate.
            /// </summary>
            /// <value>
            /// A <see cref="FileInfo" /> representing the absolute path to the
            /// import library to generate, or <see langword="null" /> if no 
            /// import library must be generated.
            /// </value>
            public FileInfo ImportLibrary {
                get {
                    string defaultImportLibrary = null;
                    if (!Project.IsManaged(_projectConfig.SolutionTask.SolutionConfig)) {
                        defaultImportLibrary = "$(OutDir)/$(TargetName).lib";
                    }

                    string importLibrary = StringUtils.ConvertEmptyToNull(
                        _projectConfig.GetToolSetting(VcConfigurationBase.LinkerTool, 
                        "ImportLibrary", defaultImportLibrary));
                    if (importLibrary == null) {
                        // no import library must be generated
                        return null;
                    }

                    if (_projectConfig.SolutionTask.OutputDir != null) {
                        importLibrary = FileUtils.CombinePaths(
                            _projectConfig.SolutionTask.OutputDir.FullName, 
                            Path.GetFileName(importLibrary));
                    } else {
                        importLibrary = FileUtils.CombinePaths(
                            Project.ProjectDirectory.FullName, importLibrary);
                    }
                    return new FileInfo(importLibrary);
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
