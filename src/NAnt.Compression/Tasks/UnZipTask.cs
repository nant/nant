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

using System.Globalization;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Extracts files from a zip archive.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see>
    ///   (SharpZipLib), an open source Zip/GZip library written entirely in C#.
    ///   </para>
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
    public class UnZipTask : ExpandBaseTask {
        #region Private Instance Fields

        private FileInfo _zipfile;
        private DirectoryInfo _toDir;
        private Encoding _encoding;
        private string _passWord;

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
                if (_toDir == null)
                    _toDir = new DirectoryInfo(Project.BaseDirectory);
                return _toDir;
            }
            set { _toDir = value; }
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

        /// <summary>
        /// The password that protect the Zip
        /// </summary>
        [TaskAttribute("password", Required = false)]
        public string PassWord {
            get { return _passWord; }
            set { _passWord = value; }
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
                    Log(Level.Info, "Unzipping '{0}' to '{1}'.", 
                        ZipFile.FullName, ToDirectory.FullName);

                    // set the password that protect the Zip
                    if(!string.IsNullOrEmpty(PassWord)) {
                        s.Password = PassWord;
                    }

                    ZipEntry entry;

                    // extract the file or directory entry
                    while ((entry = s.GetNextEntry()) != null) {
                        if (entry.IsDirectory) {
                            ExtractDirectory(s, ToDirectory.FullName,
                                entry.Name, entry.DateTime);
                        } else {
                            ExtractFile(s, ToDirectory.FullName, entry.Name,
                                entry.DateTime, entry.Size);
                        }
                    }

                    // close the zip stream
                    s.Close();
                }
            } catch (IOException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to expand '{0}' to '{1}'.", ZipFile.FullName, 
                    ToDirectory.FullName), Location, ex);
            } catch (ZipException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid zip file '{0}'.", ZipFile.FullName), Location, ex);
            }
        }

        #endregion Override implementation of Task
    }
}
