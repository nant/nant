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
using System.IO;
using System.Globalization;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Writes a message to the build log or a specified file.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The message can be specified using the <see cref="Message" /> attribute 
    ///   or as inline content.
    ///   </para>
    ///   <para>
    ///   Macros in the message will be expanded.
    ///   </para>
    ///   <para>
    ///   When writing to a file, the <see cref="MessageLevel" /> attribute is
    ///   ignored.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Writes a message with level <see cref="Level.Debug" /> to the build log.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <echo message="Hello, World!" level="Debug" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Writes a message with expanded macro to the build log.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <echo message="Base build directory = ${nant.project.basedir}" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Functionally equivalent to the previous example.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <echo>Base build directory = ${nant.project.basedir}</echo>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Writes the previous message to a file in the project directory, 
    ///   overwriting the file if it exists.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <echo file="buildmessage.txt">Base build directory = ${nant.project.basedir}</echo>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("echo")]
    public class EchoTask : Task {
        #region Private Instance Fields

        private string _message;
        private string _contents;
        private FileInfo _file;
        private bool _append = false;
        private Level _messageLevel = Level.Info;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The message to output.
        /// </summary>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set {
                if (!StringUtils.IsNullOrEmpty(value)) {
                    if (!StringUtils.IsNullOrEmpty(Contents)) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the <echo> task.", Location);
                    } else {
                        _message = value;
                    }
                } else {
                    _message = null; 
                }
            }
        }

        /// <summary>
        /// Gets or sets the inline content that should be output.
        /// </summary>
        /// <value>
        /// The inline content that should be output.
        /// </value>
        public string Contents {
            get { return _contents; }
            set { 
                if (!StringUtils.IsNullOrEmpty(value)) {
                    if (!StringUtils.IsNullOrEmpty(Message)) {
                        throw new ValidationException("Inline content and the message attribute are mutually exclusive in the <echo> task.", Location);
                    } else {
                        _contents = value;
                    }
                } else {
                    _contents = null;
                }
            }
        }

        /// <summary>
        /// The file to write the message to.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// Determines whether the <see cref="EchoTask" /> should append to the 
        /// file, or overwrite it.  By default, the file will be overwritten.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output should be appended to the file; 
        /// otherwise, <see langword="false" />. The default is 
        /// <see langword="false" />.
        /// </value>
        [TaskAttribute("append")]
        public bool Append {
            get { return _append; }
            set { _append = value; }
        }

        /// <summary>
        /// The logging level with which the message should be output. The default 
        /// is <see cref="Level.Info" />.
        /// </summary>
        [TaskAttribute("level")]
        public Level MessageLevel {
            get { return _messageLevel; }
            set {
                if (!Enum.IsDefined(typeof(Level), value)) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "An invalid level {0} was specified.", value)); 
                } else {
                    this._messageLevel = value;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Outputs the message to the build log or the specified file.
        /// </summary>
        protected override void ExecuteTask() {
            if (File != null) { // output to file
                try {
                    // ensure the output directory exists
                    File.Directory.Create();
                    // write the message to the file
                    using (StreamWriter writer = new StreamWriter(File.FullName, Append)) {
                        if (!StringUtils.IsNullOrEmpty(Message)) {
                            writer.WriteLine(Message);
                        } else if (!StringUtils.IsNullOrEmpty(Contents)) {
                            writer.WriteLine(Contents);
                        } else {
                            writer.WriteLine();
                        }
                    }
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Failed to write message to file '{0}'.", File.FullName), 
                        Location, ex);
                }
            } else { // output to build log
                if (!StringUtils.IsNullOrEmpty(Message)) {
                    Log(MessageLevel, Message);
                } else if (!StringUtils.IsNullOrEmpty(Contents)) {
                    Log(MessageLevel, Contents);
                } else {
                    Log(MessageLevel, string.Empty);
                }
            }
        }                        protected override void InitializeTask(XmlNode taskNode) {            Contents = Project.ExpandProperties(taskNode.InnerText, Location);        }

        #endregion Override implementation of Task
    }
}
