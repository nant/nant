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
            _elemSettings = elemSettings;
            _project = project;
            _settings = new ArrayList();

            string extension = string.Empty;

            // check whether build file is valid
            if (elemRoot.FirstChild == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project file '{0}' is not valid.", Project.ProjectPath),
                    Location.UnknownLocation);
            }

            _guid = ProjectSettings.GetProjectGuid(project.ProjectPath,
                elemRoot);

            // initialize hashtable for holding string settings
            Hashtable htStringSettings = new Hashtable();

            switch (elemSettings.Attributes["OutputType"].Value.ToLower(CultureInfo.InvariantCulture)) {
                case "library":
                    _settings.Add("/target:library");
                    _outputExtension = ".dll";
                    break;
                case "exe":
                    _settings.Add("/target:exe");
                    _outputExtension = ".exe";
                    // startup object only makes sense for executable assemblies
                    htStringSettings["StartupObject"] = @"/main:""{0}""";
                    break;
                case "winexe":
                    _settings.Add("/target:winexe");
                    _outputExtension = ".exe";
                    // startup object only makes sense for executable assemblies
                    htStringSettings["StartupObject"] = @"/main:""{0}""";
                    break;
                default:
                    throw new ApplicationException(string.Format("Unknown output type: {0}.", elemSettings.Attributes["OutputType"].Value));
            }

            // suppresses display of Microsoft startup banner
            _settings.Add("/nologo");

            _assemblyName = elemSettings.Attributes["AssemblyName"].Value;

            // the key file to use to sign ActiveX/COM wrappers
            _assemblyOriginatorKeyFile = StringUtils.ConvertEmptyToNull(
                elemSettings.Attributes["AssemblyOriginatorKeyFile"].Value);

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
                    _applicationIcon = new FileInfo(Path.Combine(
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
                if (StringUtils.IsNullOrEmpty(value)) {
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
        /// if the wrapper assembly should not be signed.
        /// </value>
        public string AssemblyOriginatorKeyFile {
            get { return _assemblyOriginatorKeyFile; }
        }

        public TempFileCollection TemporaryFiles {
            get { return Project.TemporaryFiles; }
        }

        public string OutputFileName {
            get { return string.Concat(AssemblyName, OutputExtension); }
        }

        public string OutputExtension {
            get { return _outputExtension; }
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
            return Path.Combine(TemporaryFiles.BasePath, fileName);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private ArrayList _settings;
        private FileInfo _applicationIcon;
        private ManagedProjectBase _project;
        private string _assemblyName;
        private string _assemblyOriginatorKeyFile;
        private string _outputExtension;
        private string _rootNamespace;
        private string _guid;
        private string _runPostBuildEvent;
        private string _preBuildEvent;
        private string _postBuildEvent;
        private XmlElement _elemSettings;

        #endregion Private Instance Fields
    }
}
