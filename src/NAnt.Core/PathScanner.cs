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

// Brad Wilson (http://www.quality.nu/contact.aspx)

using System;
using System.Collections.Specialized;
using System.IO;

namespace NAnt.Core {
    /// <summary>
    /// Used to search for files on the PATH. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The local directory is not searched (since this would already be covered 
    /// by normal use of the includes element).
    /// </para>
    /// <para>
    /// Also, advanced pattern matching isn't supported: you need to know the 
    /// exact name of the file.
    /// </para>
    /// </remarks>
    [Serializable()]
    public class PathScanner {
        #region Private Instance Fields

        private StringCollection _unscannedNames = new StringCollection();
        private StringCollection _scannedNames = new StringCollection();

        #endregion Private Instance Fields

        #region Public Instance Methods

        /// <summary>
        /// Adds a file to the list of files to be scanned for.
        /// </summary>
        /// <param name="fileName">The filename to add to the list.</param>
        public void Add(string fileName) {
            _unscannedNames.Add(fileName);
        }

        public void Clear() {
            _unscannedNames.Clear();
        }

        public StringCollection Scan() {
            // clear any files we might've found previously
            _scannedNames.Clear();

            // break apart the PATH
            string[] paths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);

            // walk the names list
            foreach(string name in _unscannedNames) {
                // walk the paths, and see if the given file is on the path
                foreach(string path in paths) {
                    //do not scan inaccessible directories.
                    if (!Directory.Exists(path)) {
                        continue;
                    }

                    string[] found = Directory.GetFiles(path, name);

                    if (found.Length > 0) {
                        _scannedNames.Add(found[0]);
                        break;
                    }
                }
            }

            // return an enumerator to the scanned (& found) files
            return _scannedNames;
        }

        #endregion Public Instance Methods
    }
}
