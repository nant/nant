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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;

using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Compression.Types;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Creates a tar file from the specified filesets.
    /// </summary>
    /// <remarks>
    ///   <para>Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see> (SharpZipLib), an open source Tar/Zip/GZip library written entirely in C#.</para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Tar all files in <c>${build.dir}</c> and <c>${doc.dir}</c> into a file
    ///   called &quot;backup.tar.gz&quot;, and apply gzip compression to it.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <tar destfile="backup.tar.gz" compression="GZip">
    ///     <fileset basedir="${bin.dir}" prefix="bin">
    ///         <include name="**/*" />
    ///     </fileset>
    ///     <fileset basedir="${doc.dir}" prefix="doc">
    ///         <include name="**/*" />
    ///     </fileset>
    /// </tar>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("tar")]
    public class TarTask : Task {
        #region Private Instance Fields

        private FileInfo _destFile;
        private TarFileSetCollection _filesets = new TarFileSetCollection();
        private bool _includeEmptyDirs = false;
        private TarCompressionMethod _compressionMethod = TarCompressionMethod.None;
        private Hashtable _addedDirs = new Hashtable();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The tar file to create.
        /// </summary>
        [TaskAttribute("destfile", Required=true)]
        public FileInfo DestFile {
            get { return _destFile; }
            set { _destFile = value; }
        }

        /// <summary>
        /// Include empty directories in the generated tar file. The default is
        /// <see langword="false" />.
        /// </summary>
        [TaskAttribute("includeemptydirs", Required=false)]
        [BooleanValidator()]
        public bool IncludeEmptyDirs {
            get { return _includeEmptyDirs; }
            set { _includeEmptyDirs = value; }
        }

        /// <summary>
        /// The set of files to be included in the archive.
        /// </summary>
        [BuildElementArray("fileset")]
        public TarFileSetCollection TarFileSets {
            get { return _filesets; }
        }

        /// <summary>
        /// The compression method. The default is <see cref="TarCompressionMethod.None" />.
        /// </summary>
        [TaskAttribute("compression")]
        public TarCompressionMethod CompressionMethod {
            get { return _compressionMethod; }
            set { _compressionMethod = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Creates the tar file.
        /// </summary>
        protected override void ExecuteTask() {
            TarArchive archive = null;
            Stream outstream = null;

            Log(Level.Info, "Tarring {0} files to '{1}'.", 
                TarFileSets.FileCount, DestFile.FullName);

            try {
                outstream = File.Create(DestFile.FullName);

                // wrap outputstream with corresponding compression method
                switch (CompressionMethod) {
                    case TarCompressionMethod.GZip:
                        outstream = new GZipOutputStream(outstream);
                        break;
                    case TarCompressionMethod.BZip2:
                        outstream = new BZip2OutputStream(outstream);
                        break;
                }

                // create tar archive
                archive = TarArchive.CreateOutputTarArchive(outstream, 
                    TarBuffer.DefaultBlockFactor);

                // do not use convert line endings of text files to \n, as this
                // converts all content to ASCII
                archive.SetAsciiTranslation(false);

                // process all filesets
                foreach (TarFileSet fileset in TarFileSets) {
                    string basePath = fileset.BaseDirectory.FullName;

                    if (Path.GetPathRoot(basePath) != basePath) {
                        basePath = Path.GetDirectoryName(basePath + Path.DirectorySeparatorChar);
                    }

                    // add files to tar
                    foreach (string file in fileset.FileNames) {
                        if (!File.Exists(file)) {
                            throw new FileNotFoundException("File no longer exists.", file);
                        }

                        // the name of the tar entry
                        string entryName;

                        // determine name of the tar entry
                        if (file.StartsWith(basePath)) {
                            entryName = file.Substring(basePath.Length);
                            if (entryName.Length > 0 && entryName[0] == Path.DirectorySeparatorChar) {
                                entryName = entryName.Substring(1);
                            }

                            // remember that directory was added to tar file, so
                            // that we won't add it again later
                            string dir = Path.GetDirectoryName(file);
                            if (_addedDirs[dir] == null) {
                                _addedDirs[dir] = dir;
                            }
                        } else {
                            // flatten directory structure
                            entryName = Path.GetFileName(file);
                        }

                        // add prefix if specified
                        if (fileset.Prefix != null) {
                            entryName = fileset.Prefix + entryName;
                        }

                        // ensure directory separators are understood on linux
                        if (Path.DirectorySeparatorChar == '\\') {
                            entryName = entryName.Replace(@"\", "/");
                        }

                        TarEntry entry = TarEntry.CreateEntryFromFile(file);
                        entry.Name = entryName;
                        entry.GroupId = fileset.Gid;
                        entry.GroupName = fileset.GroupName;
                        entry.UserId = fileset.Uid;
                        entry.UserName = fileset.UserName;
                        entry.TarHeader.mode = fileset.FileMode;
                        archive.WriteEntry(entry, true);
                    }

                    // add (possibly empty) directories to zip
                    if (IncludeEmptyDirs) {
                        // add (possibly empty) directories to tar
                        foreach (string directory in fileset.DirectoryNames) {
                            // skip directories that were already added when the 
                            // files were added
                            if (_addedDirs[directory] != null) {
                                continue;
                            }

                            // skip directories that are not located beneath the base 
                            // directory
                            if (!directory.StartsWith(basePath) || directory.Length <= basePath.Length) {
                                continue;
                            }

                            // determine tar entry name
                            string entryName = directory.Substring(basePath.Length + 1);

                            // add prefix if specified
                            if (fileset.Prefix != null) {
                                entryName = fileset.Prefix + entryName;
                            }

                            // ensure directory separators are understood on linux
                            if (Path.DirectorySeparatorChar == '\\') {
                                entryName = entryName.Replace(@"\", "/");
                            }

                            if (!entryName.EndsWith("/")) {
                                // trailing directory signals to #ziplib that we're
                                // dealing with directory entry
                                entryName += "/";
                            }

                            // create directory entry
                            TarEntry entry = TarEntry.CreateTarEntry(entryName);
                            entry.GroupId = fileset.Gid;
                            entry.GroupName = fileset.GroupName;
                            entry.UserId = fileset.Uid;
                            entry.UserName = fileset.UserName;
                            entry.TarHeader.mode = fileset.DirMode;

                            // write directory to tar file
                            archive.WriteEntry(entry, false);
                        }
                    }
                }

                // close the tar archive
                archive.CloseArchive();
            } catch (Exception ex) {
                // close the tar output stream
                if (outstream != null) {
                    outstream.Close();
                }

                // close the tar archive
                if (archive != null) {
                    archive.CloseArchive();
                }

                // delete the (possibly corrupt) tar file
                if (DestFile.Exists) {
                    DestFile.Delete();
                }

                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Tar file '{0}' could not be created.", DestFile.FullName), 
                    Location, ex);
            }
        }

        #endregion Override implementation of Task
    }

    /// <summary>
    /// Specifies the compression methods support by <see cref="TarTask" />.
    /// </summary>
    public enum TarCompressionMethod {
        /// <summary>
        /// No compression.
        /// </summary>
        None = 0,

        /// <summary>
        /// GZIP compression.
        /// </summary>
        GZip = 1,

        /// <summary>
        /// BZIP2 compression.
        /// </summary>
        BZip2 = 2
    }
}
