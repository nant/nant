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

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Copies a file or set of files to a new file or directory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Files are only copied if the source file is newer than the destination 
    ///   file, or if the destination file does not exist.  However, you can 
    ///   explicitly overwrite files with the <see cref="Overwrite" /> attribute.
    ///   </para>
    ///   <para>
    ///   A <see cref="FileSet" /> can be used to select files to copy. To use 
    ///   a <see cref="FileSet" />, the <see cref="ToDirectory" /> attribute 
    ///   must be set.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Copy a single file.</para>
    ///   <code>
    ///     <![CDATA[
    /// <copy file="myfile.txt" tofile="mycopy.txt" />
    ///     ]]>
    ///   </code>
    ///   <para>Copy a set of files to a new directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <copy todir="${build.dir}">
    ///     <fileset basedir="bin">
    ///         <include name="*.dll" />
    ///     </fileset>
    /// </copy>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("copy")]
    public class CopyTask : Task {
        #region Private Instance Fields

        private FileInfo _sourceFile;
        private FileInfo _toFile;
        private DirectoryInfo _toDirectory;
        private bool _overwrite;
        private bool _flatten;
        private FileSet _fileset = new FileSet();
        private Hashtable _fileCopyMap = new Hashtable();
        private bool _includeEmptyDirs = true;
        private string _encodingName;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to copy.
        /// </summary>
        [TaskAttribute("file")]
        public virtual FileInfo SourceFile {
            get { return _sourceFile; }
            set { _sourceFile = value; }
        }

        /// <summary>
        /// The file to copy to.
        /// </summary>
        [TaskAttribute("tofile")]
        public virtual FileInfo ToFile {
            get { return _toFile; }
            set { _toFile = value; }
        }

        /// <summary>
        /// The directory to copy to.
        /// </summary>
        [TaskAttribute("todir")]
        public virtual DirectoryInfo ToDirectory {
            get { return _toDirectory; }
            set { _toDirectory = value; }
        }

        /// <summary>
        /// Overwrite existing files even if the destination files are newer. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("overwrite")]
        [BooleanValidator()]
        public bool Overwrite {
            get { return _overwrite; }
            set { _overwrite = value; }
        }
        
        /// <summary>
        /// Ignore directory structure of source directory, copy all files into 
        /// a single directory, specified by the <see cref="ToDirectory" /> 
        /// attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("flatten")]
        [BooleanValidator()]
        public virtual bool Flatten {
            get { return _flatten; }
            set { _flatten = value; }
        }

        /// <summary>
        /// Copy any empty directories included in the <see cref="FileSet" />. 
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("includeemptydirs")]
        [BooleanValidator()]
        public bool IncludeEmptyDirs {
            get { return _includeEmptyDirs; }
            set { _includeEmptyDirs = value; }
        }

        /// <summary>
        /// Used to select the files to copy. To use a <see cref="FileSet" />, 
        /// the <see cref="ToDirectory" /> attribute must be set.
        /// </summary>
        [BuildElement("fileset")]
        public virtual FileSet CopyFileSet {
            get { return _fileset; }
            set { _fileset = value; }
        }

        /// <summary>
        /// The encoding to assume when filter-copying the files.
        /// </summary>
        [TaskAttribute("encoding")]
        public string EncodingName {
            get { return _encodingName; }
            set { _encodingName = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the encoding that will be used when filter-copying the files.
        /// </summary>
        protected Encoding Encoding {
            get { 
                if (EncodingName != null) {
                    return System.Text.Encoding.GetEncoding(EncodingName);
                }

                return null;
            }
        }

        protected Hashtable FileCopyMap {
            get { return _fileCopyMap; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Checks whether the given encoding is supported on the current 
        /// platform.
        /// </summary>
        /// <param name="taskNode">The <see cref="XmlNode" /> used to initialize the task.</param>
        protected override void InitializeTask(XmlNode taskNode) {
            if (EncodingName != null) {
                try {
                    System.Text.Encoding.GetEncoding(EncodingName);
                } catch (ArgumentException) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "{0} is not a valid encoding.",
                        EncodingName), Location);
                } catch (NotSupportedException) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "{0} encoding is not supported on the current platform.",
                        EncodingName), Location);
                }
            }

            if (Flatten && ToDirectory == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "'flatten' attribute requires that 'todir' has been set."), 
                    Location);
            }

            if (ToDirectory == null && CopyFileSet != null && CopyFileSet.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "The 'todir' should be set when using the <fileset> element"
                    + " to specify the list of files to be copied."), Location);
            }

            if (SourceFile != null && CopyFileSet != null && CopyFileSet.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "The 'file' attribute and the <fileset> element" 
                    + " cannot be combined."), Location);
            }

            if (ToFile == null && ToDirectory == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Either the 'tofile' or 'todir' attribute should be set."), 
                    Location);
            }

            if (ToFile != null && ToDirectory != null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "The 'tofile' or 'todir' attribute cannot both be set."), 
                    Location);
            }
        }

        /// <summary>
        /// Executes the Copy task.
        /// </summary>
        /// <exception cref="BuildException">A file that has to be copied does not exist or could not be copied.</exception>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (CopyFileSet.BaseDirectory == null) {
                CopyFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // NOTE: when working with file and directory names its useful to 
            // use the FileInfo an DirectoryInfo classes to normalize paths like:
            // c:\work\nant\extras\buildserver\..\..\..\bin
            
            // copy a single file.
            if (SourceFile != null) {
                if (SourceFile.Exists) {
                    FileInfo dstInfo = null;
                    if (ToFile != null) {
                        dstInfo = ToFile;
                    } else {
                        string dstFilePath = Path.Combine(ToDirectory.FullName, 
                            SourceFile.Name);
                        dstInfo = new FileInfo(dstFilePath);
                    }

                    // do the outdated check
                    bool outdated = (!dstInfo.Exists) || (SourceFile.LastWriteTime > dstInfo.LastWriteTime);

                    if (Overwrite || outdated) {
                        // add to a copy map of absolute verified paths
                        FileCopyMap.Add(SourceFile.FullName, dstInfo.FullName);
                        if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) {
                            File.SetAttributes(dstInfo.FullName, FileAttributes.Normal);
                        }
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Could not find file '{0}' to copy.", SourceFile.FullName), 
                        Location);
                }
            } else { // copy file set contents.
                // get the complete path of the base directory of the fileset, ie, c:\work\nant\src
                DirectoryInfo srcBaseInfo = CopyFileSet.BaseDirectory;
                
                // if source file not specified use fileset
                foreach (string pathname in CopyFileSet.FileNames) {
                    FileInfo srcInfo = new FileInfo(pathname);
                    if (srcInfo.Exists) {
                        // Gets the relative path and file info from the full source filepath
                        // pathname = C:\f2\f3\file1, srcBaseInfo=C:\f2, then dstRelFilePath=f3\file1`
                        string dstRelFilePath = "";
                        if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName, 0) != -1) {
                            dstRelFilePath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
                        } else {
                            dstRelFilePath = srcInfo.Name;
                        }
                        
                        if (dstRelFilePath[0] == Path.DirectorySeparatorChar) {
                            dstRelFilePath = dstRelFilePath.Substring(1);
                        }
                        
                        // The full filepath to copy to.
                        string dstFilePath = Path.Combine(ToDirectory.FullName, dstRelFilePath);
                        
                        // do the outdated check
                        FileInfo dstInfo = new FileInfo(dstFilePath);
                        bool outdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

                        if (Overwrite || outdated) {
                            FileCopyMap.Add(srcInfo.FullName, dstFilePath);
                            if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal) {
                                File.SetAttributes(dstInfo.FullName, FileAttributes.Normal);
                            }
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Could not find file '{0}' to copy.", srcInfo.FullName), 
                            Location);
                    }
                }
                
                if (IncludeEmptyDirs && !Flatten) {
                    // create any specified directories that weren't created during the copy (ie: empty directories)
                    foreach (string pathname in CopyFileSet.DirectoryNames) {
                        DirectoryInfo srcInfo = new DirectoryInfo(pathname);
                        // skip directory if not relative to base dir of fileset
                        if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName) == -1) {
                            continue;
                        }
                        string dstRelPath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
                        if (dstRelPath.Length > 0 && dstRelPath[0] == Path.DirectorySeparatorChar) {
                            dstRelPath = dstRelPath.Substring(1);
                        }

                        // The full filepath to copy to.
                        string destinationDirectory = Path.Combine(ToDirectory.FullName, dstRelPath);
                        if (!Directory.Exists(destinationDirectory)) {
                            Log(Level.Verbose, LogPrefix + "Created directory '{0}'.", destinationDirectory);
                            Directory.CreateDirectory(destinationDirectory);
                        }
                    }
                }
            }

            // do all the actual copy operations now
            DoFileOperations();
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Actually does the file copies.
        /// </summary>
        protected virtual void DoFileOperations() {
            int fileCount = FileCopyMap.Keys.Count;
            if (fileCount > 0 || Verbose) {
                if (ToFile != null) {
                    Log(Level.Info, LogPrefix + "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToFile);
                } else {
                    Log(Level.Info, LogPrefix + "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToDirectory);
                }

                // loop thru our file list
                foreach (string sourceFile in FileCopyMap.Keys) {
                    string destinationFile = (string) FileCopyMap[sourceFile];
                    if (Flatten) {
                        destinationFile = Path.Combine(ToDirectory.FullName, 
                            Path.GetFileName(destinationFile));
                    }
                    if (sourceFile == destinationFile) {
                        Log(Level.Verbose, LogPrefix + "Skipping self-copy of '{0}'.", sourceFile);
                        continue;
                    }

                    try {
                        Log(Level.Verbose, LogPrefix + "Copying '{0}' to '{1}'.", sourceFile, destinationFile);
                        
                        // create directory if not present
                        string destinationDirectory = Path.GetDirectoryName(destinationFile);
                        if (!Directory.Exists(destinationDirectory)) {
                            Directory.CreateDirectory(destinationDirectory);
                            Log(Level.Verbose, LogPrefix + "Created directory '{0}'.", destinationDirectory);
                        }

                        // actually copy the file
                        File.Copy(sourceFile, destinationFile, true);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot copy '{0}' to '{1}'.", sourceFile, destinationFile), 
                            Location, ex);
                    }
                }
            }
        }

        #endregion Protected Instance Methods
    }
}
