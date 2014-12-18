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
// Anthony LoveFrancisco (ants@fu.org)

using System;
using System.IO;
using NUnit.Framework;

using NAnt.Core;

using NAnt.VisualCpp.Tasks;

namespace Tests.NAnt.VisualCpp.Tasks
{
    [TestFixture]
    public class LinkTaskTest_HelloWorld : VisualCppTestBase {
        string _binDir;
        string _objDir;
        string _sourceDir;

        const string _test_build = @"<?xml version='1.0'?>
                <project>
                    <cl outputdir=""objs""
                        options=""-Zi -MDd -GA -Gz -YX -DWIN32 -DUNICODE -DDEBUG -D_DEBUG"" >
                        <sources>
                            <include name=""src\HelloWorld.cpp"" />
                        </sources>
                    </cl>
                    <link output=""bin\HelloWorld.exe""
                        options=""-debug"" >
                        <sources>
                            <include name=""objs\*.obj"" />
                        </sources>
                    </link>
                </project>";
        const string _helloWorld_cpp = @"
                #include <stdio.h>
                void main(void) {
                    printf(""Hello, World."");
                }";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _binDir = CreateTempDir("bin");
            _objDir = CreateTempDir("objs");
            _sourceDir = CreateTempDir("src");
            CreateTempFile(Path.Combine(_sourceDir, "HelloWorld.cpp"), _helloWorld_cpp);
        }

        /// <summary>Test to make sure simple compile works.</summary>
        [Test]
        public void Test_HelloWorld() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
            }

            RunBuild(_test_build);
            Assert.IsTrue(File.Exists(Path.Combine(_objDir, "HelloWorld.obj")),
                "Object file not created.");
            Assert.IsTrue(File.Exists(Path.Combine(_binDir, "HelloWorld.exe")),
                "Binary file not created.");
        }
    }

    [TestFixture]
    public class LinkTaskTest_CompileOnDemand : VisualCppTestBase {
        const int _sourceCount = 3;

        string _binDir;
        string _objDir;
        string _sourceDir;
        string _binPathName;
        string [] _sourcePathName = new string[_sourceCount];
        string [] _objPathName = new string[_sourceCount];

        readonly string [] _sourceFileName = new string[_sourceCount] { "main.cpp", "test1.cpp", "test2.cpp" };
        readonly string [] _sourceCode = new string[_sourceCount] {
            @"#include <stdio.h>
                extern void test1(void);
                extern void test2(void);
                void main(void) {
                    test1();
                    test2();
                }
            ",
            @"#include <stdio.h>
                void test1(void) {
                    printf(""test1 function"");
                }
            ",
            @"#include <stdio.h>
                void test2(void) {
                    printf(""test2 function"");
                }
            "
        };

        const string _test_build = @"<?xml version='1.0'?>
                <project>
                    <cl outputdir=""objs""
                        options=""-Zi -MDd -GA -Gz -YX -DWIN32 -DUNICODE -DDEBUG -D_DEBUG"" >
                        <sources>
                            <include name=""src\*.cpp"" />
                        </sources>
                    </cl>
                    <link output=""bin\MultiPart.exe""
                        options=""-debug"" >
                        <sources>
                            <include name=""objs\*.obj"" />
                        </sources>
                    </link>
                </project>";

        const string _test_compile_only = @"<?xml version='1.0'?>
                <project>
                    <cl outputdir=""objs""
                        options=""-Zi -MDd -GA -Gz -YX -DWIN32 -DUNICODE -DDEBUG -D_DEBUG"" >
                        <sources>
                            <include name=""src\*.cpp"" />
                        </sources>
                    </cl>
                </project>";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _binDir = CreateTempDir("bin");
            _objDir = CreateTempDir("objs");
            _sourceDir = CreateTempDir("src");

            for (int i = 0; i < _sourceCount; ++i) {
                _sourcePathName[i] = CreateTempFile(Path.Combine(_sourceDir, _sourceFileName[i]), _sourceCode[i]);
                _objPathName[i] = Path.ChangeExtension(Path.Combine(_objDir, _sourceFileName[i]), ".obj");
            }
            _binPathName = Path.Combine(_binDir, "MultiPart.exe");
        }

        void CleanAllObjs() {
            foreach (string objPathName in _objPathName) {
                try {
                    File.Delete(objPathName);
                } catch (Exception) {
                } finally {
                    Assert.IsFalse(File.Exists(objPathName), "Object file \"{0}\" exists.", objPathName);
                }
            }
        }

        void CleanAllBins() {
            try {
                File.Delete(_binPathName);
            } catch (Exception) {
            } finally {
                Assert.IsFalse(File.Exists(_binPathName), "Binary file \"{0}\" exists.", _binPathName);
            }
        }

        /// <summary>Test to make sure compiling all files.</summary>
        [Test]
        public void Test_BuildAll() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
            }

            CleanAllObjs();
            CleanAllBins();
            RunBuild(_test_build);
            Assert.IsTrue(File.Exists(_binPathName), "Binary file \"{0}\" not created.", _binPathName);
        }

        /// <summary>Test to make sure not to compile when everything is up to date.</summary>
        [Test]
        public void Test_BuildNothingChanged() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
            }

            string result;

            CleanAllObjs();
            CleanAllBins();
            result = RunBuild(_test_build);
            result = RunBuild(_test_build);
            Assert.IsTrue(result.IndexOf("[link]") == -1, "Shouldn't have linked anything the second time around");
        }

        /// <summary>Test to make sure compiling happens when source files change.</summary>
        [Test]
        public void Test_BuildSourceChanged() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
            }

            Test_BuildAll();
            for (int i = 0; i < _sourceCount; ++i) {
                File.SetLastWriteTime(_objPathName[i], DateTime.Now);
                RunBuild(_test_build);
                FileInfo objFileInfo = new FileInfo(_objPathName[i]);
                FileInfo binFileInfo = new FileInfo(_binPathName);
                Assert.IsTrue(binFileInfo.LastWriteTime >= objFileInfo.LastWriteTime,
                    "{0} must be newer than {1}.", _binPathName, _objPathName[i]);
            }
        }

        /// <summary>
        /// Ensures &lt;link&gt; task supports lib dirs containing spaces, and
        /// quoted dirs containing spaces (bug #1117794).
        /// </summary>
        [Test]
        public void Test_LibDirsContainingSpaces() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
            }

            CleanAllObjs();
            CleanAllBins();

            Project project = CreateFilebasedProject(_test_compile_only);
            ExecuteProject(project);

            LinkTask lt = new LinkTask();
            lt.Project = project;
            lt.Options = "-debug";
            lt.OutputFile = new FileInfo(Path.Combine(this.TempDirectory.FullName,
                "bin/HelloWorld.exe"));

            lt.LibDirs.BaseDirectory = this.TempDirectory;
            lt.LibDirs.DirectoryNames.Add("\"whatever you want\"");
            lt.LibDirs.DirectoryNames.Add("whatever you want");

            lt.Sources.BaseDirectory = this.TempDirectory;
            lt.Sources.Includes.Add("objs\\*.obj");

            ExecuteTask(lt);
        }
    }
}

