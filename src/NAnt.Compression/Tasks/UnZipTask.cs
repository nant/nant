// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Extracts files from a zip file.
    /// </summary>
    /// <remarks>
    ///   <para>Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see> (SharpZipLib), an open source Zip/GZip library written entirely in C#.</para>
    /// </remarks>
    /// <example>
    ///   <para>Extracts all the file from the zip, preserving the directory structure.</para>
    ///   <code>
    ///     <![CDATA[
    /// <unzip zipfile="backup.zip"/>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("unzip")]
    public class UnZipTask : Task {
        #region Private Instance Fields

        private FileInfo _zipfile;
        private DirectoryInfo _toDir;
        private bool _overwrite = true;
        private Encoding _encoding;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The archive file to expand.
        /// </summary>
        [TaskAttribute("zipfile", Required=true)]
        public FileInfo ZipFile {
            get { return _zipfile; }
            set { _zipfile = value; }
        }

        /// <summary>
        /// The directory where the expanded files should be stored. The 
        /// default is the project base directory.
        /// </summary>
        [TaskAttribute("todir", Required=false)]
        public DirectoryInfo ToDirectory {
            get { 
                if (_toDir == null) {
                    return new DirectoryInfo(Project.BaseDirectory);
                }
                return _toDir;
            }
            set { _toDir = value; }
        }

        /// <summary>
        /// Overwrite files, even if they are newer than the corresponding 
        /// entries in the archive. The default is <see langword="true" />.
        /// </summary>
        public bool Overwrite {
            get { return _overwrite; }
            set { _overwrite = value; }
        }

        /// <summary>
        /// The character encoding that has been used for filenames inside the
        /// zip file. The default is the system's OEM code page.
        /// </summary>
        [TaskAttribute("encoding")]
        public Encoding Encoding {
            get {
                if (_encoding == null) {
                    _encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                }
                return _encoding; 
            }
            set { _encoding = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Extracts the files from the zip file.
        /// </summary>
        protected override void ExecuteTask() {
            try {
                // ensure zip file exists
                if (!ZipFile.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Zip file '{0}' does not exist.", ZipFile.FullName),
                        Location);
                }

                // set encoding of filenames and comment
                ZipConstants.DefaultCodePage = Encoding.CodePage;

                using (ZipInputStream s = new ZipInputStream(ZipFile.OpenRead())) {
                    Log(Level.Info, "Unzipping '{0}' to '{1}' ({2} bytes).", 
                        ZipFile.FullName, ToDirectory.FullName, s.Length);

                    ZipEntry entry;

                    // extract the file or directory entry
                    while ((entry = s.GetNextEntry()) != null) {
                        if (entry.IsDirectory) {
                            ExtractDirectory(s, entry.Name, entry.DateTime);
                        } else {
                            ExtractFile(s, entry.Name, entry.DateTime);
                        }
                    }

                    // close the zip stream
                    s.Close();
                }
            } catch (IOException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to extract '{0}' to '{1}'.", ZipFile.FullName, 
                    ToDirectory.FullName), Location, ex);
            } catch (ZipException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid zip file '{0}'.", ZipFile.FullName), Location, ex);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Extracts a file entry from the specified stream.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream" /> containing the compressed entry.</param>
        /// <param name="entryName">The name of the entry including directory information.</param>
        /// <param name="entryDate">The date of the entry.</param>
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
        protected void ExtractFile(Stream inputStream, string entryName, DateTime entryDate) {
            // determine destination file
            FileInfo destFile = new FileInfo(Path.Combine(ToDirectory.FullName,
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
                ToDirectory.FullName);

            try {
                // extract the entry
                using (FileStream sw = new FileStream(destFile.FullName, FileMode.Create, FileAccess.Write)) {
                    int size = 2048;
                    byte[] data = new byte[2048];

                    while (true) {
                        size = inputStream.Read(data, 0, data.Length);
                        if (size > 0) {
                            sw.Write(data, 0, size);
                        } else {
                            break;
                        }
                    }

                    sw.Close();
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Unable to expand '{0}' to '{1}'.", entryName, ToDirectory.FullName),
                    Location, ex);
            }

            destFile.LastWriteTime = entryDate;
        }

        /// <summary>
        /// Extracts a directory entry from the specified stream.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream" /> containing the directory entry.</param>
        /// <param name="entryName">The name of the directory entry.</param>
        /// <param name="entryDate">The date of the entry.</param>
        /// <exception cref="BuildException">
        ///   <para>The destination directory for the entry could not be created.</para>
        /// </exception>
        protected void ExtractDirectory(Stream inputStream, string entryName, DateTime entryDate) {
            DirectoryInfo destDir = new DirectoryInfo(Path.Combine(
                ToDirectory.FullName, entryName));
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

        #endregion Private Instance Methods
    }
}
