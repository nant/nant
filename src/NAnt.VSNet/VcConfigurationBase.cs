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
// Gert Driesen (drieseng@users.sourceforge.net)
// Hani Atassi (haniatassi@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;
using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    /// <summary>
    /// A single build configuration for a Visual C++ project or for a specific
    /// file in the project.
    /// </summary>
    public abstract class VcConfigurationBase : ConfigurationBase {
        #region Delegates

        public delegate string ExpansionHandler(string value);

        #endregion Delegates

        #region Protected Instance Constructors

        protected VcConfigurationBase(XmlElement elem, ProjectBase parentProject, DirectoryInfo outputDir) : base(parentProject) {
            if (elem == null) {
                throw new ArgumentNullException("elem");
            }

            // output directory override (if specified)
            _outputDir = outputDir;

            // get name of configuration (also contains the targeted platform)
            _name = elem.GetAttribute("Name");

            XmlNodeList tools = elem.GetElementsByTagName("Tool");
            foreach (XmlElement toolElem in tools) {
                string toolName = toolElem.GetAttribute("Name");
                Hashtable htToolSettings = CollectionsUtil.CreateCaseInsensitiveHashtable();

                foreach(XmlAttribute attr in toolElem.Attributes) {
                    if (attr.Name != "Name") {
                        htToolSettings[attr.Name] = attr.Value;
                    }
                }

                Tools[toolName] = htToolSettings;
            }
        }

        protected VcConfigurationBase(string configName, ProjectBase parentProject, DirectoryInfo outputDir)  : base(parentProject) {
            _name = configName;

            // set output directory (if specified)
            _outputDir = outputDir;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the intermediate directory, specified relative to project 
        /// directory.
        /// </summary>
        /// <value>
        /// The intermediate directory, specified relative to project directory.
        /// </value>
        public abstract string IntermediateDir {
            get;
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
        public abstract string ReferencesPath {
            get;
        }

        public UsePrecompiledHeader UsePrecompiledHeader {
            get {
                string usePCHString = GetToolSetting("VCCLCompilerTool", 
                    "UsePrecompiledHeader");
                if (usePCHString == null) {
                    return UsePrecompiledHeader.Unspecified;
                }
                int intVal = int.Parse(usePCHString, CultureInfo.InvariantCulture);

                if (this.Project.ProductVersion >= ProductVersion.Whidbey) {
                    switch(intVal) {
                        case 0 :
                            return UsePrecompiledHeader.No;
                        case 1:
                            return UsePrecompiledHeader.Create;
                        case 2:
                            return UsePrecompiledHeader.Use;
                    }
                }
                return (UsePrecompiledHeader) Enum.ToObject(typeof(UsePrecompiledHeader), intVal);
            }
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

        #endregion Internal Instance Properties

        #region Override implementation of ConfigurationBase

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        public override DirectoryInfo OutputDir {
            get {
                if (_outputDir == null) {
                    if (RelativeOutputDir != null) {
                        _outputDir = new DirectoryInfo(FileUtils.CombinePaths(
                            Project.ProjectDirectory.FullName, RelativeOutputDir));
                    } else {
                        throw new BuildException("The output directory could not be"
                            + " determined.", Location.UnknownLocation);
                    }
                }

                return _outputDir;
            }
        }

        /// <summary>
        /// Gets the path in which the output file will be created before its
        /// copied to the actual output path.
        /// </summary>
        /// <remarks>
        /// For Visual C++ projects, the output file will be immediately
        /// created in the output path.
        /// </remarks>
        public override string BuildPath {
            get { return OutputPath; }
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
                        return string.Empty;
                    }
                } else {
                    return string.Empty;
                }
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

        #region Public Instance Methods

        /// <summary>
        /// Gets the value of a given setting for a specified tool.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="settingName">The name of the setting.</param>
        /// <returns>
        /// The value of a setting for the specified tool, or <see langword="null" />
        /// if the setting is not defined for the specified tool.
        /// </returns>
        /// <remarks>
        /// An empty setting value, which is used as a means to override the
        /// project default, will be returned as a empty <see cref="string" />.
        /// </remarks>
        public string GetToolSetting(string toolName, string settingName) {
            return GetToolSetting(toolName, settingName, (string) null);
        }

        /// <summary>
        /// Gets the value of a given setting for a specified tool.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="settingName">The name of the setting.</param>
        /// <param name="defaultValue">The value to return if setting is not defined.</param>
        /// <returns>
        /// The value of a setting for the specified tool, or 
        /// <paramref name="defaultValue" /> if the setting is not defined for
        /// the specified tool.
        /// </returns>
        /// <remarks>
        /// An empty setting value, which is used as a means to override the
        /// project default, will be returned as a empty <see cref="string" />.
        /// </remarks>
        public abstract string GetToolSetting(string toolName, string settingName, string defaultValue);

        public Hashtable GetToolArguments(string toolName, VcArgumentMap argMap) {
            return GetToolArguments(toolName, argMap, VcArgumentMap.ArgGroup.Unassigned);
        }

        public abstract Hashtable GetToolArguments(string toolName, VcArgumentMap argMap, VcArgumentMap.ArgGroup ignoreGroup);

        #endregion Public Instance Methods

        #region Protected Instance Fields

        protected readonly Hashtable Tools = CollectionsUtil.CreateCaseInsensitiveHashtable();

        #endregion Protected Instance Fields

        #region Private Instance Fields

        private readonly string _name;
        private DirectoryInfo _outputDir;

        #endregion Private Instance Fields

        #region Internal Static Fields

        internal const string CLCompilerTool = "VCCLCompilerTool";
        internal const string CustomBuildTool = "VCCustomBuildTool";
        internal const string LinkerTool = "VCLinkerTool";
        internal const string LibTool = "VCLibrarianTool";
        internal const string ResourceCompilerTool = "VCResourceCompilerTool";
        internal const string MIDLTool = "VCMIDLTool";
        internal const string PreBuildEventTool = "VCPreBuildEventTool";
        internal const string PostBuildEventTool = "VCPostBuildEventTool";
        internal const string PreLinkEventTool = "VCPreLinkEventTool";
        internal const string NMakeTool = "VCNMakeTool";

        #endregion Internal Static Fields
    }
}
