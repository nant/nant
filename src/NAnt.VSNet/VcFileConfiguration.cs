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

namespace NAnt.VSNet {
    /// <summary>
    /// Represents the configuration of a file.
    /// </summary>
    public class VcFileConfiguration : VcConfigurationBase {
        #region Internal Instance Constructors

        internal VcFileConfiguration(string relativePath, string parentName, XmlElement elem, VcProjectConfiguration parentConfig, DirectoryInfo outputDir) : base(elem, parentConfig.Project, outputDir) {
            if (relativePath == null) {
                throw new ArgumentNullException("relativePath");
            }
            if (parentName == null) {
                throw new ArgumentNullException("parentName");
            }
            if (parentConfig == null) {
                throw new ArgumentNullException("parentConfig");
            }

            _relativePath = relativePath;
            _parentName = parentName;

            string excludeFromBuild = elem.GetAttribute("ExcludedFromBuild");
            if (excludeFromBuild.Length != 0) {
                _excludeFromBuild = string.Compare(excludeFromBuild.Trim(), "true", true, CultureInfo.InvariantCulture) == 0;
            }

            _parentConfig = parentConfig;
        }

        internal VcFileConfiguration(string relativePath, string parentName, VcProjectConfiguration parentConfig, DirectoryInfo outputDir) : base(parentConfig.Name, parentConfig.Project, outputDir) {
            if (relativePath == null) {
                throw new ArgumentNullException("relativePath");
            }
            if (parentName == null) {
                throw new ArgumentNullException("parentName");
            }
            if (parentConfig == null) {
                throw new ArgumentNullException("parentConfig");
            }

            _relativePath = relativePath;
            _parentName = parentName;
            _parentConfig = parentConfig;
        }

        #endregion Internal Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets a value indication whether the file should be excluded from 
        /// the build for this configuration.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the file should be excluded from the 
        /// build for this configuration; otherwise, <see langword="false" />.
        /// </value>
        public bool ExcludeFromBuild {
            get { return _excludeFromBuild; }
        }

        /// <summary>
        /// Gets the relative path of the file.
        /// </summary>
        /// <value>
        /// The path of the file relative to the project directory.
        /// </value>
        public string RelativePath {
            get { return _relativePath; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ConfigurationBase

        /// <summary>
        /// Get the path of the output directory relative to the project
        /// directory.
        /// </summary>
        public override string RelativeOutputDir {
            get { return ExpandMacros(_parentConfig.RawRelativeOutputDir) ; }
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
                case "inputdir":
                    return Path.GetDirectoryName(FileUtils.CombinePaths(Project.ProjectDirectory.FullName, 
                        _relativePath)) + Path.DirectorySeparatorChar;
                case "inputname":
                    return Path.GetFileNameWithoutExtension(_relativePath);
                case "inputpath":
                    return FileUtils.CombinePaths(Project.ProjectDirectory.FullName, _relativePath);
                case "inputfilename":
                    return Path.GetFileName(_relativePath);
                case "inputext":
                    return Path.GetExtension(_relativePath);
                case "safeparentname":
                    return _parentName.Replace(" ", string.Empty);
                case "safeinputname":
                    return Path.GetFileNameWithoutExtension(_relativePath);
                default:
                    return _parentConfig.ExpandMacro(macro);
            }
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
            get { return ExpandMacros(_parentConfig.RawIntermediateDir); }
        }

        /// <summary>
        /// Gets the path for the output file.
        /// </summary>
        /// <value>
        /// The path for the output file, or <see langword="null" /> if there's
        /// no output file for this configuration.
        /// </value>
        public override string OutputPath {
            get { return _parentConfig.OutputPath; }
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
            get { return ExpandMacros(_parentConfig.RawReferencesPath); }
        }

        /// <summary>
        /// Gets the value of a given setting for a specified tool.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="settingName">The name of the setting.</param>
        /// <param name="projectDefault">The value to return if setting is not defined in both the file and project configuration.</param>
        /// <returns>
        /// The value of a setting for the specified tool, or 
        /// <paramref name="settingName" /> if the setting is not defined in
        /// both the file and project configuration.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   If the setting is not defined in the file configuration, then
        ///   the project level setting will be used.
        ///   </para>
        ///   <para>
        ///   An empty setting value, which is used as a means to override the
        ///   project default, will be returned as a empty <see cref="string" />.
        ///   </para>
        /// </remarks>
        public override string GetToolSetting(string toolName, string settingName, string projectDefault) {
            string setting = null;

            Hashtable toolSettings = (Hashtable) Tools[toolName];
            if (toolSettings != null) {
                setting = (string) toolSettings[settingName];
                if (setting != null) {
                    // expand macros
                    return ExpandMacros(setting);
                }
            }

            setting = _parentConfig.GetToolSetting(toolName, settingName, 
                projectDefault, new ExpansionHandler(ExpandMacros));

            return setting;
        }

        public override Hashtable GetToolArguments(string toolName, VcArgumentMap argMap, VcArgumentMap.ArgGroup ignoreGroup) {
            ExpansionHandler expander = new ExpansionHandler(ExpandMacros);

            Hashtable args;
            if (_parentConfig != null) {
                args = _parentConfig.GetToolArguments(toolName, argMap, ignoreGroup, expander);
            } else {
                args = CollectionsUtil.CreateCaseInsensitiveHashtable();
            }

            Hashtable toolSettings = (Hashtable) Tools[toolName];
            if (toolSettings != null) {
                foreach (DictionaryEntry de in toolSettings) {
                    string arg = argMap.GetArgument((string) de.Key, ExpandMacros((string) de.Value), ignoreGroup);
                    if (arg != null) {
                        args[(string) de.Key] = arg;
                    }
                }
            }
            return args;
        }

        #endregion Override implementation of VcConfigurationBase

        #region Private Instance Fields

        private readonly string _relativePath;
        private readonly string _parentName;
        private readonly bool _excludeFromBuild;
        private readonly VcProjectConfiguration _parentConfig;

        #endregion Private Instance Fields
    }
}
