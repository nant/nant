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

using System;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Exit the current build.</summary>
    /// <remarks>
    ///   <para>Exits the current build by throwing a BuildException, optionally printing additional information.</para>
    /// </remarks>
    /// <example>
    ///   <para>Will exit the current build with no further information given.</para>
    ///   <code>&lt;fail/&gt;</code>
    ///   <para>Will exit the current build and write message to build log.</para>
    ///   <code>&lt;fail message="Something wrong here."/&gt;</code>
    /// </example>
    [TaskName("fail")]
    public class FailTask : Task {
       
        string _message = null;

        /// <summary>A message giving further information on why the build exited.</summary>
        [TaskAttribute("message")]
        public string Message       { get { return _message; } set {_message = value; } }

        protected override void ExecuteTask() {
            string message = Message;
            if (message == null) {
                message = "No message";
            }
            throw new BuildException(message);
        }
    }
}
