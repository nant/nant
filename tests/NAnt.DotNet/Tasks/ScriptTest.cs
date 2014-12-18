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
using NAnt.Core;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class ScriptTest : BuildTestBase {
        [Test]
        [Category ("NotMono")]
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
            Assert.IsTrue(result.IndexOf("CSFoo=1") != -1, "VB script should have updated prop." + Environment.NewLine + result);
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
            Assert.IsTrue(result.IndexOf("Hello") != -1, "CSharp script should written something." + Environment.NewLine + result);
            Assert.IsTrue(result.IndexOf("script.me") != -1, "CSharp script should have updated prop." + Environment.NewLine + result);
        }
        
        /// <summary>
        /// Test for bug #1187957.
        /// </summary>
        [Test]
        public void Test_Tasks() {
            const string _xml = @"
                <project name=""customtasks"">
                    <script language=""c#"">
                        <code><![CDATA[
                            [TaskName(""testtask1"")]
                            public class TestTask1: Task
                            {
                                protected override void ExecuteTask()
                                {
                                    Log(Level.Info, ""Message from testtask1."");
                                }
                            }
                        ]]></code>
                    </script>

                    <script language=""c#"">
                        <code><![CDATA[
                            [TaskName(""testtask2"")]
                            public class TestTask2: Task
                            {
                                protected override void ExecuteTask()
                                {
                                    Log(Level.Info, ""Message from testtask2."");
                                }
                            }
                        ]]></code>
                    </script>

                    <testtask1 />
                    <testtask2 />
                </project>";
            RunBuild(_xml);
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
                    <script language='C#' prefix='whatever'>
                        <code>
                        <![CDATA[
                                [Function(""test"")]
                                public static string Testfunc() {
                                    return ""some other result!!!!!!!!"";
                                }
                            ]]>
                        </code>
                    </script>
                    <echo message='${script::test-func()}'/>
                    <echo message='${whatever::test()}'/>
                </project>";
            Project project = CreateFilebasedProject(_xml);
            string result = ExecuteProject(project);
            Assert.IsTrue(result.IndexOf("some result") != -1,
                "Function script should written something #1." + Environment.NewLine + result);
            Assert.IsTrue(result.IndexOf("some other result") != -1,
                "Function script should written something #2." + Environment.NewLine + result);
        }

        [Test]
        public void NamespaceImports () {
#if !NET_2_0
             if (PlatformHelper.IsMono) {
                Assert.Ignore("Skip this to make mono-1.0 on tc@codebetter.com happy. Should be removed when 1.x runtime support is dropped");
             }
#else
            string xml = @"
                <project>
                    <script language='C#'>
                        <imports>
                            <import namespace='System.Xml.Schema' />
                        </imports>
                        <references>
                            <include name='System.Xml.dll' />
                        </references>
                        <code>
                            <![CDATA[
                                public static void ScriptMain(Project project) {
                                    // ensure System.Collections namespace is imported
                                    ArrayList list = new ArrayList ();
                                    if (list == null) {
                                        // avoid compiler warning
                                    }

                                    // ensure System.IO namespace is imported
                                    MemoryStream ms = new MemoryStream ();
                                    if (ms == null) {
                                        // avoid compiler warning
                                    }

                                    // ensure System.Text namespace is imported
                                    StringBuilder sb = new StringBuilder ();
                                    if (sb == null) {
                                        // avoid compiler warning
                                    }

                                    XmlSchemaType stype = new XmlSchemaType ();
                                    project.Properties[""schema.type""] = stype.GetType ().FullName;
                                }
                            ]]>
                        </code>
                    </script>
                    <fail unless=""${schema.type=='System.Xml.Schema.XmlSchemaType'}"" />
                </project>";

            RunBuild(xml);
#endif
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

            RunBuild(_xml);
        }
    }
}
