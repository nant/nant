// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the Fail task.
    /// </summary>
    [TestFixture]
    public class FailTest : BuildTestBase {
        [Test]
        public void Test_FailMessage() {
            string _xml = @"
                    <project>
                        <fail message='Death Sucks!'/>
                    </project>";
            try {
                string result = RunBuild(_xml);
                Assert.Fail("Project should have failed:" + result);
            } catch (TestBuildException be) {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("Death Sucks!") != -1,
                    "Project did not fail from Test!");
            }
        }

        [Test]
        public void Test_FailMessageMacro() {
            string _xml = @"
                    <project>
                        <property name='prop' value='Death' />
                        <fail message='${prop} Sucks!'/>
                    </project>";
            try {
                string result = RunBuild(_xml);            
                Assert.Fail("Project should have failed:" + result);
            } catch (TestBuildException be) {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("Death Sucks!") != -1,
                    "Macro should have expanded!");
            }
        }

        [Test]
        public void Test_FailContent() {
            string _xml = @"
                    <project>
                        <fail>Death Sucks!</fail>
                    </project>";
            try {
                string result = RunBuild(_xml);            
                Assert.Fail("Project should have failed:" + result);
            }
            catch (TestBuildException be) {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("Death Sucks!") != -1,
                    "Project did not fail from Test!");
            }
        }

        [Test]
        public void Test_FailContentMacro() {
            string _xml = @"
                    <project>
                        <property name='prop' value='Death' />
                        <fail>${prop} Sucks!</fail>
                    </project>";
            try {
                string result = RunBuild(_xml);            
                Assert.Fail("Project should have failed:" + result);
            }
            catch (TestBuildException be) {
                Assert.IsTrue(be.InnerException.ToString().IndexOf("Death Sucks!") != -1,
                    "Macro should have expanded!");
            }
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_FailMessageAndInlineContent() {
            string _xml = @"
                    <project>
                        <fail message='Death Sucks!'>Death Sucks!</fail>
                    </project>";
            RunBuild(_xml);
        }
    }
}
