// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Globalization;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Deletes a file, fileset or directory.</summary>
    /// <remarks>
    ///   <para>Deletes either a single file, all files in a specified directory and its sub-directories, or a set of files specified by one or more filesets.</para>
    ///   <note>If the file attribute is set then the fileset contents will be ignored.  To delete the files in the file set ommit the file attribute in the delete element.</note>
    ///   <note>Read-only files cannot be deleted.  Use the <see cref="AttribTask" /> first to remove the read-only attribute.</note>
    /// </remarks>
    /// <example>
    ///   <para>Delete a single file.</para>
    ///   <code>&lt;delete file="myfile.txt"/&gt;</code>
    ///   <para>Delete a directory and the contents within.  If the directory does not exist the task does nothing.</para>
    ///   <code>&lt;delete dir="${build.dir}" failonerror="false"/&gt;</code>
    ///   <para>Delete a set of files.  Note the lack of file attribute in the delete element.</para>
    ///   <code>
    /// <![CDATA[
    /// <delete>
    ///     <fileset>
    ///         <includes name="${basename}-??.exe"/>
    ///         <includes name="${basename}-??.pdb"/>
    ///     </fileset>
    /// </delete>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("delete")]
    public class DeleteTask : Task {
        
        string _file = null;       
        string _dir = null;               
        FileSet _fileset = new FileSet();

        /// <summary>The file to delete.</summary>
        [TaskAttribute("file")]
        public string FileName {
            get { return _file; }
            set {_file = value; }
        }
        
        /// <summary>The directory to delete.</summary>
        [TaskAttribute("dir")]
        public string DirectoryName {
            get { return _dir; }
            set {_dir = value; }
        }

        /// <summary>All the files in the file set will be deleted.</summary>
        [FileSet("fileset")]
        public FileSet DeleteFileSet {
            get { return _fileset; }
        }

        protected override void ExecuteTask() {
            // limit task to deleting either a file or a directory or a file set
            if (FileName != null && DirectoryName != null) {
                throw new BuildException("Cannot specify 'file' and 'dir' in the same delete task.", Location);
            }

            if (FileName != null) {
                // try to delete specified file
                string path = null;
                try {
                    path = Project.GetFullPath(FileName);
                } catch (Exception e) {
                    string msg = String.Format(CultureInfo.InvariantCulture, "Could not determine path from {0}.", FileName);
                    throw new BuildException(msg, Location, e);
                }
                DeleteFile(path, true);

            } else if (DirectoryName != null) {
                // try to delete specified directory
                string path = null;
                try {
                    path = Project.GetFullPath(DirectoryName);
                } catch (Exception e) {
                    string msg = String.Format(CultureInfo.InvariantCulture, "Could not determine path from {0}", DirectoryName);
                    throw new BuildException(msg, Location, e);
                }
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException();
                Log.WriteLine( LogPrefix + "Deleting directory {0}.", path);
                RecursiveDeleteDirectory(path);
            } else {
                // delete files in fileset
                if ( DeleteFileSet.DirectoryNames.Count == 0 )
                    Log.WriteLine(LogPrefix + "Deleting {0} files.", DeleteFileSet.FileNames.Count);
                else if ( DeleteFileSet.FileNames.Count == 0 )
                    Log.WriteLine(LogPrefix + "Deleting {0} directories.", DeleteFileSet.DirectoryNames.Count);
                else
                    Log.WriteLine(LogPrefix + "Deleting {0} files and {1} directories.", DeleteFileSet.FileNames.Count, DeleteFileSet.DirectoryNames.Count);

                foreach (string path in DeleteFileSet.FileNames) {
                    DeleteFile(path, Verbose);
                }
                foreach (string path in DeleteFileSet.DirectoryNames) {
                    if (Directory.Exists(path))
                        RecursiveDeleteDirectory(path);
                }
            }
        }

        void RecursiveDeleteDirectory(string path) {
            try {
                // First, recursively delete all directories in the directory
                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                    RecursiveDeleteDirectory(dir);

                // Next, delete all files in the directory
                string[] files = Directory.GetFiles(path);
                foreach (string file in files) {
                    try {
                        File.SetAttributes(file, FileAttributes.Normal);
                        Log.WriteLineIf(Verbose, LogPrefix + "Deleting file {0}.", file);
                        File.Delete(file);
                    }
                    catch (Exception e) {
                        string msg = String.Format(CultureInfo.InvariantCulture, "Cannot delete file {0}.", file);
                        if (FailOnError) {
                            throw new BuildException(msg, Location, e);
                        }
                        Log.WriteLineIf(Verbose, LogPrefix + msg);
                    }
                }

                // Finally, delete the directory
                File.SetAttributes(path, FileAttributes.Normal);
                Log.WriteLineIf(Verbose, LogPrefix + "Deleting directory {0}.", path);
                Directory.Delete(path);
            } catch (BuildException e) {
                throw e;
            } catch (Exception e) {
                string msg = String.Format(CultureInfo.InvariantCulture, "Cannot delete directory {0}.", path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, e);
                }
                Log.WriteLineIf(Verbose, LogPrefix + msg);
            }
        }

        void DeleteFile(string path, bool verbose) {
            try {
                FileInfo deleteInfo = new FileInfo( path );
                if (deleteInfo.Exists)  {
                    Log.WriteLineIf(verbose, LogPrefix + "Deleting file {0}.", path);
                    if ( deleteInfo.Attributes != FileAttributes.Normal ) {
                        File.SetAttributes( deleteInfo.FullName, FileAttributes.Normal );
                    }
                    File.Delete(path);
                } else {
                    throw new FileNotFoundException();
                }
            } catch (Exception e) {
                string msg = String.Format(CultureInfo.InvariantCulture, "Cannot delete file {0}.", path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, e);
                }
                Log.WriteLineIf(Verbose, LogPrefix + msg);
            }
        }
    }
}
