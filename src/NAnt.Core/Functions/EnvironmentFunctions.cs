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
    /// <summary>
    /// Provide information about the current environment and platform.
    /// </summary>
    [FunctionSet("environment", "Environment")]
    public class EnvironmentFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public EnvironmentFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Gets the path to the system special folder identified by the 
        /// specified enumeration.
        /// </summary>
        /// <param name="folder">An enumerated constant that identifies a system special folder.</param>
        /// <returns>
        /// The path to the specified system special folder, if that folder 
        /// physically exists on your computer; otherwise, the empty string ("").
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="folder" /> is not a member of <see cref="Environment.SpecialFolder" />.</exception>
        [Function("get-folder-path")]
        public static string GetFolderPath(Environment.SpecialFolder folder) {
            return Environment.GetFolderPath(folder);
        }

        /// <summary>
        /// Gets the NetBIOS name of this local computer.
        /// </summary>
        /// <returns>
        /// The NetBIOS name of this local computer.
        /// </returns>
        /// <exception cref="InvalidOperationException">The name of this computer cannot be obtained.</exception>
        [Function("get-machine-name")]
        public static string GetMachineName() {
            return Environment.MachineName;
        }

        /// <summary>
        /// Gets a <see cref="Version" /> object that identifies this operating 
        /// system.
        /// </summary>
        /// <returns>
        /// A <see cref="Version" /> object that describes the major version, 
        /// minor version, build, and revision of this operating system.
        /// </returns>
        [Function("get-os-version")]
        public static Version GetOSVersion() {
            return Environment.OSVersion.Version;
        }

        /// <summary>
        /// Gets the user name of the person who started the current thread.
        /// </summary>
        /// <returns>
        /// The name of the person logged on to the system who started the 
        /// current thread.
        /// </returns>
        [Function("get-user-name")]
        public static string GetUserName() {
            return Environment.UserName;
        }

        /// <summary>
        /// Returns the value of the specified environment variable.
        /// </summary>
        /// <param name="name">The environment variable of which the value should be returned.</param>
        /// <returns>
        /// The value of the specified environment variable.
        /// </returns>
        /// <exception cref="ArgumentException">Environment variable <paramref name="name" /> does not exist.</exception>
        [Function("get-variable")]
        public static string GetVariable(string name) {
            if (!VariableExists(name)) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Environment variable \"{0}\" does not exist.", name));
            }

            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Gets a value indicating whether the specified environment variable
        /// exists.
        /// </summary>
        /// <param name="name">The environment variable that should be checked.</param>
        /// <returns>
        /// <see langword="true" /> if the environment variable exists; otherwise,
        /// <see langword="false" />.
        /// </returns>
        [Function("variable-exists")]
        public static bool VariableExists(string name) {
            return (Environment.GetEnvironmentVariable(name) != null);
        }

        /// <summary>
        /// Gets a <see cref="Version" /> object that describes the major, 
        /// minor, build, and revision numbers of the common language runtime.
        /// </summary>
        /// <returns>
        /// A Version object.
        /// </returns>
        [Function("get-version")]
        public static Version GetVersion() {
            return Environment.Version;
        }

        #endregion Public Instance Methods
    }
}
