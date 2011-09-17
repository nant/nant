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
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;

using NUnit.Framework;

using NAnt.NUnit.Types;

namespace NAnt.NUnit1.Types {
    /// <summary>
    /// Carries data specified through the test element.
    /// </summary>
    [Serializable]
    public class NUnitTestData {
        #region Public Instance Properties

        public ITest Suite {
            get { return _suite; }
            set { _suite = value; }
        }
             
        public string OutFile {
            get { return _outfile; }
            set {_outfile = value;}
        }
        
        public string ToDir {
            get { return _todir; }
            set {_todir = value;}
        }

        public string Class {
            get { return _class; }
            set { _class = value; }
        }
        
        public string Assembly {
            get { return _assembly; }
            set { _assembly = value; }
        }
        
        public bool Fork {
            get { return _fork; }
            set { _fork = value; }
        }
        
        public bool HaltOnError { 
            get { return _haltonerror; }
            set { _haltonerror = value; }
        }
        
        public bool HaltOnFailure {
            get { return _haltonfailure; }
            set { _haltonfailure = value; }
        }

        public FormatterDataCollection Formatters {
            get { return _formatters; }
        }

        public string AppConfigFile {
            get { return _appConfigFile; }
            set { _appConfigFile = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        string _todir = null;
        string _outfile = null;
        string _class = null;
        string _assembly = null;
        bool _fork = false;
        bool _haltonerror = false;
        bool _haltonfailure = false;
        ITest _suite = null;
        FormatterDataCollection _formatters = new FormatterDataCollection();
        string _appConfigFile = null;

        #endregion Private Instance Fields
    }
}
