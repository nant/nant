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

using NAnt.Core;
using NAnt.Core.Attributes;

namespace Tests.NAnt.Core.Util {

    [TestFixture]
    public class XmlLoggerTest {
        XmlLogger _log; 
        string _tempDir;

        string _format = @"<?xml version='1.0'?>
            <project name='{3}' default='test' basedir='{0}'>
                {1}
                <target name='test'>
                    {2}
                </target>
            </project>";

        [SetUp]
        protected void SetUp() {
            _log = CreateXmlLogger();
            _tempDir = TempDir.Create("NAnt.Tests.XmlLoggerTest");
        }

        [TearDown]
        protected void TearDown() {
            TempDir.Delete(_tempDir);
        }

        [Test]
        public void Test_StripFormatting() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage), "#1");

            formattedMessage = "\t[foo] " + baseMessage;
            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage), "#2");

            formattedMessage = "\t\0[foo] " + baseMessage;
            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage), "#3");

            formattedMessage = "\t\0[foo] \0" + baseMessage + '\0';
            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage), "#4");

            formattedMessage = "\t\t[foo] " + baseMessage;
            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage), "#5");

            string timestamp = "Thursday, August 01, 2002 12:52:54 AM";
            formattedMessage = String.Format(CultureInfo.InvariantCulture, "\t\t\t[tstamp] {0}", timestamp);
            Assert.AreEqual(timestamp, _log.StripFormatting(formattedMessage), "#6");
        }

        [Test]
        public void Test_StripFormattingMultiline() {
            string baseMessage = "this is a typical message." + Environment.NewLine + "Multiline message that is.";
            string formattedMessage = "[foo] " + baseMessage;

            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage));
        }

        [Test]
        public void Test_IsJustWhiteSpace() {
            string message = "";
            Assert.IsTrue(_log.IsJustWhiteSpace(message), "check failed for: {0}", message);

            message = " ";
            Assert.IsTrue(_log.IsJustWhiteSpace(message), "check failed for: {0}", message);

            message = "\0";
            Assert.IsTrue(_log.IsJustWhiteSpace(message), "check failed for: {0}", message);

            message = "        ";
            Assert.IsTrue(_log.IsJustWhiteSpace(message), "check failed for: {0}", message);

            message = "\t\t\t\t";
            Assert.IsTrue(_log.IsJustWhiteSpace(message), "check failed for: {0}", message);

            message = "hello";
            Assert.IsFalse(_log.IsJustWhiteSpace(message), "check should not have failed for: {0}", message);

            message = "hello    ";
            Assert.IsFalse(_log.IsJustWhiteSpace(message), "check should not have failed for: {0}", message);

            message = "        hello";
            Assert.IsFalse(_log.IsJustWhiteSpace(message), "check should not have failed for: {0}", message);

            message = "\t\t\thello";
            Assert.IsFalse(_log.IsJustWhiteSpace(message), "check should not have failed for: {0}", message);
        }

        [Test]
        public void Test_WriteLine() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = formattedMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_Write() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = formattedMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());

            string unformattedMessage = "message:";
            _log = CreateXmlLogger();

            args.Message = unformattedMessage;
            _log.MessageLogged(this, args);
            expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", unformattedMessage);
            Assert.AreEqual(expected, _log.ToString());

            unformattedMessage = "message with no tag in front.";
            _log = CreateXmlLogger();
            args.Message = unformattedMessage;
            _log.MessageLogged(this, args);
            expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", unformattedMessage);
            Assert.AreEqual(expected, _log.ToString());

            unformattedMessage = "BUILD SUCCESSFUL";
            _log = CreateXmlLogger();
            args.Message = unformattedMessage;
            _log.MessageLogged(this, args);
            expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", unformattedMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteStrangeCharacters() {
            string baseMessage = "this message has !@!)$)(&^%^%$$##@@}{[]\"';:<<>/+=-_. in it.";
            string formattedMessage = "[foo] " + baseMessage;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = formattedMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteEmbeddedMathFormulas() {
            string baseMessage = "this message has: x < 20 = y in it.";
            string formattedMessage = "[foo] " + baseMessage;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = formattedMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteTextWithEmbeddedCDATATag() {
            string message = @"some stuff with <xml> <![CDATA[more stuff]]> and more <![CDATA[cdata]]>";
            string expected = @"<message level=""Info""><![CDATA[some stuff with <xml> more stuff and more cdata]]></message>";

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = message;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteXmlWithDeclaration() {
            string message = @"<?xml version=""1.0"" encoding=""utf-16""?><test><a></a></test>";
            string expected = @"<message level=""Info""><test><a></a></test></message>";

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = message;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteXmlWithLeadingWhitespace() {
            string message = @"            <?xml version=""1.0"" encoding=""utf-16""?><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/>";
            string expected = @"<message level=""Info""><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/></message>";

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = message;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]        
        public void Test_WriteEmbeddedXml() {
            string baseMessage = "<a><b><![CDATA[message]]></b></a>";
            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\">{0}</message>", baseMessage);

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = baseMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_WriteEmbeddedMalformedXml() {
            string baseMessage = "<a>malformed<b>";
            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            args.Message = baseMessage;
            args.MessageLevel = Level.Info;
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_BuildStartedAndBuildFinished() {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            Project p = new Project(doc, Level.Info, 0);

            BuildEventArgs args = new BuildEventArgs(p);
            string expected = String.Format(CultureInfo.InvariantCulture, "<buildresults project=\"{0}\" />", "testproject");

            _log.BuildStarted(this, args);
            _log.BuildFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_TargetStartedAndTargetFinished() {
            Target target = new Target();
            target.Name = "foo";

            BuildEventArgs args = new BuildEventArgs(target);
            string expected = String.Format(CultureInfo.InvariantCulture, @"<target name=""{0}"" />", target.Name);

            _log.TargetStarted(this, args);
            _log.TargetFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Test_TaskStartedAndTaskFinished() {
            Task task = new TestTask();

            BuildEventArgs args = new BuildEventArgs(task);
            string expected = String.Format(CultureInfo.InvariantCulture, @"<task name=""{0}"" />", "testtask");

            _log.TaskStarted(this, args);
            _log.TaskFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        private XmlLogger CreateXmlLogger() {
            StringWriter _log = new StringWriter();
            XmlLogger logger = new XmlLogger();
            logger.OutputWriter = _log;
            return logger;
        }

        private string FormatBuildFile(string globalTasks, string targetTasks, string projectName) {
            return String.Format(CultureInfo.InvariantCulture, _format, _tempDir, globalTasks, targetTasks, projectName);
        }

        [TaskName("testtask")]
        private class TestTask : Task {
            protected override void ExecuteTask() {
            }
        }
    }
}
