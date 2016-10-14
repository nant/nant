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
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

using NAnt.Compression.Types;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Creates a zip file from the specified filesets.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see>
    ///   (SharpZipLib), an open source Tar/Zip/GZip library written entirely in C#.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Zip all files in <c>${build.dir}</c> and <c>${doc.dir}</c> into a file
    ///   called &quot;backup.zip&quot;.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <zip zipfile="backup.zip">
    ///     <fileset basedir="${bin.dir}" prefix="bin">
    ///         <include name="**/*" />
    ///     </fileset>
    ///     <fileset basedir="${doc.dir}" prefix="doc">
    ///         <include name="**/*" />
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
        private ZipFileSetCollection _filesets = new ZipFileSetCollection();
        private DateTime _stampDateTime;
        private string _comment;
        private bool _includeEmptyDirs;
        private bool _flatten;
        private DuplicateHandling _duplicateHandling = DuplicateHandling.Add;
        private Encoding _encoding;
        private Hashtable _addedDirs = new Hashtable();
        private Hashtable _fileEntries = new Hashtable();
        private string _passWord;

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
        [TaskAttribute("comment")]
        public string Comment {
            get { return _comment; }
            set { _comment = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Date/time stamp for the files in the format MM/DD/YYYY HH:MM:SS.
        /// </summary>
        [TaskAttribute("stampdatetime")]
        [DateTimeValidator()]
        public DateTime Stamp {
            get { return _stampDateTime; }
            set { _stampDateTime = value; }
        }

        /// <summary>
        /// Desired level of compression. Possible values are 0 (STORE only) 
        /// to 9 (highest). The default is <c>6</c>.
        /// </summary>
        [TaskAttribute("ziplevel")]
        [Int32ValidatorAttribute(0, 9)]
        public int ZipLevel {
            get { return _ziplevel; }
            set { _ziplevel = value; }
        }

        /// <summary>
        /// Include empty directories in the generated zip file. The default is
        /// <see langword="false" />.
        /// </summary>
        [TaskAttribute("includeemptydirs")]
        [BooleanValidator()]
        public bool IncludeEmptyDirs {
            get { return _includeEmptyDirs; }
            set { _includeEmptyDirs = value; }
        }
        
        /// <summary>
        /// Ignore directory structure of source directory, compress all files 
        /// into a single directory.
        /// The default value is <see langword="false" />.
        /// </summary>
        [TaskAttribute("flatten")]
        [BooleanValidator()]
        public virtual bool Flatten {
            get { return _flatten; }
            set { _flatten = value; }
        }

        /// <summary>
        /// The set of files to be included in the archive.
        /// </summary>
        [BuildElementArray("fileset")]
        public ZipFileSetCollection ZipFileSets {
            get { return _filesets; }
            set { _filesets = value; }
        }

        /// <summary>
        /// Specifies the behaviour when a duplicate file is found. The default
        /// is <see cref="T:NAnt.Compression.Types.DuplicateHandling.Add" />.
        /// </summary>
        [TaskAttribute("duplicate")]
        public DuplicateHandling DuplicateHandling {
            get { return _duplicateHandling; }
            set { _duplicateHandling = value; }
        }

        /// <summary>
        /// The character encoding to use for filenames and comment inside the
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

        /// <summary>
        /// The password to protect the Zip.
        /// </summary>
        [TaskAttribute("password", Required = false)]
        public string PassWord {
            get { return _passWord; }
            set { _passWord = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Creates the zip file.
        /// </summary>
        protected override void ExecuteTask() {
            ZipOutputStream zOutstream = null;
            
            Log(Level.Info, "Zipping {0} files to '{1}'.", 
                ZipFileSets.FileCount, ZipFile.FullName);
                
            try {
                if (!Directory.Exists(ZipFile.DirectoryName)) {
                    Directory.CreateDirectory(ZipFile.DirectoryName);
                }
            
                // set encoding to use for filenames and comment
                ZipConstants.DefaultCodePage = Encoding.CodePage;

                zOutstream = new ZipOutputStream(ZipFile.Create());

                // set compression level
                zOutstream.SetLevel(ZipLevel);

                // set password
                if(!String.IsNullOrEmpty(PassWord)) {
                    zOutstream.Password = PassWord;
                }

                // set comment
                if (!String.IsNullOrEmpty(Comment)) {
                    zOutstream.SetComment(Comment);
                }

                foreach (ZipFileSet fileset in ZipFileSets) {
                    string basePath = fileset.BaseDirectory.FullName;
                    if (Path.GetPathRoot(basePath) != basePath) {
                        basePath = Path.GetDirectoryName(basePath + Path.DirectorySeparatorChar);
                    }

                    // add files to zip
                    foreach (string file in fileset.FileNames) {
                        // ensure file exists (in case "asis" was used)
                        if (!File.Exists(file)) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "File '{0}' does not exist.", file), Location);
                        }

                        // the name of the zip entry
                        string entryName;

                        // determine name of the zip entry
                        if (!Flatten && file.StartsWith(basePath)) {
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
                        if (fileset.Prefix != null) {
                            entryName = fileset.Prefix + entryName;
                        }

                        // ensure directory separators are understood on linux
                        if (Path.DirectorySeparatorChar == '\\') {
                            entryName = entryName.Replace(@"\", "/");
                        }

                        // perform duplicate checking
                        if (_fileEntries.ContainsKey(entryName)) {
                            switch (DuplicateHandling) {
                                case DuplicateHandling.Add:
                                    break;
                                case DuplicateHandling.Fail:
                                    throw new BuildException(string.Format(
                                        CultureInfo.InvariantCulture, 
                                        "Duplicate file '{0}' was found.", 
                                        entryName), Location.UnknownLocation);
                                case DuplicateHandling.Preserve:
                                    // skip current entry
                                    continue;
                                default:
                                    throw new BuildException(string.Format(
                                        CultureInfo.InvariantCulture, 
                                        "Duplicate value '{0}' is not supported.", 
                                        DuplicateHandling.ToString()), 
                                        Location.UnknownLocation);
                            }
                        }

                        // create zip entry
                        ZipEntry entry = new ZipEntry(entryName);

                        // store entry (to allow for duplicate checking)
                        _fileEntries[entryName] = null;

                        // set date/time stamp on zip entry
                        if (Stamp != DateTime.MinValue) {
                            entry.DateTime = Stamp;
                        } else {
                            entry.DateTime = File.GetLastWriteTime(file);
                        }

                        // write file content to stream in small chuncks
                        using (FileStream fs = File.OpenRead(file)) {
                            // set size for backward compatibility with older unzip
                            entry.Size = fs.Length;

                            Log(Level.Verbose, "Adding {0}.", entryName);

                            // write file to zip file
                            zOutstream.PutNextEntry(entry);

                            byte[] buffer = new byte[50000];

                            while (true) {
                                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 0)
                                    break;
                                zOutstream.Write(buffer, 0, bytesRead);
                            }
                        }

                    }

                    // add (possibly empty) directories to zip
                    if (IncludeEmptyDirs) {
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

                            // determine zip entry name
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
                            ZipEntry entry = new ZipEntry(entryName);

                            // set size for backward compatibility with older unzip
                            entry.Size = 0L;

                            // write directory to zip file
                            zOutstream.PutNextEntry(entry);
                        }
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
            } finally {
                CleanUp();
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private void CleanUp() {
            _addedDirs.Clear();
            _fileEntries.Clear();
        }

        #endregion Private Instance Methods
    }
}
