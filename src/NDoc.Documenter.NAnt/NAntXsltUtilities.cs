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
        public NAntXsltUtilities(StringDictionary fileNames, StringDictionary elementNames, StringDictionary namespaceNames, StringDictionary assemblyNames, StringDictionary taskNames) {
            _fileNames = fileNames;
            _elementNames = elementNames;
            _namespaceNames = namespaceNames;
            _assemblyNames = assemblyNames;
            _taskNames = taskNames;
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns the name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for which the name will be looked up.</param>
        /// <returns>
        /// The name for the specified cref.
        /// </returns>
        public string GetName(string cref) {
            if (cref.Length < 2)
                return cref;

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
            return assemblyName != null ? assemblyName : "";
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
            return namespaceName != null ? namespaceName : "";
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
            return taskName != null ? taskName : "";
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private StringDictionary _fileNames;
        private StringDictionary _elementNames;
        private StringDictionary _namespaceNames;
        private StringDictionary _assemblyNames;
        private StringDictionary _taskNames;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string SystemPrefix = "System.";

        #endregion Private Static Fields
    }
}
