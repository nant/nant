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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents a nested path element.
    /// </summary>
    [Serializable()]
    [ElementName("pathelement")]
    public class PathElement : Element, IConditional {
        #region Private Instance Fields

        private FileInfo _file;
        private DirectoryInfo _directory;
        private PathSet _path;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of a file to add to the path. Will be replaced with 
        /// the absolute path of the file.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// The name of a directory to add to the path. Will be replaced with 
        /// the absolute path of the directory.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { return _directory; }
            set { _directory = value; }
        }

        /// <summary>
        /// A string that will be treated as a path-like string. You can use
        /// <c>:</c> or <c>;</c> as path separators and NAnt will convert it 
        /// to the platform's local conventions, while resolving references
        /// to environment variables.
        /// </summary>
        [TaskAttribute("path")]
        public PathSet Path {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the entry will be added to the
        /// path; otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of <see cref="IfDefined" />. If <see langword="false" /> 
        /// then the entry will be added to the path; otherwise, skipped. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        /// <summary>
        /// Gets the parts of a path represented by this element.
        /// </summary>
        /// <value>
        /// A <see cref="StringCollection" /> containing the parts of a path 
        /// represented by this element.
        /// </value>
        public StringCollection Parts {
            get {
                if (File != null) {
                    StringCollection parts = new StringCollection();
                    parts.Add(File.FullName);
                    return parts;
                } else if (Directory != null) {
                    StringCollection parts = new StringCollection();
                    parts.Add(Directory.FullName);
                    return parts;
                } else if (Path != null) {
                    return Path.GetElements();
                }

                return new StringCollection();
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element

        protected override void Initialize() {
            if (File == null && Directory == null && Path == null) {
                throw new BuildException(string.Format(CultureInfo.InstalledUICulture,
                    "At least \"file\", \"directory\" or \"path\" must be" 
                    + " specified."), Location);
            }

            if (File == null && Directory == null && Path == null) {
                throw new BuildException(string.Format(CultureInfo.InstalledUICulture,
                    "\"file\", \"directory\" and \"path\" cannot be specified" 
                    + " together."), Location);
            }
        }

        #endregion Override implementation of Element
    }
}