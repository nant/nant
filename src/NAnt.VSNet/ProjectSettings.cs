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
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public class ProjectSettings {
        #region Public Instance Constructors

        public ProjectSettings(XmlElement elemRoot, XmlElement elemSettings, ManagedProjectBase project) {
            _project = project;
            _settings = new ArrayList();

            // check whether build file is valid
            if (elemRoot.FirstChild == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project file '{0}' is not valid.", Project.ProjectPath),
                    Location.UnknownLocation);
            }

            _guid = ProjectSettings.GetProjectGuid(project.ProjectPath,
                elemRoot);

            // determine output type of this project
            _outputType = GetOutputType(elemSettings);

            // initialize hashtable for holding string settings
            Hashtable htStringSettings = new Hashtable();

            switch (_outputType) {
                case ManagedOutputType.Library:
                    _settings.Add("/target:library");
                    break;
                case ManagedOutputType.Executable:
                    _settings.Add("/target:exe");
                    // startup object only makes sense for executable assemblies
                    htStringSettings["StartupObject"] = @"/main:""{0}""";
                    break;
                case ManagedOutputType.WindowsExecutable:
                    _settings.Add("/target:winexe");
                    // startup object only makes sense for executable assemblies
                    htStringSettings["StartupObject"] = @"/main:""{0}""";
                    break;
            }

            // suppresses display of Microsoft startup banner
            _settings.Add("/nologo");

            _assemblyName = elemSettings.Attributes["AssemblyName"].Value;

            // the key file to use to sign ActiveX/COM wrappers
            _assemblyOriginatorKeyFile = StringUtils.ConvertEmptyToNull(
                elemSettings.Attributes["AssemblyOriginatorKeyFile"].Value);

            // the key container to use to sign ActiveX/COM wrappers
            _assemblyKeyContainerName = StringUtils.ConvertEmptyToNull(
                elemSettings.Attributes["AssemblyKeyContainerName"].Value);

            // pre and post build events are VS .NET 2003 specific, so do not 
            // assume they are there
            if (elemSettings.Attributes["RunPostBuildEvent"] != null) {
                _runPostBuildEvent = StringUtils.ConvertEmptyToNull(
                    elemSettings.Attributes["RunPostBuildEvent"].Value);
            }

            if (elemSettings.Attributes["PreBuildEvent"] != null) {
                _preBuildEvent = StringUtils.ConvertEmptyToNull(
                    elemSettings.Attributes["PreBuildEvent"].Value);
            }
            
            if (elemSettings.Attributes["PostBuildEvent"] != null) {
                _postBuildEvent = StringUtils.ConvertEmptyToNull(
                    elemSettings.Attributes["PostBuildEvent"].Value);
            }

            if (elemSettings.Attributes["RootNamespace"] != null) {
                _rootNamespace = StringUtils.ConvertEmptyToNull(
                    elemSettings.Attributes["RootNamespace"].Value);
                if (RootNamespace != null && Project.Type == ProjectType.VB) {
                    _settings.Add("/rootnamespace:" + _rootNamespace);
                }
            }

            if (elemSettings.Attributes["ApplicationIcon"] != null) {
                string value = StringUtils.ConvertEmptyToNull(
                    elemSettings.Attributes["ApplicationIcon"].Value);
                if (value != null) {
                    _applicationIcon = new FileInfo(FileUtils.CombinePaths(
                        Project.ProjectDirectory.FullName, value));
                }
            }

            // process VB.NET specific project settings
            if (Project.Type == ProjectType.VB) {
                if (elemSettings.Attributes["OptionExplicit"] != null) {
                    if (elemSettings.Attributes ["OptionExplicit"].Value == "Off") {
                        _settings.Add("/optionexplicit-");
                    } else {
                        _settings.Add("/optionexplicit+");
                    }
                }

                if (elemSettings.Attributes["OptionStrict"] != null) {
                    if (elemSettings.Attributes ["OptionStrict"].Value == "Off") {
                        _settings.Add("/optionstrict-");
                    } else {
                        _settings.Add("/optionstrict+");
                    }
                }

                if (elemSettings.Attributes["OptionCompare"] != null) {
                    if (elemSettings.Attributes ["OptionCompare"].Value == "Text") {
                        _settings.Add("/optioncompare:text");
                    } else {
                        _settings.Add("/optioncompare:binary");
                    }
                }
            }

            foreach (DictionaryEntry de in htStringSettings) {
                string value = elemSettings.GetAttribute(de.Key.ToString());
                if (String.IsNullOrEmpty(value)) {
                    // skip empty values
                    continue;
                }
                _settings.Add(string.Format(de.Value.ToString(), value));
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        /// <summary>
        /// Gets the .ico file to use as application icon.
        /// </summary>
        /// <value>
        /// The .ico file to use as application icon, or <see langword="null" /> 
        /// if no application icon should be used.
        /// </value>
        public FileInfo ApplicationIcon {
            get { return _applicationIcon; }
        }

        public string AssemblyName {
            get { return _assemblyName; }
        }

        /// <summary>
        /// Gets the key file to use to sign ActiveX/COM wrappers.
        /// </summary>
        /// <value>
        /// The path of the key file to use to sign ActiveX/COM wrappers, 
        /// relative to the project root directory, or <see langword="null" />
        /// if the wrapper assembly should not be signed using a key file.
        /// </value>
        public string AssemblyOriginatorKeyFile {
            get { return _assemblyOriginatorKeyFile; }
        }

        /// <summary>
        /// Gets the key name to use to sign ActiveX/COM wrappers.
        /// </summary>
        /// <value>
        /// The name of the key container to use to sign ActiveX/COM wrappers,
        /// or <see langword="null" /> if the wrapper assembly should not be
        /// signed using a key container.
        /// </value>
        public string AssemblyKeyContainerName {
            get { return _assemblyKeyContainerName; }
        }

        public TempFileCollection TemporaryFiles {
            get { return Project.TemporaryFiles; }
        }

        public string OutputFileName {
            get { return string.Concat(AssemblyName, OutputExtension); }
        }

        /// <summary>
        /// Gets the output type of this project.
        /// </summary>
        public ManagedOutputType OutputType {
            get { return _outputType; }
        }

        public string OutputExtension {
            get { 
                switch (OutputType) {
                    case ManagedOutputType.Library:
                        return ".dll";
                    case ManagedOutputType.Executable:
                    case ManagedOutputType.WindowsExecutable:
                    default:
                        return ".exe";
                }
            }
        }

        public string RootNamespace {
            get { return _rootNamespace; }
        }

        public string Guid {
            get { return _guid; }
        }

        /// <summary>
        /// Designates when the <see cref="PostBuildEvent" /> command line should
        /// be run. Possible values are "OnBuildSuccess", "Always" or 
        /// "OnOutputUpdated".
        /// </summary>
        public string RunPostBuildEvent {
            get { return _runPostBuildEvent; }
        }

        /// <summary>
        /// Contains commands to be run before a build takes place.
        /// </summary>
        /// <remarks>
        /// Valid commands are those in a .bat file. For more info see MSDN.
        /// </remarks>
        public string PreBuildEvent {
            get { return _preBuildEvent; }
        }

        /// <summary>
        /// Contains commands to be ran after a build has taken place.
        /// </summary>
        /// <remarks>
        /// Valid commands are those in a .bat file. For more info see MSDN.
        /// </remarks>
        public string PostBuildEvent {
            get { return _postBuildEvent; }
        }
        
        #endregion Public Instance Properties

        #region Private Instance Properties

        private ManagedProjectBase Project {
            get { return _project; }
        }

        #endregion Private Instance Properties

        #region Protected Instance Methods

        /// <summary>
        /// Determines the output type of the project from its XML definition.
        /// </summary>
        /// <param name="settingsXml">The XML definition of the project settings.</param>
        /// <returns>
        /// The output type of the project.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>
        ///   The output type of the project is not set in the specified XML 
        ///   definition.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The output type of the project is not supported.
        ///   </para>
        /// </exception>
        protected virtual ManagedOutputType GetOutputType(XmlElement settingsXml) {
            XmlAttribute outputTypeAttribute = settingsXml.Attributes["OutputType"];
            if (outputTypeAttribute == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project \"{0}\" is invalid: the output type is not set.",
                    Project.Name), Location.UnknownLocation);
            }

            switch (outputTypeAttribute.Value.ToLower(CultureInfo.InvariantCulture)) {
                case "library":
                    return ManagedOutputType.Library;
                case "exe":
                    return ManagedOutputType.Executable;
                case "winexe":
                    return ManagedOutputType.WindowsExecutable;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Output type \"{0}\" of project \"{1}\" is not supported.", 
                        outputTypeAttribute.Value, Project.Name), Location.UnknownLocation);
            }
        }

        #endregion Protected Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Gets the project GUID from the given <see cref="XmlElement" /> 
        /// holding a <c>&lt;VisualStudioProject&gt;</c> node.
        /// </summary>
        /// <param name="projectFile">The path of the project file.</param>
        /// <param name="elemRoot">The <c>&lt;VisualStudioProject&gt;</c> node from which the project GUID should be retrieved.</param>
        /// <returns>
        /// The project GUID from specified <c>&lt;VisualStudioProject&gt;</c> node.
        /// </returns>
        public static string GetProjectGuid(string projectFile, XmlElement elemRoot) {
            XmlAttribute projectGuid = (XmlAttribute) elemRoot.FirstChild.
                Attributes["ProjectGuid"];
            if (projectGuid == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project file '{0}' is not valid. There's no \"ProjectGuid\""
                    + " attribute on the <{1} ... /> node.", projectFile, 
                    elemRoot.FirstChild.Name), Location.UnknownLocation);
            }
            return projectGuid.Value.ToUpper(CultureInfo.InvariantCulture);
        }

        #endregion Public Static Methods

        #region Public Instance Methods

        public string GetTemporaryFilename(string fileName) {
            return FileUtils.CombinePaths(TemporaryFiles.BasePath, fileName);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private readonly ArrayList _settings;
        private readonly FileInfo _applicationIcon;
        private readonly ManagedProjectBase _project;
        private readonly string _assemblyName;
        private readonly string _assemblyOriginatorKeyFile;
        private readonly string _assemblyKeyContainerName;
        private readonly string _rootNamespace;
        private readonly string _guid;
        private readonly string _runPostBuildEvent;
        private readonly string _preBuildEvent;
        private readonly string _postBuildEvent;
        private readonly ManagedOutputType _outputType;

        #endregion Private Instance Fields
    }
}
