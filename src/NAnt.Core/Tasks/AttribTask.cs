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
// Chris Jenkin (oneinchhard@hotmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Changes the file attributes of a file or set of files and directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AttribTask" /> does not have the concept of turning 
    /// attributes off.  Instead you specify all the attributes that you want 
    /// turned on and the rest are turned off by default.
    /// </para>
    /// <para>
    /// Refer to the <see cref="FileAttributes" /> enumeration in the .NET SDK 
    /// for more information about file attributes.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///     Set the <c>read-only</c> file attribute for the specified file in 
    ///     the project directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <attrib file="myfile.txt" readonly="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///     Set the <c>normal</c> file attribute for the specified file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <attrib file="myfile.txt" normal="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///     Set the <c>normal</c> file attribute for all executable files in 
    ///     the current project directory and sub-directories.
    ///     </para>
    ///   <code>
    ///     <![CDATA[
    /// <attrib normal="true">
    ///     <fileset>
    ///         <include name="**/*.exe" />
    ///         <include name="bin" />
    ///     </fileset>
    /// </attrib>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("attrib")]
    public class AttribTask : Task {
        #region Private Instance Fields

        private FileInfo _file;
        private FileSet _fileset = new FileSet();
        private bool _archiveAttrib;
        private bool _hiddenAttrib;
        private bool _normalAttrib;
        private bool _readOnlyAttrib;
        private bool _systemAttrib;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the file which will have its attributes set. This is 
        /// provided as an alternate to using the task's fileset.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// All the matching files and directories in this fileset will have 
        /// their attributes set.
        /// </summary>
        [BuildElement("fileset")]
        public FileSet AttribFileSet {
            get { return _fileset; }
            set {_fileset = value; }
        }

        /// <summary>
        /// Set the archive attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("archive")]
        [BooleanValidator()]
        public bool ArchiveAttrib {
            get { return _archiveAttrib; }
            set { _archiveAttrib = value; }
        }

        /// <summary>
        /// Set the hidden attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("hidden")]
        [BooleanValidator()]
        public bool HiddenAttrib {
            get { return _hiddenAttrib; }
            set { _hiddenAttrib = value; }
        }

        /// <summary>
        /// Set the normal file attributes. This attribute is only valid if used 
        /// alone. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("normal")]
        [BooleanValidator()]
        public bool NormalAttrib {
            get { return _normalAttrib; }
            set { _normalAttrib = value; }
        }

        /// <summary>
        /// Set the read-only attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("readonly")]
        [BooleanValidator()]
        public bool ReadOnlyAttrib {
            get { return _readOnlyAttrib; }
            set { _readOnlyAttrib = value; }
        }

        /// <summary>
        /// Set the system attribute. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("system")]
        [BooleanValidator()]
        public bool SystemAttrib {
            get { return _systemAttrib; }
            set { _systemAttrib = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (AttribFileSet.BaseDirectory == null) {
                AttribFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // add the shortcut filename to the file set
            if (File != null) {
                AttribFileSet.Includes.Add(File.FullName);
            }

            if (AttribFileSet.FileNames.Count > 0) {
                // determine attributes to set on files
                FileAttributes fileAttributes = GetFileAttributes();

                // display build log message
                Log(Level.Info, "Setting file attributes for {0} files to {1}.", 
                    AttribFileSet.FileNames.Count, fileAttributes.ToString(CultureInfo.InvariantCulture));

                // perform operation on files
                foreach (string path in AttribFileSet.FileNames) {
                    SetFileAttributes(path, fileAttributes);
                }
            }

            if (AttribFileSet.DirectoryNames.Count > 0) {
                // determine attributes to set on directories
                FileAttributes directoryAttributes = GetDirectoryAttributes();

                // display build log message
                Log(Level.Info, "Setting attributes for {0} directories to {1}.", 
                    AttribFileSet.DirectoryNames.Count, directoryAttributes.ToString(CultureInfo.InvariantCulture));

                // perform operation on directories
                foreach (string path in AttribFileSet.DirectoryNames) {
                    SetDirectoryAttributes(path, directoryAttributes);
                }
            }

            if (AttribFileSet.FileNames.Count == 0 && AttribFileSet.DirectoryNames.Count == 0) {
                Log(Level.Verbose, "No matching files or directories found.");
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private FileAttributes GetFileAttributes() {
            FileAttributes fileAttributes = 0;

            if (NormalAttrib) {
                fileAttributes = FileAttributes.Normal;
            } else {
                if (ArchiveAttrib) {
                    fileAttributes |= FileAttributes.Archive;
                }
                if (HiddenAttrib) {
                    fileAttributes |= FileAttributes.Hidden;
                }
                if (ReadOnlyAttrib) {
                    fileAttributes |= FileAttributes.ReadOnly;
                }
                if (SystemAttrib) {
                    fileAttributes |= FileAttributes.System;
                }
            }

            if (!Enum.IsDefined(typeof(FileAttributes), fileAttributes)) {
                fileAttributes = FileAttributes.Normal;
            }

            return fileAttributes;
        }

        private void SetFileAttributes(string path, FileAttributes fileAttributes) {
            try {
                Log(Level.Verbose, path);
                System.IO.File.SetAttributes(path, fileAttributes);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1102"), path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, ex);
                } else {
                    Log(Level.Verbose, "{0} {1}", msg, ex.Message);
                }
            }
        }

        private FileAttributes GetDirectoryAttributes() {
            FileAttributes directoryAttributes = FileAttributes.Directory;

            if (!NormalAttrib) {
                if (ArchiveAttrib) {
                    directoryAttributes |= FileAttributes.Archive;
                }
                if (HiddenAttrib) {
                    directoryAttributes |= FileAttributes.Hidden;
                }
                if (ReadOnlyAttrib) {
                    directoryAttributes |= FileAttributes.ReadOnly;
                }
                if (SystemAttrib) {
                    directoryAttributes |= FileAttributes.System;
                }
            }

            return directoryAttributes;
        }

        private void SetDirectoryAttributes(string path, FileAttributes fileAttributes) {
            try {
                if (System.IO.Directory.Exists(path)) {
                    Log(Level.Verbose, path);
                    System.IO.File.SetAttributes(path, fileAttributes);
                } else {
                    throw new DirectoryNotFoundException();
                }
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1101"), path);
                if (FailOnError) {
                    throw new BuildException(msg, Location, ex);
                } else {
                    Log(Level.Verbose, "{0} {1}", msg, ex.Message);
                }
            }
        }

        #endregion Private Instance Methods
    }
}
