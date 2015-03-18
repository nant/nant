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

using System.Globalization;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Compression.Types;

namespace NAnt.Compression.Tasks {
    /// <summary>
    /// Extracts files from a tar archive.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see>
    ///   (SharpZipLib), an open source Zip/GZip library written entirely in C#.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Extracts all files from a gzipped tar, preserving the directory structure.</para>
    ///   <code>
    ///     <![CDATA[
    /// <untar src="nant-bin.tar.gz" dest="bin" compression="gzip" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("untar")]
    public class UnTarTask : ExpandBaseTask {
        #region Private Instance Fields

        private FileInfo _srcFile;
        private DirectoryInfo _destDir;
        private TarCompressionMethod _compressionMethod = TarCompressionMethod.None;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The archive file to expand.
        /// </summary>
        [TaskAttribute("src", Required=true)]
        public FileInfo SrcFile {
            get { return _srcFile; }
            set { _srcFile = value; }
        }

        /// <summary>
        /// The directory where to store the expanded file(s). The default is
        /// the project base directory.
        /// </summary>
        [TaskAttribute("dest", Required=false)]
        public DirectoryInfo DestinationDirectory {
            get {
                if (_destDir == null)
                    _destDir = new DirectoryInfo(Project.BaseDirectory);
                return _destDir;
            }
            set { _destDir = value; }
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
        /// Extracts the files from the archive.
        /// </summary>
        protected override void ExecuteTask() {
            Stream fs = null;

            try {
                // ensure archive exists
                if (!SrcFile.Exists)
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Tar file '{0}' does not exist.", SrcFile.FullName),
                        Location);

                fs = SrcFile.OpenRead();

                // wrap inputstream with corresponding compression method
                Stream instream;
                switch (CompressionMethod) {
                    case TarCompressionMethod.GZip:
                        instream = new GZipInputStream(fs);
                        break;
                    case TarCompressionMethod.BZip2:
                        instream = new BZip2InputStream(fs);
                        break;
                    default:
                        instream = fs;
                        break;
                }

                using (TarInputStream s = new TarInputStream(instream)) {
                    Log(Level.Info, "Expanding '{0}' to '{1}'.", 
                        SrcFile.FullName, DestinationDirectory.FullName);

                    TarEntry entry;

                    // extract the file or directory entry
                    while ((entry = s.GetNextEntry()) != null) {
                        if (entry.IsDirectory) {
                            ExtractDirectory(s, DestinationDirectory.FullName,
                                entry.Name, entry.ModTime);
                        } else {
                            ExtractFile(s, DestinationDirectory.FullName,
                                entry.Name, entry.ModTime, entry.Size);
                        }
                    }
                }
            } catch (IOException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to expand '{0}' to '{1}'.", SrcFile.FullName, 
                    DestinationDirectory.FullName), Location, ex);
            } catch (TarException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid tar file '{0}'.", SrcFile.FullName), Location, ex);
            } catch (BZip2Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid bzip2'd tar file '{0}'.", SrcFile.FullName), Location, ex);
            } catch (GZipException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid gzipped tar file '{0}'.", SrcFile.FullName), Location, ex);
            } finally {
                // close the filestream
                if (fs != null)
                    fs.Close ();
            }
        }

        #endregion Override implementation of Task
    }
}
