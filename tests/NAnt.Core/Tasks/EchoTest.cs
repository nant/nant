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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the Echo task.
    /// </summary>
    [TestFixture]
    public class EchoTest : BuildTestBase {
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }
        
        [Test]
        public void Test_EchoDefaultProjectInfo() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Go Away!") != -1, "Echo message missing:" + result);
        }

        [Test]
        public void Test_EchoDefaultProjectInfoMacro() {
            string _xml = @"
                    <project>
                        <property name='prop' value='Go' />
                        <echo message='${prop} Away!'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("Go Away!") != -1, "Macro should have expanded:" + result);
        }

        [Test]
        public void Test_EchoDebugProjectInfo() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Debug' />
                    </project>";
            string result = RunBuild(_xml, Level.Info);
            Assert.IsTrue(result.IndexOf("Go Away!") == -1, "Debug echo should not be output when Project level is Info.");
        }

        [Test]
        public void Test_EchoWarningProjectInfo() {
            string _xml = @"
                    <project>
                        <echo level='Warning'>Go Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Info);
            Assert.IsTrue(result.IndexOf("Go Away!") != -1, "Warning echo should be output when Project level is Info.");
        }

        [Test]
        public void Test_EchoWarningProjectInfoMacro() {
            string _xml = @"
                    <project>
                        <property name='prop' value='Go' />
                        <echo level='warninG'>${prop} Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Info);
            Assert.IsTrue(result.IndexOf("Go Away!") != -1, "Macro should have expanded:" + result);
        }

        [Test]
        public void Test_EchoWarningProjectError() {
            string _xml = @"
                    <project>
                        <echo level='Warning'>Go Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Error);
            Assert.IsTrue(result.IndexOf("Go Away!") == -1, "Warning echo should not be output when Project level is Error.");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EchoInvalidLevel() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Invalid' />
                    </project>";
            RunBuild(_xml, Level.Error);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EchoMessageAndInlineContent() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Debug'>Go Away!</echo>
                    </project>";
            RunBuild(_xml, Level.Info);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Encoding_Invalid() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' encoding='DoesNotExist'>Go Away!</echo>
                    </project>";
            RunBuild(_xml, Level.Info);
        }

        [Test]
        public void NewFile() {
            string msg = "\u0041\u2262\u0391\u002E!";
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo file='log' encoding='utf-8' message='" + msg + @"'/>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml1, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#A2");
            Assert.IsTrue (File.Exists(logfile), "#A3");
            using (StreamReader sr = new StreamReader (logfile, Encoding.UTF8)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual (msg, content, "#A4");
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo file='log' encoding='utf-8'>" + msg + @"</echo>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml2, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#B2");
            Assert.IsTrue (File.Exists(logfile), "#B3");
            using (StreamReader sr = new StreamReader (logfile, Encoding.UTF8)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual (msg, content, "#B4");
            }
        }

        [Test]
        public void ExistingFile_Append_Default() {
            string msg = "\u0041\u2262\u0391\u002E!";
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo file='log' message='" + msg + @"' encoding='utf-16'/>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.Unicode)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml1, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#A2");
            Assert.IsTrue (File.Exists(logfile), "#A3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (12, fs.Length, "#A4");
                using (StreamReader sr = new StreamReader (fs, Encoding.Unicode)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#A5");
                }
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo file='log' encoding='utf-16'>" + msg + @"</echo>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.Unicode)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml2, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#B2");
            Assert.IsTrue (File.Exists(logfile), "#B3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (12, fs.Length, "#B4");
                using (StreamReader sr = new StreamReader (fs, Encoding.Unicode)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#B5");
                }
            }
        }

        [Test]
        public void ExistingFile_Append_False() {
            string msg = "\u0041\u2262\u0391\u002E!";
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo append='false' file='log' message='" + msg + @"' encoding='utf-8'/>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.UTF8)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml1, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#A2");
            Assert.IsTrue (File.Exists(logfile), "#A3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (11, fs.Length, "#A4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#A5");
                }
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo append='false' file='log' encoding='utf-8'>" + msg + @"</echo>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.UTF8)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml2, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#B2");
            Assert.IsTrue (File.Exists(logfile), "#B3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (11, fs.Length, "#B4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#B5");
                }
            }
        }

        [Test]
        public void ExistingFile_Append_True() {
            string msg = "\u0041\u2262\u0391\u002E!";
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo append='true' file='log' message='" + msg + @"' encoding='utf-8'/>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.UTF8)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml1, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#A2");
            Assert.IsTrue (File.Exists(logfile), "#A3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (13, fs.Length, "#A4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual ("ok" + msg, content, "#A5");
                }
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo append='true' file='log' encoding='utf-8'>" + msg + @"</echo>
                </project>";

            using (StreamWriter sw = new StreamWriter (logfile, false, Encoding.UTF8)) {
                sw.Write ("ok");
            }

            listener = new TestBuildListener();
            result = RunBuild (_xml2, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#B2");
            Assert.IsTrue (File.Exists(logfile), "#B3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (13, fs.Length, "#B4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual ("ok" + msg, content, "#B5");
                }
            }
        }

        [Test]
        public void File_Message_Empty() {
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo file='log' message=' ' />
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml1, listener);
            Assert.IsTrue (File.Exists(logfile), "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (" ", true), "#A2");
            using (StreamReader sr = new StreamReader (logfile, Encoding.UTF8)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual (string.Empty, content, "#A3");
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo file='log' message=' '> </echo>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml2, listener);
            Assert.IsTrue (File.Exists(logfile), "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (" ", true), "#B2");
            using (StreamReader sr = new StreamReader (logfile, Encoding.UTF8)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual (string.Empty, content, "#B3");
            }

            File.Delete (logfile);

            string msg = "\u0041\u2262\n\u0391\u002E!";
            string _xml3 = @"
                <project>
                    <echo file='log' message=' ' encoding='utf-8'>" + msg + @"</echo>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml3, listener);
            Assert.IsTrue (File.Exists(logfile), "#C1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#C2");
            using (StreamReader sr = new StreamReader (logfile, Encoding.UTF8)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual (msg, content, "#C3");
            }
        }

        [Test]
        public void File_Level_Ignored() {
            string msg = "\u0041\u2262\u0391\u002E!";
            string logfile = Path.Combine (TempDirName, "log");
            TestBuildListener listener;
            string result;

            string _xml1 = @"
                <project>
                    <echo file='log' level='warning' encoding='utf-8' message='" + msg + @"'/>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml1, Level.Error, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#A1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#A2");
            Assert.IsTrue (File.Exists(logfile), "#A3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (11, fs.Length, "#A4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#A5");
                }
            }

            File.Delete (logfile);

            string _xml2 = @"
                <project>
                    <echo file='log' level='warning' encoding='utf-8'>" + msg + @"</echo>
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml2, Level.Error, listener);
            Assert.IsTrue(result.IndexOf(msg) == -1, "#B1");
            Assert.IsFalse(listener.HasMessageBeenLogged (msg, true), "#B2");
            Assert.IsTrue (File.Exists(logfile), "#B3");
            using (FileStream fs = File.OpenRead (logfile)) {
                Assert.AreEqual (11, fs.Length, "#B4");
                using (StreamReader sr = new StreamReader (fs, Encoding.UTF8)) {
                    string content = sr.ReadToEnd ();
                    Assert.AreEqual (msg, content, "#B5");
                }
            }
        }

        [Test]
        public void File_Directory_DoesNotExist() {
            string dir = Path.Combine (TempDirName, "tmp");
            string logfile = Path.Combine (dir, "log");
            TestBuildListener listener;
            string result;

            string _xml = @"
                <project>
                    <echo file='tmp/log' message='sometest' />
                </project>";

            listener = new TestBuildListener();
            result = RunBuild (_xml, listener);
            Assert.IsTrue (File.Exists(logfile), "#1");
            Assert.IsFalse(listener.HasMessageBeenLogged ("sometest", true), "#2");
            using (StreamReader sr = new StreamReader (logfile, Encoding.Default)) {
                string content = sr.ReadToEnd ();
                Assert.AreEqual ("sometest", content, "#3");
            }
        }
    }
}
