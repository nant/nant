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
        
        public FrameworkInfo( string name, 
                                string description, 
                                string version, 
                                string frameworkDir, 
                                string sdkDir, 
                                string csharpCompilerName,
                                string resgenToolName ) {
            _name = name;
            _description = description;
            _version = version;           
            
            // Does this need to be dir info.
            if (Directory.Exists(frameworkDir)) {
                _frameworkDirectory = new DirectoryInfo(frameworkDir);
            } else {
                throw new ArgumentException(string.Format("frameworkDir {} does not exist", frameworkDir) );
            }
            
            // check that the csharp compiler is present ??
            if (Directory.Exists(sdkDir)) {
                _sdkDirectory = new DirectoryInfo(sdkDir);
            } else {
                throw new ArgumentException(string.Format("sdkDirectory {0} does not exist", sdkDir)  );
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
        /// name of the resgen tool for this runtime
        /// </summary>
        public string ResGenToolName {
            get { return _resgenToolName; } 
        }
                
        /// <summary>
        /// 
        /// </summary>
        public DirectoryInfo FrameworkDirectory {
            get {            
                return _frameworkDirectory; 
            }           
        }
        
        /// <summary>
        /// 
        /// </summary>
        public DirectoryInfo SdkDirectory {
            get {                
                return _sdkDirectory; 
            }           
        }             
    }
}
