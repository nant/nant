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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("file", "File")]
    public class FileFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public FileFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns the creation date and time of the specified file.
        /// </summary>
        /// <param name="path">The file for which to obtain creation date and time information.</param>
        /// <returns>
        /// The creation date and time of the specified file.
        /// </returns>
        [Function("get-creation-time")]
        public DateTime GetCreationTime(string path) {
            string filePath = Project.GetFullPath(path);

            if (!File.Exists(filePath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "File '{0}' does not exist.", filePath));
            }

            return File.GetCreationTime(filePath);
        }

        /// <summary>
        /// Returns the date and time the specified file was last written to.
        /// </summary>
        /// <param name="path">The file for which to obtain write date and time information.</param>
        /// <returns>
        /// The date and time the specified file was last written to.
        /// </returns>
        [Function("get-last-write-time")]
        public DateTime GetLastWriteTime(string path) {
            string filePath = Project.GetFullPath(path);

            if (!File.Exists(filePath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "File '{0}' does not exist.", filePath));
            }

            return File.GetLastWriteTime(filePath);
        }

        /// <summary>
        /// Returns the date and time the specified file was last accessed.
        /// </summary>
        /// <param name="path">The file for which to obtain access date and time information.</param>
        /// <returns>
        /// The date and time the specified file was last accessed.
        /// </returns>
        [Function("get-last-access-time")]
        public DateTime GetLastAccessTime(string path) {
            string filePath = Project.GetFullPath(path);

            if (!File.Exists(filePath)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "File '{0}' does not exist.", filePath));
            }

            return File.GetLastAccessTime(filePath);
        }

        /// <summary>
        /// Gets the file version of the given file.
        /// </summary>
        /// <param name="fileName">The file to get file version info for.</param>
        /// <returns>
        /// The file version of the given file.
        /// </returns>
        [Function("get-file-version")]
        public static string GetFileVersion(string fileName) {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
            return fileVersionInfo.FileVersion;
        }

        /// <summary>
        /// Gets the product version of the given file.
        /// </summary>
        /// <param name="fileName">The file to get product version info for.</param>
        /// <returns>
        /// The product version of the given file.
        /// </returns>
        [Function("get-product-version")]
        public static string GetProductVersion(string fileName) {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
            return fileVersionInfo.ProductVersion;
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
        public bool Exists(string file) {
            return File.Exists(Project.GetFullPath(file));
        }

        /// <summary>
        /// Determines whether <paramref name="targetFile" /> is more or equal 
        /// up-to-date than <paramref name="srcFile" />.
        /// </summary>
        /// <param name="srcFile">The file to check against the target file.</param>
        /// <param name="targetFile">The file for which we want to determine the status.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="targetFile" /> is more 
        /// or equal up-to-date than <paramref name="srcFile" />; otherwise,
        /// <see langword="false" />.
        /// </returns>
        [Function("up-to-date")]
        public bool UpToDate(string srcFile, string targetFile) {
            string srcPath = Project.GetFullPath(srcFile);
            string targetPath = Project.GetFullPath(targetFile);

            if (!File.Exists(targetPath)) {
                // if targetFile does not exist, we consider it out-of-date
                return false;
            }

            // get lastwritetime of targetFile
            DateTime targetLastWriteTime = File.GetLastWriteTime(targetPath);

            // determine whether lastwritetime of srcFile is more recent
            // than lastwritetime or targetFile
            string newerFile = FileSet.FindMoreRecentLastWriteTime(
                srcPath, targetLastWriteTime);

            // return true if srcFile is not newer than target file
            return newerFile == null;
        }

        /// <summary>
        /// Gets the length of the file.
        /// </summary>
        /// <param name="file">filename</param>
        /// <returns>
        /// Length in bytes, of the file named <paramref name="file" />.
        /// </returns>
        [Function("get-length")]
        public int Length(string file) {
            FileInfo fi = new FileInfo(Project.GetFullPath(file));

            if (!fi.Exists) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                    "File '{0}' does not exist.", fi.FullName));
            }

            return (int) fi.Length;
        }

        #endregion Public Instance Methods
    }
}
