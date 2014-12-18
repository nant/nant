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
// Ian Maclean (imaclean@gmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Provide information about the current environment and platform.
    /// </summary>
    [FunctionSet("environment", "Environment")]
    public class EnvironmentFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
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
        /// <example>
        ///   <para>
        ///   Copy &quot;out.log&quot; from the project base directory to the
        ///   program files directory.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <copy file="out.log" todir="${environment::get-folder-path('ProgramFiles')}" />
        ///     ]]>
        ///   </code>
        /// </example>
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
        /// Gets an <see cref="OperatingSystem" /> object that represents the 
        /// current operating system.
        /// </summary>
        /// <returns>
        /// An <see cref="OperatingSystem" /> object that contains the current 
        /// platform identifier and version number.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Output string representation of the current operating system.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="OS=${operating-system::to-string(environment::get-operating-system())}" />
        ///     ]]>
        ///   </code>
        ///   <para>If the operating system is Windows 2000, the output is:</para>
        ///   <code>
        /// Microsoft Windows NT 5.0.2195.0
        ///   </code>
        /// </example>
        /// <seealso cref="OperatingSystemFunctions" />
        [Function("get-operating-system")]
        public static OperatingSystem GetOperatingSystem() {
            return Environment.OSVersion;
        }

        /// <summary>
        /// Gets the user name of the person who started the current thread.
        /// </summary>
        /// <returns>
        /// The name of the person logged on to the system who started the 
        /// current thread.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Modify the home directory of the current user on unix-based systems.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <exec program="usermod">
        ///     <arg value="-d" />
        ///     <arg value="/home/temp" />
        ///     <arg value="${environment::get-user-name()}" />
        /// </exec>
        ///     ]]>
        ///   </code>
        /// </example>
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
                                                          ResourceUtils.GetString("NA1095"), name));
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
        /// <example>
        ///   <para>
        ///   Execute a set of tasks only if the &quot;BUILD_DEBUG&quot; environment
        ///   variable is set.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <if test="${environment::variable-exists('BUILD_DEBUG')}">
        ///     ...
        /// </if>
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("variable-exists")]
        public static bool VariableExists(string name) {
            return (Environment.GetEnvironmentVariable(name) != null);
        }

        /// <summary>
        /// Gets a <see cref="Version" /> object that describes the major, 
        /// minor, build, and revision numbers of the Common Language Runtime.
        /// </summary>
        /// <returns>
        /// A Version object.
        /// </returns>
        /// <example>
        ///   <para>Output the major version of the CLR.</para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="Major version=${version::get-major(environment::get-version())}" />
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("get-version")]
        public static Version GetVersion() {
            return Environment.Version;
        }

        /// <summary>
        /// Gets the newline string defined for this environment.
        /// </summary>
        /// <returns>
        /// A string containing CRLF for non-Unix platforms, or LF for Unix
        /// platforms.
        /// </returns>
        /// <example>
        ///   <para>Output two lines in a log file.</para>
        ///   <code>
        ///     <![CDATA[
        /// <echo file="build.log" message="First line${environment::newline()}Second line" />
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("newline")]
        public static string NewLine() {
            return Environment.NewLine;
        }

        #endregion Public Instance Methods
    }
}
