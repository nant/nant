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
                    string msg = String.Format("Could not determine path from {0}", FileName);
                    throw new BuildException(msg, Location, e);
                }
                DeleteFile(path, true);

            } else if (DirectoryName != null) {
                // try to delete specified directory
                string path = null;
                try {
                    path = Project.GetFullPath(DirectoryName);
                } catch (Exception e) {
                    string msg = String.Format("Could not determine path from {0}", DirectoryName);
                    throw new BuildException(msg, Location, e);
                }
                DeleteDirectory(path);

            } else {
                // delete files in fileset
                Log.WriteLine(LogPrefix + "Deleting {0} files", DeleteFileSet.FileNames.Count);
                foreach (string path in DeleteFileSet.FileNames) {
                    DeleteFile(path, Verbose);
                }
            }
        }

        void DeleteDirectory(string path) {
            try {
                if (Directory.Exists(path)) {
                    // TODO: remove this once this task is fully tested and NAnt is at 1.0
                    if (path.Length <= 10) {
                        throw new NotImplementedException("Path is too close to root to delete.");
                    }

                    Log.WriteLine(LogPrefix + "Deleting directory {0}", path);
                    Directory.Delete(path, true);
                } else {
                    throw new DirectoryNotFoundException();
                }
            } catch (Exception e) {
                if (FailOnError) {
                    string msg = String.Format("Cannot delete directory {0}", path);
                    throw new BuildException(msg, Location, e);
                }
            }
        }

        void DeleteFile(string path, bool verbose) {
            try {
                if (File.Exists(path)) {
                    Log.WriteLineIf(verbose, LogPrefix + "Deleting file {0}", path);
                    File.Delete(path);
                } else {
                    throw new FileNotFoundException();
                }
            } catch (Exception e) {
                if (FailOnError) {
                    string msg = String.Format("Cannot delete file {0}", path);
                    throw new BuildException(msg, Location, e);
                }
            }
        }
    }
}
