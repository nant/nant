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

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Tar;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Zip.Types;

namespace NAnt.Zip.Tasks {
	/// <summary>
	/// Creates a tar file from a specified fileset.
	/// </summary>
	/// <remarks>
	///   <para>Uses <see href="http://www.icsharpcode.net/OpenSource/SharpZipLib/">#ziplib</see> (SharpZipLib), an open source Tar/Zip/GZip library written entirely in C#.</para>
	/// </remarks>
	/// <example>
	///   <para>Tar all files in the subdirectory <c>build</c> to <c>backup.tar</c>.</para>
	///   <code>
	///     <![CDATA[
	/// <tar destfile="backup.tar">
	///     <tarfileset basedir="build">
	///         <include name="*.*" />
	///     </fileset>
	/// </tar>
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("tar")]
	public class TarTask : Task {
		#region Private Instance Fields

		private FileInfo _destFile;
		private TarFileSet _fileset = new TarFileSet();
		private bool _includeEmptyDirs = false;
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
		[BuildElement("tarfileset")]
		public TarFileSet TarFileSet {
			get { return _fileset; }
			set { _fileset = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of Task

		/// <summary>
		/// Creates the tar file.
		/// </summary>
		protected override void ExecuteTask() {
			TarOutputStream tarOutstream = null;

			string basePath;

			// ensure base directory is set, even if fileset was not initialized
			// from XML
			if (TarFileSet.BaseDirectory == null) {
				TarFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
			}

			Log(Level.Info, "Tarring {0} files to '{1}'.", 
				TarFileSet.FileNames.Count, DestFile.FullName);

			basePath = TarFileSet.BaseDirectory.FullName;
			if (Path.GetPathRoot(basePath) != basePath) {
				basePath = Path.GetDirectoryName(basePath + Path.DirectorySeparatorChar);
			}

			try {
				// TODO: compression !!
				tarOutstream = new TarOutputStream(DestFile.Create());

				/*
				archive = TarArchive.CreateOutputTarArchive(tarOutstream, 
					TarBuffer.DefaultBlockFactor);
				archive.SetAsciiTranslation(true);
				*/

				// add files to tar
				foreach (string file in TarFileSet.FileNames) {
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
					if (TarFileSet.Prefix != null) {
						entryName = TarFileSet.Prefix + entryName;
					}

					// ensure directory separators are understood on linux
					if (Path.DirectorySeparatorChar == '\\') {
						entryName = entryName.Replace(@"\", "/");
					}

					// create tar header
					TarHeader header = new TarHeader();
					header.mode = TarFileSet.FileMode;
					header.size = new FileInfo(file).Length;

					// create tar entry
					TarEntry entry = new TarEntry(header);
					entry.Name = entryName;
					entry.GroupId = TarFileSet.Gid;
					entry.GroupName = TarFileSet.GroupName;
					entry.UserId = TarFileSet.Uid;
					entry.UserName = TarFileSet.UserName;
					entry.ModTime = File.GetLastWriteTime(file);

					Log(Level.Verbose, "Adding {0}.", entryName);
                    
					// write file to tar file
					tarOutstream.PutNextEntry(entry);

					// write file content to stream in small chuncks
					using (FileStream fs = File.OpenRead(file)) {
						byte[] buffer = new byte[50000];

						while (true) {
							int bytesRead = fs.Read(buffer, 0, buffer.Length);
							if (bytesRead == 0) {
								break;
							}
							tarOutstream.Write(buffer, 0, bytesRead);
						}
					}

					// close the tar entry
					tarOutstream.CloseEntry();
				}

				// add (possibly empty) directories to tar
				if (IncludeEmptyDirs) {
					foreach (string directory in TarFileSet.DirectoryNames) {
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
						if (TarFileSet.Prefix != null) {
							entryName = TarFileSet.Prefix + entryName;
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

						// create tar header
						TarHeader header = new TarHeader();
						header.mode = TarFileSet.DirMode;

						// create directory entry
						TarEntry entry = new TarEntry(header);
						entry.Name = entryName;
						entry.GroupId = TarFileSet.Gid;
						entry.GroupName = TarFileSet.GroupName;
						entry.UserId = TarFileSet.Uid;
						entry.UserName = TarFileSet.UserName;
						entry.ModTime = File.GetLastWriteTime(directory);

						// write directory to tar file
						tarOutstream.PutNextEntry(entry);

						// close the tar entry
						tarOutstream.CloseEntry();
					}
				}

				// close the tar output stream
				tarOutstream.Close();
			} catch (Exception ex) {
				// close the tar output stream
				if (tarOutstream != null) {
					tarOutstream.Close();
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
}
