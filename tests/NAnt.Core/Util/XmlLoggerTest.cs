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
using System.IO;
using System.Xml;
using System.Globalization;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

	[TestFixture]
    public class XmlLoggerTest  {
        XmlLogger _log; 
    	

	[SetUp]
        protected void SetUp() {
            _log = CreateXmlLogger();
        }
	
        [Test]
        public void Test_StripFormatting() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            Assertion.AssertEquals(baseMessage, _log.StripFormatting(formattedMessage));

            formattedMessage = "\t[foo] " + baseMessage;
            Assertion.AssertEquals(baseMessage, _log.StripFormatting(formattedMessage));

            formattedMessage = "\t\t[foo] " + baseMessage;
            Assertion.AssertEquals(baseMessage, _log.StripFormatting(formattedMessage));

            string timestamp = "Thursday, August 01, 2002 12:52:54 AM";
            formattedMessage = String.Format(CultureInfo.InvariantCulture, "\t\t\t[tstamp] {0}", timestamp);
            Assertion.AssertEquals(timestamp, _log.StripFormatting(formattedMessage));
        }

		[Test]
        public void Test_StripFormattingMultiline() {
            string baseMessage = "this is a typical message.\nMultiline message that is.";
            string formattedMessage = "[foo] " + baseMessage;

            Assertion.AssertEquals(baseMessage, _log.StripFormatting(formattedMessage));
        }

		[Test]
        public void Test_IsJustWhiteSpace() {
            string message = "";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check failed for: {0}", message), _log.IsJustWhiteSpace(message));

            message = " ";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check failed for: {0}", message), _log.IsJustWhiteSpace(message));

            message = "        ";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check failed for: {0}", message), _log.IsJustWhiteSpace(message));

            message = "\t\t\t\t";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check failed for: {0}", message), _log.IsJustWhiteSpace(message));

            message = "hello";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check should not have failed for: {0}", message), !_log.IsJustWhiteSpace(message));

            message = "hello    ";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check should not have failed for: {0}", message), !_log.IsJustWhiteSpace(message));

            message = "        hello";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check should not have failed for: {0}", message), !_log.IsJustWhiteSpace(message));

            message = "\t\t\thello";
            Assertion.Assert(String.Format(CultureInfo.InvariantCulture, "check should not have failed for: {0}", message), !_log.IsJustWhiteSpace(message));
        }

		[Test]
        public void Test_WriteLine() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            _log.WriteLine(formattedMessage);

            string expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());

        }

		[Test]
        public void Test_Write() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            _log.Write(formattedMessage);
            string expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());

            string unformattedMessage = "message:";
            _log = CreateXmlLogger();
            _log.Write(unformattedMessage);
            expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", unformattedMessage);
            Assertion.AssertEquals(expected, _log.ToString());

            unformattedMessage = "message with no tag in front.";
            _log = CreateXmlLogger();
            _log.Write(unformattedMessage);
            expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", unformattedMessage);
            Assertion.AssertEquals(expected, _log.ToString());

            unformattedMessage = "BUILD SUCCESSFUL";
            _log = CreateXmlLogger();
            _log.Write(unformattedMessage);
            expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", unformattedMessage);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_WriteStrangeCharacters() {
            string baseMessage = "this message has !@!)$)(&^%^%$$##@@}{[]\"';:<<>/+=-_. in it.";
            string formattedMessage = "[foo] " + baseMessage;

            _log.Write(formattedMessage);

            string expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());
        }
		
		[Test]
        public void Test_WriteEmbeddedMathFormulas() {
            string baseMessage = "this message has: x < 20 = y in it.";
            string formattedMessage = "[foo] " + baseMessage;

            _log.Write(formattedMessage);

            string expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_WriteTextWithEmbeddedCDATATag() {
            string message = @"some stuff with <xml> <![CDATA[more stuff]]> and more <![CDATA[cdata]]>";
            string expected = @"<message><![CDATA[some stuff with <xml> more stuff and more cdata]]></message>";

            _log.Write(message);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_WriteXmlWithDeclaration() {
            string message = @"<?xml version=""1.0"" encoding=""utf-16""?><test><a></a></test>";
            string expected = @"<message><test><a></a></test></message>";

            _log.Write(message);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_WriteXmlWithLeadingWhitespace() {
            string message = @"            <?xml version=""1.0"" encoding=""utf-16""?><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/>";
            string expected = @"<message><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/></message>";

            _log.Write(message);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]        
        public void Test_WriteEmbeddedXml() {
            string baseMessage = "<a><b><![CDATA[message]]></b></a>";
            string expected = String.Format(CultureInfo.InvariantCulture, "<message>{0}</message>", baseMessage);

            _log.Write(baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_WriteEmbeddedMalformedXml() {
            string baseMessage = "<a>malformed<b>";
            string expected = String.Format(CultureInfo.InvariantCulture, "<message><![CDATA[{0}]]></message>", baseMessage);

            _log.Write(baseMessage);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_BuildStartedAndBuildFinished() {
            string name = "foo";
            BuildEventArgs args = new BuildEventArgs(name);
            string expected = String.Format(CultureInfo.InvariantCulture, "<buildresults project=\"{0}\" />", name);

            _log.BuildStarted(this, args);
            _log.BuildFinished(this, args);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_TargetStartedAndTargetFinished() {
            string name = "foo";
            BuildEventArgs args = new BuildEventArgs(name);
            string expected = String.Format(CultureInfo.InvariantCulture, @"<target name=""{0}"" />", name);

            _log.TargetStarted(this, args);
            _log.TargetFinished(this, args);
            Assertion.AssertEquals(expected, _log.ToString());
        }

		[Test]
        public void Test_TaskStartedAndTaskFinished() {
            string name = "foo";
            BuildEventArgs args = new BuildEventArgs(name);
            string expected = String.Format(CultureInfo.InvariantCulture, @"<task name=""{0}"" />", name);

            _log.TaskStarted(this, args);
            _log.TaskFinished(this, args);
            Assertion.AssertEquals(expected, _log.ToString());
        }

        private XmlLogger CreateXmlLogger() {
            StringWriter _log = new StringWriter();
            return new XmlLogger(_log);
        }
    }
}