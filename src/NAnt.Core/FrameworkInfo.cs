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
using System.IO;
using System.Globalization;
    
namespace SourceForge.NAnt {
   

    /// <summary>
    /// Encalsulates information about installed frameworks incuding version information and directory locations for finding tools
    /// </summary>
    public class FrameworkInfo  {
        // privte members
        string          _name;
        string          _description;
        string          _version;
        string          _csharpCompilerName; // move this to task specific section..
        string          _resgenToolName;         
        DirectoryInfo   _frameworkDirectory;
        DirectoryInfo   _sdkDirectory;
        DirectoryInfo   _frameworkAssemblyDirectory;
        FileInfo        _runtimEngine;
        
        public FrameworkInfo( string name, 
                                string description, 
                                string version, 
                                string frameworkDir, 
                                string sdkDir, 
                                string frameworkAssemblyDir,
                                string csharpCompilerName,
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
                                   
            if (Directory.Exists(sdkDir)) {
                _sdkDirectory = new DirectoryInfo(sdkDir);
            } else {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "sdkDirectory {0} does not exist", sdkDir)  );
            }           
            // if runtime engine is blank assume we aren't using one
            if ( runtimeEngine != "" ) {
                string runtimeEnginePath = _frameworkDirectory.FullName + Path.DirectorySeparatorChar + runtimeEngine;
                if ( File.Exists(runtimeEnginePath ) ){
                    _runtimEngine = new FileInfo( runtimeEnginePath );
                } else {
                    throw new ArgumentException(string.Format( CultureInfo.InvariantCulture, "runtime Engine {0} does not exist", runtimeEnginePath )  );            
                }
            }
            // Validate that these tools exist ..
            _csharpCompilerName = csharpCompilerName;
            _resgenToolName = resgenToolName;
        }
              
        // public properties
        public string Name {
            get { return _name; }           
        }
        /// <summary>
        /// 
        /// </summary>
        public string Description {
            get { return _description; }          
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string Version {
            get { return _version; }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string CSharpCompilerName {
            get { return _csharpCompilerName; } 
        }
        
        /// <summary>
        /// name of the resgen tool for this framework
        /// </summary>
        public string ResGenToolName {
            get { return _resgenToolName; } 
        }
                
        /// <summary>
        /// Base directory of the framework tools
        /// </summary>
        public DirectoryInfo FrameworkDirectory {
            get {            
                return _frameworkDirectory; 
            }           
        }
        /// <summary>
        /// Path to the runtime engine for this framework. ( not required for many frameworks )
        /// </summary>
        public FileInfo RuntimeEngine {
            get {            
                return _runtimEngine; 
            }           
        }
       
        /// <summary>
        /// Directory where the System assemblies are located
        /// </summary>
        public DirectoryInfo FrameworkAssemblyDirectory {
            get {            
                return _frameworkAssemblyDirectory; 
            }           
        }
        
        /// <summary>
        /// Director containing the framework SDK tools
        /// </summary>
        public DirectoryInfo SdkDirectory {
            get {                
                return _sdkDirectory; 
            }           
        }             
    }
}
