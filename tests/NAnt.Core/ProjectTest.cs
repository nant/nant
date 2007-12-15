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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.IO;
using System.Globalization;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    /// <summary>
    /// Checks if project is initialized correctly. Checks the following props:
    /// nant.task.*
    /// nant.project.name
    /// nant.project.buildfile
    /// nant.version (not null)
    /// nant.location (not null && == projectAssembly.Location path)
    /// nant.basedir
    /// nant.default
    /// nant.filename
    /// </summary>
    [TestFixture]
    public class ProjectTest : BuildTestBase {
        #region Private Instance Fields

        private string _format = @"<?xml version='1.0'?>
            <project name='ProjectTest' default='test' basedir='{0}'>
                {1}
                <target name='test'>
                    {2}
                </target>
            </project>";

        private string _buildFileName;

        #endregion Private Instance Fields

        #region Public Instance Methods

        [Test]
        public void Test_Initialization_FSBuildFile() {
            // create the build file in the temp folder
            TempFile.CreateWithContents(FormatBuildFile("", ""), _buildFileName);

            Project p = new Project(_buildFileName, Level.Error, 0);

            Assert.IsNotNull(p.Properties["nant.version"], "Property ('nant.version') not defined.");
            Assert.IsNotNull(p.Properties["nant.location"], "Property ('nant.location') not defined.");

            Assert.AreEqual(new Uri(_buildFileName), p.Properties["nant.project.buildfile"]);
            Assert.AreEqual(TempDirName, p.Properties["nant.project.basedir"]);
            Assert.AreEqual("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            Assert.AreEqual("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${task::exists('fail')}.", null));
        }

        [Test]
        public void Test_Initialization_DOMBuildFile() {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Error, 0);

            Assert.IsNotNull(p.Properties["nant.version"], "Property not defined.");
            Assert.IsNull(p.Properties["nant.project.buildfile"], "location of buildfile should not exist!");
            Assert.IsNotNull(p.Properties["nant.project.basedir"], "nant.project.basedir should not be null");
            Assert.AreEqual(TempDirName, p.Properties["nant.project.basedir"]);
            Assert.AreEqual("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            Assert.AreEqual("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${task::exists('fail')}.", null));
        }

        [Test]
        public void Test_OnBuildStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.BuildStarted += new BuildEventHandler(b.BuildStarted);
            p.OnBuildStarted(this, new BuildEventArgs(p));

            Assert.IsTrue(b._buildStarted);
        }

        [Test]
        public void Test_OnBuildFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.BuildFinished += new BuildEventHandler(b.BuildFinished);
            p.OnBuildStarted(this, new BuildEventArgs(p));
            p.OnBuildFinished(this, new BuildEventArgs(p));

            Assert.IsTrue(b._buildFinished);
        }

        [Test]
        public void Test_OnTargetStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.TargetStarted += new BuildEventHandler(b.TargetStarted);
            p.OnTargetStarted(this, new BuildEventArgs(p));

            Assert.IsTrue(b._targetStarted);
        }

        [Test]
        public void Test_OnTargetFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.TargetFinished += new BuildEventHandler(b.TargetFinished);
            p.OnTargetFinished(this, new BuildEventArgs(p));

            Assert.IsTrue(b._targetFinished);
        }

        [Test]
        public void Test_OnTaskStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.TaskStarted += new BuildEventHandler(b.TaskStarted);
            p.OnTaskStarted(this, new BuildEventArgs(p));

            Assert.IsTrue(b._taskStarted);
        }

        [Test]
        public void Test_OnTaskFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info, 0);

            p.TaskFinished += new BuildEventHandler(b.TaskFinished);
            p.OnTaskFinished(this, new BuildEventArgs(p));

            Assert.IsTrue(b._taskFinished);
        }

        [Test] // bug #1556326 
        public void Remove_Readonly_Property () {
            Project p = CreateFilebasedProject("<project />");
            p.Properties.AddReadOnly ("test", "value1");
            Assert.IsTrue (p.Properties.IsReadOnlyProperty ("test"), "#1");
            Assert.IsTrue (p.Properties.Contains ("test"), "#2");
            p.Properties.Remove ("test");
            Assert.IsFalse (p.Properties.IsReadOnlyProperty ("test"), "#3");
            Assert.IsFalse (p.Properties.Contains ("test"), "#4");
            p.Properties.Add ("test", "value2");
            Assert.IsFalse (p.Properties.IsReadOnlyProperty ("test"), "#5");
            Assert.IsTrue (p.Properties.Contains ("test"), "#6");
        }

        [Test]
        public void TargetFramework() {
            Project p = CreateEmptyProject();

            FrameworkInfo tf = p.TargetFramework;
            Assert.IsNotNull (tf, "#1");
            Assert.IsNotNull(tf.ClrVersion, "#2");
            Assert.IsNotNull(tf.Description, "#3");
            Assert.IsNotNull(tf.Family, "#4");
            Assert.IsNotNull(tf.FrameworkAssemblyDirectory, "#5");
            Assert.IsNotNull(tf.FrameworkDirectory, "#6");
            Assert.IsTrue(tf.IsValid, "#7");
            Assert.IsNotNull(tf.Name, "#8");
            Assert.IsNotNull(tf.Project, "#9");
            Assert.AreNotSame(p, tf.Project, "#10");
            Assert.IsNotNull(tf.TaskAssemblies, "#11");
            Assert.IsNotNull(tf.Version, "#12");
        }

        [Test]
        public void TargetFramework_Invalid () {
            FrameworkInfo invalid = null;

            Project p = CreateEmptyProject();
            foreach (FrameworkInfo framework in p.Frameworks) {
                if (!framework.IsValid) {
                    invalid = framework;
                    break;
                }
            }

            if (invalid == null) {
                Assert.Ignore("Tests requires at least one invalid framework.");
            }

            FrameworkInfo original = p.TargetFramework;

            try {
                p.TargetFramework = invalid;
                Assert.Fail ("#A1");
            } catch (BuildException ex) {
                Assert.IsNotNull(ex.InnerException, "#A2");
                Assert.AreSame(original, p.TargetFramework, "#A3");
            }

            try {
                p.TargetFramework = invalid;
                Assert.Fail ("#B1");
            } catch (BuildException ex) {
                Assert.IsNotNull(ex.InnerException, "#B2");
                Assert.AreSame(original, p.TargetFramework, "#B3");
            }
        }

        [Test]
        public void TargetFramework_Null() {
            Project p = CreateEmptyProject();
            try {
                p.TargetFramework = null;
                Assert.Fail("#1");
            } catch (ArgumentNullException ex) {
                Assert.AreEqual(typeof(ArgumentNullException), ex.GetType(), "#2");
                Assert.IsNull(ex.InnerException, "#3");
                Assert.IsNotNull(ex.Message, "#4");
                Assert.IsNotNull(ex.ParamName, "#5");
                Assert.AreEqual("value", ex.ParamName, "#6");
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _buildFileName = Path.Combine(TempDirName, "test.build");
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods
        
        private void CheckCommon(Project p) {
            Assert.AreEqual("ProjectTest", p.Properties["nant.project.name"], "#1");
            Assert.IsTrue(TaskExists(p, "al"), "#2");
            Assert.IsTrue(TaskExists(p, "attrib"), "#3");
            Assert.IsTrue(TaskExists(p, "call"), "#4");
            Assert.IsTrue(TaskExists(p, "copy"), "#5");
            Assert.IsTrue(TaskExists(p, "delete"), "#6");
            Assert.IsTrue(TaskExists(p, "echo"), "#7");
            Assert.IsTrue(TaskExists(p, "exec"), "#8");
            Assert.IsTrue(TaskExists(p, "fail"), "#9");
            Assert.IsTrue(TaskExists(p, "include"), "#10");
            Assert.IsTrue(TaskExists(p, "mkdir"), "#11");
            Assert.IsTrue(TaskExists(p, "move"), "#12");
            Assert.IsTrue(TaskExists(p, "nant"), "#13");
            Assert.IsTrue(TaskExists(p, "nunit2"), "#14");
            Assert.IsTrue(TaskExists(p, "property"), "#15");
            Assert.IsTrue(TaskExists(p, "sleep"), "#16");
            Assert.IsTrue(TaskExists(p, "style"), "#17");
            Assert.IsTrue(TaskExists(p, "sysinfo"), "#18");
            Assert.IsTrue(TaskExists(p, "touch"), "#19");
            Assert.IsTrue(TaskExists(p, "tstamp"), "#20");
        }

        private string FormatBuildFile(string globalTasks, string targetTasks) {
            return string.Format(CultureInfo.InvariantCulture, _format, TempDirName, globalTasks, targetTasks);
        }

        private bool TaskExists (Project p, string taskName) {
            string val = p.ExpandProperties("${task::exists('" + taskName + "')}", 
                Location.UnknownLocation);
            return val == Boolean.TrueString;
        }

        #endregion Private Instance Methods

        class MockBuildEventListener : IBuildListener {
            public bool _buildStarted = false;
            public bool _buildFinished = false;
            public bool _targetStarted = false;
            public bool _targetFinished = false;
            public bool _taskStarted = false;
            public bool _taskFinished = false;
            public bool _messageLogged = false;

            public void BuildStarted(object sender, BuildEventArgs e) {
                _buildStarted = true;
            }

            public void BuildFinished(object sender, BuildEventArgs e) {
                _buildFinished = true;
            }

            public void TargetStarted(object sender, BuildEventArgs e) {
                _targetStarted = true;
            }

            public void TargetFinished(object sender, BuildEventArgs e) {
                _targetFinished = true;
            }

            public void TaskStarted(object sender, BuildEventArgs e) {
                _taskStarted = true;
            }

            public void TaskFinished(object sender, BuildEventArgs e) {
                _taskFinished = true;
            }

            public void MessageLogged(object sender, BuildEventArgs e) {
                _messageLogged = true;
            }
        }
    }
}

