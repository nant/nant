// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Mike Krueger (mike@icsharpcode.net)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Zip.Types;

namespace NAnt.Zip.Tasks {
    /// <summary>
    /// Creates a zip file from a specified fileset.
    /// </summary>
    /// <remarks>
    ///   <para>Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see> (SharpZipLib), an open source Tar/Zip/GZip library written entirely in C#.</para>
    /// </remarks>
    /// <example>
    ///   <para>Zip all files in the subdirectory <c>build</c> to <c>backup.zip</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <zip zipfile="backup.zip">
    ///     <fileset basedir="build">
    ///         <include name="*.*" />
    ///     </fileset>
    /// </zip>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("zip")]
    public class ZipTask : Task {
        #region Private Instance Fields

        private FileInfo _zipfile;
        private int _ziplevel = 6; 
        private ZipFileSet _fileset = new ZipFileSet();
        private DateTime _stampDateTime = DateTime.MinValue;
        private string _comment = null;
        private bool _includeEmptyDirs = false;
        private Hashtable _addedDirs = new Hashtable();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The zip file to create.
        /// </summary>
        [TaskAttribute("zipfile", Required=true)]
        public FileInfo ZipFile {
            get { return _zipfile; }
            set { _zipfile = value; }
        }

        /// <summary>
        /// The comment for the file.
        /// </summary>
        [TaskAttribute("comment", Required=false)]
        public string Comment {
            get { return _comment; }
            set { _comment = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// An optional date/time stamp for the files.
        /// </summary>
        [TaskAttribute("stampdatetime", Required=false)]
        public string Stamp { 
            get { return _stampDateTime.ToString("G", DateTimeFormatInfo.InvariantInfo); }
            set {
                try {
                    _stampDateTime = DateTime.Parse(value, DateTimeFormatInfo.InvariantInfo);
                } catch (FormatException ex) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid string representation {0} of a DateTime value.", value), "Stamp", ex);
                }
            } 
        }

        /// <summary>
        /// Desired level of compression. Possible values are 0 (STORE only) 
        /// to 9 (highest). The default is <c>6</c>.
        /// </summary>
        [TaskAttribute("ziplevel", Required=false)]
        [Int32ValidatorAttribute(0, 9)]
        public int ZipLevel {
            get { return _ziplevel; }
            set { _ziplevel = value; }
        }

        /// <summary>
        /// Include empty directories in the generated zip file. The default is
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
        [BuildElement("fileset")]
        public ZipFileSet ZipFileSet {
            get { return _fileset; }
            set { _fileset = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Creates the zip file.
        /// </summary>
        protected override void ExecuteTask() {
            ZipOutputStream zOutstream = null;
            string basePath;

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (ZipFileSet.BaseDirectory == null) {
                ZipFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            Log(Level.Info, "Zipping {0} files to '{1}'.", 
                ZipFileSet.FileNames.Count, ZipFile.FullName);

            basePath = ZipFileSet.BaseDirectory.FullName;
            if (Path.GetPathRoot(basePath) != basePath) {
                basePath = Path.GetDirectoryName(basePath + Path.DirectorySeparatorChar);
            }

            try {
                zOutstream = new ZipOutputStream(ZipFile.Create());

                // set compression level
                zOutstream.SetLevel(ZipLevel);

                // set comment
                if (!StringUtils.IsNullOrEmpty(Comment)) {
                    zOutstream.SetComment(Comment);
                }

                // add files to zip
                foreach (string file in ZipFileSet.FileNames) {
                    if (File.Exists(file)) {
                        // the name of the zip entry
                        string entryName;

                        // determine name of the zip entry
                        if (file.StartsWith(basePath)) {
                            entryName = file.Substring(basePath.Length);
                            if (entryName.Length > 0 && entryName[0] == Path.DirectorySeparatorChar) {
                                entryName = entryName.Substring(1);
                            }

                            // remember that directory was added to zip file, so
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
                        if (ZipFileSet.Prefix != null) {
                            entryName = ZipFileSet.Prefix + entryName;
                        }

                        // ensure directory separators are understood on linux
                        if (Path.DirectorySeparatorChar == '\\') {
                            entryName = entryName.Replace(@"\", "/");
                        }

                        // create zip entry
                        ZipEntry entry = new ZipEntry(entryName);

                        // set time/date stamp on zip entry
                        if (_stampDateTime != DateTime.MinValue) {
                            entry.DateTime = _stampDateTime;
                        } else {
                            entry.DateTime = File.GetLastWriteTime(file);
                        }

                        Log(Level.Verbose, "Adding {0}.", entryName);
                        
                        // write file to zip file
                        zOutstream.PutNextEntry(entry);

                        // write file content to stream in small chuncks
                        using (FileStream fs = File.OpenRead(file)) {
                            byte[] buffer = new byte[50000];

                            while (true) {
                                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 0) {
                                    break;
                                }
                                zOutstream.Write(buffer, 0, bytesRead);
                            }
                        }
                    } else {
                        throw new FileNotFoundException("File no longer exists.", file);
                    }
                }

                // add (possibly empty) directories to zip
                if (IncludeEmptyDirs) {
                    foreach (string directory in ZipFileSet.DirectoryNames) {
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

                        // determine zip entry name
                        string entryName = directory.Substring(basePath.Length + 1);

                        // add prefix if specified
                        if (ZipFileSet.Prefix != null) {
                            entryName = ZipFileSet.Prefix + entryName;
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
                        ZipEntry entry = new ZipEntry(entryName);

                        // write directory to zip file
                        zOutstream.PutNextEntry(entry);
                    }
                }

                zOutstream.Close();
                zOutstream.Finish();
            } catch (Exception ex) {
                // close the zip output stream
                if (zOutstream != null) {
                    zOutstream.Close();
                }

                // delete the (possibly corrupt) zip file
                if (ZipFile.Exists) {
                    ZipFile.Delete();
                }

                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Zip file '{0}' could not be created.", ZipFile.FullName), 
                    Location, ex);
            }
        }

        #endregion Override implementation of Task
    }
}
