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

// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SourceForge.NAnt {
    /// <summary>Used to wrap log messages in xml  &lt;message/&gt; elements</summary>
    public class XmlLogger : LogListener, IBuildEventConsumer {

        public class Elements {
            public const string BUILD_RESULTS = "buildresults";
            public const string MESSAGE = "message";
        }

        public class Attributes {
            public const string PROJECT = "project";
        }

        private TextWriter _writer = Console.Out;
        private XmlTextWriter _xmlWriter = new XmlTextWriter(Console.Out);

        public XmlLogger() {

        }

        public XmlLogger(TextWriter writer) {
            _writer = writer;
            _xmlWriter = new XmlTextWriter(_writer);
            _xmlWriter.Formatting = Formatting.Indented;

        }

        public string StripFormatting(string message) {
            string result = message;

            //looking for zero or more white space from front of line followed by
            //one or more of just about anything between [ and ] followed by a message
            //which we will capture.
            Regex r = new Regex(@"(?ms)^\s*?\[[\s\w\d]+\](.+)");

            Match m = r.Match(message);

            if(m.Success) {
                result = m.Groups[1].Captures[0].Value.Trim();
            }

            return result;
        }

        public bool IsJustWhiteSpace(string message) {
            Regex r = new Regex(@"^\s*$");

            return r.Match(message).Success;
        }

        #region LogListener Overrides

        public override void Write(string formattedMessage) {
            string rawMessage = StripFormatting(formattedMessage);

            if (IsJustWhiteSpace(rawMessage)) {
                return;
            }

            _xmlWriter.WriteStartElement(Elements.MESSAGE);
            _xmlWriter.WriteCData(rawMessage);
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        public override void WriteLine(string message) {
            Write(message);
        }

        #endregion

        #region IBuildEventConsumer Implementation

        public void BuildStarted(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteStartElement(Elements.BUILD_RESULTS);
            _xmlWriter.WriteAttributeString(Attributes.PROJECT, args.Name);
        }

        public void BuildFinished(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteEndElement();
        }

        public void TargetStarted(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteStartElement(args.Name);
            _xmlWriter.Flush();
        }

        public void TargetFinished(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        public void TaskStarted(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteStartElement(args.Name);
            _xmlWriter.Flush();
        }

        public void TaskFinished(object obj, BuildEventArgs args)
        {
            _xmlWriter.WriteEndElement();
            _xmlWriter.Flush();
        }

        #endregion
    }
}
