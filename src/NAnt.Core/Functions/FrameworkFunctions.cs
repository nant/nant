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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions to retrieve information about the current framework environment.
    /// </summary>
    [FunctionSet("framework", "NAnt")]
    public class FrameworkFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameworkFunctions"/> class.
        /// </summary>
        /// <param name="project">The parent project.</param>
        /// <param name="properties">The projects properties.</param>
        public FrameworkFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Checks whether the specified framework exists, and is valid.
        /// </summary>
        /// <param name="framework">The framework to test.</param>
        /// <returns>
        /// <see langword="true" /> if the specified framework exists ; otherwise,
        /// <see langword="false" />.
        /// </returns>
        [Function("exists")]
        public bool Exists(string framework) {
            FrameworkInfo fi = Project.Frameworks [framework];
            return (fi != null && fi.IsValid);
        }

        /// <summary>
        /// Checks whether the SDK for the specified framework is installed.
        /// </summary>
        /// <param name="framework">The framework to test.</param>
        /// <returns>
        /// <see langword="true" /> if the SDK for specified framework is installed; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("sdk-exists")]
        public bool SdkExists(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            return (fi.SdkDirectory != null);
        }

        /// <summary>
        /// Gets the identifier of the current target framework.
        /// </summary>
        /// <returns>
        /// The identifier of the current target framework.
        /// </returns>
        [Function("get-target-framework")]
        public string GetTargetFramework() {
            return Project.TargetFramework.Name;
        }

        /// <summary>
        /// Gets the identifier of the runtime framework.
        /// </summary>
        /// <returns>
        /// The identifier of the runtime framework.
        /// </returns>
        [Function("get-runtime-framework")]
        public string GetRuntimeFramework() {
            return Project.RuntimeFramework.Name;
        }

        /// <summary>
        /// Gets the family of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the family should be returned.</param>
        /// <returns>
        /// The family of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-family")]
        public string GetFamily(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return the family of the specified framework
            return fi.Family;
        }

        /// <summary>
        /// Gets the version of the current target framework.
        /// </summary>
        /// <returns>
        /// The version of the current target framework.
        /// </returns>
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-version")]
        public Version GetVersion() {
            return Project.TargetFramework.Version;
        }

        /// <summary>
        /// Gets the version of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the version should be returned.</param>
        /// <returns>
        /// The version of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-version")]
        public Version GetVersion(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return the version of the specified framework
            return fi.Version;
        }

        /// <summary>
        /// Gets the description of the current target framework.
        /// </summary>
        /// <returns>
        /// The description of the current target framework.
        /// </returns>
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-description")]
        public string GetDescription() {
            return Project.TargetFramework.Description;
        }

        /// <summary>
        /// Gets the description of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the description should be returned.</param>
        /// <returns>
        /// The description of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-description")]
        public string GetDescription(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return the description of the specified framework
            return fi.Description;
        }

        /// <summary>
        /// Gets the Common Language Runtime version of the current target
        /// framework.
        /// </summary>
        /// <returns>
        /// The Common Language Runtime version of the current target framework.
        /// </returns>
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-clr-version")]
        public Version GetClrVersion() {
            return Project.TargetFramework.ClrVersion;
        }

        /// <summary>
        /// Gets the Common Language Runtime version of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the Common Language Runtime version should be returned.</param>
        /// <returns>
        /// The Common Language Runtime version of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-clr-version")]
        public Version GetClrVersion(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return the family of the specified framework
            return fi.ClrVersion;
        }

        /// <summary>
        /// Gets the framework directory of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the framework directory should be returned.</param>
        /// <returns>
        /// The framework directory of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-framework-directory")]
        public string GetFrameworkDirectory(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return full path to the framework directory of the specified framework
            return fi.FrameworkDirectory.FullName;
        }

        /// <summary>
        /// Gets the assembly directory of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the assembly directory should be returned.</param>
        /// <returns>
        /// The assembly directory of the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-assembly-directory")]
        public string GetAssemblyDirectory(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // return full path to the assembly directory of the specified framework
            return fi.FrameworkAssemblyDirectory.FullName;
        }

        /// <summary>
        /// Gets the SDK directory of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the SDK directory should be returned.</param>
        /// <returns>
        /// The SDK directory of the specified framework, or an empty 
        /// <see cref="string" /> if the SDK of the specified framework is not 
        /// installed.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-sdk-directory")]
        public string GetSdkDirectory(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            // get the SDK directory of the specified framework
            DirectoryInfo sdkDirectory = fi.SdkDirectory;
            // return directory or empty string if SDK is not installed
            return (sdkDirectory != null) ? sdkDirectory.FullName : string.Empty;
        }

        /// <summary>
        /// Gets the absolute path of the specified tool for the current
        /// target framework.
        /// </summary>
        /// <param name="tool">The file name of the tool to search for.</param>
        /// <returns>
        /// The absolute path to <paramref name="tool" /> if found in one of the
        /// configured tool paths; otherwise, an error is reported.
        /// </returns>
        /// <exception cref="FileNotFoundException"><paramref name="tool" /> could not be found in the configured tool paths.</exception>
        /// <remarks>
        ///   <para>
        ///   The configured tool paths are scanned in the order in which they
        ///   are defined in the framework configuration.
        ///   </para>
        ///   <para>
        ///   The file name of the tool to search should include the extension.
        ///   </para>
        /// </remarks>
        /// <example>
        ///   <para>Use <b>gacutil</b> to install an assembly in the GAC.</para>
        ///   <code>
        ///     <![CDATA[
        /// <exec program="${framework::get-tool-path('gacutil.exe')}" managed="strict">
        ///     <arg value="/i" />
        ///     <arg file="Cegeka.HealthFramework.dll" />
        /// </exec>
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("get-tool-path")]
        public string GetToolPath(string tool) {
            string toolPath = Project.TargetFramework.GetToolPath (tool);
            if (toolPath == null) {
                throw new FileNotFoundException (string.Format (CultureInfo.InvariantCulture,
                    "\"{0}\" could not be found in any of the configured " +
                    "tool paths.", tool));
            }
            return toolPath;
        }

        /// <summary>
        /// Gets the runtime engine of the specified framework.
        /// </summary>
        /// <param name="framework">The framework of which the runtime engine should be returned.</param>
        /// <returns>
        /// The full path to the runtime engine of the specified framework, or
        /// an empty <see cref="string" /> if no runtime engine is defined
        /// for the specified framework.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        /// <seealso cref="FrameworkFunctions.GetRuntimeFramework()" />
        /// <seealso cref="FrameworkFunctions.GetTargetFramework()" />
        [Function("get-runtime-engine")]
        public string GetRuntimeEngine(string framework) {
            // obtain framework and ensure it's valid
            FrameworkInfo fi = GetFramework(framework);
            return fi.RuntimeEngine;
        }

        /// <summary>
        /// Gets a comma-separated list of frameworks filtered by the specified
        /// <see cref="FrameworkTypes" />.
        /// </summary>
        /// <param name="types">A bitwise combination of <see cref="FrameworkTypes" /> values that filter the frameworks to retrieve.</param>
        /// <returns>
        /// A comma-separated list of frameworks filtered by the specified
        /// <see cref="FrameworkTypes" />, sorted on name.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Define a <b>build-all</b> target that executes the <b>build</b>
        ///   target once for each installed framework targeting compact
        ///   devices.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <target name="build-all">
        ///     <foreach item="String" in="${framework::get-frameworks('installed compact')}" delim="," property="framework">
        ///         <property name="nant.settings.currentframework" value="${framework}" />
        ///         <call target="build" />
        ///     </foreach>
        /// </target>
        /// 
        /// <target name="build">
        ///     ...
        /// </target>
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("get-frameworks")]
        public string GetFrameworks(FrameworkTypes types) {
            FrameworkInfo[] frameworks = Project.GetFrameworks(types);
            if (frameworks.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < frameworks.Length; i++) {
                if (i > 0)
                    sb.Append (",");
                sb.Append (frameworks [i].Name);
            }

            return sb.ToString();
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Checks whether the specified framework is valid.
        /// </summary>
        /// <param name="framework">The framework to check.</param>
        /// <exception cref="ArgumentException"><paramref name="framework" /> is not a valid framework identifier.</exception>
        private FrameworkInfo GetFramework(string framework) {
            if (framework == Project.TargetFramework.Name) {
                return Project.TargetFramework;
            }

            FrameworkInfo fi = Project.Frameworks [framework];
            if (fi == null) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1096"), framework));
            } else {
                // ensure framework is valid
                fi.Validate();
                return fi;
            }
        }

        #endregion Private Instance Methods
    }
}
