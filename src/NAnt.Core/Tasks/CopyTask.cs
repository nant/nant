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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Text;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Copies a file or fileset to a new file or directory.
    /// </summary>
    /// <remarks>
    ///   <para>Files are only copied if the source file is newer than the destination file, or if the destination file does not exist. However, you can explicitly overwrite files with the overwrite attribute.</para>
    ///   <para>Filesets are used to select files to copy. To use a fileset, the todir attribute must be set.</para>
    /// </remarks>
    /// <example>
    ///   <para>Copy a single file.</para>
    ///   <code>&lt;copy file="myfile.txt" tofile="mycopy.txt"/&gt;</code>
    ///   <para>Copy a set of files to a new directory.</para>
    ///   <code>
    /// <![CDATA[
    /// <copy todir="${build.dir}">
    ///     <fileset basedir="bin">
    ///         <includes name="*.dll"/>
    ///     </fileset>
    /// </copy>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("copy")]
    public class CopyTask : Task {

        string _sourceFile = null;
        string _toFile = null;
        string _toDirectory = null;
        bool _overwrite = false;
        //bool _includeEmptyDirs = false;
        //bool _preserveLastModified = false;
        FileSet _fileset = new FileSet();
        Hashtable _fileCopyMap = new Hashtable();

        /// <summary>The file to copy.</summary>
        [TaskAttribute("file")]
        public string SourceFile        { get { return _sourceFile; } set {_sourceFile = value; } }

        /// <summary>The file to copy to.</summary>
        [TaskAttribute("tofile")]
        public string ToFile            { get { return _toFile; } set {_toFile = value; } }

        /// <summary>The directory to copy to.</summary>
        [TaskAttribute("todir")]
        public string ToDirectory       { get { return _toDirectory; } set {_toDirectory = value; } }

        // /// <summary>Copy empty directories included with the nested fileset(s). Defaults to "true".</summary>
        //[TaskAttribute("includeEmptyDirs")]
        //[BooleanValidator()]
        //public bool IncludeEmptyDirs    { get { return (_includeEmptyDirs); } set {_includeEmptyDirs = value; } }

        /// <summary>Overwrite existing files even if the destination files are newer. Defaults to "false".</summary>
        [TaskAttribute("overwrite")]
        [BooleanValidator()]
        public bool Overwrite           { get { return (_overwrite); } set {_overwrite = value; } }

        // /// <summary>Give the copied files the same last modified time as the original files. Defaults to "false".</summary>
        // [TaskAttribute("preserveLastModified")]
        // [BooleanValidator()]
        // public bool PreserveLastModified{ get { return (_preserveLastModified); } set {_preserveLastModified = value; } }

        /// <summary>Filesets are used to select files to copy. To use a fileset, the todir attribute must be set.</summary>
        [FileSet("fileset")]
        public FileSet CopyFileSet      { get { return _fileset; } }

        protected Hashtable FileCopyMap {
            get { return _fileCopyMap; }
        }

        /// <summary>
        /// Actually does the file (and possibly empty directory) copies.
        /// </summary>
        protected virtual void DoFileOperations() {
            int fileCount = FileCopyMap.Keys.Count;
            if (fileCount > 0 || Verbose) {
                if (ToDirectory != null) {
                    Log.WriteLine(LogPrefix + "Copying {0} file{1} to {2}", fileCount, ( fileCount != 1 ) ? "s" : "", Project.GetFullPath(ToDirectory));
                } else {
                    Log.WriteLine(LogPrefix + "Copying {0} file{1}", fileCount, ( fileCount != 1 ) ? "s" : "" );
                }

                // loop thru our file list
                foreach (string sourcePath in FileCopyMap.Keys) {
                    string dstPath = (string)FileCopyMap[sourcePath];
                    if (sourcePath == dstPath) {
                        Log.WriteLineIf(Verbose, LogPrefix + "Skipping self-copy of {0}" + sourcePath);
                        continue;
                    }

                    try {
                        Log.WriteLineIf(Verbose, LogPrefix + "Copying {0} to {1}", sourcePath, dstPath);

                        // create directory if not present
                        string dstDirectory = Path.GetDirectoryName(dstPath);
                        if (!Directory.Exists(dstDirectory)) {
                            Directory.CreateDirectory(dstDirectory);
                            Log.WriteLineIf(Verbose, LogPrefix + "Created directory {0}", dstDirectory);
                        }

                        File.Copy(sourcePath, dstPath, true);
                    } catch (Exception e) {
                        string msg = String.Format(CultureInfo.InvariantCulture, "Cannot copy {0} to {1}", sourcePath, dstPath);
                        throw new BuildException(msg, Location, e);
                    }
                }
            }

            // TODO: handle empty directories in the fileset, refer to includeEmptyDirs attribute at
            // http://jakarta.apache.org/ant/manual/CoreTasks/copy.html
        }

        /// <summary>
        /// Executes the Copy task.
        /// </summary>
        /// <exception cref="BuildException">A file that has to be copied does not exist or could not be copied.</exception>
        protected override void ExecuteTask() {
            // NOTE: when working with file and directory names its useful to 
            // use the FileInfo an DirectoryInfo classes to normalize paths like:
            // c:\work\nant\extras\buildserver\..\..\..\bin

            if (SourceFile != null) {
                // Copy single file.

                FileInfo srcInfo = new FileInfo(Project.GetFullPath(SourceFile));
                if (srcInfo.Exists) {
                    FileInfo dstInfo = null;
                    if (ToFile != null) {
                        dstInfo = new FileInfo(Project.GetFullPath(ToFile));
                    } else {
                        string dstDirectoryPath = Project.GetFullPath(ToDirectory);
                        string dstFilePath = Path.Combine(dstDirectoryPath, srcInfo.Name);
                        dstInfo = new FileInfo(dstFilePath);

                    }

                    // do the outdated check
                    bool outdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

                    if (Overwrite || outdated) {
                        // add to a copy map of absolute verified paths
                        FileCopyMap.Add(srcInfo.FullName, dstInfo.FullName);
                        if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) 
                            File.SetAttributes( dstInfo.FullName, FileAttributes.Normal );
                    }
                } else {
                    string msg = String.Format(CultureInfo.InvariantCulture, "Could not find file {0} to copy.", srcInfo.FullName);
                    throw new BuildException(msg, Location);
                }
            } else {
                // Copy file set contents.

                // get the complete path of the base directory of the fileset, ie, c:\work\nant\src
                DirectoryInfo srcBaseInfo = new DirectoryInfo(CopyFileSet.BaseDirectory);
                
                DirectoryInfo dstBaseInfo = new DirectoryInfo(Project.GetFullPath(ToDirectory));

                // if source file not specified use fileset
                foreach (string pathname in CopyFileSet.FileNames) {
                    FileInfo srcInfo = new FileInfo(pathname);
                    if (srcInfo.Exists) {
                        // Gets the relative path and file info from the full source filepath
                        // pathname = C:\f2\f3\file1, srcBaseInfo=C:\f2, then dstRelFilePath=f3\file1`
                        string dstRelFilePath = "";
                        if (srcInfo.FullName.IndexOf( "", 0) != -1 ) {
                            dstRelFilePath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
                        } else {
                            dstRelFilePath = srcInfo.Name;
                        }
                        
                        if( dstRelFilePath[0] == Path.DirectorySeparatorChar ) {
                            dstRelFilePath = dstRelFilePath.Substring(1);
                        }
                        
                        // The full filepath to copy to.
                        string dstFilePath = Path.Combine(dstBaseInfo.FullName, dstRelFilePath);
                        
                        // do the outdated check
                        FileInfo dstInfo = new FileInfo(dstFilePath);
                        bool outdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

                        if (Overwrite || outdated) {
                            FileCopyMap.Add(srcInfo.FullName, dstFilePath);
                            if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) 
                                File.SetAttributes( dstInfo.FullName, FileAttributes.Normal );
                        }
                    } else {
                        string msg = String.Format(CultureInfo.InvariantCulture, "Could not find file {0} to copy.", srcInfo.FullName);
                        throw new BuildException(msg, Location);
                    }
                }

                // Create any specified directories that weren't created during the copy (ie: empty directories)
                foreach (string pathname in CopyFileSet.DirectoryNames) {
                    DirectoryInfo srcInfo = new DirectoryInfo(pathname);
                    string dstRelPath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
                    if(dstRelPath.Length > 0 && dstRelPath[0] == Path.DirectorySeparatorChar ) {
                        dstRelPath = dstRelPath.Substring(1);
                    }

                    // The full filepath to copy to.
                    string dstPath = Path.Combine(dstBaseInfo.FullName, dstRelPath);
                    if (!Directory.Exists(dstPath)) {
                        Log.WriteLineIf(Verbose, LogPrefix + "Created directory {0}", dstPath);
                        Directory.CreateDirectory(dstPath);
                    }
                }
            }

            // do all the actual copy operations now...
            DoFileOperations();
        }
    }
}
