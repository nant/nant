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

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Xml;

namespace NAnt.VSNet {
    public class ProjectSettings {
        #region Public Instance Constructors

        public ProjectSettings(XmlElement elemRoot, XmlElement elemSettings, TempFileCollection tfc) {
            _elemSettings = elemSettings;
            _tfc = tfc;
            Directory.CreateDirectory(_tfc.BasePath);

            _settings = new ArrayList();

            string extension = string.Empty;

            if (elemRoot.FirstChild.Name == "VisualBasic") {
                _type = ProjectType.VBNet;
            } else {
                _type = ProjectType.CSharp;
            }

            _guid = elemRoot.FirstChild.Attributes["ProjectGuid"].Value.ToUpper(CultureInfo.InvariantCulture);

            switch (elemSettings.Attributes["OutputType"].Value.ToLower(CultureInfo.InvariantCulture)) {
                case "library":
                    _settings.Add("/target:library");
                    _outputExtension = ".dll";
                    break;
                case "exe":
                    _settings.Add("/target:exe");
                    _outputExtension = ".exe";
                    break;
                case "winexe":
                    _settings.Add("/target:winexe");
                    _outputExtension = ".exe";
                    break;
                default:
                    throw new ApplicationException(string.Format("Unknown output type: {0}.", elemSettings.Attributes["OutputType"].Value));
            }

            // suppresses display of Microsoft startup banner
            _settings.Add("/nologo");

            _name = elemSettings.Attributes["AssemblyName"].Value;

            if (elemSettings.Attributes["RootNamespace"] != null) {
                _rootNamespace = elemSettings.Attributes["RootNamespace"].Value;
                if (_type == ProjectType.VBNet) {
                    _settings.Add("/rootnamespace:" + _rootNamespace);
                }
            }

            Hashtable htStringSettings = new Hashtable();

            htStringSettings["ApplicationIcon"] = @"/win32icon:""{0}""";

            foreach (DictionaryEntry de in htStringSettings) {
                string strValue = elemSettings.GetAttribute(de.Key.ToString());
                if (strValue != null && strValue.Length > 0) {
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

        public string Name {
            get { return _name; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _tfc; }
        }

        public string OutputFile {
            get { return string.Concat(Name, OutputExtension); }
        }

        public string OutputExtension {
            get { return _outputExtension; }
        }

        public string RootNamespace {
            get { return _rootNamespace; }
        }

        public string GUID {
            get { return _guid; }
        }

        public ProjectType Type {
            get { return _type; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public string GetTemporaryFilename(string fileName) {
            return Path.Combine(_tfc.BasePath, fileName);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private ArrayList _settings;
        private string _name;
        private string _rootDirectory;
        private string _outputExtension;
        private string _rootNamespace;
        private string _guid;
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
