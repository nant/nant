// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez (ScottHernandez@hotmail.com)
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Globalization;
using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class CallTest : BuildTestBase {
    
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }

        [Test]
        public void Test_Call() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // set-up build file
            string _xml = @"
                    <project>
                        <target name='one'>
                            <echo message='one--' />
                        </target>
                        <call target='one' />
                    </project>";

            // run the build
            string result = RunBuild(_xml, listener);

            // check whether 'one' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("one"), "'one' target was not called." + Environment.NewLine + result);
        }

        [Test]
        public void Test_CallDependencies() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // set-up build file
            string _xml = @"
                    <project>
                        <target name='one' depends='two'>
                            <echo message='one--'/>
                        </target>
                        <target name='two'>
                            <echo message='two--'/>
                        </target>
                        <call target='one' />
                    </project>";

            // run the build
            string result = RunBuild(_xml, listener);

            // check whether 'one' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("one"), "'one' target was not called." + Environment.NewLine + result);

            // check whether 'two' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("two"), "'two' target was not called." + Environment.NewLine + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_CallCircularDependencies() {
            // set-up build file
            string _xml = @"
                    <project default='one'>
                        <target name='one' depends='two'>
                            <echo message='one--'/>
                        </target>
                        <target name='two'>
                            <call target='one' />
                        </target>
                        <call target='one' />
                    </project>";

            // run the build
            RunBuild(_xml);
        }

        [Test]
        public void Test_Cascade() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // set-up build file
            string _xml = @"
                <project default='rebuild'>
	                <target name='clean' />
                    <target name='init' />
	                <target name='compile' depends='init' />
	                <target name='build'>
		                <call target='compile' {0} />
		                <call target='compile' {0} />
	                </target>
	                <target name='rebuild' depends='clean, build' />
                </project>";

            // run the build
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xml, ""), 
                listener);
            // check whether 'compile' target has been executed twice
            Assert.AreEqual(2, listener.GetTargetExecutionCount("compile"), "#A1");
            // check whether 'clean' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("clean"), "#A2");
            // check whether 'build' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("build"), "#A3");
            // check whether 'init' target has been executed once
            Assert.AreEqual(2, listener.GetTargetExecutionCount("init"), "#A4");
            // check whether 'call' task has been executed twice
            Assert.AreEqual(2, listener.GetTaskExecutionCount("call"), "#A5");

            // construct new listener for tracking build events
            listener = new TestBuildListener();

            // run the build with cascade set to "false"
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xml,
                "cascade=\"false\""), listener);
            // check whether 'compile' target has been executed twice
            Assert.AreEqual(2, listener.GetTargetExecutionCount("compile"), "#B1");
            // check whether 'clean' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("clean"), "#B2");
            // check whether 'build' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("build"), "#B3");
            // check whether 'init' target has been executed once
            Assert.AreEqual(1, listener.GetTargetExecutionCount("init"), "#B4");
            // check whether 'call' task has been executed twice
            Assert.AreEqual(2, listener.GetTaskExecutionCount("call"), "#B5");
        }
    }
}