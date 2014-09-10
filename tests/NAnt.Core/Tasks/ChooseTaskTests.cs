//
// NAntContrib
// Copyright (C) 2001-2005 Gerry Shaw
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Gert Driesen (drieseng@users.sourceforge.net)

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class ChooseTaskTest : BuildTestBase {
        [Test]
        public void Test_ConditionalExecution1() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""false"">
                                <patternset id=""when1.sources"">
                                    <include name=""**/*.cs"" />
                                    <exclude name=""**/*Test*"" />
                                </patternset>
                                <copy todir=""."">
                                    <fileset>
                                        <patternset refid=""when1.sources"" />
                                    </fileset>
                                </copy>
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <when test=""true"">
                                <property name=""when2"" value=""a"" />
                                <patternset id=""when2.sources"">
                                    <include name=""**/*.cs"" />
                                    <exclude name=""**/*Test*"" />
                                </patternset>
                                <property name=""when2"" value=""${when2}b"" />
                                <property name=""when2"" value=""${when2}c"" if=""${true == false}"" />
                                <copy todir=""."">
                                    <fileset>
                                        <patternset refid=""when2.sources"" />
                                    </fileset>
                                </copy>
                                <property name=""when2"" value=""${when2}d"" />
                            </when>
                            <otherwise>
                                <patternset id=""otherwise.sources"">
                                    <include name=""**/*.cs"" />
                                    <exclude name=""**/*Test*"" />
                                </patternset>
                                <copy todir=""."">
                                    <fileset>
                                        <patternset refid=""otherwise.sources"" />
                                    </fileset>
                                </copy>
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                        </choose>
                        <fail if=""${property::exists('when1')}"">#1</fail>
                        <fail unless=""${property::exists('when2')}"">#2</fail>
                        <fail unless=""${when2=='abd'}"">#3</fail>
                        <fail if=""${property::exists('otherwise')}"">#4</fail>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_ConditionalExecution2() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <when test=""false"">
                                <property name=""when2"" value=""executed"" />
                            </when>
                        </choose>
                        <fail unless=""${property::exists('when1')}"">#1</fail>
                        <fail if=""${property::exists('when2')}"">#2</fail>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_ConditionalExecution3() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""false"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <when test=""false"">
                                <property name=""when2"" value=""executed"" />
                            </when>
                        </choose>
                        <fail if=""${property::exists('when1')}"">#1</fail>
                        <fail if=""${property::exists('when2')}"">#2</fail>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_Fallback() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""false"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <otherwise>
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                        </choose>
                        <fail if=""${property::exists('when1')}"">#1</fail>
                        <fail unless=""${property::exists('otherwise')}"">#2</fail>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ChildOrder1() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""false"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <otherwise>
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                            <when test=""true"">
                                <property name=""when2"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ChildOrder2() {
            string _xml = @"
                    <project>
                        <choose>
                            <otherwise>
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                            <when test=""false"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_EmptyWhenChild() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"" />
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_EmptyOtherwiseChild() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""false"" />
                            <otherwise />
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_MissingWhenChild1() {
            string _xml = @"
                    <project>
                        <choose />
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_MissingWhenChild2() {
            string _xml = @"
                    <project>
                        <choose>
                            <otherwise>
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidWhenCondition() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""whatever"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_MissingWhenCondition() {
            string _xml = @"
                    <project>
                        <choose>
                            <when>
                                <property name=""when1"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EmptyWhenCondition() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test="""">
                                <property name=""when1"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidChild() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <if test=""true"" />
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidExtension() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"">
                                <doesnotexist />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidWhenParameter() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"" if=""true"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidOtherwiseParameter() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"">
                                <property name=""when1"" value=""executed"" />
                            </when>
                            <otherwise if=""true"">
                                <property name=""otherwise"" value=""executed"" />
                            </otherwise>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        public void Test_FailOnError_False() {
            string _xml = @"
                    <project>
                        <choose failonerror=""false"">
                            <when test=""true"">
                                <fail>Some reason</fail>
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_FailOnError_True() {
            string _xml = @"
                    <project>
                        <choose>
                            <when test=""true"">
                                <fail>Some reason</fail>
                            </when>
                        </choose>
                    </project>";
            RunBuild(_xml);
        }
    }
}
