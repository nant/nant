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
    [FunctionSet("file", "File")]
    public class FileFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public FileFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Gets the creation date and time of the specified file or directory.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain creation date and time information.</param>
        /// <returns>
        /// The creation date and time of the specified file or directory.
        /// </returns>
        [Function("get-creation-time")]
        public static DateTime GetCreationTime(string path) {
            return File.GetCreationTime(path);
        }

        /// <summary>
        /// Gets the date and time the specified file or directory was last written to.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain write date and time information.</param>
        /// <returns>
        /// The date and time the specified file or directory was last written to.
        /// </returns>
        [Function("get-last-write-time")]
        public static DateTime GetLastWriteTime(string path) {
            return File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Gets the date and time the specified file or directory was last accessed.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain access date and time information.</param>
        /// <returns>
        /// The date and time the specified file or directory was last accessed.
        /// </returns>
        [Function("get-last-access-time")]
        public static DateTime GetLastAccessTime(string path) {
            return File.GetLastAccessTime(path);
        }

        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        /// <param name="file">The file to check.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="file" /> refers to an 
        /// existing file; otherwise, <see langword="false" />.
        /// </returns>
        [Function("exists")]
        public static bool Exists(string file) {
            return File.Exists(file);
        }

        /// <summary>
        /// Gets the length of the file.
        /// </summary>
        /// <param name="file">filename</param>
        /// <returns>
        /// Length in bytes, of the file named <paramref name="file" />.
        /// </returns>
        [Function("get-length")]
        public static int Length(string file) {
            FileInfo fi = new FileInfo(file);
            return (int)fi.Length;
        }

        #endregion Public Static Methods
    }
}
