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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("path", "Path")]
    public class PathFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public PathFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns the fully qualified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("get-full-path")]
        public string GetFullPath(string path) {
            return Project.GetFullPath(path);
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Combines two paths.
        /// </summary>
        /// <param name="path1">first path</param>
        /// <param name="path2">second path</param>
        /// <returns>
        /// A string containing the combined paths. If one of the specified paths 
        /// is a zero-length string, this method returns the other path. If 
        /// <paramref name="path2" /> contains an absolute path, this method 
        /// returns <paramref name="path2" />.
        /// </returns>
        [Function("combine")]
        public static string Combine(string path1, string path2) {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// Changes the extension of the path string.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        [Function("change-extension")]
        public static string ChangeExtension(string path, string extension) {
            return Path.ChangeExtension(path, extension);
        }

        /// <summary>
        /// Returns the directory information for the specified path string.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <returns></returns>
        [Function("get-directory-name")]
        public static string GetDirectoryName(string path) {
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Returns the extension for the specified path string.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("get-extension")]
        public static string GetExtension(string path) {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// Returns the filename for the specified path string.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("get-file-name")]
        public static string GetFileName(string path) {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// Returns the filename without extension for the specified path string.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("get-file-name-without-extension")]
        public static string GetFileNameWithoutExtension(string path) {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets the root directory of the specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("get-path-root")]
        public static string GetPathRoot(string path) {
            return Path.GetPathRoot(path);
        }

        /// <summary>
        /// Returns a unique filename in a temporary directory.
        /// </summary>
        /// <returns></returns>
        [Function("get-temp-file-name")]
        public static string GetTempFileName() {
            return Path.GetTempFileName();
        }

        /// <summary>
        /// Gets the path to the temporary directory.
        /// </summary>
        /// <returns></returns>
        [Function("get-temp-path")]
        public static string GetTempPath() {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Determines whether a path string includes an extension.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("has-extension")]
        public static bool HasExtension(string path) {
            return Path.HasExtension(path);
        }

        /// <summary>
        /// Determines whether a path string is absolute.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Function("is-path-rooted")]
        public static bool IsPathRooted(string path) {
            return Path.IsPathRooted(path);
        }

        #endregion Public Static Methods
   }
}
