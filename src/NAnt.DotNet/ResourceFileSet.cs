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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean ( ian_maclean@another.com )

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using SourceForge.NAnt;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;


namespace SourceForge.NAnt.Tasks {

    /// <summary>
    /// Specialized fileset class for managing resources. Has an additional namespace 
    /// </summary>
    public class ResourceFileSet : FileSet {
    
        public ResourceFileSet( ResourceFileSet source) : base(source){ // copy constructor
            _prefix = source._prefix;                             
        }
        // default constructor
        public ResourceFileSet() : base(){
        }
        
        string _prefix = ""; //Default to empty prefix
        /// <summary>Indicates the prefix to prepend to the actual resource.  This is usually the default namspace of the assembly.</summary>
        [TaskAttribute("prefix")]	
        public string Prefix {
                get { return _prefix; }
                set { _prefix = value; }	    
        }

        public FileSet ResxFiles {
            get {
                return getResxFileNames();
            }
        }
        public FileSet NonResxFiles {
            get {
                return getNonResxFileNames();
            }
        }
        /// <summary>
        /// Return a FileSet containing all the non-resx filenames
        /// </summary>
        /// <returns></returns>
        private FileSet getNonResxFileNames(){
            FileSet retFileSet = new FileSet(this);
            retFileSet.Includes.Clear();          
            foreach ( string file in FileNames ) {
                if (Path.GetExtension( file) != ".resx" ){
                    retFileSet.Includes.Add( file);
                }                
            }   
            retFileSet.Scan();
            return retFileSet;
        }
        /// <summary>
        /// Return a FileSet containing all the resx filenames
        /// </summary>
        /// <returns></returns>
        private FileSet getResxFileNames()
        {
            FileSet retFileSet = new FileSet(this);
            retFileSet.Includes.Clear();
            foreach ( string file in FileNames ){
                if (Path.GetExtension( file) == ".resx" ){
                    retFileSet.Includes.Add( file);
                }                
            }   
            retFileSet.Scan();
            return retFileSet;
        }	    	   
    }
 }