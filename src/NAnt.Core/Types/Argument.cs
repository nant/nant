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
    /// Represents a command-line argument.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When passed to an external application, the argument will be quoted
    ///   when appropriate. This does not apply to the <see cref="Line" />
    ///   parameter, which is always passed as is.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   A single command-line argument containing a space character.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <arg value="-l -a" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Two separate command-line arguments.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <arg line="-l -a" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   A single command-line argument with the value <c>\dir;\dir2;\dir3</c>
    ///   on DOS-based systems and <c>/dir:/dir2:/dir3</c> on Unix-like systems.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <arg path="/dir;/dir2:\dir3" />
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable]
    [ElementName("arg")]
    public class Argument : Element, IConditional {
        #region Private Instance Fields

        private FileInfo _file;
        private DirectoryInfo _directory;
        private PathSet _path;
        private string _value;
        private string _line;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Argument" /> class
        /// with the given path.
        /// </summary>
        public Argument(PathSet value) {
            _path = value;
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
        /// File and individual path elements will be quoted if necessary.
        /// </remarks>
        public override string ToString() {
            if (File != null) {
                return QuoteArgument(File.FullName);
            } else if (Directory != null) {
                return QuoteArgument(Directory.FullName);
            }  else if (Path != null) {
                return QuoteArgument(Path.ToString());
            } else if (Value != null) {
                return QuoteArgument(Value);
            } else if (Line != null) {
                return Line;
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
        /// The value for a directory-based command-line argument; will be
        /// replaced with the absolute path of the directory.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { return _directory; }
            set { _directory = value; }
        }

        /// <summary>
        /// The value for a PATH-like command-line argument; you can use 
        /// <c>:</c> or <c>;</c> as path separators and NAnt will convert it 
        /// to the platform's local conventions, while resolving references to 
        /// environment variables.
        /// </summary>
        /// <remarks>
        /// Individual parts will be replaced with the absolute path, resolved
        /// relative to the project base directory.
        /// </remarks>
        [TaskAttribute("path")]
        public PathSet Path {
            get { return _path; }
            set { 
                // check if path hasn't already been set using <path> element
                if (_path != null) {
                    throw new BuildException("Either set the path using the \"path\""
                        + " attribute or the <path> element. You cannot set both.",
                        Location);
                }
                _path = value; 
            }
        }

        
        /// <summary>
        /// Sets a single command-line argument and treats it like a PATH - ensures 
        /// the right separator for the local platform is used.
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
                _path = value; 
            }
        }

        /// <summary>
        /// List of command-line arguments; will be passed to the executable
        /// as is.
        /// </summary>
        [TaskAttribute("line")]
        public string Line {
            get { return _line; }
            set { _line = value; }
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
                } else if (Directory != null) {
                    return Directory.FullName;
                } else if (Path != null) {
                    return Path.ToString();
                } else if (Line != null) {
                    return Line;
                } else {
                    return Value;
                }
            }
        }

        #endregion Internal Instance Properties

        #region Private Static Methods

        /// <summary>
        /// Quotes a command line argument if it contains a single quote or a
        /// space.
        /// </summary>
        /// <param name="argument">The command line argument.</param>
        /// <returns>
        /// A quoted command line argument if <paramref name="argument" /> 
        /// contains a single quote or a space; otherwise, 
        /// <paramref name="argument" />.
        /// </returns>
        private static string QuoteArgument(string argument) {
            if (argument.IndexOf("\"") > -1) {
                // argument is already quoted
                return argument;
            } else if (argument.IndexOf("'") > -1 || argument.IndexOf(" ") > -1) {
                // argument contains space and is not quoted, so quote it
                return '\"' + argument + '\"';
            } else {
                return argument;
            }
        }

        #endregion Private Static Methods
    }
}
