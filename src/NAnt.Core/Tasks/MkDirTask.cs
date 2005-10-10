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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Creates a directory and any non-existent parent directory if necessary.
    /// </summary>
    /// <example>
    ///   <para>Create the directory <c>build</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <mkdir dir="build" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Create the directory tree <c>one/two/three</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <mkdir dir="one/two/three" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("mkdir")]
    public class MkDirTask : Task {
        #region Private Instance Fields

        private DirectoryInfo _dir;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The directory to create.
        /// </summary>
        [TaskAttribute("dir", Required=true)]
        public DirectoryInfo Dir {
            get { return _dir; }
            set { _dir = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Creates the directory specified by the <see cref="Dir" /> property.
        /// </summary>
        /// <exception cref="BuildException">The directory could not be created.</exception>
        protected override void ExecuteTask() {
            try {
                if (!Dir.Exists) {
                    Log(Level.Info, "Creating directory '{0}'.", Dir.FullName);
                    Dir.Create();
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1137"), Dir.FullName), 
                    Location, ex);
            }
        }

        #endregion Override implementation of Task
    }
}
