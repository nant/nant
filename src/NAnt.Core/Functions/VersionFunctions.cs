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
// Gert Driesen (gert.driesen@ardatis.com)

using System;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("version", "Version")]
    public class VersionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public VersionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Gets the value of the major component of a given version string.
        /// </summary>
        /// <param name="version">A string containing the major, minor, build, and revision numbers, where each number is delimited with a period character ('.').</param>
        /// <returns>
        /// The major version number.
        /// </returns>
        [Function("get-major")]
        public static int GetMajor(string version) {
            Version aVersion = new Version(version);
            return aVersion.Major;
        }

        /// <summary>
        /// Gets the value of the minor component of a given version string.
        /// </summary>
        /// <param name="version">A string containing the major, minor, build, and revision numbers, where each number is delimited with a period character ('.').</param>
        /// <returns>
        /// The minor version number.
        /// </returns>
        [Function("get-minor")]
        public static int GetMinor(string version) {
            Version aVersion = new Version(version);
            return aVersion.Minor;
        }

        /// <summary>
        /// Gets the value of the build component of a given version string.
        /// </summary>
        /// <param name="version">A string containing the major, minor, build, and revision numbers, where each number is delimited with a period character ('.').</param>
        /// <returns>
        /// The build number, or -1 if the build number is undefined.
        /// </returns>
        [Function("get-build")]
        public static int GetBuild(string version) {
            Version aVersion = new Version(version);
            return aVersion.Build;
        }

        /// <summary>
        /// Gets the value of the revision component of a given version string.
        /// </summary>
        /// <param name="version">A string containing the major, minor, build, and revision numbers, where each number is delimited with a period character ('.').</param>
        /// <returns>
        /// The revision number, or -1 if the revision number is undefined.
        /// </returns>
        [Function("get-revision")]
        public static int GetRevision(string version) {
            Version aVersion = new Version(version);
            return aVersion.Revision;
        }

        #endregion Public Static Methods
    }
}
