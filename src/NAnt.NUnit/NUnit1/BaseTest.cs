// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
using System.Xml;    
using SourceForge.NAnt.Attributes;
using NUnit.Framework;
using NUnit.Runner;

namespace SourceForge.NAnt.Tasks.NUnit {
	
	/// <summary>
	/// Class to represent a test element of an NUnit task  
	/// </summary>
	public class BaseTest : Element {
		    
        string _class = null;                
        string _assembly = null;        	    
        bool _fork = false;                
        bool _haltonerror = false;                
        bool _haltonfailure = false;
        string _appConfigFile = null;
		
		// Attribute properties		   
        /// <summary>Class Name of the test</summary>
        [TaskAttribute("class", Required=true)]
        public string Class             { get { return _class; } set { _class = value; } }
        
        /// <summary>Assembly to Load the test from</summary>
        [TaskAttribute("assembly", Required=true)]
        public string Assembly          { get { return Project.GetFullPath(_assembly); } set { _assembly = value; } }
        
        /// <summary>Run the tests in a separate AppDomain</summary>
        [TaskAttribute("fork")]
        [BooleanValidator()]
        public bool Fork                { get { return _fork; } set { _fork = value; } }
        
        /// <summary>Stop the build process if an error occurs during the test run</summary>
        [TaskAttribute("haltonerror")]
        [BooleanValidator()]
        public bool HaltOnError         { get { return _haltonerror; } set { _haltonerror = value; } }
        
        /// <summary>Stop the build process if a test fails (errors are considered failures as well).</summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure       { get { return _haltonfailure; } set { _haltonfailure = value; }}

		  [TaskAttribute("appconfig")]
        public string AppConfigFile {
           get { return _appConfigFile; }
           set { _appConfigFile = value; }
        }
	}
}
