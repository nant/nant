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

// Ian MacLean (ian_maclean@another.com)

using System;
using System.Globalization;
using System.IO;
    
namespace SourceForge.NAnt {

    /// <summary>
    /// Encalsulates information about installed frameworks incuding version information 
    /// and directory locations for finding tools.
    /// </summary>
    public class FrameworkInfo {
        #region Private Instance Fields

        string          _name;
        string          _description;
        string          _version;
        string          _csharpCompilerName; // move this to task specific section..
        string          _basicCompilerName;
        string          _jsharpCompilerName;
        string          _jscriptCompilerName;
        string          _resgenToolName;         
        DirectoryInfo   _frameworkDirectory;
        DirectoryInfo   _sdkDirectory;
        DirectoryInfo   _frameworkAssemblyDirectory;
        FileInfo        _runtimEngine;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        public FrameworkInfo( string name, 
                                string description, 
                                string version, 
                                string frameworkDir, 
                                string sdkDir, 
                                string frameworkAssemblyDir,
                                string csharpCompilerName,
                                string basicCompilerName,
                                string jsharpCompilerName,
                                string jscriptCompilerName,
                                string resgenToolName,
                                string runtimeEngine ) {
            _name = name;
            _description = description;
            _version = version;           
                        
            if (Directory.Exists(frameworkDir)) {
                _frameworkDirectory = new DirectoryInfo(frameworkDir);
            } else {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "frameworkDir {0} does not exist", frameworkDir) );
            }
            
            if (Directory.Exists(frameworkAssemblyDir)) {
                _frameworkAssemblyDirectory = new DirectoryInfo(frameworkAssemblyDir);
            } else {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "framework Assembly Dir {0} does not exist", frameworkAssemblyDir) );
            }
                                   
            if (sdkDir != null && Directory.Exists(sdkDir)) {
                _sdkDirectory = new DirectoryInfo(sdkDir);
            }           

            // if runtime engine is blank assume we aren't using one
            if (runtimeEngine != null && runtimeEngine.Length != 0) {
                string runtimeEnginePath = _frameworkDirectory.FullName + Path.DirectorySeparatorChar + runtimeEngine;
                if ( File.Exists(runtimeEnginePath ) ){
                    _runtimEngine = new FileInfo( runtimeEnginePath );
                } else {
                    throw new ArgumentException(string.Format( CultureInfo.InvariantCulture, "runtime Engine {0} does not exist", runtimeEnginePath )  );            
                }
            }
            // Validate that these tools exist ..
            _csharpCompilerName = csharpCompilerName;
            _basicCompilerName = basicCompilerName;
            _jsharpCompilerName = jsharpCompilerName;
            _jscriptCompilerName = jscriptCompilerName;
            _resgenToolName = resgenToolName;
        }

        #endregion Public Instance Constructors
              
        #region Public Instance Properties

        /// <summary>
        /// Gets the name of this framework.
        /// </summary>
        public string Name {
            get { return _name; }
        }
        /// <summary>
        /// Gets the description of this framework.
        /// </summary>
        public string Description {
            get { return _description; }
        }
        
        /// <summary>
        /// Gets the version of this framework.
        /// </summary>
        public string Version {
            get { return _version; }
        }
        
        /// <summary>
        /// Gets the name of the C# compiler for this framework.
        /// </summary>
        public string CSharpCompilerName {
            get { return _csharpCompilerName; }
        }

        /// <summary>
        /// Gets the name of the Basic compiler for this framework.
        /// </summary>
        public string BasicCompilerName {
            get { return _basicCompilerName; }
        }

        /// <summary>
        /// Gets the name of the J# compiler for this framework.
        /// </summary>
        public string JSharpCompilerName {
            get { return _jsharpCompilerName; }
        }

        /// <summary>
        /// Gets the name of the JScript compiler for this framework.
        /// </summary>
        public string JScriptCompilerName {
            get { return _jscriptCompilerName; }
        }
        
        /// <summary>
        /// Gets the name of the resgen tool for this framework.
        /// </summary>
        public string ResGenToolName {
            get { return _resgenToolName; }
        }
                
        /// <summary>
        /// Gets the base directory of the framework tools for this framework.
        /// </summary>
        public DirectoryInfo FrameworkDirectory {
            get { return _frameworkDirectory; }
        }
        /// <summary>
        /// Gets the path to the runtime engine for this framework. (not required for many frameworks )
        /// </summary>
        public FileInfo RuntimeEngine {
            get { return _runtimEngine; }
        }
       
        /// <summary>
        /// Gets the directory where the system assemblies are located.
        /// </summary>
        public DirectoryInfo FrameworkAssemblyDirectory {
            get { return _frameworkAssemblyDirectory; }
        }
        
        /// <summary>
        /// Gets the directory containing the framework SDK tools.
        /// </summary>
        public DirectoryInfo SdkDirectory {
            get { return _sdkDirectory; }
        }

        #endregion Public Instance Properties
    }
}
