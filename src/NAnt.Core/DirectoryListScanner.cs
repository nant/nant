// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Kevin Dente (kevindente@yahoo.com)

using System;
using System.Collections.Specialized;
using System.IO;

using NAnt.Core.Attributes;

namespace NAnt.Core {
    /// <summary>
    /// Used to search for files on an arbitrary list of directories. 
    /// Advanced pattern matching isn't supported here: you need to know the 
    /// exact name of the file.
    /// </summary>
    public class DirectoryListScanner {
        #region Private Instance Fields

        private string _baseDirectory = null;
        private StringCollection _unscannedNames = new StringCollection();
        private StringCollection _scannedNames = new StringCollection();
        private string _directoryList = String.Empty;

        #endregion Private Instance Fields

        #region Public Instance Properties

        [TaskAttribute("dir")]
        public string DirectoryList {
            get { return _directoryList; }
            set { _directoryList = value; }
        }

        /// <summary>
        /// Gets or sets the base directory of this file set.
        /// </summary>
        /// <value>
        /// The base directory of this file set. The default is the project base
        /// directory.
        /// </value>
        public string BaseDirectory {
            get { return _baseDirectory; }
            set { _baseDirectory = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Adds a file to the list to be scanned.
        /// </summary>
        /// <param name="fileName">The filename to add to the list</param>
        public void Add(string fileName) {
            _unscannedNames.Add(fileName);
        }

        public void Clear() {
            _unscannedNames.Clear();
        }

        public StringCollection Scan() {
            // clear any files we might've found previously
            _scannedNames.Clear();

            if (BaseDirectory == null) {
                BaseDirectory = Environment.CurrentDirectory;
            }

            // break apart the directory list
            string[] paths = _directoryList.Split(System.IO.Path.PathSeparator);

            string fullPath = null;

            // walk the names list
            foreach(string name in _unscannedNames) {
                // walk the paths, and see if the given file is on the path
                foreach(string path in paths) {
                    if ((path.Length != 0) && 
                        (Directory.Exists(path))) {

                        if (Path.IsPathRooted(path))
                            fullPath = Path.GetFullPath(path);
                        else
                            fullPath = Path.GetFullPath(Path.Combine(BaseDirectory, path));

                        string[] found = Directory.GetFiles(fullPath, name);

                        if(found.Length > 0) {
                            _scannedNames.Add(found[0]);
                            break;
                        }
                    }
                }
            }

            // return an enumerator to the scanned (& found) files
            return _scannedNames;
        }

        #endregion Public Instance Methods
    }
}
