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

            Project p = new Project(_buildFileName, Level.Info);

            Assertion.AssertNotNull("Property ('nant.version') not defined.", p.Properties["nant.version"]);
            Assertion.AssertNotNull("Property ('nant.location') not defined.", p.Properties["nant.location"]);

            Assertion.AssertEquals(new Uri(_buildFileName), p.Properties["nant.project.buildfile"]);
            Assertion.AssertEquals(TempDirName, p.Properties["nant.project.basedir"]);
            Assertion.AssertEquals("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            Assertion.AssertEquals("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${nant.tasks.fail}.", null));
        }

		[Test]
        public void Test_Initialization_DOMBuildFile() {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            Assertion.AssertNotNull("Property not defined.", p.Properties["nant.version"]);

            Assertion.AssertNull("location of buildfile should not exist!", p.Properties["nant.project.buildfile"]);
            Assertion.AssertNotNull("nant.project.basedir should not be null", p.Properties["nant.project.basedir"]);
            Assertion.AssertEquals(TempDirName, p.Properties["nant.project.basedir"]);
            Assertion.AssertEquals("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            Assertion.AssertEquals("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${nant.tasks.fail}.", null));
        }

		[Test]
        public void Test_OnBuildStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.BuildStarted += new BuildEventHandler(b.BuildStarted);
            p.OnBuildStarted(this, new BuildEventArgs(p));

            Assertion.Assert(b._buildStarted);
        }

		[Test]
        public void Test_OnBuildFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.BuildFinished += new BuildEventHandler(b.BuildFinished);
            p.OnBuildFinished(this, new BuildEventArgs(p));

            Assertion.Assert(b._buildFinished);
        }

		[Test]
        public void Test_OnTargetStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.TargetStarted += new BuildEventHandler(b.TargetStarted);
            p.OnTargetStarted(this, new BuildEventArgs(p));

            Assertion.Assert(b._targetStarted);
        }

		[Test]
        public void Test_OnTargetFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.TargetFinished += new BuildEventHandler(b.TargetFinished);
            p.OnTargetFinished(this, new BuildEventArgs(p));

            Assertion.Assert(b._targetFinished);
        }
        
		[Test]
        public void Test_OnTaskStarted() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.TaskStarted += new BuildEventHandler(b.TaskStarted);
            p.OnTaskStarted(this, new BuildEventArgs(p));

            Assertion.Assert(b._taskStarted);
        }

		[Test]
        public void Test_OnTaskFinished() {
            MockBuildEventListener b = new MockBuildEventListener();

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc, Level.Info);

            p.TaskFinished += new BuildEventHandler(b.TaskFinished);
            p.OnTaskFinished(this, new BuildEventArgs(p));

            Assertion.Assert(b._taskFinished);
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
            Assertion.AssertEquals("ProjectTest", p.Properties["nant.project.name"]);

            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.al"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.attrib"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.call"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.copy"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.delete"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.echo"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.exec"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.fail"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.include"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.mkdir"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.move"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.nant"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.nunit"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.nunit2"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.property"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.script"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.sleep"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.style"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.sysinfo"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.touch"]);
            Assertion.AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.tstamp"]);
        }

        private string FormatBuildFile(string globalTasks, string targetTasks) {
            return string.Format(CultureInfo.InvariantCulture, _format, TempDirName, globalTasks, targetTasks);
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

