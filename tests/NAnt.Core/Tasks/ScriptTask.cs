// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez (ScottHernandez@hotmail.com)
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
using System.Reflection;
using System.Text;
using System.Xml;
using NAnt.Core;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class ScriptTaskTest : BuildTestBase {
        [Test]
        public void Test_VB() {
            string _xml = @"
            <project>
               <script language='VB'>
                    <code>
                        <![CDATA[
                            Public Shared Sub ScriptMain(p As NAnt.Core.Project)
                                p.Properties(""foo"")=1
                            End Sub
                        ]]>
                    </code>
                </script>
                <echo message='CSFoo=${foo}'/>
            </project>";

            string result = RunBuild(_xml);
            Assertion.Assert("VB script should have updated prop." + Environment.NewLine + result, result.IndexOf("CSFoo=1") != -1);
        }

        [Test]
        public void Test_CSharp() {
            string _xml = @"
            <project>
                <script language='C#'>
                    <code>
                        <![CDATA[
                            public static void ScriptMain(Project project) {
                                Console.WriteLine(""Hello"");
                                project.Properties[""from.script""] = ""script.me"";
                            }
                        ]]>
                    </code>
                </script>
                <echo message='hi from ${from.script}'/>
            </project>";

            string result = RunBuild(_xml);
            Assertion.Assert("CSharp script should written something." + Environment.NewLine + result, result.IndexOf("Hello") != -1);
            Assertion.Assert("CSharp script should have updated prop." + Environment.NewLine + result, result.IndexOf("script.me") != -1);
        }
        
        [Test]
        public void Test_Functions() {
            string _xml = @"
            <project>
                <script language='C#'>
                    <code>
                    <![CDATA[
                            [Function(""test-func"")]
                            public static string Testfunc() {
                                return ""some result!!!!!!!!"";
                            }
                        ]]>
                    </code>
                </script>
                <echo message='${script::test-func()}'/>
            </project>";
            //_xml = string.Format(_xml, "test-func", "some result !!!!!!!!" );
            string result = RunBuild(_xml);
            Assertion.Assert("Function script should have defined a new custom function." + Environment.NewLine + result, TypeFactory.LookupFunction("script::test-func") != null );
            Assertion.Assert("Function script should written something." + Environment.NewLine + result, result.IndexOf("some result") != -1);
            
        }
        [Test]
        public void Test_2ScriptsInOneProject() {
            string _xml = @"
            <project>
                <script language='C#'>
                    <code>
                        <![CDATA[
                            public static void ScriptMain(Project project) {
                                int v = 1;
                                int p = 1;
                                v += p;
                                //do nothing.
                            }
                        ]]>
                    </code>
                </script>
                <script language='C#'>
                    <code>
                        <![CDATA[
                            public static void ScriptMain(Project project) {
                                int v = 1;
                                int p = 1;
                                v += p;
                                //do nothing.
                            }
                        ]]>
                    </code>
                </script>
            </project>";

            string result = RunBuild(_xml);
        }
    }
}
