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
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an environment variable.
    /// </summary>
    [Serializable()]
    public class EnvironmentVariable : Element {
        #region Private Instance Fields

        private string _name;
        private string _value;
        private string _literalValue;
        private FileInfo _file;
        private PathList _path;
        private bool _ifDefined = true;
        private bool _unlessDefined = false;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the environment variable.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string VariableName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The literal value for the environment variable.
        /// </summary>
        [TaskAttribute("value")]
        public string LiteralValue {
            get { return _literalValue; }
            set { 
                _value = value;
                _literalValue = value;
            }
        }

        /// <summary>
        /// The value for a file-based environment variable. NAnt will convert 
        /// it to an absolute filename.
        /// </summary>
        [TaskAttribute("file")]
        [StringValidator(AllowEmpty=false)]
        public FileInfo File {
            get { return _file; }
            set { 
                _value = value.ToString();
                _file = value;
            }
        }

        /// <summary>
        /// The value for a PATH like environment variable. You can use 
        /// <code>;</code> or <code>;</code> as path separators and NAnt will 
        /// convert it to the platform's local conventions.
        /// </summary>
        [TaskAttribute("path")]
        public PathList Path {
            get { return _path; }
            set { 
                _value = value.ToString(); 
                _path = value;
            }
        }

        /// <summary>
        /// Gets the value of the environment variable.
        /// </summary>
        public string Value {
            get { return _value; }
        }

        /// <summary>
        /// Indicates if the environment variable should be passed to the 
        /// external program.  If <see langword="true" /> then the environment
        /// variable will be passed;  otherwise, skipped. The default is 
        /// <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the environment variable should not be passed to the 
        /// external program.  If <see langword="false" /> then the environment
        /// variable will be passed;  otherwise, skipped. The default is 
        /// <see langword="false" />.
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
