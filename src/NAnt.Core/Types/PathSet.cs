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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Types {
    /// <summary>
    /// <para>
    /// Paths are groups of files and/or directories that need to be passed as a single
    /// unit. The order in which parts of the path are specified in the build file is 
    /// retained, and duplicate parts are automatically suppressed.
    /// </para>
    /// </summary>
    /// <example>
    ///   <para>
    ///   Define a global <c>&lt;path&gt;</c> that can be referenced by other
    ///   tasks or types.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <path id="includes-path">
    ///             <pathelement path="%INCLUDE%" />
    ///             <pathelement dir="${build.dir}/include" />
    ///         </path>
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [ElementName("path")]
    public class PathSet : DataTypeBase {
        #region Private Instance Fields

        private ArrayList _elements = new ArrayList();
        private StringCollection _translatedElements = new StringCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly bool _dosBasedFileSystem = (Path.PathSeparator == ';');

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathSet" /> class.
        /// </summary>
        public PathSet() {
        }

        /// <summary>
        /// Invoked by <see cref="Element.AttributeConfigurator" /> for build 
        /// attributes with an underlying <see cref="PathSet" /> type.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to be used to resolve relative paths.</param>
        /// <param name="path">The string representing a path.</param>
        public PathSet(Project project, string path) {
            base.Project = project;
            _translatedElements = PathSet.TranslatePath(project, path);
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
            StringCollection parts = GetElements();
            // empty path return empty string
            if (parts.Count == 0) {
                return "";
            }

            // path containing one or more elements
            StringBuilder result = new StringBuilder(parts[0], parts.Count);
            for (int i = 1; i < parts.Count; i++) {
                result.Append(Path.PathSeparator);
                result.Append(parts[i]);
            }

            return result.ToString();
        }

        #endregion Override implementation of Object

        #region Public Instance Methods

        /// <summary>
        /// Defines a set of path elements to add to the current path.
        /// </summary>
        /// <param name="path">The <see cref="PathSet" /> to add.</param>
        [BuildElement("path")]
        public void AddPath(PathSet path) {
            _elements.Add(path);
        }

        /// <summary>
        /// Defines a path element to add to the current path.
        /// </summary>
        /// <param name="pathElement">The <see cref="PathElement" /> to add.</param>
        [BuildElement("pathelement")]
        public void AddPathElement(PathElement pathElement) {
            _elements.Add(pathElement);
        }

        /// <summary>
        /// Returns all path elements defined by this path object.
        /// </summary>
        /// <returns>
        /// A list of path elements.
        /// </returns>
        public StringCollection GetElements() {
            StringCollection result = StringUtils.Clone(_translatedElements);
            
            foreach (object path in _elements) {
                if (path is PathSet) {
                    foreach (string part in ((PathSet) path).GetElements()) {
                        if (!result.Contains(part)) {
                            result.Add(part);
                        }
                    }
                } else if (path is PathElement) {
                    PathElement pathElement = (PathElement) path;
                    if (!pathElement.IfDefined || pathElement.UnlessDefined) {
                        continue;
                    }

                    foreach (string part in ((PathElement) path).Parts) {
                        if (!result.Contains(part)) {
                            result.Add(part);
                        }
                    }
                }
            }

            return result;
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
        /// resolved and duplicate entries removed.
        /// </returns>
        public static StringCollection TranslatePath(Project project, string source) {
            StringCollection result = new StringCollection();

            if (source == null) {
                return result;
            }

            string[] parts = source.Split(':',';');
            for (int i = 0; i < parts.Length; i++) {
                string part = parts[i];

                // on a DOS filesystem, the ':' character might be part of a 
                // drive spec and in that case we need to combine the current
                // and the next part
                if (part.Length == 1 && Char.IsLetter(part[0]) && _dosBasedFileSystem && (parts.Length > i + 1)) {
                    string nextPart = parts[i + 1].Trim();
                    if (nextPart.StartsWith("\\") || nextPart.StartsWith("/")) {
                        part += ":" + nextPart;
                        // skip the next part as we've also processed it
                        i++;
                    }
                }

                // expand env variables in part
                string expandedPart = Environment.ExpandEnvironmentVariables(part);

                // check if part is a reference to an environment variable
                // that could not be expanded (does not exist)
                if (expandedPart.StartsWith("%") && expandedPart.EndsWith("%")) {
                    continue;
                }

                // resolve each part in the expanded environment variable to a 
                // full path
                foreach (string path in expandedPart.Split(Path.PathSeparator)) {
                    try {
                        string absolutePath = project.GetFullPath(path);
                        if (!result.Contains(absolutePath)) {
                            result.Add(absolutePath);
                        }
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