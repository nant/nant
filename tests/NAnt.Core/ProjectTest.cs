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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.IO;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    /// <summary>
    /// Tests project is initialized correctly. Checks the following props:
    /// nant.task.*
    /// nant.project.name
    /// nant.project.buildfile
    /// nant.version (not null)
    /// nant.location (not null && == projectAssembly.Location path)
    /// nant.basedir
    /// nant.default
    /// nant.filename
    /// </summary>
    public class ProjectTest : BuildTestBase {

        string _format = @"<?xml version='1.0'?>
            <project name='ProjectTest' default='test' basedir='{0}'>
                {1}
                <target name='test'>
                    {2}
                </target>
            </project>";

        string _buildFileName;

        public ProjectTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
            _buildFileName = Path.Combine(TempDirName, "test.build");
        }

        public void Test_Initialization_FSBuildFile() {
            // create the build file in the temp folder
               TempFile.CreateWithContents(FormatBuildFile("", ""), _buildFileName);

            Project p = new Project(_buildFileName);

            AssertNotNull("Property ('nant.version') not defined.", p.Properties["nant.version"]);
            AssertNotNull("Property ('nant.location') not defined.", p.Properties["nant.location"]);

            AssertEquals(new Uri(_buildFileName), p.Properties["nant.project.buildfile"]);
            AssertEquals(TempDirName, p.Properties["nant.project.basedir"]);
            AssertEquals("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            AssertEquals("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${nant.tasks.fail}."));
        }

        public void Test_Initialization_DOMBuildFile() {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(FormatBuildFile("", ""));
            Project p = new Project(doc);

            AssertNotNull("Property not defined.", p.Properties["nant.version"]);

            AssertNull("location of buildfile should not exist!", p.Properties["nant.project.buildfile"]);
            AssertNotNull("nant.project.basedir should not be null", p.Properties["nant.project.basedir"]);
            AssertEquals(TempDirName, p.Properties["nant.project.basedir"]);
            AssertEquals("test", p.Properties["nant.project.default"]);

            CheckCommon(p);

            AssertEquals("The value is " + Boolean.TrueString + ".", p.ExpandProperties("The value is ${nant.tasks.fail}."));
        }
        private void CheckCommon(Project p) {
            AssertEquals("ProjectTest", p.Properties["nant.project.name"]);


            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.al"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.attrib"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.call"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.copy"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.delete"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.echo"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.exec"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.fail"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.include"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.mkdir"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.move"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.nant"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.nunit"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.property"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.script"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.sleep"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.style"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.sysinfo"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.touch"]);
            AssertEquals(Boolean.TrueString, p.Properties["nant.tasks.tstamp"]);
        }

        private string FormatBuildFile(string globalTasks, string targetTasks) {
            return String.Format(_format, TempDirName, globalTasks, targetTasks);
        }

        class MockBuildEventConsumer : IBuildEventConsumer {
            public bool _buildStarted = false;
            public bool _buildFinished = false;
            public bool _targetStarted = false;
            public bool _targetFinished = false;
            public bool _taskStarted = false;
            public bool _taskFinished = false;

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
        }

        public void Test_OnBuildStarted() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.BuildStarted += new BuildEventHandler(b.BuildStarted);
            Project.OnBuildStarted(this, new BuildEventArgs("notused"));

            Assert(b._buildStarted);
        }

        public void Test_OnBuildFinished() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.BuildFinished += new BuildEventHandler(b.BuildFinished);
            Project.OnBuildFinished(this, new BuildEventArgs("notused"));

            Assert(b._buildFinished);
        }

        public void Test_OnTargetStarted() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.TargetStarted += new BuildEventHandler(b.TargetStarted);
            Project.OnTargetStarted(this, new BuildEventArgs("notused"));

            Assert(b._targetStarted);
        }

        public void Test_OnTargetFinished() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.TargetFinished += new BuildEventHandler(b.TargetFinished);
            Project.OnTargetFinished(this, new BuildEventArgs("notused"));

            Assert(b._targetFinished);
        }

        public void Test_OnTaskStarted() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.TaskStarted += new BuildEventHandler(b.TaskStarted);
            Project.OnTaskStarted(this, new BuildEventArgs("notused"));

            Assert(b._taskStarted);
        }

        public void Test_OnTaskFinished() {
            MockBuildEventConsumer b = new MockBuildEventConsumer();

            Project.TaskFinished += new BuildEventHandler(b.TaskFinished);
            Project.OnTaskFinished(this, new BuildEventArgs("notused"));

            Assert(b._taskFinished);
        }
    }
}
