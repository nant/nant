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
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Moves a file or set of files to a new file or directory.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Files are only moved if the source file is newer than the destination 
    ///   file, or if the destination file does not exist.  However, you can 
    ///   explicitly overwrite files with the <see cref="CopyTask.Overwrite" /> attribute.
    ///   </para>
    ///   <para>
    ///   A <see cref="FileSet" /> can be used to select files to move. To use 
    ///   a <see cref="FileSet" />, the <see cref="CopyTask.ToDirectory" /> attribute 
    ///   must be set.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Move a single file.</para>
    ///   <code>
    ///     <![CDATA[
    /// <move file="myfile.txt" tofile="mytarget.txt" />
    ///     ]]>
    ///   </code>
    ///   <para>Move a set of files.</para>
    ///   <code>
    ///     <![CDATA[
    /// <move todir="${build.dir}">
    ///     <fileset basedir="bin">
    ///         <includes name="*.dll" />
    ///     </fileset>
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
        public override string SourceFile {
            get { return base.SourceFile; }
            set { base.SourceFile = value; }
        }

        /// <summary>
        /// The file to move to.
        /// </summary>
        [TaskAttribute("tofile")]
        public override string ToFile {
            get { return base.ToFile; }
            set { base.ToFile = value; }
        }

        /// <summary>
        /// The directory to move to.
        /// </summary>
        [TaskAttribute("todir")]
        public override string ToDirectory {
            get { return base.ToDirectory; }
            set { base.ToDirectory = value; }
        }

        /// <summary>
        /// Used to select the files to move. To use a <see cref="FileSet" />, 
        /// the <see cref="ToDirectory" /> attribute must be set.
        /// </summary>
        [FileSet("fileset")]
        public override FileSet CopyFileSet {
            get { return base.CopyFileSet; }
            set { base.CopyFileSet = value; }
        }

        /// <summary>
        /// Actually does the file (and possibly empty directory) moves.
        /// </summary>
        protected override void DoFileOperations() {
            if (FileCopyMap.Count > 0) {
                // loop thru our file list
                foreach (string sourcePath in FileCopyMap.Keys) {
                    string destinationPath = (string) FileCopyMap[sourcePath];
                    if (sourcePath == destinationPath) {
                        Log(Level.Warning, "Skipping self-move of {0}." + sourcePath);
                        continue;
                    }

                    try {
                        // check if directory exists
                        if (Directory.Exists(sourcePath)) {
                            Log(Level.Verbose, LogPrefix + "Moving directory {0} to {1}.", sourcePath, destinationPath);
                            Directory.Move(sourcePath, destinationPath);
                        }
                        else {
                            DirectoryInfo todir = new DirectoryInfo(destinationPath);
                            if (!todir.Exists) {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            }

                            Log(Level.Verbose, LogPrefix + "Moving {0} to {1}.", sourcePath, destinationPath);
                            // IM look into how Ant does this for directories

                            // move the file
                            File.Move(sourcePath, destinationPath);
                        }
                    } catch (IOException ioe) {
                        string msg = String.Format(CultureInfo.InvariantCulture, "Failed to move {0} to {1}\n{2}", sourcePath, destinationPath, ioe.ToString());
                        throw new BuildException(msg, Location);
                    }
                }
                Log(Level.Info, LogPrefix + "{0} files moved.", FileCopyMap.Count);
            }
        }

        #endregion Override implementation of CopyTask
    }
}
