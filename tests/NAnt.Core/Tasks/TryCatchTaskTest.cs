// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gert Driesen
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
// Troy Laurin (fiontan@westnet.com.au)

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the TryCatch task.
    /// </summary>
    [TestFixture]
    public class TryCatchTaskTest : BuildTestBase {
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }
        
        [Test]
        public void Test_CatchExceptionWithoutMessage() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <fail message='Exception text' />
                            </try>
                            <catch>
                                <echo message='Catch!' />
                            </catch>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Catch!") != -1, "Exception should have been caught");
        }

        [Test]
        public void Test_CatchExceptionWithMessage() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <fail message='Exception text' />
                            </try>
                            <catch property='ex'>
                                <echo message='Catch: ${ex}' />
                            </catch>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Catch: Exception text") != -1, "Exception message should have been caught and displayed");
        }

        [Test]
        public void Test_CatchWithoutFail() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <echo message='No exception' />
                            </try>
                            <catch>
                                <echo message='Catch!' />
                            </catch>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("No exception") != -1, "Try block should have run");
            Assert.IsTrue(result.IndexOf("Catch!") == -1, "Catch block shouldn't have run");
        }

        [Test]
        public void Test_CatchExceptionAndFinally() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <fail message='Exception text' />
                            </try>
                            <catch>
                                <echo message='Catch!' />
                            </catch>
                            <finally>
                                <echo message='Finally!' />
                            </finally>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Catch!") != -1, "Exception should have been caught");
            Assert.IsTrue(result.IndexOf("Finally!") != -1, "Finally block should have run");
        }

        [Test]
        public void Test_CatchWithoutFailAndFinally() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <echo message='No exception' />
                            </try>
                            <catch>
                                <echo message='Catch!' />
                            </catch>
                            <finally>
                                <echo message='Finally!' />
                            </finally>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("No exception") != -1, "Try block should have run");
            Assert.IsTrue(result.IndexOf("Catch!") == -1, "Catch block shouldn't have run");
            Assert.IsTrue(result.IndexOf("Finally!") != -1, "Finally block should have run");
        }

        [Test]
        public void Test_PropertyScopePreset() {
            string _xml = @"
                    <project>
                        <property name='ex' value='Original' />
                        <trycatch>
                            <try>
                                <fail message='Exception text' />
                            </try>
                            <catch property='ex'>
                                <echo message='Catch: ${ex}' />
                            </catch>
                            <finally>
                                <echo message='Finally: ${ex}' />
                            </finally>
                        </trycatch>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Catch: Exception text") != -1, "Exception message should have been caught and displayed");
            Assert.IsTrue(result.IndexOf("Finally: Original") != -1, "Exception property should be reset outside the catch block");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_MessagePropertyScopeEmpty() {
            string _xml = @"
                    <project>
                        <trycatch>
                            <try>
                                <fail message='Exception text' />
                            </try>
                            <catch property='ex'>
                                <echo message='Catch: ${ex}' />
                            </catch>
                            <finally>
                                <echo message='Finally: ${ex}' />
                            </finally>
                        </trycatch>
                    </project>";
            RunBuild(_xml);
        }
    }
}
