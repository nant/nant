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
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

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
            string result = RunBuild(_xml);
        }
    }
}