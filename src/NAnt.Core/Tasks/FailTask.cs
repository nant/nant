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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Exits the current build by throwing a <see cref="BuildException" />, 
    /// optionally printing additional information.
    /// </summary>
    /// <example>
    ///   <para>Exits the current build without giving further information.</para>
    ///   <code>
    ///     <![CDATA[
    /// <fail />
    ///     ]]>
    ///   </code>
    ///   <para>Exits the current build and writes a message to the build log.</para>
    ///   <code>
    ///     <![CDATA[
    /// <fail message="Something wrong here." />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("fail")]
    public class FailTask : Task {
        #region Private Instance Fields

        private string _message = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// A message giving further information on why the build exited.
        /// </summary>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set {_message = SetStringValue(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            string message = Message;

            if (message == null) {
                message = "No message";
            }
            throw new BuildException(message);
        }

        #endregion Override implementation of Task
    }
}
