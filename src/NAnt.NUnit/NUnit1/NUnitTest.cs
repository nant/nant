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
// Ian MacLean (ian_maclean@another.com)
// Gert Driesen (gert.driesen@ardatis.com)

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.NUnit1.Types {
    /// <summary>
    /// Represents a test element of an NUnit task.
    /// </summary>
    [ElementName("test")]
    public class NUnitTest : Element {
        #region Private Instance Fields

        string _class = null;
        string _assembly = null;
        bool _fork = false;
        bool _haltonerror = false;
        bool _haltonfailure = false;
        string _appConfigFile = null;
        string _todir = null;
        string _outfile = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>Base name of the test result. The full filename is determined by this attribute and the extension of formatter</summary>
        [TaskAttribute("outfile")]
        public string OutFile { get { return _outfile; } set {_outfile = value;} }
        
        /// <summary>Directory to write the reports to.</summary>
        [TaskAttribute("todir")]
        public string ToDir { get { return _todir; } set {_todir = value;} }

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

        #endregion Public Instance Properties

        #region Internal Instance Methods

        internal NUnitTestData GetTestData() {
            NUnitTestData data = new NUnitTestData();
            data.OutFile = OutFile;
            data.ToDir = ToDir;
            data.Class = Class;
            data.Assembly = Assembly;
            data.Fork = Fork;
            data.HaltOnError = HaltOnError;
            data.HaltOnFailure = HaltOnFailure;
            data.AppConfigFile = AppConfigFile;
            return data;
        }

        #endregion Internal Instance Methods
    }
}
