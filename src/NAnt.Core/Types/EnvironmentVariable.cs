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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an environment variable.
    /// </summary>
    [Serializable()]
    [ElementName("env")]
    public class EnvironmentVariable : Element, IConditional {
        #region Private Instance Fields

        private string _name;
        private string _value;
        private string _literalValue;
        private FileInfo _file;
        private DirectoryInfo _directory;
        private PathSet _path;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        /// <summary>
        /// Initializes a <see cref="EnvironmentVariable" /> instance with the
        /// specified name and value.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        public EnvironmentVariable(string name, string value) {
            _name = name;
            _value = value;
        }
        
        /// <summary>
        /// Initializes a <see cref="EnvironmentVariable" /> instance.
        /// </summary>
        public EnvironmentVariable() {
        }

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
        public FileInfo File {
            get { return _file; }
            set { 
                _value = value.ToString();
                _file = value;
            }
        }

        /// <summary>
        /// The value for a directory-based environment variable. NAnt will 
        /// convert it to an absolute path.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { return _directory; }
            set { 
                _value = value.ToString();
                _directory = value;
            }
        }

        /// <summary>
        /// The value for a PATH like environment variable. You can use 
        /// <c>:</c> or <c>;</c> as path separators and NAnt will convert it to 
        /// the platform's local conventions.
        /// </summary>
        [TaskAttribute("path")]
        public PathSet Path {
            get { return _path; }
            set { 
                _value = value.ToString(); 
                _path = value;
            }
        }

        /// <summary>
        /// Sets a single environment variable and treats it like a PATH - 
        /// ensures the right separator for the local platform is used.
        /// </summary>
        [BuildElement("path")]
        public PathSet PathSet {
            get { return _path; }
            set {
                // check if path hasn't already been set using "path" attribute
                if (_path != null) {
                    throw new BuildException("Either set the path using the \"path\""
                        + " attribute or the <path> element. You cannot set both.",
                        Location);
                }
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

    /// <summary>
    /// A set of environment variables.
    /// </summary>
    [Serializable]
    [ElementName("environment")]
    public class EnvironmentSet : Element {
        #region Private Instance Fields

        private OptionCollection _options = new OptionCollection();
        private EnvironmentVariableCollection _environmentVariables = new EnvironmentVariableCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Environment variable to pass to a program.
        /// </summary>
        [BuildElementArray("option")]
        [Obsolete("Use <variable> element instead.")]
        public OptionCollection Options {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// Environment variable to pass to a program.
        /// </summary>
        [BuildElementArray("variable")]
        public EnvironmentVariableCollection EnvironmentVariables {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }

        #endregion Public Instance Properties
    }
}
