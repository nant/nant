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

using System;
using System.Xml;

using NUnit.Framework;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks.NUnit {
    [ElementName("test")]
    public class NUnitTest : BaseTest {
        #region Private Instance Fields

        string _todir = null;
        string _outfile = null;
        ITest _suite = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        public ITest Suite {
            get { return _suite; }
            set { _suite = value; }
        }
             
        /// <summary>Base name of the test result. The full filename is determined by this attribute and the extension of formatter</summary>
        [TaskAttribute("outfile")]
        public string OutFile { get { return _outfile; } set {_outfile = value;} }
        
        /// <summary>Directory to write the reports to.</summary>
        [TaskAttribute("todir")]
        public string ToDir { get { return _todir; } set {_todir = value;} }

        #endregion Public Instance Properties

        #region Internal Instance Methods

        internal NUnitTestData GetTestData() {
            NUnitTestData data = new NUnitTestData();
            data.Suite = Suite;
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
