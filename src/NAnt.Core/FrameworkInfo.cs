// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Encalsulates information about installed frameworks incuding version 
    /// information and directory locations for finding tools.
    /// </summary>
    [Serializable()]
    public class FrameworkInfo {
        #region Private Instance Fields

        private readonly string _name;
        private readonly string _family;
        private readonly string _description;
        private readonly string _version;
        private readonly string _clrVersion;
        private readonly DirectoryInfo _frameworkDirectory;
        private readonly DirectoryInfo _sdkDirectory;
        private readonly DirectoryInfo _frameworkAssemblyDirectory;
        private readonly FileInfo _runtimeEngine;
        private readonly PropertyDictionary _properties;
        private EnvironmentVariableCollection _environmentVariables;
        private FileSet _extensions;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameworkInfo" /> class
        /// with a name, description, version, runtime engine, directory information
        /// and properties.
        /// </summary>
        /// <param name="name">The name of the framework.</param>
        /// <param name="family">The family of the framework.</param>
        /// <param name="description">The description of the framework.</param>
        /// <param name="version">The version number of the framework.</param>
        /// <param name="clrVersion">The Common Language Runtime version of the framework.</param>
        /// <param name="frameworkDir">The directory of the framework.</param>
        /// <param name="sdkDir">The directory containing the SDK tools for the framework, if available.</param>
        /// <param name="frameworkAssemblyDir">The directory containing the system assemblies for the framework.</param>
        /// <param name="runtimeEngine">The name of the runtime engine, if required.</param>
        /// <param name="properties">Collection of framework specific properties.</param>
        public FrameworkInfo(string name, string family, string description, string version, 
            string clrVersion, string frameworkDir, string sdkDir, string frameworkAssemblyDir, 
            string runtimeEngine, PropertyDictionary properties) {

            _extensions = new FileSet();
            _extensions.BaseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            _family = family;
            _description = description;
            _clrVersion = clrVersion;

            if (name == null) {
                throw new ArgumentNullException("name", "Framework name not configured.");
            } else {
                _name = name;
            }

            if (version == null) {
                throw new ArgumentNullException("version", string.Format(
                    CultureInfo.InvariantCulture, "Version not configured for framework '{0}'.", name));
            } else {
                _version = version;
            }

            if (frameworkDir == null) {
                throw new ArgumentNullException("frameworkDir", string.Format(
                    CultureInfo.InvariantCulture, "Framework directory not configured for framework '{0}'.", name));
            }

            if (frameworkAssemblyDir == null) {
                throw new ArgumentNullException("frameworkAssemblyDir", string.Format(
                    CultureInfo.InvariantCulture, "Framework assembly directory not configured for framework '{0}'.", name));
            }

            if (properties == null) {
                throw new ArgumentNullException("properties", string.Format(
                    CultureInfo.InvariantCulture, "Framework properties not configured for framework '{0}'.", name));
            } else {
                _properties = properties;
            }

            // ensure the framework directory exists
            if (Directory.Exists(frameworkDir)) {
                _frameworkDirectory = new DirectoryInfo(frameworkDir);
            } else {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture, "Framework directory '{0}' does not exist.", frameworkDir));
            }

            // ensure the framework assembly directory exists
            if (Directory.Exists(frameworkAssemblyDir)) {
                _frameworkAssemblyDirectory = new DirectoryInfo(frameworkAssemblyDir);
                // only consider framework assembly directory valid if an assembly
                // named "System.dll" exists in that directory
                if (!File.Exists(Path.Combine(_frameworkAssemblyDirectory.FullName, "System.dll"))) {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, "The 'System.dll' assembly" 
                            + " does not exist in framework assembly directory" 
                            + " '{0}'.", frameworkAssemblyDir));
                }
            } else {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture, "Framework assembly directory '{0}' does not exist.", frameworkAssemblyDir));
            }

            // the sdk directory does not actually have to exist for a framework
            // to be considered valid
            if (sdkDir != null && Directory.Exists(sdkDir)) {
                _sdkDirectory = new DirectoryInfo(sdkDir);
            }

            // if runtime engine is blank assume we aren't using one
            if (!StringUtils.IsNullOrEmpty(runtimeEngine)) {
                string runtimeEnginePath = _frameworkDirectory.FullName + Path.DirectorySeparatorChar + runtimeEngine;
                if (File.Exists(runtimeEnginePath)) {
                    _runtimeEngine = new FileInfo(runtimeEnginePath);
                } else {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, "Runtime engine '{0}' does not exist.", runtimeEnginePath));
                }
            }
        }

        #endregion Public Instance Constructors
              
        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the framework.
        /// </summary>
        /// <value>The name of the framework.</value>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets the family of the framework.
        /// </summary>
        /// <value>
        /// The family of the framework.
        /// </value>
        public string Family {
            get { return _family; }
        }

        /// <summary>
        /// Gets the description of the framework.
        /// </summary>
        /// <value>
        /// The description of the framework.
        /// </value>
        public string Description {
            get { return _description; }
        }
        
        /// <summary>
        /// Gets the version of the framework.
        /// </summary>
        /// <value>
        /// The version of the framework.
        /// </value>
        public string Version {
            get { return _version; }
        }

        /// <summary>
        /// Gets the Common Language Runtime of the framework.
        /// </summary>
        /// <value>
        /// The Common Language Runtime of the framework.
        /// </value>
        public string ClrVersion {
            get { return _clrVersion; }
        }
        
        /// <summary>
        /// Gets the base directory of the framework tools for the framework.
        /// </summary>
        /// <value>
        /// The base directory of the framework tools for the framework.
        /// </value>
        public DirectoryInfo FrameworkDirectory {
            get { return _frameworkDirectory; }
        }
        /// <summary>
        /// Gets the path to the runtime engine for this framework.
        /// </summary>
        /// <value>
        /// The path to the runtime engine for the framework or null if no
        /// runtime gine is configured for the framework.
        /// </value>
        public FileInfo RuntimeEngine {
            get { return _runtimeEngine; }
        }
       
        /// <summary>
        /// Gets the directory where the system assemblies for the framework 
        /// are located.
        /// </summary>
        /// <value>
        /// The directory where the system assemblies for the framework are 
        /// located.
        /// </value>
        public DirectoryInfo FrameworkAssemblyDirectory {
            get { return _frameworkAssemblyDirectory; }
        }
        
        /// <summary>
        /// Gets the directory containing the SDK tools for the framework.
        /// </summary>
        /// <value>
        /// The directory containing the SDK tools for the framework or a null 
        /// refererence if the sdk directory
        /// </value>
        public DirectoryInfo SdkDirectory {
            get { return _sdkDirectory; }
        }

        /// <summary>
        /// Gets the properties defined for this framework.
        /// </summary>
        /// <value>
        /// The properties defined for this framework.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the collection of properties for this framework in the 
        /// NAnt configuration file.
        /// </para>
        /// </remarks>
        public PropertyDictionary Properties {
            get { return _properties; }
        }

        /// <summary>
        /// Gets or sets the collection of environment variables that should be 
        /// passed to external programs that are launched in the runtime engine 
        /// of the current framework.
        /// </summary>
        /// <value>
        /// The collection of environment variables that should be passed to 
        /// external programs that are launched in the runtime engine of the
        /// current framework.
        /// </value>
        public EnvironmentVariableCollection EnvironmentVariables {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }

        /// <summary>
        /// Gets the set of assemblies and directories that should scanned for
        /// NAnt extension classes.
        /// </summary>
        /// <value>
        /// The set of assemblies and directories that should be scanned for 
        /// NAnt extension classes.
        /// </value>
        public FileSet Extensions {
            get { return _extensions; }
        }

        #endregion Public Instance Properties
    }
}
