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
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace NAnt.Core {
    /// <summary>
    /// Used to wrap log messages in xml &lt;message/&gt; elements.
    /// </summary>
    [Serializable()]
    public class XmlLogger : IBuildLogger, ISerializable {
        #region Private Instance Fields

        private TextWriter _writer = Console.Out;
        private Level _threshold = Level.Info;
        [NonSerialized()]
        private XmlTextWriter _xmlWriter;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger" /> class.
        /// </summary>
        public XmlLogger() {
            _xmlWriter = new XmlTextWriter(_writer);
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger" /> class 
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected XmlLogger(SerializationInfo info, StreamingContext context) {
            _writer = info.GetValue("Writer", typeof(TextWriter)) as TextWriter;
            _threshold = (Level) info.GetValue("Threshold", typeof(Level));
            _xmlWriter = new XmlTextWriter(_writer);
        }

        #endregion Protected Instance Constructors

        #region Implementation of ISerializable

        /// <summary>
        /// Populates <paramref name="info" /> with the data needed to serialize 
        /// the <see cref="XmlLogger" /> instance.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Writer", _writer);
            info.AddValue("Threshold", _threshold);
        }

        #endregion Implementation of ISerializable

        #region Override implementation of Object

        /// <summary>
        /// Returns the contents of log captured.
        /// </summary>
        public override string ToString() {
            return _writer.ToString();
        }

        #endregion Override implementation of Object

        #region Implementation of IBuildListener

        /// <summary>
        /// Signals that a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        public void BuildStarted(object sender, BuildEventArgs e) {
            _xmlWriter.WriteStartElement(Elements.BuildResults);
            _xmlWriter.WriteAttributeString(Attributes.Project, e.Project.ProjectName);
        }

        /// <summary>
        /// Signals that the last target has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public void BuildFinished(object sender, BuildEventArgs e) {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Signals that a target has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public void TargetStarted(object sender, BuildEventArgs e) {
            _xmlWriter.WriteStartElement(Elements.Target);
            WriteNameAttribute(e.Target.Name);
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Signals that a target has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public void TargetFinished(object sender, BuildEventArgs e) {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Signals that a task has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public void TaskStarted(object sender, BuildEventArgs e) {
            _xmlWriter.WriteStartElement(Elements.Task);
            WriteNameAttribute(e.Task.Name);
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Signals that a task has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public void TaskFinished(object sender, BuildEventArgs e) {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Signals that a message has been logged.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// Only messages with a priority higher or equal to the threshold of 
        /// the logger will actually be output in the build log.
        /// </remarks>
        public void MessageLogged(object sender, BuildEventArgs e) {
            if (e.MessageLevel >= Threshold) {
                string rawMessage = StripFormatting(e.Message.Trim());
                if (IsJustWhiteSpace(rawMessage)) {
                    return;
                }
                
                _xmlWriter.WriteStartElement(Elements.Message);

                // write message level as attribute
                _xmlWriter.WriteAttributeString(Attributes.MessageLevel, e.MessageLevel.ToString(CultureInfo.InvariantCulture));
                
                if (IsValidXml(rawMessage)) {
                    rawMessage = Regex.Replace(rawMessage, @"<\?.*\?>", string.Empty);
                    _xmlWriter.WriteRaw(rawMessage);
                } else {
                    _xmlWriter.WriteCData(StripCData(rawMessage));
                }
                _xmlWriter.WriteEndElement();
                _xmlWriter.Flush();
            }
        }

        #endregion Implementation of IBuildListener

        #region Implementation of IBuildLogger

        /// <summary>
        /// Gets or sets the highest level of message this logger should respond 
        /// to.
        /// </summary>
        /// <value>The highest level of message this logger should respond to.</value>
        /// <remarks>
        /// Only messages with a message level higher than or equal to the given 
        /// level should be written to the log.
        /// </remarks>
        public Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which the logger is 
        /// to send its output.
        /// </summary>
        public TextWriter OutputWriter {
            get { return _writer; }
            set { 
                _writer = value;
                _xmlWriter = new XmlTextWriter(value);
                _xmlWriter.Formatting = Formatting.Indented;
            }
        }

        /// <summary>
        /// Flushes buffered build events or messages to the underlying storage.
        /// </summary>
        public void Flush() {
            _xmlWriter.Flush();
        }

        #endregion Implementation of IBuildLogger

        #region Public Instance Methods

        public string StripFormatting(string message) {
            //looking for zero or more white space from front of line followed by
            //one or more of just about anything between [ and ] followed by a message
            //which we will capture. '    [blah] 
            Regex r = new Regex(@"(?ms)^\s*?\[[\s\w\d]+\](.+)");

            Match m = r.Match(message);
            if (m.Success) {
                return m.Groups[1].Captures[0].Value.Trim();
            }
            return message;
        }

        public bool IsJustWhiteSpace(string message) {
            Regex r = new Regex(@"^\s*$");
            return r.Match(message).Success;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private bool IsValidXml(string message) {
            if (Regex.Match(message, @"^<.*>").Success) {
                // validate xml
                XmlValidatingReader reader = new XmlValidatingReader(message, XmlNodeType.Element, null);

                try { 
                    while (reader.Read()) {
                    } 
                } catch { 
                    return false; 
                } finally { 
                    reader.Close(); 
                }
                return true;
            }
            return false;
        }
        
        private string StripCData(string message) {
            string strippedMessage = Regex.Replace(message, @"<!\[CDATA\[", string.Empty);
            return Regex.Replace(strippedMessage, @"\]\]>", string.Empty);
        }

        private void WriteNameAttribute(string name) {
            _xmlWriter.WriteAttributeString("name", name);
        }

        #endregion Private Instance Methods

        private class Elements {
            public const string BuildResults = "buildresults";
            public const string Message = "message";
            public const string Target = "target";
            public const string Task = "task";
            public const string Status = "status";
        }

        private class Attributes {
            public const string Project = "project";
            public const string MessageLevel = "level";
        }
    }
}
