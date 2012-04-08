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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;

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
    ///   Entire directory structures can be copied to a new location.  For this
    ///   to happen, the following criteria must be met:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///       Everything in the fileset is included
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///       The directory structure is not flattened
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///       Empty directories are included
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///       Destination directory does not exist
    ///       </description>
    ///     </item>
    ///   </list>
    ///   <para>
    ///   If any of these items are not met, then the files within the source
    ///   directory will be copied over instead of the entire directory structure.
    ///   </para>
    ///   <para>
    ///   When a <see cref="FileSet" /> is used to select files or directories to
    ///   copy, the <see cref="ToDirectory" /> attribute must be set. Files that are
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
    /// <copy todir="target/dir">
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
        /// The set of files to perform a file operation on.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   FileCopyMap should now be considered a readonly hashtable. Any changes to
        ///   this property will not be taken into account during the file operation
        ///   task. To interact with the file operation, use the
        ///   <see cref="OperationMap"/> property.
        ///   </para>
        ///   <para>
        ///   The key of the <see cref="Hashtable" /> is the absolute path of
        ///   the destination file and the value is a <see cref="FileDateInfo" />
        ///   holding the path and last write time of the most recently updated
        ///   source file that is selected to be copied or moved to the
        ///   destination file.
        ///   </para>
        ///   <para>
        ///   On Windows, the <see cref="Hashtable" /> is case-insensitive.
        ///   </para>
        /// </remarks>
        [ObsoleteAttribute("FileCopyMap is now considered a readonly hashtable. To interact with file operation, use the OperationMap property")]
        protected Hashtable FileCopyMap
        {
            get { return _operationMap.ConvertToHashtable(); }
        }

        /// <summary>
        /// Gets the operation map containing all the files/directories to
        /// perform file operations on.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The type of class for this object inherits from KeyedCollection
        ///   and is structured so that the key of this collection contains the
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
            }
            // This copy/moves the entire directory.  In order for this to occur, the
            // following criteria needs to be met:
            // * Everything in the fileset is included
            // * The directory structure is not flattened
            // * Empty directories are included
            // * and either
            //   * the destination directory does not exist
            //   * or the destination directory is the same as source directory but
            //     with different casing (ie: C:\nant to C:\NAnt)
            else if (CopyFileSet.IsEverythingIncluded && !Flatten && IncludeEmptyDirs &&
                FileOperation.TargetDirectoryDoesNotExist(CopyFileSet.BaseDirectory, 
                    ToDirectory))
            {
                OperationMap.Add(new FileOperation(CopyFileSet.BaseDirectory, ToDirectory));
            }
            // Otherwise, copy/move the individual files.
            else
            {
                // If no includes were specified, add all files and subdirectories
                // from the fileset's base directory to the fileset.
                if (CopyFileSet.Includes.Count == 0)
                {
                    CopyFileSet.Includes.Add("**/*");

                    // Make sure to rescan the fileset after adding "**/*"
                    CopyFileSet.Scan();
                }

                // copy file set contents.
                // get the complete path of the base directory of the fileset,
                // ie, c:\work\nant\src
                DirectoryInfo srcBaseInfo = CopyFileSet.BaseDirectory;

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
                        throw CreateSourceFileNotFoundException(srcInfo.FullName);
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
            // If the operation map is empty, exit (return) the method.
            if (OperationMap.Count <= 0)
            {
                return;
            }

            // Get the number of file and directory copies to display to
            // the user.
            int fileMovements = OperationMap.CountFileOperations();
            int dirMovements = OperationMap.CountDirectoryOperations();

            // Output the number of file copies
            if (fileMovements > 0)
            {
                if (ToFile != null) {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.",
                        fileMovements, (fileMovements != 1) ? "s" : "", ToFile);
                } else {
                    Log(Level.Info, "Copying {0} file{1} to '{2}'.",
                        fileMovements, (fileMovements != 1) ? "s" : "", ToDirectory);
                }
            }

            // Output the number of directory copies
            if (dirMovements > 0)
            {
                if (ToFile != null) {
                    Log(Level.Info, "Copying {0} {1} to '{2}'.",
                        dirMovements, (dirMovements != 1) ? "directories" : "directory",
                        ToFile);
                } else {
                    Log(Level.Info, "Copying {0} {1} to '{2}'.",
                        dirMovements, (dirMovements != 1) ? "directories" : "directory",
                        ToDirectory);
                }
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
                        default:
                            throw new
                                BuildException("Unrecognized copy operation. " +
                                "The copy task can only copy a file to file, " +
                                "file to directory, or directory to directory.");
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

        protected virtual BuildException CreateSourceFileNotFoundException (string sourceFile) {
            return new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Could not find file '{0}' to copy.", sourceFile),
                Location);
        }

        #endregion Protected Instance Methods

        /// <summary>
        /// Holds the absolute paths and last write time of a given file.
        /// </summary>
        protected class FileDateInfo
        {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileDateInfo"/> class
            /// for the specified <paramref name="file"/>.
            /// </summary>
            /// <param name="file">
            /// A <see cref="System.IO.FileSystemInfo"/> object containing
            /// the full path and last write time of the file the object represents.
            /// </param>
            public FileDateInfo(FileSystemInfo file)
                : this(file.FullName, file.LastWriteTime) {}

            /// <summary>
            /// Initializes a new instance of the <see cref="FileDateInfo" />
            /// class for the specified file and last write time.
            /// </summary>
            /// <param name="path">The absolute path of the file.</param>
            /// <param name="lastWriteTime">The last write time of the file.</param>
            public FileDateInfo(string path, DateTime lastWriteTime)
            {
                _path = path;
                _lastWriteTime = lastWriteTime;
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            /// <summary>
            /// Gets the absolute path of the current file.
            /// </summary>
            /// <value>
            /// The absolute path of the current file.
            /// </value>
            public string Path
            {
                get { return _path; }
            }

            /// <summary>
            /// Gets the time when the current file was last written to.
            /// </summary>
            /// <value>
            /// The time when the current file was last written to.
            /// </value>
            public DateTime LastWriteTime
            {
                get { return _lastWriteTime; }
            }

            #endregion Public Instance Properties

            #region Private Instance Fields
            
            private DateTime _lastWriteTime;
            private string _path;

            #endregion Private Instance Fields
        }

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
                    if (IsFileSystemType<FileInfo>(SourceInfo) &&
                        IsFileSystemType<FileInfo>(TargetInfo))
                    {
                        return OperationType.FileToFile;
                    }
                    if (IsFileSystemType<FileInfo>(SourceInfo) &&
                        IsFileSystemType<DirectoryInfo>(TargetInfo))
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
            /// <c>true</c> if both paths match; otherwise <c>false</c>.
            /// </returns>
            public bool SourceEqualsTarget()
            {
                return _comparer.Compare(_source.FullName, _target.FullName) == 0;
            }

            /// <summary>
            /// Checks to see whether or not the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.SourceInfo"/>
            /// is identical to the full path of
            /// <see cref="P:NAnt.Core.Tasks.CopyTask.FileOperation.TargetInfo"/>.
            /// </summary>
            /// <remarks>
            /// The difference between this method and SourceEqualsTarget is
            /// that the casing of the path is never ignored regardless of
            /// operating system.
            /// </remarks>
            /// <returns>
            /// <c>true</c> if both paths are identical; otherwise <c>false</c>.
            /// </returns>
            public bool SourceIsIdenticalToTarget()
            {
                return _source.FullName.Equals(_target.FullName, StringComparison.InvariantCulture);
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
            /// <c>true</c> if the target file is considered out of date; otherwise
            /// <c>false</c>
            /// </returns>
            public static bool TargetIsOutdated(FileSystemInfo source, FileSystemInfo target)
            {
                return (!target.Exists) || (source.LastWriteTime > target.LastWriteTime);
            }

            /// <summary>
            /// Checks to see if the target directory does not exist or that
            /// it does match the source directory name but not string casing.
            /// </summary>
            /// <param name="source">
            /// Source directory to check against <paramref name="target"/>.
            /// </param>
            /// <param name="target">
            /// The target directory to validate.
            /// </param>
            /// <returns>
            /// <c>true</c> if the target directory does not exist or matches the source
            /// directory name but not casing; otherwise <c>false</c>
            /// </returns>
            public static bool TargetDirectoryDoesNotExist(DirectoryInfo source, DirectoryInfo target)
            {
                // If the target doesn't exist, return true.
                if (!target.Exists)
                {
                    return true;
                }
                // Otherwise, check to see if the source and target paths are the same when ignoring case.
                // Return the result of the path comparison.
                return source.FullName.Equals(target.FullName, StringComparison.InvariantCultureIgnoreCase);
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
            /// <c>true</c> if <paramref name="item"/> is the same type as
            /// <typeparamref name="TFileSystemInfo"/>; otherwise, <c>false</c>.
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
            /// <c>true</c> if the <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/>
            /// contains an element with the specified key; otherwise, <c>false</c>.
            /// </returns>
            public bool ContainsKey(string key)
            {
                if (Dictionary != null)
                {
                    return Dictionary.ContainsKey(key);
                }
                return false;
            }

            /// <summary>
            /// Counts the number of directory operations in a collection.
            /// </summary>
            /// <returns>
            /// The number of directory operations performed by this collection.
            /// </returns>
            public int CountDirectoryOperations()
            {
                int result = 0;
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].SourceType == typeof(DirectoryInfo))
                    {
                        result++;
                    }
                }
                return result;
            }

            /// <summary>
            /// Counts the number of file operations in a collection.
            /// </summary>
            /// <returns>
            /// The number of file operations performed by this collection.
            /// </returns>
            public int CountFileOperations()
            {
                int result = 0;
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].SourceType == typeof(FileInfo))
                    {
                        result++;
                    }
                }
                return result;
            }

            /// <summary>
            /// Converts the current instance of
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/> to
            /// the old style FileCopyMap hashtable.
            /// </summary>
            /// <returns>
            /// The contents of
            /// <see cref="NAnt.Core.Tasks.CopyTask.FileOperationMap"/> in a
            /// new hashtable.
            /// </returns>
            public Hashtable ConvertToHashtable()
            {
                // Setup var to return
                Hashtable result;

                // Initialize return var with the proper case sensitivity
                // based on underlying OS.
                if (PlatformHelper.IsUnix)
                {
                    result = new Hashtable();
                }
                else
                {
                    result = CollectionsUtil.CreateCaseInsensitiveHashtable();
                }

                // Loop through this collection and load the return hashtable var.
                for (int i = 0; i < this.Count; i++)
                {
                    FileOperation temp = this[i];
                    string sourceFileName;
                    string targetFileName;

                    // For a FileToFile operation, load the file names in the return
                    // hashtable var as is.
                    if (temp.OperationType == CopyTask.OperationType.FileToFile)
                    {
                        result.Add(temp.Target, new FileDateInfo(temp.SourceInfo));
                    }
                    // For a FileToDirectory operation, use the source file name as
                    // the target file name and load accordingly.
                    else if (temp.OperationType == CopyTask.OperationType.FileToDirectory)
                    {
                        sourceFileName = Path.GetFileName(temp.Source);
                        targetFileName = Path.Combine(temp.Target, sourceFileName);

                        result.Add(targetFileName, new FileDateInfo(temp.SourceInfo));
                    }
                    // For other operations (ie: DirectoryToDirectory), scan the
                    // source directory for all files and load them into the
                    // return hashtable var.
                    else
                    {
                        // Retrieve all files from the current path and any subdirectories.
                        DirectoryScanner dirScan = new DirectoryScanner();
                        dirScan.BaseDirectory = temp.SourceInfo as DirectoryInfo;
                        dirScan.Includes.Add("**/*");
                        dirScan.Scan();
                        StringCollection sourceFiles = dirScan.FileNames;

                        for (int s = 0; s < sourceFiles.Count; s++)
                        {
                            string source = sourceFiles[s];
                            sourceFileName = Path.GetFileName(source);
                            targetFileName = Path.Combine(temp.Target, sourceFileName);

                            result.Add(targetFileName, new FileDateInfo(sourceFileName,
                                File.GetLastWriteTime(sourceFileName)));
                        }
                    }

                }
                return result;
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
