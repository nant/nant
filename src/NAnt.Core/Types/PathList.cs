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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    [Serializable()]
    public class PathList {
        #region Private Instance Fields

        private Project _project;
        private StringCollection _pathElements;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Invoked by <see cref="Element.AttributeConfigurator" /> for build 
        /// attributes with an underlying <see cref="PathList" /> type.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to be used to resolve relative paths.</param>
        /// <param name="pathList"></param>
        public PathList(Project project, string pathList) {
            _project = project;
            _pathElements = PathList.TranslatePath(project, pathList);
        }

        #endregion Public Instance Constructors

        #region Override implementation of Object

        /// <summary>
        /// Returns a textual representation of the path, which can be used as
        /// PATH environment variable definition.
        /// </summary>
        /// <returns>
        /// A textual representation of the path.
        /// </returns>
        public override string ToString() {
            // empty path return empty string
            if (_pathElements.Count == 0) {
                return "";
            }

            // path containing one or more elements
            StringBuilder result = new StringBuilder(_pathElements[0], _pathElements.Count);
            for (int i = 1; i < _pathElements.Count; i++) {
                result.Append(Path.PathSeparator);
                result.Append(_pathElements[i]);
            }

            return result.ToString();
        }

        #endregion Override implementation of Object

        #region Public Instance Methods

        /// <summary>
        /// Returns all path elements defined by this path object.
        /// </summary>
        /// <returns>
        /// A list of path elements.
        /// </returns>
        public StringCollection GetElements() {
            return _pathElements;
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Splits a PATH (with ; or : as separators) into its parts, while 
        /// resolving references to environment variables.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to be used to resolve relative paths.</param>
        /// <param name="source">The path to translate.</param>
        /// <returns>
        /// A PATH split up its parts, with references to environment variables
        /// resolved.
        /// </returns>
        public static StringCollection TranslatePath(Project project, string source) {
            StringCollection result = new StringCollection();

            if (source == null) {
                return result;
            }

            string[] parts = source.Split(';', ':');
            foreach (string part in parts) {
                // expand env variables in part
                string expandedPart = Environment.ExpandEnvironmentVariables(part);

                // check if part is a reference to an environment variable
                // that could not be expanded (does not exist)
                if (expandedPart.StartsWith("%") && expandedPart.EndsWith("%")) {
                    continue;
                }

                // resolve each path in the expanded environment variable to a 
                // full path
                foreach (string path in expandedPart.Split(Path.PathSeparator)) {
                    try {
                        result.Add(project.GetFullPath(path));
                    } catch (Exception ex) {
                        project.Log(Level.Verbose, "Dropping path element '{0}'"
                            + " as it could not be resolved to a full path. {1}", 
                            path, ex.Message);
                    }
                }
            }

            return result;
        }

        #endregion Public Static Methods
    }
}