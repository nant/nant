// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;
using System.Globalization;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>
    /// Sets the given property to <c>true</c> if the requested resource is available 
    /// at runtime, and <c>false</c> if the resource is not available.</summary>
    /// <example>
    ///   <para>
    ///   Sets the <c>myfile.present</c> property to <c>true</c> if the file is 
    ///   available on the filesystem and <c>false</c> if the file is not available.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="File" resource="myfile.txt" property="myfile.present" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Sets the <c>build.dir.present</c> property to <c>true</c> if the directory 
    ///   is available on the filesystem and <c>false</c> if the directory is not
    ///   available..
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <available type="Directory" resource="build" property="build.dir.present" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("available")]
    public class AvailableTask : Task {

        public enum ResourceType : int {
            File = 1,
            Directory = 2
        }

        #region Public Instance Properties

        /// <summary>The resource which must be available.</summary>
        [TaskAttribute("resource", Required=true)]
        public string Resource          { get { return _resource; } set {_resource = value; } }

        /// <summary>The type of resource which must be present - either <c>File</c> or <c>Directory</c>.</summary>
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

        /// <summary>The property that must be set if the resource is available.</summary>
        [TaskAttribute("property", Required=true)]
        public string PropertyName      { get { return _propertyName; } set {_propertyName = value; } }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the Available task.
        /// </summary>
        /// <remarks>
        /// Sets the property identifier by <see cref="PropertyName" /> to <c>true</c>
        /// when the resource exists and to <c>false</c> when the resource doesn't exist.
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
        /// <returns><c>true</c> if the resource is available, <c>false</c> otrherwise</returns>
        /// <exception cref="BuildException">The availability of the resource could not be evaluated.</exception>
        protected virtual bool Evaluate() {
            bool resourceAvailable = false;

            try {
                switch ( Type ){
                    case ResourceType.File:
                        FileInfo fileInfo = new FileInfo(Project.GetFullPath(Resource));
                        if (fileInfo.Exists) {
                            resourceAvailable = true;
                        } else {
                            Log.WriteIf(Verbose, "Unable to find " + Type + " " + Resource);
                            resourceAvailable = false;
                        }
                        break;
                    case ResourceType.Directory:
                        // check if specified directory is available on filesystem
                        DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(Resource));
                        if (dirInfo.Exists) {
                            resourceAvailable = true;
                        } else {
                            Log.WriteIf(Verbose, "Unable to find " + Type + " " + Resource);
                            resourceAvailable = false;
                        }
                        break;
                }
                return resourceAvailable;
            } catch (Exception e) {
                string message = string.Format(CultureInfo.InvariantCulture, "Unable to check if {0} resource {1} is available.", Type, Resource);
                throw new BuildException(message, Location, e);
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Fields

        ResourceType _resourceType;
        string _resource = null;
        string _propertyName = null;

        #endregion
    }
}
