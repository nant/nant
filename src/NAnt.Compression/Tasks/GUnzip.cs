// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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

using System.Globalization;
using System.IO;

using ICSharpCode.SharpZipLib.GZip;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Expands a file packed using GZip compression.
    /// </summary>
    /// <example>
    ///   <para>Expands &quot;test.tar.gz&quot; to &quot;test2.tar&quot;.</para>
    ///   <code>
    ///     <![CDATA[
    /// <gunzip src="test.tar.gz" dest="test.tar" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("gunzip")]
    public class GUnzip : Task {
        #region Private Instance Fields

        private FileInfo _srcFile;
        private FileInfo _destFile;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to expand.
        /// </summary>
        [TaskAttribute("src", Required=true)]
        public FileInfo SrcFile {
            get { return _srcFile; }
            set { _srcFile = value; }
        }

        /// <summary>
        /// The destination file.
        /// </summary>
        [TaskAttribute("dest", Required=true)]
        public FileInfo DestFile {
            get { return _destFile; }
            set { _destFile = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Extracts the file from the gzip archive.
        /// </summary>
        protected override void ExecuteTask() {
            try {
                using (GZipInputStream gzs = new GZipInputStream(SrcFile.OpenRead())) {
                    Log(Level.Info, "Expanding '{0}' to '{1}' ({2} bytes).", 
                        SrcFile.FullName, DestFile.FullName, gzs.Length);

                    // holds data from src file
                    byte[] data = new byte[8 * 1024];

                    // first read from input to ensure we're dealing with valid
                    // src file before we actually create the dest file
                    int size = gzs.Read(data, 0, data.Length);

                    // write expanded data to dest file
                    using (FileStream fs = new FileStream(DestFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        while (size > 0) {
                            fs.Write(data, 0, size);
                            size = gzs.Read(data, 0, data.Length);
                        }
                        // close output stream
                        fs.Close();
                    }
                    // close input stream
                    gzs.Close();
                }
            } catch (IOException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to expand '{0}' to '{1}'.", SrcFile.FullName, 
                    DestFile.FullName), Location, ex);
            } catch (GZipException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid gzip file '{0}'.", SrcFile.FullName), Location, ex);
            }
        }

        #endregion Override implementation of Task
    }
}
