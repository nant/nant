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
using System.Globalization;
using System.IO;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Zip.Tasks {
    /// <summary>
    /// Creates a zip file from a specified fileset.
    /// </summary>
    /// <remarks>
    ///   <para>Uses <a href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</a> (SharpZipLib), an open source Zip/GZip library written entirely in C#.</para>
    /// </remarks>
    /// <example>
    ///   <para>Zip all files in the subdirectory <c>build</c> to <c>backup.zip</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <zip zipfile="backup.zip">
    ///     <fileset basedir="build">
    ///         <includes name="*.*"/>
    ///     </fileset>
    /// </zip>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("zip")]
    public class ZipTask : Task {
        #region Private Instance Fields

        private string _zipfile = null;
        private int _ziplevel = 6; 
        private FileSet _fileset = new FileSet();
        private Crc32 crc = new Crc32();
        private DateTime _stampDateTime = DateTime.MinValue;
        private string _comment = null;
        private bool _includeEmptyDirs = false;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The zip file to create.
        /// </summary>
        [TaskAttribute("zipfile", Required=true)]
        public string ZipFileName {
            get { return (_zipfile != null) ? Project.GetFullPath(_zipfile) : null; }
            set { _zipfile = SetStringValue(value); }
        }

        /// <summary>
        /// The comment for the file.
        /// </summary>
        [TaskAttribute("comment", Required=false)]
        public string Comment {
            get { return _comment; }
            set { _comment = SetStringValue(value); }
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
        /// Desired level of compression (default is <c>6</c>).
        /// </summary>
        /// <value>0 - 9 (0 - STORE only, 1-9 DEFLATE (1-lowest, 9-highest))</value>
        [TaskAttribute("ziplevel", Required=false)]
        [Int32ValidatorAttribute(0, 9)]
        public int ZipLevel {
            get { return _ziplevel; }
            set { _ziplevel = value; }
        }
        
        /// <summary>
        /// Include empty directories in the generated zip file. Defaults to 
        /// <c>false</c>.
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
        [FileSet("fileset")]
        public FileSet ZipFileSet {
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

            Log(Level.Info, LogPrefix + "Zipping {0} files to {1}.", ZipFileSet.FileNames.Count, ZipFileName);

            string basePath = Path.GetDirectoryName(Project.GetFullPath(Path.GetFullPath(ZipFileSet.BaseDirectory)) + Path.DirectorySeparatorChar);

            // TO-DO check if all matching files and directories are located 
            // under the base path and throw BuildException if not ?

            try {
                zOutstream = new ZipOutputStream(File.Create(ZipFileName));

                // set compression level
                if (ZipLevel > 0) {
                    zOutstream.SetLevel(ZipLevel);
                } else {
                    zOutstream.SetMethod(ZipOutputStream.STORED);

                    // setting the method to store still compresses the files
                    // setting the level to 0 fixes this
                    zOutstream.SetLevel(ZipLevel);
                }

                // set comment
                if (Comment != null) {
                    zOutstream.SetComment(Comment);
                }

                // add files to zip
                foreach (string file in ZipFileSet.FileNames) {
                    if (File.Exists(file)) {
                        // read source file
                        FileStream fStream = File.OpenRead(file);
                        long fileSize = fStream.Length;
                        byte[] buffer = new byte[fileSize];
                        fStream.Read(buffer, 0, buffer.Length);
                        fStream.Close();
                        
                        // determine name of the zip entry
                        string entryName = file.Substring(basePath.Length + 1);

                        // create zip entry                       
                        ZipEntry entry = new ZipEntry(entryName);
                        
                        // set time/date stamp on zip entry
                        if (_stampDateTime != DateTime.MinValue) {
                            entry.DateTime = _stampDateTime;
                        } else {
                            entry.DateTime = File.GetLastWriteTime(file);
                        }

                        Log(Level.Verbose, LogPrefix + "Adding {0}.", entryName);
                        
                        if (ZipLevel == 0) {
                            entry.Size = fileSize;
                            
                            // calculate crc32 of current file
                            crc.Reset();
                            crc.Update(buffer);
                            entry.Crc  = crc.Value;
                        }
                        
                        // write file to zip file
                        zOutstream.PutNextEntry(entry);
                        zOutstream.Write(buffer, 0, buffer.Length);
                    } else {
                        throw new FileNotFoundException("File no longer exists.", file);
                    }
                }

                // add (possibly empty) directories to zip
                if (IncludeEmptyDirs) {
                    foreach (string directory in ZipFileSet.DirectoryNames) {
                        // skip directories that are not located beneath the base 
                        // directory
                        if (!directory.StartsWith(basePath) || directory.Length <= basePath.Length) {
                            continue;
                        }

                        // determine zip entry name
                        string entryName = directory.Substring(basePath.Length + 1);

                        // create directory entry (done by adding a trailing slash)
                        ZipEntry entry = new ZipEntry(entryName + "/");

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
                if (File.Exists(ZipFileName)) {
                    File.Delete(ZipFileName);
                }

                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Zip file '{0}' could not be created.", ZipFileName), Location, ex);
            }
        }

        #endregion Override implementation of Task
    }
}
