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

namespace Tests.NAnt.VisualCpp.Tasks {
    [TestFixture]
    public class ClTaskTest_HelloWorld : VisualCppTestBase {
        string _objDir;
        string _sourceDir;

        const string _test_build = @"<?xml version='1.0'?>
                <project>
                    <cl outputdir=""objs""
                        options=""-Zi -MDd -GA -Gz -YX"" >
                        <sources>
                            <include name=""src\HelloWorld.cpp"" />
                        </sources>
                        <defines>
                            <define name=""WIN32"" />
                            <define name=""UNICODE"" />
                            <define name=""DEBUG"" />
                            <define name=""_DEBUG"" />
                            <define name=""TEST"" if=""false"" />
                        </defines>
                    </cl>
                </project>";
        const string _helloWorld_cpp = @"
                #include <stdio.h>
                #ifdef TEST
                    causes error
                #endif
                void main(void) {
                    printf(""Hello, World."");
                }";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _objDir = CreateTempDir("objs");
            _sourceDir = CreateTempDir("src");
            CreateTempFile(Path.Combine(_sourceDir, "HelloWorld.cpp"), _helloWorld_cpp);
        }

        /// <summary>Test to make sure simple compile works.</summary>
        [Test]
        public void Test_HelloWorldCompile() {
            if (!CanCompileAndLink) {
                return;
            }

            RunBuild(_test_build);
            Assert.IsTrue(File.Exists(Path.Combine(_objDir, "HelloWorld.obj")),
                "Object file not created.");
        }
    }

    [TestFixture]
    public class ClTaskTest_CompileOnDemand : VisualCppTestBase {
        const int _sourceCount = 3;

        string _objDir;
        string _sourceDir;
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
                        options=""-Zi -MDd -GA -Gz -YX"" >
                        <sources>
                            <include name=""src\*.cpp"" />
                        </sources>
                        <defines>
                            <define name=""WIN32"" />
                            <define name=""UNICODE"" />
                            <define name=""DEBUG"" />
                            <define name=""_DEBUG"" />
                        </defines>
                    </cl>
                </project>";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _objDir = CreateTempDir("objs");
            _sourceDir = CreateTempDir("src");

            for (int i = 0; i < _sourceCount; ++i) {
                _sourcePathName[i] = CreateTempFile(Path.Combine(_sourceDir, _sourceFileName[i]), _sourceCode[i]);
                _objPathName[i] = Path.ChangeExtension(Path.Combine(_objDir, _sourceFileName[i]), ".obj");
            }
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

        /// <summary>Test to make sure compiling all files.</summary>
        [Test]
        public void Test_BuildAll() {
            if (!CanCompileAndLink) {
                return;
            }

            CleanAllObjs();
            RunBuild(_test_build);
            for (int i = 0; i < _sourceCount; ++i) {
                Assert.IsTrue(File.Exists(_objPathName[i]), "Object file \"{0}\" not created.", _objPathName[i]);
            }
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
            result = RunBuild(_test_build);
            result = RunBuild(_test_build);
            Assert.IsTrue(result.IndexOf("[cl]") == -1, "Shouldn't have compiled anything the second time around");
        }

        /// <summary>Test to make sure compiling happens when source files change.</summary>
        [Test]
        public void Test_BuildSourceChanged() {
            if (!CanCompileAndLink) {
                Assert.Ignore ("Compiler, linker or header files are not available"
                    + " or do not match the expected version.");
                return;
            }

            Test_BuildAll();

            for (int i = 0; i < _sourceCount; ++i) {
                File.SetLastWriteTime(_sourcePathName[i], DateTime.Now);
                RunBuild(_test_build);
                FileInfo sourceFileInfo = new FileInfo(_sourcePathName[i]);
                FileInfo objFileInfo = new FileInfo(_objPathName[i]);
                Assert.IsTrue(objFileInfo.LastWriteTime >= sourceFileInfo.LastWriteTime,
                    "{0} must be newer than {1}.", _objPathName[i], _sourcePathName[i]);
            }
        }
    }
}

