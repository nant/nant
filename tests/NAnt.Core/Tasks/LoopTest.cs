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

// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {

    [TestFixture]
    public class LoopTest : BuildTestBase {
    
        [Test]
        public void Test_Loop_String_Default_Delim() {
            string _xml = @"
                    <project>
                        <foreach item='String' in='1,2,3,4;5' delim=';,' property='count'>
                            <echo message='Count:${count}'/>
                        </foreach>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Count:1") != -1);
            Assert.IsTrue(result.IndexOf("Count:2") != -1);
            Assert.IsTrue(result.IndexOf("Count:3") != -1);
            Assert.IsTrue(result.IndexOf("Count:4") != -1);
            Assert.IsTrue(result.IndexOf("Count:5") != -1);
        }

        [Test]
        public void Test_Loop_Files() {
            string _xml = @"
                    <project>
                        <foreach item='File' in='${nant.project.basedir}' property='file'>
                            <echo message='File:${file}'/>
                        </foreach>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("test.build") != -1);
        }
        
        [Test]
        public void Test_Loop_Files_From_FileSet() {
            string _xml = @"
                    <project>
                        <foreach item='File' property='file'>
                            <in>
                                <items basedir='${nant.project.basedir}'>
                                    <include name='*'/>
                                </items>
                            </in>
                            <do>
                                <echo message='File:${file}'/>
                            </do>
                        </foreach>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("test.build") != -1);
        }

        [Test]
        public void Test_Loop_Folders() {
            string _xml = @"
                    <project>
                        <mkdir dir='${nant.project.basedir}/foo'/>
                        <mkdir dir='${nant.project.basedir}/bar'/>
                        <foreach item='Folder' in='${nant.project.basedir}' property='folder'>
                            <echo message='Folder:${folder}'/>
                        </foreach>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("foo") != -1);
            Assert.IsTrue(result.IndexOf("bar") != -1);
        }

        [Test]
        public void Test_Loop_Folders_From_FileSet() {
            string _xml = @"
                    <project>
                        <mkdir dir='${nant.project.basedir}/foo'/>
                        <mkdir dir='${nant.project.basedir}/bar'/>
                        <foreach item='Folder' property='dir'>
                            <in>
                                <items basedir='${nant.project.basedir}'>
                                    <include name='*'/>
                                </items>
                            </in>
                            <do>
                                <echo message='Dir:${dir}'/>
                            </do>
                        </foreach>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("foo") != -1);
            Assert.IsTrue(result.IndexOf("bar") != -1);
        }

        [Test]
        public void Test_Loop_Lines() {
            string strTempFile = CreateTempFile("looptest.loop_lines_test.txt");
            using ( StreamWriter sw = new StreamWriter( strTempFile ) ) {
                sw.WriteLine( "x,y" );
                sw.WriteLine( "x2,y2  " );
                sw.WriteLine( "x3  ,y3" );
                sw.WriteLine( "x4,  y4" );
                sw.Close();

                string _xml = String.Format( @"
                    <project>
                        <!-- Hello from inside -->
                        <foreach item='Line' delim=',;' trim='Both' in='{0}' property='x,y'>
                            <echo message='|${{x}}=${{y}}|'/>
                        </foreach>
                    </project>", strTempFile );
                string result = RunBuild(_xml);
                Assert.IsTrue(result.IndexOf("|x=y|") != -1);
                Assert.IsTrue(result.IndexOf("|x2=y2|") != -1);
                Assert.IsTrue(result.IndexOf("|x3=y3|") != -1);
                Assert.IsTrue(result.IndexOf("|x4=y4|") != -1);
            }
        }

        [Test]
        public void Test_Loop_Lines_No_Delim() {
            string strTempFile = CreateTempFile("looptest.loop_lines_test.txt");
            using ( StreamWriter sw = new StreamWriter( strTempFile ) ) {
                sw.WriteLine( "x,y " );
                sw.WriteLine( "x2,y2  " );
                sw.WriteLine( "  x3  ,y3 " );
                sw.WriteLine( "  x4,  y4 " );
                sw.Close();

                string _xml = String.Format( @"
                    <project>
                        <!-- Hello from inside -->
                        <foreach item='Line' trim='Start' in='{0}' property='x'>
                            <echo message='|${{x}}|'/>
                        </foreach>
                    </project>", strTempFile );
                string result = RunBuild(_xml);
                Assert.IsTrue(result.IndexOf("|x,y |") != -1);
                Assert.IsTrue(result.IndexOf("|x2,y2  |") != -1);
                Assert.IsTrue(result.IndexOf("|x3  ,y3 |") != -1);
                Assert.IsTrue(result.IndexOf("|x4,  y4 |") != -1);
            }
        }
    }
}