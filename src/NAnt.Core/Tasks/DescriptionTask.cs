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

// Gerry Shaw (gerry_shaw@yahoo.com)

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// An empty task that allows a build file to contain a description.
    /// </summary>
    /// <example>
    ///   <para>Set a description.</para>
    ///   <code>
    ///     <![CDATA[
    /// <description>This is a description.</description>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("description")]
    public class DescriptionTask : Task {
        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
        }

        #endregion Override implementation of Task
    }
}
