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

    /// <summary>Writes a message to the build log.</summary>
    /// <remarks>
    ///   <para>Macros in the message will be expanded.</para>
    /// </remarks>
    /// <example>
    ///   <para>Writes message to build log.</para>
    ///   <code>&lt;echo message="Hello, World!"/&gt;</code>
    ///   <para>Writes message with expanded macro to build log.</para>
    ///   <code>&lt;echo message="Base build directory = ${nant.project.basedir}"/&gt;</code>
    /// </example>
    [TaskName("echo")]
    public class EchoTask : Task {

        string _message = null;

        /// <summary>The message to display.</summary>
        [TaskAttribute("message", Required=true)]
        public string Message {
            get { return _message; }
            set { _message = value; }
        }

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + Message);
        }
    }
}
