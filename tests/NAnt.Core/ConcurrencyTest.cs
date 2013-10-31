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
// Dmitry Kostenko (codeforsmile@gmail.com)

using System;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class ConcurrencyTest : BuildTestBase {
        #region Private Static Fields

        private const string TwoIndependentTargets = @"
            <project default='Target1'>
                <target name='Target1' depends='Target2 Target3' />
                <target name='Target2'>
                    <sleep seconds='1' />
                </target>
                <target name='Target3'>
                    <sleep seconds='1' />
                </target>
            </project>";

        private const string DependentTargets = @"
            <project default='Target1'>
                <target name='Target1' depends='Target2 Target3' />
                <target name='Target2' depends='Target3'>
                    <sleep seconds='1' />
                </target>
                <target name='Target3'>
                    <sleep seconds='1' />
                </target>
            </project>";

        #endregion Private Static Fields

        #region Public Instance Methods

        [Test]
        public void IndependentTargetsExecuteInParallel() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            Project project = CreateFilebasedProject(TwoIndependentTargets, Level.Info);
            project.RunTargetsInParallel = true;

            
            //use Project.AttachBuildListeners to attach.
            IBuildListener[] listeners = {listener};
            project.AttachBuildListeners(new BuildListenerCollection(listeners));

            // execute the project
            ExecuteProject(project);
            
            DateTime target2startTime = listener.GetTargetStartTime("Target2");
            DateTime target2finishTime = listener.GetTargetFinishTime("Target2");
            DateTime target3startTime = listener.GetTargetStartTime("Target3");
            DateTime target3finishTime = listener.GetTargetFinishTime("Target3");

            Assert.IsTrue(target2startTime <= target2finishTime, "Target2 start time is after finish time");
            Assert.IsTrue(target3startTime <= target3finishTime, "Target3 start time is after finish time");

            Assert.IsTrue(target3startTime <= target2finishTime, "Target3 was not started until Target2 was finished");
            Assert.IsTrue(target2startTime <= target3finishTime, "Target2 was not started until Target3 was finished");
        }

        [Test]
        public void DependentTargetsExecuteSequentially() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            Project project = CreateFilebasedProject(DependentTargets, Level.Info);
            project.RunTargetsInParallel = true;

            
            //use Project.AttachBuildListeners to attach.
            IBuildListener[] listeners = {listener};
            project.AttachBuildListeners(new BuildListenerCollection(listeners));

            // execute the project
            ExecuteProject(project);
            
            DateTime target2startTime = listener.GetTargetStartTime("Target2");
            DateTime target2finishTime = listener.GetTargetFinishTime("Target2");
            DateTime target3startTime = listener.GetTargetStartTime("Target3");
            DateTime target3finishTime = listener.GetTargetFinishTime("Target3");

            Assert.IsTrue(target2startTime <= target2finishTime, "Target2 start time is after finish time");
            Assert.IsTrue(target3startTime <= target3finishTime, "Target3 start time is after finish time");

            Assert.IsTrue(target3finishTime <= target2startTime, "Target2 was started before Target3 was finished");
        }

        #endregion Public Instance Methods
    }
}
