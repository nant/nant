// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.Core.Filters;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Moves a file, a directory, or set of files to a new file or directory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Files are only moved if the source file is newer than the destination
    ///   file, or if the destination file does not exist.  However, you can
    ///   explicitly overwrite files with the <see cref="CopyTask.Overwrite" /> 
    ///   attribute.
    ///   </para>
    ///   <para>
    ///   Entire directory structures can be moved to a new location.  For this
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
    ///   directory will be moved over instead of the entire directory structure.
    ///   </para>
    ///   <para>
    ///   A <see cref="FileSet" /> can be used to select files or directories to move.
    ///   To use a <see cref="FileSet" />, the <see cref="CopyTask.ToDirectory" />
    ///   attribute must be set.
    ///   </para>
    ///   <h3>Encoding</h3>
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
    ///   If you employ filters in your move operation, you should limit the 
    ///   move to text files. Binary files will be corrupted by the move 
    ///   operation.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Move a single file while changing its encoding from "latin1" to 
    ///   "utf-8".
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <move
    ///     file="myfile.txt"
    ///     tofile="mycopy.txt"
    ///     inputencoding="latin1"
    ///     outputencoding="utf-8" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Move a set of files.</para>
    ///   <code>
    ///     <![CDATA[
    /// <move todir="${build.dir}">
    ///     <fileset basedir="bin">
    ///         <include name="*.dll" />
    ///     </fileset>
    /// </move>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Move a set of files to a directory, replacing <c>@TITLE@</c> with 
    ///   "Foo Bar" in all files.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <move todir="../backup/dir">
    ///     <fileset basedir="src_dir">
    ///         <include name="**/*" />
    ///     </fileset>
    ///     <filterchain>
    ///         <replacetokens>
    ///             <token key="TITLE" value="Foo Bar" />
    ///         </replacetokens>
    ///     </filterchain>
    /// </move>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Move an entire directory and its contents.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <move todir="target/dir">
    ///   <fileset basedir="source/dir"/>
    /// </move>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("move")]
    public class MoveTask : CopyTask {
        #region Override implementation of CopyTask

        /// <summary>
        /// The file to move.
        /// </summary>
        [TaskAttribute("file")]
        public override FileInfo SourceFile {
            get { return base.SourceFile; }
            set { base.SourceFile = value; }
        }

        /// <summary>
        /// The file to move to.
        /// </summary>
        [TaskAttribute("tofile")]
        public override FileInfo ToFile {
            get { return base.ToFile; }
            set { base.ToFile = value; }
        }

        /// <summary>
        /// The directory to move to.
        /// </summary>
        [TaskAttribute("todir")]
        public override DirectoryInfo ToDirectory {
            get { return base.ToDirectory; }
            set { base.ToDirectory = value; }
        }

        /// <summary>
        /// Used to select the files to move. To use a <see cref="FileSet" />,
        /// the <see cref="ToDirectory" /> attribute must be set.
        /// </summary>
        [BuildElement("fileset")]
        public override FileSet CopyFileSet {
            get { return base.CopyFileSet; }
            set { base.CopyFileSet = value; }
        }
        /// <summary>
        /// Ignore directory structure of source directory, move all files into
        /// a single directory, specified by the <see cref="ToDirectory" />
        /// attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("flatten")]
        [BooleanValidator()]
        public override bool Flatten {
            get { return base.Flatten; }
            set { base.Flatten = value; }
        }

        /// <summary>
        /// Chain of filters used to alter the file's content as it is moved.
        /// </summary>
        [BuildElement("filterchain")]
        public override FilterChain Filters {
            get { return base.Filters; }
            set { base.Filters = value; }
        }

        /// <summary>
        /// Actually does the file moves.
        /// </summary>
        protected override void DoFileOperations() {
            // If the operation map is empty, exit (return) the method.
            if (OperationMap.Count <= 0)
            {
                return;
            }

            // loop thru our file list
            for (int i = 0; i < OperationMap.Count; i++)
            {
                // Setup a temporary var to hold the current file operation
                // details.
                FileOperation currentOperation = OperationMap[i];
                if (currentOperation.SourceIsIdenticalToTarget())
                {
                    Log(Level.Warning, String.Format("Skipping self-move of {0}.",
                        currentOperation.Source));
                    continue;
                }

                try
                {
                    Log(Level.Verbose, "Moving {0}.", currentOperation.ToString());

                    string destinationDirectory = null;

                    switch (currentOperation.OperationType)
                    {
                        case OperationType.FileToFile:
                            // Setup the dest directory var
                            destinationDirectory =
                                Path.GetDirectoryName(currentOperation.Target);

                            // create directory if not present
                            if (!Directory.Exists(destinationDirectory))
                            {
                                Directory.CreateDirectory(destinationDirectory);
                                Log(Level.Verbose, "Created directory '{0}'.",
                                    destinationDirectory);
                            }

                            // Ensure the target file is removed before
                            // attempting to move.
                            if (File.Exists(currentOperation.Target))
                            {
                                File.Delete(currentOperation.Target);
                            }

                            // move the file with filters
                            FileUtils.MoveFile(currentOperation.Source,
                                currentOperation.Target, Filters,
                                InputEncoding, OutputEncoding);

                            break;
                        case OperationType.FileToDirectory:
                            // Setup the dest directory var
                            destinationDirectory = currentOperation.Target;

                            // Setup a local var that combines the directory
                            // of the target path with the source file name.
                            string targetFile = Path.Combine(destinationDirectory,
                                Path.GetFileName(currentOperation.Source));

                            // create directory if not present
                            if (!Directory.Exists(destinationDirectory))
                            {
                                Directory.CreateDirectory(destinationDirectory);
                                Log(Level.Verbose, "Created directory '{0}'.",
                                    destinationDirectory);
                            }

                            // Ensure the target file is removed before
                            // attempting to move.
                            if (File.Exists(targetFile))
                            {
                                File.Delete(targetFile);
                            }

                            // move the file with filters
                            FileUtils.MoveFile(currentOperation.Source,
                                targetFile, Filters, InputEncoding, OutputEncoding);

                            break;
                        case OperationType.DirectoryToDirectory:

                            // Move over the entire directory with filters
                            FileUtils.MoveDirectory(currentOperation.Source,
                                currentOperation.Target, Filters, InputEncoding,
                                OutputEncoding);
                            break;
                        default:
                            throw new
                                BuildException("Unrecognized move operation. " +
                                "The move task can only move a file to file, " +
                                "file to directory, or directory to directory.");
                    }
                }
                catch (IOException ex)
                {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Failed to move {0}.", currentOperation.ToString()),
                        Location, ex);
                }
            }

            int fileMovements = OperationMap.CountFileOperations();
            int dirMovements = OperationMap.CountDirectoryOperations();

            if (fileMovements > 0)
            {
                Log(Level.Info, "{0} file{1} moved.", fileMovements,
                    fileMovements != 1 ? "s" : "");
            }
            if (dirMovements > 0)
            {
                Log(Level.Info, "{0} {1} moved.", dirMovements,
                    dirMovements != 1 ? "directories" : "directory");
            }
        }

        protected override BuildException CreateSourceFileNotFoundException (string sourceFile) {
            return new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Could not find file '{0}' to move.", sourceFile),
                Location);
        }

        #endregion Override implementation of CopyTask

    }
}

