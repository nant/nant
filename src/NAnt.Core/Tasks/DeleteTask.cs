// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Globalization;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Deletes a file, fileset or directory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Deletes either a single file, all files in a specified directory and 
    ///   its sub-directories, or a set of files specified by one or more filesets.
    ///   </para>
    ///   <para>
    ///   If the <see cref="File" /> or <see cref="Directory" /> attribute is 
    ///   set then the fileset contents will be ignored. To delete the files 
    ///   in the fileset ommit the <see cref="File" /> and <see cref="Directory" />
    ///   attributes in the <c>&lt;delete&gt;</c> element.
    ///   </para>
    ///   <para>
    ///   If the specified file or directory does not exist, no error is 
    ///   reported.
    ///   </para>
    ///   <note>
    ///   Read-only files cannot be deleted.  Use the <see cref="AttribTask" /> 
    ///   first to remove the read-only attribute.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>Delete a single file.</para>
    ///   <code>
    ///     <![CDATA[
    /// <delete file="myfile.txt" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Delete a directory and the contents within. If the directory does not 
    ///   exist, no error is reported.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <delete dir="${build.dir}" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Delete a set of files.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <delete>
    ///     <fileset>
    ///         <include name="${basename}-??.exe" />
    ///         <include name="${basename}-??.pdb" />
    ///     </fileset>
    /// </delete>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("delete")]
    public class DeleteTask : Task {
        #region Private Instance Fields

        private FileInfo _file;
        private DirectoryInfo _dir;
        private FileSet _fileset = new FileSet();
        private bool _includeEmptyDirs = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to delete.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }
        
        /// <summary>
        /// The directory to delete.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { return _dir; }
            set { _dir = value; }
        }

        /// <summary>
        /// Remove any empty directories included in the <see cref="FileSet" />. 
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("includeemptydirs")]
        [BooleanValidator()]
        public bool IncludeEmptyDirs {
            get { return _includeEmptyDirs; }
            set { _includeEmptyDirs = value; }
        }

        /// <summary>
        /// All the files in the file set will be deleted.
        /// </summary>
        [BuildElement("fileset")]
        public FileSet DeleteFileSet {
            get { return _fileset; }
            set { _fileset = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Controls whether to show the name of each deleted file or directory.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("verbose")]
        [BooleanValidator()]
        public override bool Verbose {
            get { return base.Verbose; }
            set { base.Verbose = value; }
        }

        /// <summary>
        /// Ensures the supplied attributes are valid.
        /// </summary>
        protected override void Initialize() {
            // limit task to deleting either a file, directory
            if (File != null && Directory != null) {
                throw new BuildException("Cannot specify both 'file' and 'dir'" 
                    + " attribute in the same <delete> task.", Location);
            }

            // limit task to deleting either a file/directory or fileset
            if ((File != null || Directory != null) && DeleteFileSet.Includes.Count != 0) {
                throw new BuildException("Cannot specify both 'file' or 'dir'" 
                    + " attribute and use <fileset> in the same <delete> task.", 
                    Location);
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">If a directory cannot be deleted.</exception>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (DeleteFileSet.BaseDirectory == null) {
                DeleteFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (File != null) { // delete a single file
                // delete the file in verbose mode
                DeleteFile(File.FullName, true);
            } else if (Directory != null) { // delete the directory
                // explicitly check if directory exist here (while the same
                // check is performed in RecursiveDeleteDirectory), to avoid
                // output of Info message below
                if (!Directory.Exists) {
                    return;
                }
                // log message here if we're not in verbose mode, otherwise the 
                // RecursiveDeleteDirectory method will output verbose message 
                // for each directory that is removed
                if (!Verbose) {
                    Log(Level.Info, "Deleting directory '{0}'.", Directory.FullName);
                }
                RecursiveDeleteDirectory(Directory.FullName);
            } else { // delete files or directories in fileset
                if (DeleteFileSet.FileNames.Count > 0) {
                    Log(Level.Info, "Deleting {0} files.", DeleteFileSet.FileNames.Count);
                    foreach (string path in DeleteFileSet.FileNames) {
                        DeleteFile(path, Verbose);
                    }
                }

                if (DeleteFileSet.DirectoryNames.Count > 0 && IncludeEmptyDirs) {
                    int dirCount = 0;
                    foreach (string path in DeleteFileSet.DirectoryNames) {
                        string[] entries = System.IO.Directory.GetFileSystemEntries(path);
                        if (entries == null || entries.Length == 0) {
                            try {
                                DeleteDirectory(path);
                                dirCount++;
                            } catch (Exception ex) {
                                string msg = string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA1113"), path);
                                if (FailOnError) {
                                    throw new BuildException(msg, Location, ex);
                                }
                                Log(Level.Warning, "{0} {1}", msg, ex.Message);
                            }
                        }
                    }

                    if (dirCount > 0) {
                        Log(Level.Info, "Deleted {0} directories.", dirCount);
                    }
                }
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private void RecursiveDeleteDirectory(string path) {
            try {
                // skip the directory if it doesn't exist
                if (!System.IO.Directory.Exists(path)) {
                    return;
                }

                // first, recursively delete all directories in the directory
                string[] dirs = System.IO.Directory.GetDirectories(path);
                foreach (string dir in dirs) {
                    RecursiveDeleteDirectory(dir);
                }

                // next, delete all files in the directory
                string[] files = System.IO.Directory.GetFiles(path);
                foreach (string file in files) {
                    try {
                        System.IO.File.SetAttributes(file, FileAttributes.Normal);
                        Log(Level.Verbose, "Deleting file '{0}'.", file);
                        System.IO.File.Delete(file);
                    } catch (Exception ex) {
                        string msg = string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1114"), file);
                        if (FailOnError) {
                            throw new BuildException(msg, Location, ex);
                        }
                        Log(Level.Verbose, "{0} {1}", msg, ex.Message);
                    }
                }
                DeleteDirectory(path);
            } catch (BuildException ex) {
                throw ex;
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1113"), path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, ex);
                }
                Log(Level.Warning, "{0} {1}", msg, ex.Message);
            }
        }

        private void DeleteFile(string path, bool verbose) {
            try {
                FileInfo deleteInfo = new FileInfo(path);
                if (!deleteInfo.Exists) {
                    return;
                }
                if (verbose) {
                    Log(Level.Info, "Deleting file {0}.", path);
                }
                if (deleteInfo.Attributes != FileAttributes.Normal) {
                    System.IO.File.SetAttributes(deleteInfo.FullName, 
                        FileAttributes.Normal);
                }
                System.IO.File.Delete(path);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1114"), path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, ex);
                }
                Log(Level.Warning, "{0} {1}", msg, ex.Message);
            }
        }

        private void DeleteDirectory(string path) {
            // ensure path is not read-only
            System.IO.File.SetAttributes(path, FileAttributes.Normal);
            // write output to build log
            Log(Level.Verbose, "Deleting directory '{0}'.", path);
            // finally, delete the directory
            System.IO.Directory.Delete(path);
        }

        #endregion Private Instance Methods
    }
}
