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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// A task to create a zip file from a specified fileset.
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

        string _zipfile = null;
        int _ziplevel = 6; 
        FileSet _fileset = new FileSet();
        Crc32 crc = new Crc32();
        DateTime _stampDateTime = DateTime.MinValue;
        string _comment = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>The zip file to create.</summary>
        [TaskAttribute("zipfile", Required=true)]
        public string ZipFileName { get { return Project.GetFullPath(_zipfile); } set {_zipfile = value; } }

        /// <summary>The comment for the file</summary>
        [TaskAttribute("comment", Required=false)]
        public string Comment     { get { return _comment; } set {_comment = value; } }

        /// <summary>An optional date/time stamp for the files.</summary>
        [TaskAttribute("stampdatetime", Required=false)]
        public string Stamp { 
            get { return _stampDateTime.ToString("G", DateTimeFormatInfo.InvariantInfo); }
            set {
                try {
                    _stampDateTime = DateTime.Parse(value, DateTimeFormatInfo.InvariantInfo);
                } catch (FormatException exc) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid string representatation {0} of a DateTime value.", value), "Stamp", exc);
                }
            } 
        }
        
        /// <summary>Desired level of compression (default is 6).</summary>
        /// <value>0 - 9 (0 - STORE only, 1-9 DEFLATE (1-lowest, 9-highest))</value>
        [TaskAttribute("ziplevel", Required=false)]
        public int    ZipLevel    { get { return _ziplevel; } set {_ziplevel = value; } }
        
        /// <summary>The set of files to be included in the archive.</summary>
        [FileSet("fileset")]
        public FileSet ZipFileSet { get { return _fileset; } }

        #endregion Public Instance Properties

        #region Override implementation of Task
                
        protected override void ExecuteTask() {

            int fileCount = ZipFileSet.FileNames.Count;
            Log.WriteLine(LogPrefix + "Zipping {0} files to {1}", fileCount, ZipFileName);

            ZipOutputStream zOutstream = new ZipOutputStream(File.Create(ZipFileName));
            int zipLevel = ZipLevel;
            if (zipLevel > 0) {
                zOutstream.SetLevel(zipLevel);
            } else {
                zOutstream.SetMethod(ZipOutputStream.STORED);
            }
            string basePath = Path.GetDirectoryName(Project.GetFullPath(ZipFileSet.BaseDirectory));
            
            //set comment
            if (_comment != null) {
                zOutstream.SetComment(_comment);
            }

            foreach (string file in ZipFileSet.FileNames) {
                if (File.Exists(file)) {
                    // read source file
                    FileStream fStream = File.OpenRead(file);
                    long   fileSize = fStream.Length;
                    byte[] buffer = new byte[fileSize];
                    fStream.Read(buffer, 0, buffer.Length);
                    fStream.Close();
                    
                    // create ZIP entry
                    string entryName = file.Substring(basePath.Length + 1);
                   
                    ZipEntry entry = new ZipEntry(entryName);
                    
                    //set time/date stamp on files
                    if (_stampDateTime != DateTime.MinValue) {
                        entry.DateTime = _stampDateTime;
                    } else {
                        entry.DateTime = File.GetLastWriteTime(file);
                    }

                    if (Verbose) {
                        Log.WriteLine(LogPrefix + "Adding {0}", entryName);
                    }
                    
                    if (zipLevel == 0) {
                        entry.Size = fileSize;
                        
                        // calculate crc32 of current file
                        crc.Reset();
                        crc.Update(buffer);
                        entry.Crc  = crc.Value;
                    }
                    
                    // write file to ZIP
                    zOutstream.PutNextEntry(entry);
                    zOutstream.Write(buffer, 0, buffer.Length);
                } else {
                    throw new FileNotFoundException();
                }
            }
            zOutstream.Finish();
            zOutstream.Close();
        }

        #endregion Override implementation of Task
    }
}
