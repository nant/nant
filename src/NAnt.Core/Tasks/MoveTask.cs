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
using System.Collections;
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
    ///   A <see cref="FileSet" /> can be used to select files to move. To use
    ///   a <see cref="FileSet" />, the <see cref="CopyTask.ToDirectory" /> 
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
    /// <move tofile="target/dir">
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
            string destinationPath;
            string sourcePath;
            bool isDir;
            StringComparison strCmp = CopyFileSet.CaseSensitive ?
                StringComparison.InvariantCulture :
                StringComparison.InvariantCultureIgnoreCase;

            if (FileCopyMap.Count > 0) {
                // loop thru our file list
                foreach (DictionaryEntry fileEntry in FileCopyMap) {
                    destinationPath = (string) fileEntry.Key;
                    sourcePath = ((FileDateInfo) fileEntry.Value).Path;
                    isDir = ((FileDateInfo) fileEntry.Value).IsDirectory;

                    if (sourcePath.Equals(destinationPath, StringComparison.InvariantCulture))
                    {
                        Log(Level.Warning, "Skipping self-move of {{0}}.{0}", sourcePath);
                        continue;
                    }

                    try {
                        // check if directory exists
                        if (isDir) {
                            Log(Level.Verbose, "Moving directory '{0}' to '{1}'.", 
                                sourcePath, destinationPath);

                            if (sourcePath.Equals(destinationPath, strCmp))
                            {
                                string rootDir = !String.IsNullOrEmpty(Path.GetDirectoryName(destinationPath)) ?
                                    Path.GetDirectoryName(destinationPath) : destinationPath;
                                string tmpDir = Path.Combine(rootDir, Path.GetRandomFileName());

                                Directory.Move(sourcePath, tmpDir);
                                Directory.Move(tmpDir, destinationPath);
                            }
                            else
                            {
                                Directory.Move(sourcePath, destinationPath);
                            }
                        } else {
                            DirectoryInfo todir = new DirectoryInfo(destinationPath);
                            if (!todir.Exists) {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            }

                            Log(Level.Verbose, "Moving '{0}' to '{1}'.", sourcePath, destinationPath);

                            // if destination file exists, remove it first if
                            // in overwrite mode
                            if (File.Exists(destinationPath)) {
                                Log(Level.Verbose, "Removing '{0}' before moving '{1}'.", destinationPath, sourcePath);
                                File.Delete(destinationPath);
                            }

                            // move the file and apply filters
                            FileUtils.MoveFile(sourcePath, destinationPath, Filters, 
                                InputEncoding, OutputEncoding);
                        }
                    } catch (IOException ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Failed to move {0} to {1}.", sourcePath, destinationPath),
                            Location, ex);
                    }
                }
                if (FileCount > 0)
                    Log(Level.Info, "{0} file{1} moved.", FileCount, 
                        (FileCount > 1) ? "s" : "");

                if (DirectoryCount > 0) 
                    Log(Level.Info, "{0} director{1} moved.", DirectoryCount,
                        (DirectoryCount > 1) ? "ies" : "y");
            }
        }

        /// <summary>
        /// Creates a source file not found exception.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <returns>
        /// The exception instance.
        /// </returns>
        protected override BuildException CreateSourceFileNotFoundException (string sourceFile) {
            return new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Could not find file '{0}' to move.", sourceFile),
                Location);
        }

        #endregion Override implementation of CopyTask
    }
}

