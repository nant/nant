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
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>
    /// Touch a file and/or fileset(s) -- corresponds to the Unix touch command.
    /// </summary>
    /// <example>
    ///   <para>Touch the <c>Main.cs</c> file.  The current time is used.</para>
    ///   <code><![CDATA[<touch file="Main.cs"/>]]></code>
    ///   <para>Touch all executable files in the current directory and its subdirectories.</para>
    ///   <code>
    /// <![CDATA[
    /// <touch>
    ///     <fileset>
    ///         <includes name="**/*.exe"/>
    ///         <includes name="**/*.dll"/>
    ///     </fileset>
    /// </touch>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("touch")]
    public class TouchTask : Task {
        
        string _file = null;
        string _millis = null;
        string _datetime = null;
        FileSet _fileset = new FileSet();

        /// <summary>Assembly Filename (required unless a fileset is specified).</summary>
        [TaskAttribute("file")]
        public string FileName  { get { return _file; } set { _file = value; } }

        /// <summary>Specifies the new modification time of the file in milliseconds since midnight Jan 1 1970.</summary>
        [TaskAttribute("millis")]
        public string Millis    { get { return _millis; } set { _millis = value; } }

        /// <summary>Specifies the new modification time of the file in the format MM/DD/YYYY HH:MM AM_or_PM.</summary>
        [TaskAttribute("datetime")]
        public string Datetime  { get { return _datetime; }  set { _datetime = value; } }

        /// <summary>Fileset to use instead of single file.</summary>
        [FileSet("fileset")]
        public FileSet TouchFileSet { get { return _fileset; } }

        ///<summary>Initializes task and ensures the supplied attributes are valid.</summary>
        ///<param name="taskNode">Xml node used to define this task instance.</param>
        protected override void InitializeTask(System.Xml.XmlNode taskNode) {
            // limit task to either millis or a date string
            if (_millis != null && _datetime != null) {
                throw new BuildException("Cannot specify 'millis' and 'datetime' in the same touch task", Location);
            }
        }

        protected override void ExecuteTask() {

            DateTime touchDateTime = DateTime.Now;
            if (_millis != null) {
                touchDateTime = GetDateTime(Convert.ToInt32(_millis));
            }

            if (_datetime != null) {
                touchDateTime = GetDateTime(_datetime);
            }

            // try to touch specified file
            if (FileName != null) {
                string path = null;
                try {
                    path = Project.GetFullPath(FileName);
                } catch (Exception e) {
                    string msg = String.Format("Could not determine path from {0}", FileName);
                    throw new BuildException(msg, Location, e);
                }
                // touch file(s)
                TouchFile(path, touchDateTime);

            } else {
                // touch files in fileset
                // only use the file set if file attribute has NOT been set
                foreach (string path in TouchFileSet.FileNames) {
                    TouchFile(path, touchDateTime);
                }
            }
        }

        void TouchFile(string path, DateTime touchDateTime) {
            try {
                if (File.Exists(path)) {
                    Log.WriteLineIf(Verbose, LogPrefix + "Touching file {0} with {1}", path, touchDateTime.ToString());
                    File.SetLastWriteTime(path, touchDateTime);
                } else {
                    throw new FileNotFoundException();
                }
            } catch (Exception e) {
                // swallow any errors and move on
                Log.WriteLineIf(Verbose, LogPrefix + "Error: {0}", e.Message);
            }
        }

        private DateTime GetDateTime(string dateText){
            DateTime touchDateTime = new DateTime();
            if (dateText != "") {
                touchDateTime = DateTime.Parse(dateText);
            }
            else {
                touchDateTime = DateTime.Now;
            }
            return touchDateTime;
        }

        private DateTime GetDateTime(int milliSeconds) {

            DateTime touchDateTime = DateTime.Parse("01/01/1970 00:00:00").AddMilliseconds(milliSeconds);
            return touchDateTime;
        }
    }
}
