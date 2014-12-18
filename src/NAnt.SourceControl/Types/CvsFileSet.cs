// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System.Collections;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.SourceControl.Tasks;

namespace NAnt.SourceControl.Types {
    /// <summary>
    /// A <see cref="CvsFileSet" /> is a <see cref="FileSet" /> with extra 
    /// attributes useful in the context of the <see cref="CvsTask" />.
    /// </summary>
    [ElementName("cvsfileset")]
    public class CvsFileSet : FileSet {
        #region Private Instance Fields

        private bool _useCvsIgnore = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Indicates whether the entires in the .cvsignore should be used to limit the 
        /// file list; <see langword="true"/> to exclude files in .cvsignore, otherwise
        /// <see langword="false"/>.  The default is <see langword="true"/>.
        /// </summary>
        [TaskAttribute("usecvsignore", Required=false)]
        public bool UseCvsIgnore{
            get { return this._useCvsIgnore; }
            set { this._useCvsIgnore = value; }
        }

        #endregion Public Instance Properties

        #region Override Implementation of Element

        /// <summary>
        /// Initialize the <see cref="CvsFileSet"/> object and locate the .cvsignore
        /// files to add to the exclude list.
        /// </summary>
        protected override void Initialize() {
            if (UseCvsIgnore) {
                ArrayList ignoreFiles = new ArrayList();
                this.ScanCvsIgnores(base.BaseDirectory, ignoreFiles);

                foreach (string ignoreFile in ignoreFiles) {
                    Excludes.Add(ignoreFile);
                }
            }

            base.Initialize();
        }

        #endregion Override Implementation of Element

        #region Private Instance Methods

        private void ScanCvsIgnores(DirectoryInfo dir, ArrayList ignoreFiles) {
            foreach (FileInfo file in dir.GetFiles("*.cvsignore")) {
                AddCvsIgnores(file, ignoreFiles);
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories()) {
                this.ScanCvsIgnores(subDir, ignoreFiles);
            }
        }

        private void AddCvsIgnores(FileInfo file, ArrayList ignoreFiles) {
            using (StreamReader reader = new StreamReader(file.FullName)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    // if the .cvsignore is at the start of the file then
                    // exclude a *.[extension] pattern
                    if (line.IndexOf(".") == 0) {
                        ignoreFiles.Add(Path.Combine(file.DirectoryName, "*" + line));
                    } else {
                        ignoreFiles.Add(Path.Combine(file.DirectoryName, line));
                    }
                }
            }
        }

        #endregion Private Instance Methods
    }
}
