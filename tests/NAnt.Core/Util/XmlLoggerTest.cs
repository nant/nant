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
// Owen Rogers (orogers@thoughtworks.com | exortech@gmail.com)

using System;
using System.Globalization;
using System.IO;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace Tests.NAnt.Core.Util {
    [TestFixture]
    public class XmlLoggerTest {
        private XmlLogger _log;
        private string _tempDir;

        private string _format = @"<?xml version='1.0'?>
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
        public void StripFormatting() {
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
        public void StripFormattingMultiline() {
            string baseMessage = "this is a typical message." + Environment.NewLine + "Multiline message that is.";
            string formattedMessage = "[foo] " + baseMessage;

            Assert.AreEqual(baseMessage, _log.StripFormatting(formattedMessage));
        }

        [Test]
        public void IsJustWhiteSpace() {
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
        public void WriteLine() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            BuildEventArgs args = CreateBuildEventArgs(formattedMessage, Level.Info);
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void Write() {
            string baseMessage = "this is a typical message.";
            string formattedMessage = "[foo] " + baseMessage;

            BuildEventArgs args = CreateBuildEventArgs(formattedMessage, Level.Info);
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
        public void WriteStrangeCharacters() {
            string baseMessage = "this message has !@!)$)(&^%^%$$##@@}{[]\"';:<<>/+=-_. in it.";
            string formattedMessage = "[foo] " + baseMessage;

            BuildEventArgs args = CreateBuildEventArgs(formattedMessage, Level.Info);
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteEmbeddedMathFormulas() {
            string baseMessage = "this message has: x < 20 = y in it.";
            string formattedMessage = "[foo] " + baseMessage;

            BuildEventArgs args = CreateBuildEventArgs(formattedMessage, Level.Info);
            _log.MessageLogged(this, args);

            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteTextWithEmbeddedCDATATag() {
            string message = @"some stuff with <xml> <![CDATA[more stuff]]> and more <![CDATA[cdata]]>";
            string expected = @"<message level=""Info""><![CDATA[some stuff with <xml> more stuff and more cdata]]></message>";

            BuildEventArgs args = CreateBuildEventArgs(message, Level.Info);
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteXmlWithDeclaration() {
            string message = @"<?xml version=""1.0"" encoding=""utf-16""?><test><a></a></test>";
            string expected = @"<message level=""Info""><test><a></a></test></message>";

            BuildEventArgs args = CreateBuildEventArgs(message, Level.Info);
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteXmlWithLeadingWhitespace() {
            string message = @"            <?xml version=""1.0"" encoding=""utf-16""?><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/>";
            string expected = @"<message level=""Info""><testsuite name=""tw.ccnet.acceptance Tests"" tests=""14"" time=""19.367"" errors=""0"" failures=""0""/></message>";

            BuildEventArgs args = CreateBuildEventArgs(message, Level.Info);
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteEmbeddedXml() {
            string baseMessage = "<a><b><![CDATA[message]]></b></a>";
            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\">{0}</message>", baseMessage);

            BuildEventArgs args = CreateBuildEventArgs(baseMessage, Level.Info);
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void WriteEmbeddedMalformedXml() {
            string baseMessage = "<a>malformed<b>";
            string expected = string.Format(CultureInfo.InvariantCulture, "<message level=\"Info\"><![CDATA[{0}]]></message>", baseMessage);

            BuildEventArgs args = CreateBuildEventArgs(baseMessage, Level.Info);
            _log.MessageLogged(this, args);

            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void BuildStartedAndBuildFinished() {
            string expected = "<buildresults project=\"testproject\"><duration>123</duration></buildresults>";
            _log = CreateXmlLogger(CreateDateTimeProvider(123));

            BuildEventArgs args = new BuildEventArgs(CreateProject());
            _log.BuildStarted(this, args);
            _log.BuildFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void TargetStartedAndTargetFinished() {
            string expected = @"<target name=""foo""><duration>123</duration></target>";
            _log = CreateXmlLogger(CreateDateTimeProvider(123));

            BuildEventArgs args = CreateBuildEventArgsWithTarget("foo");
            _log.TargetStarted(this, args);
            _log.TargetFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        [Test]
        public void TaskStartedAndTaskFinished() {
            string expected = @"<task name=""testtask""><duration>321</duration></task>";
            _log = CreateXmlLogger(CreateDateTimeProvider(321));

            BuildEventArgs args = new BuildEventArgs(new TestTask());
            _log.TaskStarted(this, args);
            _log.TaskFinished(this, args);
            Assert.AreEqual(expected, _log.ToString());
        }

        private MockDateTimeProvider CreateDateTimeProvider(int duration) {
            MockDateTimeProvider mockDateTimeProvider = new MockDateTimeProvider();
            mockDateTimeProvider.SetExpectedNow(new DateTime(2004, 12, 1, 1, 0, 0));
            mockDateTimeProvider.SetExpectedNow(new DateTime(2004, 12, 1, 1, 0, 0, duration));
            return mockDateTimeProvider;
        }

        private XmlLogger CreateXmlLogger() {
            return CreateXmlLogger(new DateTimeProvider());
        }

        private XmlLogger CreateXmlLogger(DateTimeProvider dtProvider) {
            XmlLogger logger = new XmlLogger(new StopWatchStack(dtProvider));
            logger.OutputWriter = new StringWriter();
            return logger;
        }

        private BuildEventArgs CreateBuildEventArgsWithTarget(string targetName) {
            Target target = new Target();
            target.Name = targetName;
            return new BuildEventArgs(target);
        }

        private string FormatBuildFile(string globalTasks, string targetTasks, string projectName) {
            return String.Format(CultureInfo.InvariantCulture, _format, _tempDir, globalTasks, targetTasks, projectName);
        }

        private BuildEventArgs CreateBuildEventArgs(string formattedMessage, Level level) {
            BuildEventArgs args = new BuildEventArgs(new Target());
            args.Message = formattedMessage;
            args.MessageLevel = level;
            return args;
        }

        private Project CreateProject() {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", "", "testproject"));
            return new Project(doc, Level.Info, 0);
        }

        [TaskName("testtask")]
        private class TestTask : Task {
            protected override void ExecuteTask() {}
        }
    }
}