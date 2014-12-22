// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
using System.Diagnostics;
using System.IO;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Functions that provide version information for a physical file on disk.
    /// </summary>
    [FunctionSet("fileversioninfo", "Version")]
    public class FileVersionInfoFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersionInfoFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public FileVersionInfoFunctions(Project project, PropertyDictionary properties) : base(project, properties) {}

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Returns a <see cref="FileVersionInfo" /> representing the version 
        /// information associated with the specified file.
        /// </summary>
        /// <param name="fileName">The file to retrieve the version information for.</param>
        /// <returns>
        /// A <see cref="FileVersionInfo" /> containing information about the file.
        /// </returns>
        /// <exception cref="FileNotFoundException">The file specified cannot be found.</exception>
        [Function("get-version-info")]
        public FileVersionInfo GetVersionInfo(string fileName) {
            return FileVersionInfo.GetVersionInfo(
                Project.GetFullPath(fileName));
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Gets the name of the company that produced the file.
        /// </summary>
        /// <param name="fileVersionInfo">A <see cref="FileVersionInfo" /> instance containing version information about a file.</param>
        /// <returns>
        /// The name of the company that produced the file.
        /// </returns>
        [Function("get-company-name")]
        public static string GetCompanyName(FileVersionInfo fileVersionInfo) {
            return fileVersionInfo.CompanyName;
        }
        
        /// <summary>
        /// Gets the file version of a file.
        /// </summary>
        /// <param name="fileVersionInfo">A <see cref="FileVersionInfo" /> instance containing version information about a file.</param>
        /// <returns>
        /// The file version of a file.
        /// </returns>
        /// <see cref="VersionFunctions" />
        [Function("get-file-version")]
        public static Version GetFileVersion(FileVersionInfo fileVersionInfo) {
            return new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart,
                fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);
        }

        /// <summary>
        /// Gets the name of the product the file is distributed with.
        /// </summary>
        /// <param name="fileVersionInfo">A <see cref="FileVersionInfo" /> instance containing version information about a file.</param>
        /// <returns>
        /// The name of the product the file is distributed with.
        /// </returns>
        [Function("get-product-name")]
        public static string GetProductName(FileVersionInfo fileVersionInfo) {
            return fileVersionInfo.ProductName;
        }

        /// <summary>
        /// Gets the product version of a file.
        /// </summary>
        /// <param name="fileVersionInfo">A <see cref="FileVersionInfo" /> instance containing version information about a file.</param>
        /// <returns>
        /// The product version of a file.
        /// </returns>
        /// <see cref="VersionFunctions" />
        [Function("get-product-version")]
        public static Version GetProductVersion(FileVersionInfo fileVersionInfo) {
            return new Version(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart,
                fileVersionInfo.ProductBuildPart, fileVersionInfo.ProductPrivatePart);
        }

        #endregion Public Static Methods
    }
}
