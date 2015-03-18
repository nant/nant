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
// Jay Turpin (jayturpin@hotmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Globalization;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Touches a file or set of files -- corresponds to the Unix touch command.  
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the file specified does not exist, the task will create it.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Touch the <c>Main.cs</c> file.  The current time is used.</para>
    ///   <code>
    ///     <![CDATA[
    /// <touch file="Main.cs" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Touch all executable files in the project base directory and its 
    ///   subdirectories.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <touch>
    ///     <fileset>
    ///         <include name="**/*.exe" />
    ///         <include name="**/*.dll" />
    ///     </fileset>
    /// </touch>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("touch")]
    public class TouchTask : Task {
        #region Private Instance Fields

        private FileInfo _file;
        private long _millis;
        private DateTime _datetime;
        private FileSet _fileset = new FileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to touch.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// Specifies the new modification time of the file(s) in milliseconds 
        /// since midnight Jan 1 1970.
        /// </summary>
        [TaskAttribute("millis")]
        public long Millis {
            get { return _millis; }
            set { _millis = value; }
        }

        /// <summary>
        /// Specifies the new modification time of the file in the format 
        /// MM/DD/YYYY HH:MM:SS.
        /// </summary>
        [TaskAttribute("datetime")]
        [DateTimeValidator()]
        public DateTime Datetime {
            get { return _datetime; }
            set { _datetime = value; }
        }

        /// <summary>
        /// Used to select files that should be touched.
        /// </summary>
        [BuildElement("fileset")]
        public FileSet TouchFileSet {
            get { return _fileset; }
            set { _fileset = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Ensures the supplied attributes are valid.
        /// </summary>
        protected override void Initialize() {
            // limit task to either millis or a date string
            if (Millis != 0 && Datetime != DateTime.MinValue) {
                throw new BuildException("Cannot specify 'millis' and 'datetime'"
                    + " in the same <touch> task.", Location);
            }

            // limit task to touching either a file or fileset
            if (File != null && TouchFileSet.Includes.Count != 0) {
                throw new BuildException("Cannot specify both 'file' attribute" 
                    + " and use <fileset> in the same <touch> task.", Location);
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            DateTime touchDateTime = DateTime.Now;

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (TouchFileSet.BaseDirectory == null) {
                TouchFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (Millis != 0) {
                touchDateTime = GetDateTime(Millis);
            } else if (Datetime != DateTime.MinValue) {
                touchDateTime = Datetime;
            }

            // try to touch specified file
            if (File != null) {
                // touch file
                TouchFile(File.FullName, touchDateTime);
            } else {
                // touch files in fileset
                // only use the file set if file attribute has NOT been set
                foreach (string path in TouchFileSet.FileNames) {
                    TouchFile(path, touchDateTime);
                }
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private void TouchFile(string path, DateTime touchDateTime) {
            try {
                if (System.IO.File.Exists(path)) {
                    Log(Level.Verbose, "Touching file '{0}' with '{1}'.", 
                        path, touchDateTime.ToString(CultureInfo.InvariantCulture));
                } else {
                    Log(Level.Verbose, "Creating file '{0}' with '{1}'.", 
                        path, touchDateTime.ToString(CultureInfo.InvariantCulture));
                    // create the file (and ensure stream is closed)
                    using (FileStream fs = System.IO.File.Create(path)) {
                    }
                }
                System.IO.File.SetLastWriteTime(path, touchDateTime);
            } catch (Exception ex) {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1152"), path);

                if (FailOnError) {
                    throw new BuildException(msg, Location, ex);
                }

                // swallow any errors and move on
                Log(Level.Verbose, "{0} {1}", msg, ex.Message);
            }
        }

        private DateTime GetDateTime(long milliSeconds) {
            DateTime touchDateTime = DateTime.Parse("01/01/1970 00:00:00", CultureInfo.InvariantCulture);
            return touchDateTime.Add(TimeSpan.FromMilliseconds(milliSeconds));            
        }

        #endregion Private Instance Methods
    }
}
