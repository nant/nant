// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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

using System.Collections.Specialized;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// Provides an extension object for the Xslt transformations.
    /// </summary>
    public class NAntXsltUtilities {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NAntXsltUtilities" />
        /// class.
        /// </summary>
        public NAntXsltUtilities(StringDictionary fileNames, StringDictionary elementNames, StringDictionary namespaceNames, StringDictionary assemblyNames, StringDictionary taskNames, SdkDocVersion linkToSdkDocVersion) {
            _fileNames = fileNames;
            _elementNames = elementNames;
            _namespaceNames = namespaceNames;
            _assemblyNames = assemblyNames;
            _taskNames = taskNames;
            _linkToSdkDocVersion = linkToSdkDocVersion;

            switch (linkToSdkDocVersion) {
                case SdkDocVersion.SDK_v1_0:
                    _sdkDocBaseUrl = SdkDoc10BaseUrl;
                    _sdkDocExt = SdkDocPageExt;
                    break;
                case SdkDocVersion.SDK_v1_1:
                    _sdkDocBaseUrl = SdkDoc11BaseUrl;
                    _sdkDocExt = SdkDocPageExt;
                    break;
                case SdkDocVersion.MsdnOnline:
                    _sdkDocBaseUrl = MsdnOnlineSdkBaseUrl;
                    _sdkDocExt = MsdnOnlineSdkPageExt;
                    break;
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the base url for links to system types.
        /// </summary>
        /// <value>
        /// The base url for links to system types.
        /// </value>
        public string SdkDocBaseUrl {
            get { return _sdkDocBaseUrl; }
        }

        /// <summary>
        /// Gets the page file extension for links to system types.
        /// </summary>
        public string SdkDocExt {
            get { return _sdkDocExt; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Returns an href for a cref.
        /// </summary>
        /// <param name="cref">The cref for which the href will be looked up.</param>
        /// <returns>
        /// The href for the specified cref.
        /// </returns>
        public string GetHRef(string cref) {
            if ((cref.Length < 2) || (cref[1] != ':')) {
                return string.Empty;
            }

            if (cref.Length < 9 || cref.Substring(2, 7) != SystemPrefix) {
                string fileName = _fileNames[cref];
                if (fileName == null && cref.StartsWith("F:")) {
                    fileName = _fileNames["E:" + cref.Substring(2)];
                }

                if (fileName == null) {
                    return string.Empty;
                } else {
                    return fileName;
                }
            } else {
                switch (cref.Substring(0, 2)) {
                    case "N:":  // Namespace
                        return SdkDocBaseUrl + cref.Substring(2).Replace(".", "") + SdkDocExt;
                    case "T:":  // Type: class, interface, struct, enum, delegate
                        return SdkDocBaseUrl + cref.Substring(2).Replace(".", "") + "ClassTopic" + SdkDocExt;
                    case "F:":  // Field
                        // do not generate href for fields, as the .NET SDK does 
                        // not have separate pages for enum fields, and we have no
                        // way of knowing whether it's a reference to an enum field 
                        // or class field.
                        return string.Empty;
                    case "P:":  // Property
                    case "M:":  // Method
                    case "E:":  // Event
                        return GetFilenameForSystemMember(cref);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns the name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for which the name will be looked up.</param>
        /// <returns>
        /// The name for the specified cref.
        /// </returns>
        public string GetName(string cref) {
            if (cref.Length < 2) {
                return cref;
            }

            if (cref[1] == ':') {
                if (cref.Length < 9 || cref.Substring(2, 7) != SystemPrefix) {
                    string name = _elementNames[cref];
                    if (name != null) {
                        return name;
                    }
                }

                int index;
                if ((index = cref.IndexOf(".#c")) >= 0) {
                    cref = cref.Substring(2, index - 2);
                } else if ((index = cref.IndexOf("(")) >= 0) {
                    cref = cref.Substring(2, index - 2);
                } else {
                    cref = cref.Substring(2);
                }
            }

            return cref.Substring(cref.LastIndexOf(".") + 1);
        }

        /// <summary>
        /// Returns the assembly name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for which the assembly name will be looked up.</param>
        /// <returns>
        /// The assembly name for the specified cref.
        /// </returns>
        public string GetAssemblyName(string cref) {
            string assemblyName = _assemblyNames[cref];
            return assemblyName != null ? assemblyName : string.Empty;
        }

        /// <summary>
        /// Returns the namespace name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for which the namespace name will be looked up.</param>
        /// <returns>
        /// The namespace name for the specified cref.
        /// </returns>
        public string GetNamespaceName(string cref) {
            string namespaceName = _namespaceNames[cref];
            return namespaceName != null ? namespaceName : string.Empty;
        }

        /// <summary>
        /// Returns the NAnt task name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for the task name will be looked up.</param>
        /// <returns>
        /// The NAnt task name for the specified cref.
        /// </returns>
        public string GetTaskName(string cref) {
            string taskName = _taskNames[cref];
            return taskName != null ? taskName : string.Empty;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetFilenameForSystemMember(string cref) {
            string crefName;
            int index;
            if ((index = cref.IndexOf(".#c")) >= 0) {
                crefName = cref.Substring(2, index - 2) + ".ctor";
            } else if ((index = cref.IndexOf("(")) >= 0) {
                crefName = cref.Substring(2, index - 2);
            } else {
                crefName = cref.Substring(2);
            }
            index = crefName.LastIndexOf(".");
            string crefType = crefName.Substring(0, index);
            string crefMember = crefName.Substring(index + 1);
            return SdkDocBaseUrl + crefType.Replace(".", "") + "Class" + crefMember + "Topic" + SdkDocExt;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private StringDictionary _fileNames;
        private StringDictionary _elementNames;
        private StringDictionary _namespaceNames;
        private StringDictionary _assemblyNames;
        private StringDictionary _taskNames;
        private SdkDocVersion _linkToSdkDocVersion;
        private string _sdkDocBaseUrl; 
        private string _sdkDocExt; 

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string SdkDoc10BaseUrl = "ms-help://MS.NETFrameworkSDK/cpref/html/frlrf";
        private const string SdkDoc11BaseUrl = "ms-help://MS.NETFrameworkSDKv1.1/cpref/html/frlrf";
        private const string SdkDocPageExt = ".htm";
        private const string MsdnOnlineSdkBaseUrl = "http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf";
        private const string MsdnOnlineSdkPageExt = ".asp";
        private const string SystemPrefix = "System.";

        #endregion Private Static Fields
    }
}
