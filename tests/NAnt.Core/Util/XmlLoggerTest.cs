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

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    public class XmlLoggerTest : TestCase {

        public XmlLoggerTest(String name) : base(name) {
        }

        public static NUnit.Framework.ITest Suite
        {
            get
            {
                return new NUnit.Framework.TestSuite(typeof(XmlLoggerTest));
            }
        }

        public void Test_StripFormatting() {

            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            XmlLogger log = new XmlLogger();
            AssertEquals(baseMessage, log.StripFormatting(formattedMessage));

            formattedMessage = "\t[foo] " + baseMessage;
            AssertEquals(baseMessage, log.StripFormatting(formattedMessage));

            formattedMessage = "\t\t[foo] " + baseMessage;
            AssertEquals(baseMessage, log.StripFormatting(formattedMessage));

            string timestamp = "Thursday, August 01, 2002 12:52:54 AM";
            formattedMessage = String.Format("\t\t\t[tstamp] {0}", timestamp);
            AssertEquals(timestamp, log.StripFormatting(formattedMessage));
        }

        public void Test_StripFormattingMultiline() {
            string baseMessage = "this is a typical message.\nMultiline message that is.";
            string formattedMessage = "[foo] " + baseMessage;

            XmlLogger log = new XmlLogger();
            AssertEquals(baseMessage, log.StripFormatting(formattedMessage));
        }

        public void Test_IsJustWhiteSpace() {
            string message;
            XmlLogger log = new XmlLogger();

            message = "";
            Assert(String.Format("check failed for: {0}", message), log.IsJustWhiteSpace(message));

            message = " ";
            Assert(String.Format("check failed for: {0}", message), log.IsJustWhiteSpace(message));

            message = "        ";
            Assert(String.Format("check failed for: {0}", message), log.IsJustWhiteSpace(message));

            message = "\t\t\t\t";
            Assert(String.Format("check failed for: {0}", message), log.IsJustWhiteSpace(message));

            message = "hello";
            Assert(String.Format("check should not have failed for: {0}", message), !log.IsJustWhiteSpace(message));

            message = "hello    ";
            Assert(String.Format("check should not have failed for: {0}", message), !log.IsJustWhiteSpace(message));

            message = "        hello";
            Assert(String.Format("check should not have failed for: {0}", message), !log.IsJustWhiteSpace(message));

            message = "\t\t\thello";
            Assert(String.Format("check should not have failed for: {0}", message), !log.IsJustWhiteSpace(message));

        }

        public void Test_WriteLine() {

            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            StringWriter writer = new StringWriter();

            XmlLogger log = new XmlLogger(writer);

            log.WriteLine(formattedMessage);

            string expected = String.Format("<message><![CDATA[{0}]]></message>", baseMessage);
            AssertEquals(expected, writer.ToString());

        }

        public void Test_Write() {

            StringWriter writer;;
            XmlLogger log;
            string expected;

            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;
            writer = new StringWriter();
            log = new XmlLogger(writer);
            log.Write(formattedMessage);
            expected = String.Format("<message><![CDATA[{0}]]></message>", baseMessage);
            AssertEquals(expected, writer.ToString());

            string unformattedMessage = "message:";
            writer = new StringWriter();
            log = new XmlLogger(writer);
            log.Write(unformattedMessage);
            expected = String.Format("<message><![CDATA[{0}]]></message>", unformattedMessage);
            AssertEquals(expected, writer.ToString());

            unformattedMessage = "message with no tag in front.";
            writer = new StringWriter();
            log = new XmlLogger(writer);
            log.Write(unformattedMessage);
            expected = String.Format("<message><![CDATA[{0}]]></message>", unformattedMessage);
            AssertEquals(expected, writer.ToString());

            unformattedMessage = "BUILD SUCCESSFUL";
            writer = new StringWriter();
            log = new XmlLogger(writer);
            log.Write(unformattedMessage);
            expected = String.Format("<message><![CDATA[{0}]]></message>", unformattedMessage);
            AssertEquals(expected, writer.ToString());
        }

        public void Test_WriteStrangeCharacters() {
            string baseMessage = "this message has !@!)$)(&^%^%$$##@@}{[]\"';:<<>/+=-_. in it.";
            string formattedMessage = "[foo] " + baseMessage;

            StringWriter writer = new StringWriter();

            XmlLogger log = new XmlLogger(writer);

            log.Write(formattedMessage);

            string expected = String.Format("<message><![CDATA[{0}]]></message>", baseMessage);
            AssertEquals(expected, writer.ToString());
        }

        public void Test_WriteEmbeddedMathFormulas() {
            string baseMessage = "this message has: x < 20 = y in it.";
            string formattedMessage = "[foo] " + baseMessage;

            StringWriter writer = new StringWriter();

            XmlLogger log = new XmlLogger(writer);

            log.Write(formattedMessage);

            string expected = String.Format("<message><![CDATA[{0}]]></message>", baseMessage);
            AssertEquals(expected, writer.ToString());
        }

        public void Test_BuildStartedAndBuildFinished() {

            string name = "foo";
            BuildEventArgs args = new BuildEventArgs(name);

            StringWriter writer = new StringWriter();
            XmlLogger log = new XmlLogger(writer);
            string expected = String.Format("<buildresults project=\"{0}\" />", name);

            log.BuildStarted(this, args);
            log.BuildFinished(this, args);
            AssertEquals(expected, writer.ToString());
        }

        public void Test_TargetStartedAndTargetFinished() {

            string name = "foo";

            BuildEventArgs args = new BuildEventArgs(name);

            string expected = String.Format("<{0} />", name);

            StringWriter writer = new StringWriter();
            XmlLogger log = new XmlLogger(writer);

            log.TargetStarted(this, args);
            log.TargetFinished(this, args);
            AssertEquals(expected, writer.ToString());
        }

        public void Test_TaskStartedAndTaskFinished() {

            string name = "foo";

            BuildEventArgs args = new BuildEventArgs(name);

            string expected = String.Format("<{0} />", name);

            StringWriter writer = new StringWriter();
            XmlLogger log = new XmlLogger(writer);

            log.TaskStarted(this, args);
            log.TaskFinished(this, args);
            AssertEquals(expected, writer.ToString());
        }
    }
}