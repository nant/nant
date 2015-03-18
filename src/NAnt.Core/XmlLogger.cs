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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Used to wrap log messages in xml &lt;message/&gt; elements.
    /// </summary>
    [Serializable()]
    public class XmlLogger : IBuildLogger, ISerializable {
        private readonly StopWatchStack _stopWatchStack;

        #region Private Instance Fields

        private TextWriter _outputWriter;
        private StringWriter _buffer = new StringWriter();
        private Level _threshold = Level.Info;

        [NonSerialized()]
        private XmlTextWriter _xmlWriter;

        /// <summary>
        /// Holds the stack of currently executing projects.
        /// </summary>
        private Stack _projectStack = new Stack();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger" /> class.
        /// </summary>
        public XmlLogger() : this(new StopWatchStack(new DateTimeProvider())) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger"/> class.
        /// </summary>
        /// <param name="stopWatchStack">The stop watch stack.</param>
        public XmlLogger(StopWatchStack stopWatchStack) {
            _xmlWriter = new XmlTextWriter(_buffer);
            _stopWatchStack = stopWatchStack;
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
            _outputWriter = info.GetValue("OutputWriter", typeof (TextWriter)) as TextWriter;
            _buffer = info.GetValue("Buffer", typeof (StringWriter)) as StringWriter;
            _threshold = (Level) info.GetValue("Threshold", typeof (Level));
            _xmlWriter = new XmlTextWriter(_buffer);
            _projectStack = (Stack) info.GetValue("ProjectStack", typeof (Stack));
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
            info.AddValue("OutputWriter", _outputWriter);
            info.AddValue("Buffer", _buffer);
            info.AddValue("Threshold", _threshold);
            info.AddValue("ProjectStack", _projectStack);
        }

        #endregion Implementation of ISerializable

        #region Override implementation of Object

        /// <summary>
        /// Returns the contents of log captured.
        /// </summary>
        public override string ToString() {
            return _buffer.ToString();
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
            lock (_xmlWriter) {
                _stopWatchStack.PushStart();
                _xmlWriter.WriteStartElement(Elements.BuildResults);
                _xmlWriter.WriteAttributeString(Attributes.Project, e.Project.ProjectName);
            }
            // add an item to the project stack
            _projectStack.Push(null);
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
            lock (_xmlWriter) {
                if (e.Exception != null) {
                    _xmlWriter.WriteStartElement("failure");
                    WriteErrorNode(e.Exception);
                    _xmlWriter.WriteEndElement();
                }

                // output total build duration
                WriteDuration();

                // close buildresults node
                _xmlWriter.WriteEndElement();
                _xmlWriter.Flush();
            }

            // remove an item from the project stack
            _projectStack.Pop();

            // check if there are still nested projects executing
            if (_projectStack.Count != 0) {
                // do not yet persist build results, as the main project is 
                // not finished yet
                return;
            }

            try {
                // write results to file
                if (OutputWriter != null) {
                    OutputWriter.Write(_buffer.ToString());
                    OutputWriter.Flush();
                }
                else { // Xmlogger is used as BuildListener
                    string outFileName = e.Project.Properties["XmlLogger.file"];
                    if (outFileName == null) {
                        outFileName = "log.xml";
                    }
                    // convert to full path relative to project base directory
                    outFileName = e.Project.GetFullPath(outFileName);
                    // write build log to file
                    using (StreamWriter writer = new StreamWriter(new FileStream(outFileName, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8)) {
                        writer.Write(_buffer.ToString());
                    }
                }
            }
            catch (Exception ex) {
                throw new BuildException("Unable to write to log file.", ex);
            }
        }

        /// <summary>
        /// Signals that a target has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public void TargetStarted(object sender, BuildEventArgs e) {
            lock (_xmlWriter) {
                _stopWatchStack.PushStart();
                _xmlWriter.WriteStartElement(Elements.Target);
                WriteNameAttribute(e.Target.Name);
                _xmlWriter.Flush();
            }
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
            lock (_xmlWriter) {
                // output total target duration
                WriteDuration();
                // close target element
                _xmlWriter.WriteEndElement();
                _xmlWriter.Flush();
            }
        }

        /// <summary>
        /// Signals that a task has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public void TaskStarted(object sender, BuildEventArgs e) {
            lock (_xmlWriter) {
                _stopWatchStack.PushStart();
                _xmlWriter.WriteStartElement(Elements.Task);
                WriteNameAttribute(e.Task.Name);
                _xmlWriter.Flush();
            }
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
            lock (_xmlWriter) {
                // output total target duration
                WriteDuration();
                // close task element
                _xmlWriter.WriteEndElement();
                _xmlWriter.Flush();
            }
        }

        private void WriteDuration() {
            _xmlWriter.WriteElementString("duration", XmlConvert.ToString(_stopWatchStack.PopStop().TotalMilliseconds));
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

                lock (_xmlWriter) {
                    _xmlWriter.WriteStartElement(Elements.Message);

                    // write message level as attribute
                    _xmlWriter.WriteAttributeString(Attributes.MessageLevel, e.MessageLevel.ToString(CultureInfo.InvariantCulture));

                    if (IsValidXml(rawMessage)) {
                        rawMessage = Regex.Replace(rawMessage, @"<\?.*\?>", string.Empty);
                        _xmlWriter.WriteRaw(rawMessage);
                    }
                    else {
                        _xmlWriter.WriteCData(StripCData(rawMessage));
                    }
                    _xmlWriter.WriteEndElement();
                    _xmlWriter.Flush();
                }
            }
        }

        #endregion Implementation of IBuildListener

        #region Implementation of IBuildLogger

        /// <summary>
        /// Gets or sets the highest level of message this logger should respond 
        /// to.
        /// </summary>
        /// <value>
        /// The highest level of message this logger should respond to.
        /// </value>
        /// <remarks>
        /// Only messages with a message level higher than or equal to the given 
        /// level should be written to the log.
        /// </remarks>
        public Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to produce emacs (and other
        /// editor) friendly output.
        /// </summary>
        /// <value>
        /// <see langword="false" /> as it has no meaning in XML format.
        /// </value>
        public virtual bool EmacsMode {
            get { return false; }
            set {}
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which the logger is 
        /// to send its output.
        /// </summary>
        public TextWriter OutputWriter {
            get { return _outputWriter; }
            set { _outputWriter = value; }
        }

        /// <summary>
        /// Flushes buffered build events or messages to the underlying storage.
        /// </summary>
        public void Flush() {
            lock (_xmlWriter) {
                _xmlWriter.Flush();
            }
        }

        #endregion Implementation of IBuildLogger

        #region Public Instance Methods

        public string StripFormatting(string message) {
            // will hold the message stripped from whitespace and null characters
            string strippedMessage;

            // looking for whitespace or null characters from front of line 
            // followed by one or more of just about anything between [ and ] 
            // followed by a message which we will capture. eg. '    [blah] 
            Regex r = new Regex(@"(?ms)^[\s\0]*?\[[\s\w\d]+\](.+)");

            Match m = r.Match(message);
            if (m.Success) {
                strippedMessage = m.Groups[1].Captures[0].Value;
                strippedMessage = strippedMessage.Replace("\0", string.Empty);
                strippedMessage = strippedMessage.Trim();
            }
            else {
                strippedMessage = message.Replace("\0", string.Empty);
            }

            return strippedMessage;
        }

        public bool IsJustWhiteSpace(string message) {
            Regex r = new Regex(@"^[\s\0]*$");
            return r.Match(message).Success;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void WriteErrorNode(Exception exception) {
            // this method assumes that a synchronization
            // lock on _xmlWriter is already held
            if (exception == null) {
                // build success
                return;
            }
            else {
                BuildException buildException = exception as BuildException;

                if (buildException != null) {
                    // start build error node
                    _xmlWriter.WriteStartElement("builderror");
                }
                else {
                    // start build error node
                    _xmlWriter.WriteStartElement("internalerror");
                }

                // write exception type
                _xmlWriter.WriteElementString("type", exception.GetType().FullName);

                // write location for build exceptions
                if (buildException != null) {
                    // write raw exception message
                    if (buildException.RawMessage != null) {
                        _xmlWriter.WriteStartElement("message");
                        _xmlWriter.WriteCData(StripCData(buildException.RawMessage));
                        _xmlWriter.WriteEndElement();
                    }

                    if (buildException.Location != null) {
                        if (!String.IsNullOrEmpty(buildException.Location.ToString())) {
                            _xmlWriter.WriteStartElement("location");
                            _xmlWriter.WriteElementString("filename", buildException.Location.FileName);
                            _xmlWriter.WriteElementString("linenumber",
                                buildException.Location.LineNumber.ToString(CultureInfo.InvariantCulture));
                            _xmlWriter.WriteElementString("columnnumber",
                                buildException.Location.ColumnNumber.ToString(CultureInfo.InvariantCulture));
                            _xmlWriter.WriteEndElement();
                        }
                    }
                }
                else {
                    // write exception message
                    if (exception.Message != null) {
                        _xmlWriter.WriteStartElement("message");
                        _xmlWriter.WriteCData(StripCData(exception.Message));
                        _xmlWriter.WriteEndElement();
                    }
                }

                // write stacktrace of exception to build log
                _xmlWriter.WriteStartElement("stacktrace");
                _xmlWriter.WriteCData(exception.StackTrace);
                _xmlWriter.WriteEndElement();

                // write information about inner exception
                WriteErrorNode(exception.InnerException);

                // close failure node
                _xmlWriter.WriteEndElement();
            }
        }

        private bool IsValidXml(string message) {
            if (Regex.Match(message, @"^<.*>").Success) {
                XmlValidatingReader reader = null;

                try {
                    // validate xml
                    reader = new XmlValidatingReader(message, 
                        XmlNodeType.Document, null);

                    while (reader.Read()) {
                    }

                    // the xml is valid
                    return true;
                } catch {
                    return false;
                } finally {
                    if (reader != null) {
                        reader.Close();
                    }
                }
            }
            return false;
        }

        private string StripCData(string message) {
            string strippedMessage = Regex.Replace(message, @"<!\[CDATA\[", string.Empty);
            return Regex.Replace(strippedMessage, @"\]\]>", string.Empty);
        }

        private void WriteNameAttribute(string name) {
            // this method assumes that a synchronization
            // lock on _xmlWriter is already held
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