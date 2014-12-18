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

using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Exits the current build by throwing a <see cref="BuildException" />, 
    /// optionally printing additional information.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The cause of the build failure can be specified using the <see cref="Message" /> 
    ///   attribute or as inline content.
    ///   </para>
    ///   <para>
    ///   Macros in the message will be expanded.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Exits the current build without giving further information.</para>
    ///   <code>
    ///     <![CDATA[
    /// <fail />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Exits the current build and writes a message to the build log.</para>
    ///   <code>
    ///     <![CDATA[
    /// <fail message="Something wrong here." />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Functionally equivalent to the previous example.</para>
    ///   <code>
    ///     <![CDATA[
    /// <fail>Something wrong here.</fail>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("fail")]
    public class FailTask : Task {
        #region Private Instance Fields

        private string _message;
        private string _contents;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// A message giving further information on why the build exited.
        /// </summary>
        /// <remarks>
        /// Inline content and <see cref="Message" /> are mutually exclusive.
        /// </remarks>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set {
                if (!String.IsNullOrEmpty(value)) {
                    if (!String.IsNullOrEmpty(Contents)) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the <fail> task.", Location);
                    } else {
                        _message = value;
                    }
                } else {
                    _message = null; 
                }
            }
        }

        /// <summary>
        /// Gets or sets the inline content that should be output in the build
        /// log, giving further information on why the build exited.
        /// </summary>
        /// <value>
        /// The inline content that should be output in the build log.
        /// </value>
        /// <remarks>
        /// Inline content and <see cref="Message" /> are mutually exclusive.
        /// </remarks>
        public string Contents {
            get { return _contents; }
            set { 
                if (!String.IsNullOrEmpty(value)) {
                    if (!String.IsNullOrEmpty(Message)) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the <fail> task.", Location);
                    } else {
                        _contents = value;
                    }
                } else {
                    _contents = null;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">During execution.</exception>
        protected override void ExecuteTask() {
            const string defaultMessage = "No message.";
            string message;

            if (!String.IsNullOrEmpty(Message)) {
                message = Message;
            } else if (!String.IsNullOrEmpty(Contents)) {
                message = Contents;
            } else {
                message = defaultMessage;
            }

            throw new BuildException(message, Location);
        }

        protected override void Initialize() {
            Contents = Project.ExpandProperties(XmlNode.InnerText, Location);
        }

        #endregion Override implementation of Task
    }
}
