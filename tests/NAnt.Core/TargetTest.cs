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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class TargetTest : BuildTestBase {
        #region Private Static Fields

        private const string BuildFragment = @"
            <project default='{0}'>
                <target name='Target1' depends='Target2 Target3'>
                    <echo message='Target1 executed'/>
                </target>
                <target name='Target2' if='{1}'>
                    <echo message='Target2 executed'/>
                </target>
                <target name='Target3' unless='{2}' depends='{3}'>
                    <echo message='Target3 executed'/>
                </target>
            </project>";

        private const string BuildFragment1 = @"
            <project>
                <target name='Target'/>
                <target name='*'/>
                </project>";

        private const string BuildFragment2 = @"
            <project>
                <target name='Init' />
                <target name='Release' depends='Init'/>
                <target name='Build' depends='Release'/>
            </project>";

        #endregion Private Static Fields

        #region Public Instance Methods

        [Test]
        public void Test_Normal() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "false", "true", string.Empty), listener);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target1"), "Target1 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target2"), "Target2 should not have executed." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target3"), "Target3 should not have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_If() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "true", "true", string.Empty), listener);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target1"), "Target1 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target2"), "Target2 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target3"), "Target3 should not have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_Unless() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "false", "false", string.Empty), listener);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target1"), "Target1 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target2"), "Target2 should not have executed." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target3"), "Target3 should have executed once." + Environment.NewLine + result);
        }

        [Test]
        public void Test_Depends() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "true", "false", "Target2"), listener);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target1"), "Target1 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target2"), "Target2 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target3"), "Target3 should have executed once." + Environment.NewLine + result);
        }

        [Test]
        public void Test_Depends2() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "true", "false", string.Empty), listener);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target1"), "Target1 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target2"), "Target2 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target3"), "Target3 should have executed once." + Environment.NewLine + result);
        }

        /// <summary>
        /// Ensures dependencies of a target are executed before the condition
        /// is checked. Meaning, dependencies are executed even if the target
        /// itself should not be executed (because of condition that is not met).
        /// </summary>
        [Test]
        public void Test_Depends3() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target3", "true", "true", "Target2"), listener);

            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target1"), "Target1 should not have executed." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Target2"), "Target2 should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target3"), "Target3 should not have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_Wild() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            Project project = CreateFilebasedProject(BuildFragment1);
            //use Project.AttachBuildListeners to attach.
            IBuildListener[] listners = {listener};
            project.AttachBuildListeners(new BuildListenerCollection(listners));

            //add targets like they are added from the command line.
            project.BuildTargets.Add("WildTarget");

            string result = ExecuteProject(project);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("WildTarget"), "WildTarget should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(0, listener.GetTargetExecutionCount("Target"), "Target should not have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_CommandLineTargets() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            Project project = CreateFilebasedProject(BuildFragment2);

            //use Project.AttachBuildListeners to attach.
            IBuildListener[] listners = {listener};
            project.AttachBuildListeners(new BuildListenerCollection(listners));
             
            //add targets like they are added from the command line.
            project.BuildTargets.Add("Release");
            project.BuildTargets.Add("Build");

            string result = ExecuteProject(project);

            Assert.AreEqual(1, listener.GetTargetExecutionCount("Init"), "'Init' target should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Release"), "'Release' target should have executed once." + Environment.NewLine + result);
            Assert.AreEqual(1, listener.GetTargetExecutionCount("Build"), "'Build' target should have executed once." + Environment.NewLine + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_CircularDependency() {
            // run the build with Target1 dependent on Target3 and vice versa
            RunBuild(FormatBuildFile("Target1", "true", "false", "Target1"));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_UnknowDependentTarget() {
            // run the build with an unknown dependent target
            RunBuild(FormatBuildFile("Target1", "true", "false", "Unknown"));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_UnknowDefaultTarget() {
            // run the build with an unknown default target
            RunBuild(FormatBuildFile("Unknown", "true", "false", string.Empty));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string defaultTarget, string a, string b, string target3Depends) {
            return string.Format(CultureInfo.InvariantCulture, BuildFragment, 
                defaultTarget, a, b, target3Depends);
        }

        #endregion Private Instance Methods
    }
}
