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
// Ian Maclean (imaclean@gmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions to work with path strings.
    /// </summary>
    [FunctionSet("path", "Path")]
    public class PathFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public PathFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns the fully qualified path.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain absolute path information.</param>
        /// <returns>
        /// A string containing the fully qualified location of <paramref name="path" />,
        /// such as "C:\MyFile.txt".
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> is a zero-length string, contains only white space, or contains one or more invalid characters.</exception>
        /// <exception cref="NotSupportedException"><paramref name="path" /> contains a colon (":").</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</exception>
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
        /// <exception cref="ArgumentException"><paramref name="path1" /> or <paramref name="path2" /> contain one or more invalid characters.</exception>
        [Function("combine")]
        public static string Combine(string path1, string path2) {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// Changes the extension of the path string.
        /// </summary>
        /// <param name="path">The path information to modify. The path cannot contain any of the characters 
        /// defined in <see cref="T:System.IO.Path.InvalidPathChars"/>InvalidPathChars.</param>
        /// <param name="extension">The new extension (with a leading period). Specify a null reference 
        /// to remove an existing extension from <paramref name="path" />.</param>
        /// <returns>
        /// <para>
        /// A string containing the modified path information.
        /// </para>
        /// <para>
        /// On Windows-based desktop platforms, if <paramref name="path" /> is 
        /// an empty <see cref="string" />, the path information is returned 
        /// unmodified. If <paramref name="path" /> has no extension, the returned 
        /// path <see cref="string" /> contains <paramref name="extension" /> 
        /// appended to the end of <paramref name="path" />.
        /// </para>
        /// </returns>
        /// <remarks>
        /// For more information see the <see cref="T:System.IO.Path"/> documentation.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("change-extension")]
        public static string ChangeExtension(string path, string extension) {
            return Path.ChangeExtension(path, extension);
        }

        /// <summary>
        /// Returns the directory information for the specified path string.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        /// <returns>
        /// A <see cref="string" /> containing directory information for 
        /// <paramref name="path" />, or an empty <see cref="string" /> if 
        /// <paramref name="path" /> denotes a root directory, or does not
        /// contain directory information.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains invalid characters, is empty, or contains only white spaces.</exception>
        [Function("get-directory-name")]
        public static string GetDirectoryName(string path) {
            string dirName = Path.GetDirectoryName(path);
            return StringUtils.ConvertNullToEmpty(dirName);
        }

        /// <summary>
        /// Returns the extension for the specified path string.
        /// </summary>
        /// <param name="path">The path string from which to get the extension.</param>
        /// <returns>
        /// A <see cref="string" /> containing the extension of the specified 
        /// <paramref name="path" /> (including the "."), or an empty 
        /// <see cref="string" /> if <paramref name="path" /> does not have 
        /// extension information.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("get-extension")]
        public static string GetExtension(string path) {
            return Path.GetExtension(path);
        }

        /// <summary>
        /// Returns the filename for the specified path string.
        /// </summary>
        /// <param name="path">The path string from which to obtain the file name and extension.</param>
        /// <returns>
        /// <para>
        /// A <see cref="string" /> consisting of the characters after the last 
        /// directory character in path. 
        /// </para>
        /// <para>
        /// If the last character of <paramref name="path" /> is a directory or 
        /// volume separator character, an empty <see cref="string" /> is returned.
        /// </para>
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("get-file-name")]
        public static string GetFileName(string path) {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// Returns the filename without extension for the specified path string.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        /// A <see cref="string" /> containing the <see cref="string" /> returned 
        /// by <see cref="GetFileName" />, minus the last period (.) and all 
        /// characters following it.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("get-file-name-without-extension")]
        public static string GetFileNameWithoutExtension(string path) {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets the root directory of the specified path.
        /// </summary>
        /// <param name="path">The path from which to obtain root directory information.</param>
        /// <returns>
        /// A <see cref="string" /> containing the root directory of 
        /// <paramref name="path" />, such as "C:\", or an empty <see cref="string" /> 
        /// if <paramref name="path" /> does not contain root directory information.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains invalid characters, or is empty.</exception>
        [Function("get-path-root")]
        public static string GetPathRoot(string path) {
            string pathRoot = Path.GetPathRoot(path);
            return StringUtils.ConvertNullToEmpty(pathRoot);
        }

        /// <summary>
        /// Returns a uniquely named zero-byte temporary file on disk and returns the full path to that file.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> containing the name of the temporary file.
        /// </returns>
        [Function("get-temp-file-name")]
        public static string GetTempFileName() {
            return Path.GetTempFileName();
        }

        /// <summary>
        /// Gets the path to the temporary directory.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> containing the path information of a 
        /// temporary directory.
        /// </returns>
        [Function("get-temp-path")]
        public static string GetTempPath() {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Determines whether a path string includes an extension.
        /// </summary>
        /// <param name="path">The path to search for an extension.</param>
        /// <returns>
        /// <see langword="true" />. if the characters that follow the last 
        /// directory separator or volume separator in the <paramref name="path" /> 
        /// include a period (.) followed by one or more characters; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("has-extension")]
        public static bool HasExtension(string path) {
            return Path.HasExtension(path);
        }

        /// <summary>
        /// Determines whether a path string is absolute.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>
        /// <see langword="true" /> if path contains an absolute <paramref name="path" />; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="path" /> contains one or more invalid characters.</exception>
        [Function("is-path-rooted")]
        public static bool IsPathRooted(string path) {
            return Path.IsPathRooted(path);
        }

        #endregion Public Static Methods
   }
}
