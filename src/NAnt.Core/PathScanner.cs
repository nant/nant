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

using NAnt.Core.Util;

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
    public sealed class PathScanner : ICloneable {
        #region Private Instance Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        
        private StringCollection _unscannedNames = new StringCollection();

        #endregion Private Instance Fields

        #region Implementation of ICloneable

        /// <summary>
        /// Creates a shallow copy of the <see cref="PathScanner" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="PathScanner" />.
        /// </returns>
        object ICloneable.Clone() {
            return Clone();
        }

        /// <summary>
        /// Creates a shallow copy of the <see cref="PathScanner" />.
        /// </summary>
        /// <returns>
        /// A shallow copy of the <see cref="PathScanner" />.
        /// </returns>
        public PathScanner Clone() {
            PathScanner clone = new PathScanner();
            clone._unscannedNames = Clone(_unscannedNames);
            return clone;
        }

        #endregion Implementation of ICloneable

        #region Public Instance Methods

        /// <summary>
        /// Adds a file to the list of files to be scanned for.
        /// </summary>
        /// <param name="fileName">The filename or search pattern to add to the list.</param>
        public void Add(string fileName) {
            _unscannedNames.Add(fileName);
        }

        public void Clear() {
            _unscannedNames.Clear();
        }

        /// <summary>
        /// Scans all direcetories in the PATH environment variable for files.
        /// </summary>
        /// <returns>
        /// List of matching files found in the PATH.
        /// </returns>
        public StringCollection Scan() {
            return Scan("PATH");
        }

        /// <summary>
        /// Scans all directories in the given environment variable for files.
        /// </summary>
        /// <param name="name">The environment variable of which the directories should be scanned.</param>
        /// <returns>
        /// List of matching files found in the directory of the given 
        /// environment variable.
        /// </returns>
        public StringCollection Scan(string name) {
            StringCollection scannedNames = new StringCollection();

            string envValue = Environment.GetEnvironmentVariable(name);
            if (envValue == null) {
                return scannedNames;
            }

            // break apart the PATH
            string[] paths = envValue.Split(Path.PathSeparator);

            // walk the names list
            foreach (string unscannedName in _unscannedNames) {
                // check if file is rooted
                if (Path.IsPathRooted(unscannedName)) {
                    if (File.Exists(unscannedName)) {
                        scannedNames.Add(unscannedName);
                    } else {
                        // no need to scan paths in environment variable for
                        // this file as it does not exist
                        continue;
                    }
                }

                string fileName = Path.GetFileName(unscannedName);
                string directoryName = Path.GetDirectoryName(unscannedName);

                // walk the paths, and see if the given file is on the path
                foreach (string path in paths) {
                    //do not scan inaccessible directories.
                    if (!Directory.Exists(path)) {
                        continue;
                    }

                    // search pattern might include directory part (eg. foo\bar.txt)
                    string scanPath = path;
                    if (!String.IsNullOrEmpty(directoryName)) {
                        scanPath = FileUtils.CombinePaths(path, directoryName);
                        //do not scan inaccessible directories.
                        if (!Directory.Exists(scanPath)) {
                            continue;
                        }
                    }

                    try {
                        string[] found = Directory.GetFiles(scanPath, fileName);

                        if (found.Length > 0) {
                            scannedNames.Add(found[0]);
                            break;
                        } 
                    } catch (UnauthorizedAccessException e) {
                        // In case of UnauthorizedAccessException,
                        // log the issue as a warning and move on to
                        // the next path.
                        logger.Warn( string.Format("Access to the path \"{0}\" is denied.", scanPath), e );
                        continue;
                    }
                }
            }

            // return an enumerator to the scanned (& found) files
            return scannedNames;
        }


        #endregion Public Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Creates a shallow copy of the specified <see cref="StringCollection" />.
        /// </summary>
        /// <param name="stringCollection">The <see cref="StringCollection" /> that should be copied.</param>
        /// <returns>
        /// A shallow copy of the specified <see cref="StringCollection" />.
        /// </returns>
        private static StringCollection Clone(StringCollection stringCollection) {
            string[] strings = new string[stringCollection.Count];
            stringCollection.CopyTo(strings, 0);
            StringCollection clone = new StringCollection();
            clone.AddRange(strings);
            return clone;
        }

        #endregion Private Static Methods
    }
}
