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
using System.IO;

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents a command-line argument.
    /// </summary>
    [ElementName("arg")]
    public class Argument : DataTypeBase {
        #region Private Instance Fields

        private string _value;
        private FileInfo _file;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class.
        /// </summary>
        public Argument() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class
        /// with the specified command-line argument.
        /// </summary>
        public Argument(string value) {
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class
        /// with the given file.
        /// </summary>
        public Argument(FileInfo value) {
            _file = value;
        }

        #endregion Public Instance Constructors

        #region Override implementation of Object

        /// <summary>
        /// Returns the argument as a <see cref="string" />.
        /// </summary>
        /// <returns>
        /// The argument as a <see cref="string" />.
        /// </returns>
        /// <remarks>
        /// File arguments will be quoted.
        /// </remarks>
        public override string ToString() {
            if (File != null) {
                // quote file arguments if necessary
                string argument = File.FullName;
                if (argument.IndexOf("\"") > -1) {
                    // argument is already quoted
                    return argument;
                } else if (argument.IndexOf("'") > -1 || argument.IndexOf(" ") > -1) {
                    // argument contains space and is not quoted, so quote it
                    return '\"' + argument + '\"';
                } else {
                    return argument;
                }
            } else if (Value != null) {
                return Value;
            } else {
                return string.Empty;
            }
        }

        #endregion Override implementation of Object

        #region Public Instance Properties

        /// <summary>
        /// A single command-line argument; can contain space characters.
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// The name of a file as a single command-line argument; will be 
        /// replaced with the absolute filename of the file.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// Indicates if the argument should be passed to the external program. 
        /// If <see langword="true" /> then the argument will be passed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the argument should not be passed to the external 
        /// program. If <see langword="false" /> then the argument will be 
        /// passed; otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        /// <summary>
        /// Gets string value corresponding with the argument.
        /// </summary>
        internal string StringValue {
            get {
                if (File != null) {
                    return File.FullName;
                } else {
                    return Value;
                }
            }
        }

        #endregion Internal Instance Properties
    }
}
