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
// Brian Deacon (bdeacon@vidya.com)

using System;
using System.Globalization;
using System.Xml;

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Writes a message to the build log.
    /// </summary>
    /// <remarks>
    ///   <para>Macros in the message will be expanded.</para>
    /// </remarks>
    /// <example>
    ///   <para>Writes message to build log.</para>
    ///   <code>&lt;echo message="Hello, World!"/&gt;</code>
    ///   <para>Writes message with expanded macro to build log.</para>
    ///   <code>&lt;echo message="Base build directory = ${nant.project.basedir}"/&gt;</code>
    ///   <para>Functionally equivalent to the previous example.</para>
    ///   <code>&lt;echo&gt;Base build directory = ${nant.project.basedir}&lt;/echo&gt;</code>
    ///   <para>Triggers a ValidationException</para>
    ///   <code>&lt;echo message="Hello, World!"&gt;Hello, World&lt;/echo&gt;</code>
    /// </example>
    [TaskName("echo")]
    public class EchoTask : Task {
        #region Private Instance Fields

        string _message = null;
        string _contents = null;
        Level _level = Level.Info;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The message to display.
        /// </summary>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set {
                if (value != null && value.Trim().Length > 0) {
                    if (Contents != null) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the echo task.", Location);
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
        /// log.
        /// </summary>
        /// <value>
        /// The inline content that should be output in the build log.
        /// </value>
        public string Contents {
            get { return _contents; }
            set { 
                if (value != null && value.Trim().Length > 0) {
                    if (Message != null) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the echo task.", Location);
                    } else {
                        _contents = value;
                    }
                } else {
                    _contents = null;
                }
            }
        }

        /// <summary>
        /// The logging level with which the message should be output. The default 
        /// is <see cref="P:Level.Info" />?
        /// </summary>
        [TaskAttribute("level")]
        public Level Level {
            get { return _level; }
            set {
                if (!Enum.IsDefined(typeof(Level), value)) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "An invalid level {0} was specified.", value)); 
                } else {
                    this._level = value;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Outputs the message to the build log.
        /// </summary>
        protected override void ExecuteTask() {
            if (Message != null) {
                Log(Level, LogPrefix + Message);
            } else if (Contents != null) {
                Log(Level, LogPrefix + Contents);
            } else {
                Log(Level, LogPrefix);
            }
        }
        protected override void InitializeTask(XmlNode taskNode) {            Contents = Project.ExpandProperties(taskNode.InnerText, Location);        }

        #endregion Override implementation of Task
    }
}
