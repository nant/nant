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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Types {
    /// <summary>
    /// Represents a command-line argument.
    /// </summary>
    public class Argument : Element {
        #region Private Instance Fields

        private string _value = null;
        private string _file = null;
        private bool _ifDefined = true;
        private bool _unlessDefined = false;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class.
        /// </summary>
        public Argument() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class
        /// with the specified value.
        /// </summary>
        public Argument(string value) {
            _value = value;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Value of this argument.
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// File of this argument.
        /// </summary>
        [TaskAttribute("file")]
        public string File {
            get { return _file; }
            set { _file = Project.GetFullPath(value); }
        }

        /// <summary>
        /// Indicates if the argument should be passed to the external program. 
        /// If true then the argument will be passed; otherwise skipped. 
        /// Default is "true".
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the argument should not be passed to the external program. 
        /// If false then the argument will be passed; otherwise skipped. 
        /// Default is "false".
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
