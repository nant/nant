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
    [FunctionSet("directory", "Directory")]
    public class DirectoryFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public DirectoryFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns the creation date and time of the specified directory.
        /// </summary>
        /// <param name="path">The directory for which to obtain creation date and time information.</param>
        /// <returns>
        /// The creation date and time of the specified directory.
        /// </returns>
        [Function("get-creation-time")]
        public DateTime GetCreationTime(string path) {
            string dirPath = Project.GetFullPath(path);

            if (!Directory.Exists(dirPath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "Directory '{0}' does not exist.", dirPath));
            }

            return Directory.GetCreationTime(dirPath);
        }

        /// <summary>
        /// Returns the date and time the specified directory was last written to.
        /// </summary>
        /// <param name="path">The directory for which to obtain write date and time information.</param>
        /// <returns>
        /// The date and time the specified directory was last written to.
        /// </returns>
        [Function("get-last-write-time")]
        public DateTime GetLastWriteTime(string path) {
            string dirPath = Project.GetFullPath(path);

            if (!Directory.Exists(dirPath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "Directory '{0}' does not exist.", dirPath));
            }

            return Directory.GetLastWriteTime(dirPath);
        }

        /// <summary>
        /// Returns the date and time the specified directory was last accessed.
        /// </summary>
        /// <param name="path">The directory for which to obtain access date and time information.</param>
        /// <returns>
        /// The date and time the specified directory was last accessed.
        /// </returns>
        [Function("get-last-access-time")]
        public DateTime GetLastAccessTime(string path) {
            string dirPath = Project.GetFullPath(path);

            if (!Directory.Exists(dirPath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "Directory '{0}' does not exist.", dirPath));
            }

            return Directory.GetLastAccessTime(dirPath);
        }

        /// <summary>
        /// Determines whether the given path refers to an existing directory 
        /// on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="path" /> refers to an
        /// existing directory; otherwise, <see langword="false" />.
        /// </returns>
        [Function("exists")]
        public bool Exists(string path) {
            return Directory.Exists(Project.GetFullPath(path));
        }

        #endregion Public Instance Methods
    }
}
