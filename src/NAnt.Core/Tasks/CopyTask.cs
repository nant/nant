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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (imaclean@gmail.com)
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Core.Filters;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Copies a file, a directory, or set of files to a new file or directory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Files are only copied if the source file is newer than the destination 
    ///   file, or if the destination file does not exist.  However, you can 
    ///   explicitly overwrite files with the <see cref="Overwrite" /> attribute.
    ///   </para>
    ///   <para>
    ///   When a <see cref="FileSet" /> is used to select files to copy, the 
    ///   <see cref="ToDirectory" /> attribute must be set. Files that are 
    ///   located under the base directory of the <see cref="FileSet" /> will
    ///   be copied to a directory under the destination directory matching the
    ///   path relative to the base directory of the <see cref="FileSet" />,
    ///   unless the <see cref="Flatten" /> attribute is set to
    ///   <see langword="true" />.
    ///   </para>
    ///   <para>
    ///   Files that are not located under the the base directory of the
    ///   <see cref="FileSet" /> will be copied directly under to the destination 
    ///   directory, regardless of the value of the <see cref="Flatten" />
    ///   attribute.
    ///   </para>
    ///   <h4>Encoding</h4>
    ///   <para>
    ///   Unless an encoding is specified, the encoding associated with the 
    ///   system's current ANSI code page is used.
    ///   </para>
    ///   <para>
    ///   An UTF-8, little-endian Unicode, and big-endian Unicode encoded text 
    ///   file is automatically recognized, if the file starts with the 
    ///   appropriate byte order marks.
    ///   </para>
    ///   <note>
    ///   If you employ filters in your copy operation, you should limit the copy 
    ///   to text files. Binary files will be corrupted by the copy operation.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Copy a single file while changing its encoding from "latin1" to 
    ///   "utf-8".
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <copy 
    ///     file="myfile.txt"
    ///     tofile="mycopy.txt"
    ///     inputencoding="latin1"
    ///     outputencoding="utf-8" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
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
    /// <example>
    ///   <para>
    ///   Copy a set of files to a directory, replacing <c>@TITLE@</c> with 
    ///   "Foo Bar" in all files.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <copy todir="../backup/dir">
    ///     <fileset basedir="src_dir">
    ///         <include name="**/*" />
    ///     </fileset>
    ///     <filterchain>
    ///         <replacetokens>
    ///             <token key="TITLE" value="Foo Bar" />
    ///         </replacetokens>
    ///     </filterchain>
    /// </copy>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Copy an entire directory and its contents.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <copy tofile="target/dir">
    ///   <fileset basedir="source/dir"/>
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
        private FileOperationMap _operationMap;
        private bool _includeEmptyDirs = true;
        private FilterChain _filters;
        private Encoding _inputEncoding;
        private Encoding _outputEncoding;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initialize new instance of the <see cref="CopyTask" />.
        /// </summary>
        public CopyTask() {
            if (PlatformHelper.IsUnix) {
                _operationMap = new FileOperationMap();
            } else {
                _operationMap = new FileOperationMap(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        #endregion Public Instance Constructors

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
        /// Chain of filters used to alter the file's content as it is copied.
        /// </summary>
        [BuildElement("filterchain")]
        public virtual FilterChain Filters {
            get { return _filters; }
            set { _filters = value; }
        }

        /// <summary>
        /// The encoding to use when reading files. The default is the system's
        /// current ANSI code page.
        /// </summary>
        [TaskAttribute("inputencoding")]
        public Encoding InputEncoding {
            get { return _inputEncoding; }
            set { _inputEncoding = value; }
        }

        /// <summary>
        /// The encoding to use when writing the files. The default is
        /// the encoding of the input file.
        /// </summary>
        [TaskAttribute("outputencoding")]
        public Encoding OutputEncoding {
            get { return _outputEncoding; }
            set { _outputEncoding = value; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the operation map containing all the files/directories to
        /// perform file operations on.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The type of class for this object inherits from
        ///   <see cref="System.Collections.ObjectModel.KeyedCollection"/> and
        ///   is structured so that the key of this collection contains the
        ///   full path of the target file/location while the value contains
        ///   the <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> object
        ///   with the operation details.
        ///   </para>
        ///   <para>
        ///   On Windows, the <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>
        ///   is case-insensitive.
        ///   </para>
        /// </remarks>
        protected FileOperationMap OperationMap
        {
            get { return _operationMap; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Checks whether the task is initialized with valid attributes.
        /// </summary>
        protected override void Initialize() {
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
                    "The 'tofile' and 'todir' attribute cannot both be set."), 
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

            // Clear previous copied files
            _operationMap.Clear();

            // copy a single file object.
            if (SourceFile != null)
            {
                // Setup the necessary local vars
                FileOperation operation;
                FileSystemInfo srcInfo;
                FileSystemInfo dstInfo;

                // If the full path in the SourceFile is an actual file,
                // assign the SourceFile object as is to srcInfo.
                if (SourceFile.Exists)
                {
                    srcInfo = SourceFile;
                }
                // If the full path in the SoureFile is a directory,
                // assign the SourceFile object as a DirectoryInfo object to srcInfo.
                else if (Directory.Exists(SourceFile.FullName))
                {
                    srcInfo = new DirectoryInfo(SourceFile.FullName);
                }
                // Otherwise, throw an error.
                else
                {
                    throw CreateSourceFileNotFoundException(SourceFile.FullName);
                }

                // If the ToFile object is not null, assign it to dstInfo;
                // otherwise, assign the ToDirectory object to dstInfo.
                if (ToFile != null)
                {
                    dstInfo = ToFile;
                }
                else
                {
                    dstInfo = ToDirectory;
                }

                // Initialize the operation var with the srcInfo and dstInfo
                // objects that were assigned above.
                operation = new FileOperation(srcInfo, dstInfo);

                // If the user specified "Overwrite" or the target file/path
                // is considered outdated, ensure that the target file/path is
                // normalized before adding to the operation map.
                if (Overwrite || operation.Outdated)
                {
                    operation.NormalizeTargetAttributes();
                    _operationMap.Add(operation);
                }
            } else {
                // copy file set contents.
                // get the complete path of the base directory of the fileset, ie, c:\work\nant\src
                DirectoryInfo srcBaseInfo = CopyFileSet.BaseDirectory;

                // If no includes were specified, add all files and subdirectories
                // from the fileset's base directory to the fileset.
                if (CopyFileSet.Includes.Count == 0)
                {
                    CopyFileSet.Includes.Add("**/*");
                }

                // if source file not specified use fileset
                foreach (string pathname in CopyFileSet.FileNames) {
                    FileInfo srcInfo = new FileInfo(pathname);
                    if (srcInfo.Exists) {
                        // will holds the full path to the destination file
                        string dstFilePath;

                        if (Flatten) {
                            dstFilePath = Path.Combine(ToDirectory.FullName, 
                                srcInfo.Name);
                        } else {
                            // Gets the relative path and file info from the full 
                            // source filepath
                            // pathname = C:\f2\f3\file1, srcBaseInfo=C:\f2, then 
                            // dstRelFilePath=f3\file1
                            string dstRelFilePath = "";
                            if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName, 0) != -1) {
                                dstRelFilePath = srcInfo.FullName.Substring(
                                    srcBaseInfo.FullName.Length);
                            } else {
                                dstRelFilePath = srcInfo.Name;
                            }
                        
                            if (dstRelFilePath[0] == Path.DirectorySeparatorChar) {
                                dstRelFilePath = dstRelFilePath.Substring(1);
                            }
                        
                            // The full filepath to copy to.
                            dstFilePath = Path.Combine(ToDirectory.FullName, 
                                dstRelFilePath);
                        }
                        
                        // Setup both the destination info and file operation vars.
                        FileInfo dstInfo = new FileInfo(dstFilePath);
                        FileOperation operation = new FileOperation(srcInfo, dstInfo);

                        // If the user specified "Overwrite" or the target file/path
                        // is considered outdated, proceed to add to operation map.
                        if (Overwrite || operation.Outdated)
                        {

                            // if multiple source files are selected to be copied 
                            // to the same destination file, then only the last
                            // updated source should actually be copied
                            if (_operationMap.ContainsKey(dstInfo.FullName))
                            {
                                _operationMap[dstInfo.FullName].UpdateSource(srcInfo);
                            }
                            else
                            {
                                // ensure that the target file/path is normalized
                                // before adding to the operation map.
                                operation.NormalizeTargetAttributes();
                                _operationMap.Add(operation);
                            }
                        }
                    } else {
                        throw CreateSourceFileNotFoundException (srcInfo.FullName);
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
                            try {
                                Directory.CreateDirectory(destinationDirectory);
                            } catch (Exception ex) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Failed to create directory '{0}'.", destinationDirectory),
                                 Location, ex);
                            }
                            Log(Level.Verbose, "Created directory '{0}'.", destinationDirectory);
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
            int fileCount = OperationMap.Count;
            if (fileCount > 0 || Verbose) {
                if (ToFile != null) {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.",
                        fileCount, (fileCount != 1) ? "s" : "", ToFile);
                } else {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.",
                        fileCount, (fileCount != 1) ? "s" : "", ToDirectory);
                }

                // loop thru our file list
                for (int i = 0; i < OperationMap.Count; i++)
                {
                    // Setup a temporary var to hold the current file operation
                    // details.
                    FileOperation currentOperation = OperationMap[i];
                    if (currentOperation.SourceEqualsTarget())
                    {
                        Log(Level.Verbose, "Skipping self-copy of '{0}'.",
                            currentOperation.Source);
                        continue;
                    }

                    try
                    {
                        Log(Level.Verbose, "Copying {0}.", currentOperation.ToString());

                        switch (currentOperation.OperationType)
                        {
                            case OperationType.FileToFile:
                                // create directory if not present
                                string destinationDirectory =
                                    Path.GetDirectoryName(currentOperation.Target);
                                if (!Directory.Exists(destinationDirectory))
                                {
                                    Directory.CreateDirectory(destinationDirectory);
                                    Log(Level.Verbose, "Created directory '{0}'.",
                                        destinationDirectory);
                                }

                                // Ensure the target file is removed before
                                // attempting to copy.
                                if (File.Exists(currentOperation.Target))
                                {
                                    File.Delete(currentOperation.Target);
                                }
        
                                // copy the file with filters
                                FileUtils.CopyFile(currentOperation.Source,
                                    currentOperation.Target, Filters,
                                    InputEncoding, OutputEncoding);
                                break;
                            case OperationType.FileToDirectory:
                                // Setup a local var that combines the directory
                                // of the target path with the source file name.
                                string targetFile = Path.Combine(currentOperation.Target,
                                    Path.GetFileName(currentOperation.Source));
                                // create directory if not present
                                if (!Directory.Exists(currentOperation.Target))
                                {
                                    Directory.CreateDirectory(currentOperation.Target);
                                    Log(Level.Verbose, "Created directory '{0}'.",
                                        currentOperation.Target);
                                }

                                // Ensure the target file is removed before
                                // attempting to copy.
                                if (File.Exists(targetFile))
                                {
                                    File.Delete(targetFile);
                                }
        
                                // copy the file with filters
                                FileUtils.CopyFile(currentOperation.Source,
                                    targetFile, Filters, InputEncoding, OutputEncoding);
                                break;
                            case OperationType.DirectoryToDirectory:
                                // Throw a build exception if the target directory
                                // already exists.
                                if (Directory.Exists(currentOperation.Target))
                                {
                                    throw new BuildException(
                                        string.Format(CultureInfo.InvariantCulture,
                                        "Failed to copy {0}.  Directory '{1}' already exists.",
                                        currentOperation.ToString(),
                                        currentOperation.Target));
                                }
                                
                                // Copy over the entire directory with filters
                                FileUtils.CopyDirectory(currentOperation.Source,
                                    currentOperation.Target, Filters, InputEncoding,
                                    OutputEncoding);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot copy {0}.", currentOperation.ToString()),
                            Location, ex);
                    }
                }
            }
        }

        protected virtual BuildException CreateSourceFileNotFoundException (string sourceFile) {
            return new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Could not find file '{0}' to copy.", sourceFile),
                Location);
        }

        #endregion Protected Instance Methods

        /// <summary>
        /// Provides methods and properties to properly manage file operations for
        /// NAnt file system based tasks (such as CopyTask and MoveTask).
        /// </summary>
        protected class FileOperation
        {
            #region Private Instance Fields

            private FileSystemInfo _source;
            private FileSystemInfo _target;
            private StringComparer _comparer;

            #endregion Private Instance Fields

            #region Public Constructors

            /// <summary>
            /// Initializes a new instance of the
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> class with the
            /// source and target locations specified.
            /// </summary>
            /// <param name="source">
            /// A <see cref="FileSystemInfo"/> object representing the file/location
            /// where the file operation will start.
            /// </param>
            /// <param name="target">
            /// A <see cref="FileSystemInfo"/> object representing the file/location
            /// where the file operation will end.
            /// </param>
            public FileOperation(FileSystemInfo source, FileSystemInfo target)
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                if (target == null)
                {
                    throw new ArgumentNullException("target");
                }
                if (IsFileSystemType<DirectoryInfo>(source) &&
                    IsFileSystemType<FileInfo>(target))
                {
                    throw new BuildException("Cannot transfer directory to file");
                }
                _source = source;
                _target = target;
            }

            #endregion Public Constructors

            #region Public Instance Properties

            /// <summary>
            /// Gets or sets the string comparer to use when comparing
            /// the source path to the target path.
            /// </summary>
            public StringComparer Comparer
            {
                get { return _comparer; }
                set { _comparer = value; }
            }

            /// <summary>
            /// Gets the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>.
            /// </summary>
            public string Source
            {
                get { return _source.FullName; }
            }

            /// <summary>
            /// Gets the details of the source path.
            /// </summary>
            public FileSystemInfo SourceInfo
            {
                get { return _source; }
            }

            /// <summary>
            /// Gets the type of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>.
            /// </summary>
            public Type SourceType
            {
                get { return _source.GetType(); }
            }

            /// <summary>
            /// Gets the type of the file operation an instance of
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> represents.
            /// </summary>
            public OperationType OperationType
            {
                get
                {
                    if (IsFileSystemType<FileInfo>(SourceType) &&
                        IsFileSystemType<FileInfo>(TargetType))
                    {
                        return OperationType.FileToFile;
                    }
                    if (IsFileSystemType<FileInfo>(SourceType) &&
                        IsFileSystemType<DirectoryInfo>(TargetType))
                    {
                        return OperationType.FileToDirectory;
                    }
                    return OperationType.DirectoryToDirectory;
                }
            }

            /// <summary>
            /// Gets a value indicating whether
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/> is
            /// outdated.
            /// </summary>
            /// <value>
            /// <c>true</c> if
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/> is
            /// outdated (or simply a directory); otherwise, <c>false</c>.
            /// </value>
            public bool Outdated
            {
                get
                {
                    return IsFileSystemType<DirectoryInfo>(_target) ||
                        (IsFileSystemType<FileInfo>(_target) &&
                        TargetIsOutdated(_source, _target));
                }
            }

            /// <summary>
            /// Gets the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/>.
            /// </summary>
            public string Target
            {
                get { return _target.FullName; }
            }

            /// <summary>
            /// Gets the details of the target path.
            /// </summary>
            public FileSystemInfo TargetInfo
            {
                get { return _target; }
            }

            /// <summary>
            /// Gets the type of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/>.
            /// </summary>
            public Type TargetType
            {
                get { return _target.GetType(); }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            /// <summary>
            /// Normalizes the attributes of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/>.
            /// </summary>
            public void NormalizeTargetAttributes()
            {
                if (IsFileSystemType<FileInfo>(_target) &&
                    _target.Exists &&
                    _target.Attributes != FileAttributes.Normal)
                {
                    File.SetAttributes(_target.FullName, FileAttributes.Normal);
                }
            }

            /// <summary>
            /// Checks to see whether or not the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>
            /// matches the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/>.
            /// </summary>
            /// <remarks>
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.Comparer"/> is
            /// used to check path equality.
            /// </remarks>
            /// <returns>
            /// <b>true</b> if both paths match; otherwise <b>false</b>.
            /// </returns>
            public bool SourceEqualsTarget()
            {
                return _comparer.Compare(_source.FullName, _target.FullName) == 0;
            }

            /// <summary>
            /// Updates the source of a given instance based on the
            /// <see cref="P:System.IO.FileSystemInfo.LastWriteTime"/>.
            /// <remarks>
            /// If the LastWriteTime property of the <paramref name="newSource"/>
            /// is greater than the LastWriteTime property of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>, then
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/> is
            /// replaced with <paramref name="newSource"/>.
            /// </remarks>
            /// </summary>
            /// <param name='newSource'>
            /// The new <see cref="System.IO.FileSystemInfo"/> to replace
            /// the current <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>
            /// object.
            /// </param>
            public void UpdateSource(FileSystemInfo newSource)
            {
                if (_source.LastWriteTime < newSource.LastWriteTime)
                {
                    _source = newSource;
                }
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents the current
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/>.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents the current
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/>.
            /// </returns>
            public override string ToString()
            {
                return String.Format("'{0}' to '{1}'", Source, Target);
            }

            #endregion Public Instance Methods

            #region Public Static Methods

            /// <summary>
            /// Checks to see if a given <see cref="System.IO.FileSystemInfo"/>
            /// target is considered outdated.
            /// </summary>
            /// <param name='source'>
            /// A <see cref="System.IO.FileSystemInfo"/> used for comparison purposes
            /// against <paramref name="target"/>.
            /// </param>
            /// <param name='target'>
            /// The <see cref="System.IO.FileSystemInfo"/> to check.
            /// </param>
            /// <returns>
            /// <b>true</b> if the target file is considered out of date; otherwise
            /// <b>false</b>
            /// </returns>
            public static bool TargetIsOutdated(FileSystemInfo source, FileSystemInfo target)
            {
                return (!target.Exists) || (source.LastWriteTime > target.LastWriteTime);
            }

            #endregion Public Static Methods

            #region Private Instance Methods

            /// <summary>
            /// Checks to see whether <paramref name="item"/> is a file type or
            /// a directory type.
            /// </summary>
            /// <typeparam name="TFileSystemInfo">
            /// The FileSystemInfo type used to compare <paramref name="item"/> with.
            /// </typeparam>
            /// <param name="item">
            /// The object to check.
            /// </param>
            /// <returns>
            /// <b>true</b> if <paramref name="item"/> is the same type as
            /// <typeparamref name="TFileSystemInfo"/>; otherwise, <b>false</b>.
            /// </returns>
            private bool IsFileSystemType<TFileSystemInfo>(FileSystemInfo item)
                where TFileSystemInfo : FileSystemInfo
            {
                return item.GetType() == typeof(TFileSystemInfo);
            }

            #endregion Private Instance Methods
        }

        /// <summary>
        /// A collection class used to track all of the 
        /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> objects for 
        /// a given file operation task (such as the CopyTask or MoveTask).
        /// </summary>
        protected class FileOperationMap : KeyedCollection<string, FileOperation>
        {
            #region Private Instance Fields

            /// <summary>
            /// The StringComparer used when comparing file paths.
            /// </summary>
            private StringComparer _stringComparer;

            #endregion Private Instance Fields

            #region Public Constructors

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>
            /// class that uses the default string comparer.
            /// </summary>
            public FileOperationMap() : base(StringComparer.InvariantCulture)
            {
                _stringComparer = StringComparer.InvariantCulture;
            }

            /// <summary>
            /// Initializes a new instance of the 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>
            /// class that uses the specified string comparer.
            /// </summary>
            /// <param name="comparer">
            /// The string comparer to use when comparing keys in the
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>.
            /// </param>
            public FileOperationMap(StringComparer comparer) : base(comparer)
            {
                _stringComparer = comparer;
            }

            #endregion Public Constructors

            #region Public Instance Methods

            /// <summary>
            /// Determines whether the 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/> contains the 
            /// specified key.
            /// </summary>
            /// <param name="key">
            /// The key to locate in the 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>.
            /// </param>
            /// <returns>
            /// <b>true</b> if the <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/> 
            /// contains an element with the specified key; otherwise, <b>false</b>.
            /// </returns>
            public bool ContainsKey(string key)
            {
                if (Dictionary != null)
                {
                    return Dictionary.ContainsKey(key);
                }
                return false;
            }

            #endregion Public Instance Methods

            #region Protected Instance Methods

            /// <summary>
            /// Extracts the key from the specified 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> element.
            /// </summary>
            /// <param name="item">
            /// The <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> from which to 
            /// extract the key.
            /// </param>
            /// <returns>
            /// The key for the specified 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/>.
            /// </returns>
            protected override string GetKeyForItem(FileOperation item)
            {
                return item.Target;
            }

            /// <summary>
            /// Inserts an element into the 
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/> at the 
            /// specified index.
            /// </summary>
            /// <param name="index">
            /// The zero-based index at which item should be inserted.
            /// </param>
            /// <param name="item">
            /// The <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> to insert.
            /// </param>
            protected override void InsertItem(int index, FileOperation item)
            {
                // Assigns the string comparer to the item before calling
                // the base method.
                item.Comparer = _stringComparer;
                base.InsertItem(index, item);
            }

            /// <summary>
            /// Replaces the item at the specified index with the specified item.
            /// </summary>
            /// <param name="index">
            /// The zero-based index of the item to be replaced.
            /// </param>
            /// <param name="item">
            /// The new item.
            /// </param>
            protected override void SetItem(int index, FileOperation item)
            {
                // Assigns the string comparer to the item before calling
                // the base method.
                item.Comparer = _stringComparer;
                base.SetItem(index, item);
            }

            #endregion Protected Instance Methods
        }

        /// <summary>
        /// Used to identify the type of operation a given
        /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperation"/> represent.
        /// </summary>
        protected enum OperationType
        {
            /// <summary>
            /// Indicates that the operation is from file to file.
            /// </summary>
            FileToFile = 0,

            /// <summary>
            /// Indicates that the operation is from file to directory.
            /// </summary>
            FileToDirectory = 1,

            /// <summary>
            /// Indicates that the operation is from directory to directory.
            /// </summary>
            DirectoryToDirectory = 2
        }
    }
}
