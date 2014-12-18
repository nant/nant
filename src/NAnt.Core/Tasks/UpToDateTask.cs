// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Check modification dates on groups of files.
    /// </summary>
    /// <remarks>
    /// If all <see cref="TargetFiles" /> are same or newer than all <see cref="SourceFiles" />, the specified property is set to <see langword="true" />, otherwise it
    /// is set to <see langword="false" />.
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Check file dates. If <c>myfile.dll</c> is same or newer than <c>myfile.cs</c>, then set <c>myfile.dll.uptodate</c> property 
    ///   to either <see langword="true" /> or <see langword="false" />.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <uptodate property="myfile.dll.uptodate">
    ///     <sourcefiles>
    ///         <include name="myfile.cs" />
    ///     </sourcefiles>
    ///     <targetfiles>
    ///         <include name="myfile.dll" />
    ///     </targetfiles>
    /// </uptodate>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("uptodate")]
    public class UpToDateTask : Task {
        #region Private Instance Fields

        private string _propertyName;
        private FileSet _sourceFiles;
        private FileSet _targetFiles;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Property that will be set to <see langword="true" /> or <see langword="false" /> depending on the 
        /// result of the date check.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string PropertyName {
            get { return _propertyName; }
            set { _propertyName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The <see cref="FileSet" /> that contains list of source files. 
        /// </summary>
        [BuildElement("sourcefiles")]
        public FileSet SourceFiles {
            get { return _sourceFiles; }
            set { _sourceFiles = value; }
        } 

        /// <summary>
        /// The <see cref="FileSet" /> that contains list of target files. 
        /// </summary>
        [BuildElement("targetfiles")]
        public FileSet TargetFiles {
            get { return _targetFiles; }
            set { _targetFiles = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            bool value = true;
            
            FileInfo primaryFile = _targetFiles.MostRecentLastWriteTimeFile;
            if (primaryFile == null || !primaryFile.Exists) {
                value = false;
                Log(Level.Verbose, "Destination file(s) do(es) not exist.");
            } else {
                string newerFile = FileSet.FindMoreRecentLastWriteTime(_sourceFiles.FileNames, primaryFile.LastWriteTime);
                bool needsAnUpdate = (newerFile != null);
                if (needsAnUpdate) {
                    value = false;
                    Log(Level.Verbose, "{0} is newer than {1}.", newerFile, primaryFile.Name);
                }
            }
            Project.Properties[PropertyName] = Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        #endregion Override implementation of Task
    }
}
