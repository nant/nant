// NAnt - A .NET build tool
// Copyright (C) 2009 Gert Driesen
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

using System;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Compression.Tasks {
	/// <summary>
	/// Summary description for ExpandTask.
	/// </summary>
	public abstract class ExpandBaseTask : Task {
        private bool _overwrite = true;

        /// <summary>
        /// Overwrite files, even if they are newer than the corresponding 
        /// entries in the archive. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("overwrite", Required=false)]
        public virtual bool Overwrite {
            get { return _overwrite; }
            set { _overwrite = value; }
        }

        /// <summary>
        /// Extracts a file entry from the specified stream.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream" /> containing the compressed entry.</param>
        /// <param name="destDirectory">The directory where to store the expanded file.</param>
        /// <param name="entryName">The name of the entry including directory information.</param>
        /// <param name="entryDate">The date of the entry.</param>
        /// <param name="entrySize">The uncompressed size of the entry.</param>
        /// <exception cref="BuildException">
        ///   <para>The destination directory for the entry could not be created.</para>
        ///   <para>-or-</para>
        ///   <para>The entry could not be extracted.</para>
        /// </exception>
        /// <remarks>
        /// We cannot rely on the fact that the directory entry of a given file
        /// is created before the file is extracted, so we should create the
        /// directory if it doesn't yet exist.
        /// </remarks>
        protected void ExtractFile(Stream inputStream, string destDirectory, string entryName, DateTime entryDate, long entrySize) {
            // determine destination file
            FileInfo destFile = new FileInfo(Path.Combine(destDirectory,
                entryName));

            // ensure destination directory exists
            if (!destFile.Directory.Exists) {
                try {
                    destFile.Directory.Create();
                    destFile.Directory.LastWriteTime = entryDate;
                    destFile.Directory.Refresh();
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Directory '{0}' could not be created.", destFile.DirectoryName),
                        Location, ex);
                }
            }

            // determine if entry actually needs to be extracted
            if (!Overwrite && destFile.Exists && destFile.LastWriteTime >= entryDate) {
                Log(Level.Debug, "Skipping '{0}' as it is up-to-date.", 
                    destFile.FullName);
                return;
            }

            Log(Level.Verbose, "Extracting '{0}' to '{1}'.", entryName, 
                destDirectory);

            try {
                // extract the entry
                using (FileStream sw = new FileStream(destFile.FullName, FileMode.Create, FileAccess.Write)) {
                    int size = 2048;
                    byte[] data = new byte[size];

                    while (true) {
                        size = inputStream.Read(data, 0, data.Length);
                        if (size == 0) {
                            break;
                        }
                        sw.Write(data, 0, size);
                    }

                    sw.Close();
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Unable to expand '{0}' to '{1}'.", entryName, destDirectory),
                    Location, ex);
            }

            destFile.LastWriteTime = entryDate;
        }

        /// <summary>
        /// Extracts a directory entry from the specified stream.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream" /> containing the directory entry.</param>
        /// <param name="destDirectory">The directory where to create the subdirectory.</param>
        /// <param name="entryName">The name of the directory entry.</param>
        /// <param name="entryDate">The date of the entry.</param>
        /// <exception cref="BuildException">
        ///   <para>The destination directory for the entry could not be created.</para>
        /// </exception>
        protected void ExtractDirectory(Stream inputStream, string destDirectory, string entryName, DateTime entryDate) {
            DirectoryInfo destDir = new DirectoryInfo(Path.Combine(
                destDirectory, entryName));
            if (!destDir.Exists) {
                try {
                    destDir.Create();
                    destDir.LastWriteTime = entryDate;
                    destDir.Refresh();
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Directory '{0}' could not be created.", destDir.FullName),
                        Location, ex);
                }
            }
        }
	}
}
