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
using System.Reflection;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions for working with version objects.
    /// </summary>
    [FunctionSet("version", "Version")]
    public class VersionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public VersionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Gets the value of the major component of a given version.
        /// </summary>
        /// <param name="version">A version.</param>
        /// <returns>
        /// The major version number.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions.GetVersion(AssemblyName)" />
        /// <seealso cref="EnvironmentFunctions.GetVersion()" />
        /// <seealso cref="OperatingSystemFunctions.GetVersion(OperatingSystem)" />
        [Function("get-major")]
        public static int GetMajor(Version version) {
            return version.Major;
        }

        /// <summary>
        /// Gets the value of the minor component of a given version.
        /// </summary>
        /// <param name="version">A version.</param>
        /// <returns>
        /// The minor version number.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions.GetVersion(AssemblyName)" />
        /// <seealso cref="EnvironmentFunctions.GetVersion()" />
        /// <seealso cref="OperatingSystemFunctions.GetVersion(OperatingSystem)" />
        [Function("get-minor")]
        public static int GetMinor(Version version) {
            return version.Minor;
        }

        /// <summary>
        /// Gets the value of the build component of a given version.
        /// </summary>
        /// <param name="version">A version.</param>
        /// <returns>
        /// The build number, or -1 if the build number is undefined.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions.GetVersion(AssemblyName)" />
        /// <seealso cref="EnvironmentFunctions.GetVersion()" />
        /// <seealso cref="OperatingSystemFunctions.GetVersion(OperatingSystem)" />
        [Function("get-build")]
        public static int GetBuild(Version version) {
            return version.Build;
        }

        /// <summary>
        /// Gets the value of the revision component of a given version.
        /// </summary>
        /// <param name="version">A version.</param>
        /// <returns>
        /// The revision number, or -1 if the revision number is undefined.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions.GetVersion(AssemblyName)" />
        /// <seealso cref="EnvironmentFunctions.GetVersion()" />
        /// <seealso cref="OperatingSystemFunctions.GetVersion(OperatingSystem)" />
        [Function("get-revision")]
        public static int GetRevision(Version version) {
            return version.Revision;
        }

        #endregion Public Static Methods
    }

    /// <summary>
    /// Class which provides NAnt functions for converting strings to version objects and vice versa.
    /// </summary>
    [FunctionSet("version", "Conversion")]
    public class VersionConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public VersionConversionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the specified string representation of a version to 
        /// its <see cref="Version" /> equivalent.
        /// </summary>
        /// <param name="version">A string containing the major, minor, build, and revision numbers, where each number is delimited with a period character ('.').</param>
        /// <returns>
        /// A <see cref="Version" /> instance representing the specified 
        /// <see cref="string" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="version" /> has fewer than two components or more than four components.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A major, minor, build, or revision component is less than zero.</exception>
        /// <exception cref="FormatException">At least one component of <paramref name="version" /> does not parse to a decimal integer.</exception>
        [Function("parse")]
        public static Version Parse(string version) {
            return new Version(version);
        }

        /// <summary>
        /// Converts the specified <see cref="Version" /> to its equivalent
        /// string representation.
        /// </summary>
        /// <param name="value">A <see cref="Version" /> to convert.</param>
        /// <returns>
        /// The string representation of the values of the major, minor, build, 
        /// and revision components of the specified <see cref="Version" />.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions.GetVersion(AssemblyName)" />
        /// <seealso cref="EnvironmentFunctions.GetVersion()" />
        /// <seealso cref="OperatingSystemFunctions.GetVersion(OperatingSystem)" />
        [Function("to-string")]
        public static string ToString(Version value) {
            return value.ToString();
        }

        #endregion Public Static Methods
    }
}
