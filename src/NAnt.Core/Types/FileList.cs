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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian@maclean.ms)
// Kevin Dente (kevindente@yahoo.com)

using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Types {
    /// <summary>
    /// Summary description for FileList.
    /// </summary>
    public class FileList : Element { 

        private DirectoryListScanner _scanner = new DirectoryListScanner();
        private bool _hasScanned = false;
        private string _files = String.Empty;
        private StringCollection _fileNames = null;

        /// <summary>The list of directories in this file list.  Default is project base directory.</summary>
        [TaskAttribute("dir")]
        public string DirectoryList {
            get { return _scanner.DirectoryList; }
            set { _scanner.DirectoryList = value; }
        }

        /// <summary>A semi-colon delimited list of files in this file list.</summary>
        [TaskAttribute("files")]
        public string Files {
            get { return _files; }
            set { _files = value; }
        }
        /// <summary>The collection of file names that match the file list.</summary>
        public StringCollection FileNames {
            get {
                if (!_hasScanned) {
                    Scan();
                }
                return _fileNames;
            }
        }

        public void Scan() {
            try {
                // Break apart the directory list
                _scanner.BaseDirectory = Project.BaseDirectory;
                string[] fileList = _files.Split(System.IO.Path.PathSeparator);
                foreach(string file in fileList) {
                    _scanner.Add(file);
                }

                _fileNames = _scanner.Scan();

            } catch (Exception e) {
                throw new BuildException("Error creating file list.", Location, e);
            }
            
            _hasScanned = true;
        }
    }
}
