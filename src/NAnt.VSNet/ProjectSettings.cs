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

using NAnt.Core.Util;

namespace NAnt.VSNet {
    public class ProjectSettings {
        #region Public Instance Constructors

        public ProjectSettings(XmlElement elemRoot, XmlElement elemSettings, TempFileCollection tfc) {
            _elemSettings = elemSettings;
            _tfc = tfc;

            // ensure the temp dir exists
            Directory.CreateDirectory(_tfc.BasePath);

            _settings = new ArrayList();

            string extension = string.Empty;

            if (elemRoot.FirstChild.Name == "VisualBasic") {
                _type = ProjectType.VBNet;
            } else {
                _type = ProjectType.CSharp;
            }

            _guid = elemRoot.FirstChild.Attributes["ProjectGuid"].Value.ToUpper(CultureInfo.InvariantCulture);

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
                if (RootNamespace != null && Type == ProjectType.VBNet) {
                    _settings.Add("/rootnamespace:" + _rootNamespace);
                }
            }

            htStringSettings["ApplicationIcon"] = @"/win32icon:""{0}""";

            foreach (DictionaryEntry de in htStringSettings) {
                string strValue = elemSettings.GetAttribute(de.Key.ToString());
                if (!StringUtils.IsNullOrEmpty(strValue)) {
                    _settings.Add(string.Format(de.Value.ToString(), strValue));
                }
            }
        }

        #endregion Public Instance Constructors

        #region Finalizer

        ~ProjectSettings() {
            _tfc.Delete();
        }

        #endregion Finalizer

        #region Public Instance Properties

        public string[] Settings {
            get { return (string[]) _settings.ToArray(typeof(string)); }
        }

        public string RootDirectory {
            get { return _rootDirectory; }
            set { _rootDirectory = value; }
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
            get { return _tfc; }
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

        public ProjectType Type {
            get { return _type; }
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

        #region Public Instance Methods

        public string GetTemporaryFilename(string fileName) {
            return Path.Combine(_tfc.BasePath, fileName);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private ArrayList _settings;
        private string _assemblyName;
        private string _assemblyOriginatorKeyFile;
        private string _rootDirectory;
        private string _outputExtension;
        private string _rootNamespace;
        private string _guid;
        private string _runPostBuildEvent;
        private string _preBuildEvent;
        private string _postBuildEvent;
        private XmlElement _elemSettings;
        private TempFileCollection _tfc;
        private ProjectType _type;

        #endregion Private Instance Fields
    }

    public enum ProjectType {
        VBNet = 0,
        CSharp = 1
    }
}
