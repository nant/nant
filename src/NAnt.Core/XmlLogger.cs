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

// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace SourceForge.NAnt {
    /// <summary>
    /// Used to wrap log messages in xml &lt;message/&gt; elements.
    /// </summary>
    public class XmlLogger : LogListener, IBuildEventConsumer {
        #region Private Instance Fields

        private TextWriter _writer = Console.Out;
        private XmlTextWriter _xmlWriter = new XmlTextWriter(Console.Out);

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger" /> class.
        /// </summary>
        public XmlLogger() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlLogger" /> class
        /// with the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the build output should be written.</param>
        public XmlLogger(TextWriter writer) {
            _writer = writer;
            _xmlWriter = new XmlTextWriter(_writer);
            _xmlWriter.Formatting = Formatting.Indented;
        }

        #endregion Public Instance Constructors

        #region Override implementation of LogListener

        public override void Write(string formattedMessage) {
            WriteLine(formattedMessage, null);
        }

        public override void WriteLine(string message) {
            WriteLine(message, null);
        }
        
        public override void WriteLine(string message, string messageType) {
            string rawMessage = StripFormatting(message.Trim());
            if (IsJustWhiteSpace(rawMessage)) {
                return;
            }
            
            _xmlWriter.WriteStartElement(Elements.Message);

            if (messageType != null && messageType.Length != 0) {
                _xmlWriter.WriteAttributeString(Attributes.MessageType, messageType);
            }
            
            if (IsValidXml(rawMessage)) {
                rawMessage = Regex.Replace(rawMessage, @"<\?.*\?>", string.Empty);
                _xmlWriter.WriteRaw(rawMessage);
            } else {
                _xmlWriter.WriteCData(StripCData(rawMessage));
            }
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        public override void Flush() {
            _writer.Flush();
        }

        #endregion Override implementation of LogListener

        #region Override implementation of Object

        /// <summary>
        /// Returns the contents of log captured.
        /// </summary>
        public override string ToString() {
            return _writer.ToString();
        }

        #endregion Override implementation of Object

        #region Implementation of IBuildEventConsumer

        public void BuildStarted(object obj, BuildEventArgs args) {
            _xmlWriter.WriteStartElement(Elements.BuildResults);
            _xmlWriter.WriteAttributeString(Attributes.Project, args.Name);
        }

        public void BuildFinished(object obj, BuildEventArgs args) {
            _xmlWriter.WriteEndElement();
        }

        public void TargetStarted(object obj, BuildEventArgs args) {
            _xmlWriter.WriteStartElement(Elements.Target);
            WriteNameAttribute(args.Name);
            _xmlWriter.Flush();
        }

        public void TargetFinished(object obj, BuildEventArgs args) {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        public void TaskStarted(object obj, BuildEventArgs args) {
            _xmlWriter.WriteStartElement(Elements.Task);
            WriteNameAttribute(args.Name);
            _xmlWriter.Flush();
        }

        public void TaskFinished(object obj, BuildEventArgs args) {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        #endregion Implementation of IBuildEventConsumer

        #region Public Instance Methods

        public string StripFormatting(string message) {
            //looking for zero or more white space from front of line followed by
            //one or more of just about anything between [ and ] followed by a message
            //which we will capture. '    [blah] 
            Regex r = new Regex(@"(?ms)^\s*?\[[\s\w\d]+\](.+)");

            Match m = r.Match(message);
            if(m.Success) {
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
                } catch (Exception) { 
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
            public const string MessageType = "type";
        }
    }
}
