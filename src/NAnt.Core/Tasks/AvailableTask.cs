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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;
using System.Globalization;

using NAnt.Core.Attributes;
using NAnt.Core.Functions;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Checks if a resource is available at runtime.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The specified property is set to <see langword="true" /> if the 
    ///   requested resource is available at runtime, and <see langword="false" /> 
    ///   if the resource is not available.
    ///   </para>
    ///   <note>
    ///   we advise you to use the following functions instead:
    ///   </note>
    ///   <list type="table">
    ///     <listheader>
    ///         <term>Function</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="FileFunctions.Exists(string)" /></term>
    ///         <description>Determines whether the specified file exists.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DirectoryFunctions.Exists(string)" /></term>
    ///         <description>Determines whether the given path refers to an existing directory on disk.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="FrameworkFunctions.Exists(string)" /></term>
    ///         <description>Checks whether the specified framework exists..</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="FrameworkFunctions.SdkExists(string)" /></term>
    ///         <description>Checks whether the SDK for the specified framework is installed.</description>
    ///     </item>
    ///   </list>  
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Sets the <c>myfile.present</c> property to <see langword="true" /> if the 
    ///   file is available on the filesystem and <see langword="false" /> if the 
    ///   file is not available.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="File" resource="myfile.txt" property="myfile.present" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Sets the <c>build.dir.present</c> property to <see langword="true" /> 
    ///   if the directory is available on the filesystem and <see langword="false" /> 
    ///   if the directory is not available.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="Directory" resource="build" property="build.dir.present" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Sets the <c>mono-0.21.framework.present</c> property to <see langword="true" /> 
    ///   if the Mono 0.21 framework is available on the current system and 
    ///   <see langword="false" /> if the framework is not available.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="Framework" resource="mono-0.21" property="mono-0.21.framework.present" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Sets the <c>net-1.1.frameworksdk.present</c> property to <see langword="true" /> 
    ///   if the .NET 1.1 Framework SDK is available on the current system and 
    ///   <see langword="false" /> if the SDK is not available.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="FrameworkSDK" resource="net-1.1" property="net-1.1.frameworksdk.present" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("available")]
    [Obsolete("Use functions instead.", false)]
    public class AvailableTask : Task {
        /// <summary>
        /// Defines the possible resource checks.
        /// </summary>
        public enum ResourceType : int {
            /// <summary>
            /// Determines whether a given file exists.
            /// </summary>
            File = 1,

            /// <summary>
            /// Determines whether a given directory exists.
            /// </summary>
            Directory = 2,

            /// <summary>
            /// Determines whether a given framework is available.
            /// </summary>
            Framework = 3,

            /// <summary>
            /// Determines whether a given SDK is available.
            /// </summary>
            FrameworkSDK = 4
        }

        #region Public Instance Properties

        /// <summary>
        /// The resource which must be available.
        /// </summary>
        [TaskAttribute("resource", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Resource {
            get { return _resource; }
            set { _resource = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The type of resource which must be present.
        /// </summary>
        [TaskAttribute("type", Required=true)]
        public ResourceType Type { 
            get { return _resourceType; }
            set {
                if (!Enum.IsDefined(typeof(ResourceType), value)) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "An invalid type {0} was specified.", value)); 
                } else {
                    this._resourceType = value;
                }
            } 
        }

        /// <summary>
        /// The property that must be set if the resource is available.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string PropertyName {
            get { return _propertyName; }
            set { _propertyName = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sets the property identified by <see cref="PropertyName" /> to 
        /// <see langword="true" /> when the resource exists and to <see langword="false" /> 
        /// when the resource doesn't exist.
        /// </para>
        /// </remarks>
        /// <exception cref="BuildException">The availability of the resource could not be evaluated.</exception>
        protected override void ExecuteTask() {
            Project.Properties[PropertyName] = Evaluate().ToString(CultureInfo.InvariantCulture);
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Evaluates the availability of a resource.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the resource is available; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        /// <exception cref="BuildException">The availability of the resource could not be evaluated.</exception>
        protected virtual bool Evaluate() {
            bool resourceAvailable = false;

            switch(Type) {
                case ResourceType.File:
                    resourceAvailable = CheckFile();
                    break;
                case ResourceType.Directory:
                    resourceAvailable = CheckDirectory();
                    break;
                case ResourceType.Framework:
                    resourceAvailable = CheckFramework();
                    break;
                case ResourceType.FrameworkSDK:
                    resourceAvailable = CheckFrameworkSDK();
                    break;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, "No resource check is implemented for {0}", Type));
            }

            if (!resourceAvailable) {
                Log(Level.Verbose, "Unable to find {0} {1}.", Type, Resource);
            }

            return resourceAvailable;
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Checks if the file specified in the <see cref="Resource" /> property is 
        /// available on the filesystem.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the file exists; otherwise, <see langword="false" />.
        /// </returns>
        private bool CheckFile() {
            try {
                FileInfo fileInfo = new FileInfo(Project.GetFullPath(Resource));
                return fileInfo.Exists;
            } catch (ArgumentException ex) {
                throw new BuildException(string.Format(
                    CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1104"),
                    Resource), Location, ex);
            }
        }

        /// <summary>
        /// Checks if the directory specified in the <see cref="Resource" /> 
        /// property is available on the filesystem.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the directory exists; otherwise, <see langword="false" />.
        /// </returns>
        private bool CheckDirectory() {
            try {
                DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(Resource));
                return dirInfo.Exists;
            } catch (ArgumentException ex) {
                throw new BuildException(string.Format(
                    CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1103"),
                    Resource), Location, ex);
            }
        }

        /// <summary>
        /// Checks if the framework specified in the <see cref="Resource" /> 
        /// property is available on the current system.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the framework is available; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private bool CheckFramework() {
            return Project.Frameworks.Contains(Resource);
        }

        /// <summary>
        /// Checks if the SDK for the framework specified in the <see cref="Resource" /> 
        /// property is available on the current system.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the SDK for the specified framework is 
        /// available; otherwise, <see langword="false" />.
        /// </returns>
        private bool CheckFrameworkSDK() {
            FrameworkInfo framework = Project.Frameworks[Resource];
            if (framework != null) {
                return framework.SdkDirectory != null;
            } else {
                return false;
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private ResourceType _resourceType;
        private string _resource;
        private string _propertyName;

        #endregion Private Instance Fields
    }
}
