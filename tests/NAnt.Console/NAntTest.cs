#if (!BuildWithVSNet)
// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Console {
    [TestFixture]
    public class NAntTest : BuildTestBase {

        [Test]
        public void Test_BuildFileOption() {
            string filename = "file1.ha";
            string baseDirectory = TempDir.Create(Path.Combine(TempDirName, "foo"));
            string build1FileName = TempFile.CreateWithContents("<project/>", Path.Combine(baseDirectory, filename));

            string oldCurrDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = baseDirectory;
            using (ConsoleCapture c = new ConsoleCapture()) {
                bool errors = false;
                try {
                    //check filename only, should be resolvable via currentdirectory
                    Assertion.Assert("Using filepath failed", 0 == ConsoleDriver.Main(new string[] {"-buildfile:" + filename}));
                    //check absolute
                    Assertion.Assert("Using absolute filepath failed", 0 == ConsoleDriver.Main(new string[] {@"-buildfile:" + build1FileName}));
                    //check relative path, should be resolvable via currentdirectory
                    Assertion.Assert("Using relative filepath failed", 0 == ConsoleDriver.Main(new string[] {"-buildfile:.\\" + filename}));
                    //check relative path, should be resolvable via currentdirectory
                    Assertion.Assert("Using relative filepath failed", 0 == ConsoleDriver.Main(new string[] {"-buildfile:..\\foo\\" + filename}));
                }
                catch (Exception e) {
                    e.ToString();
                    errors = true;
                    throw;
                }
                finally {
                    string results = c.Close();
                    if(errors)
                        System.Console.Write(results);
                }
            }
            Environment.CurrentDirectory = oldCurrDir;
        }
 /*
         [Test]
        public void Test_BuildFileDoubleOption() {
            string filename1 = "file1.ha";
            string filename2 = "file2.ha";
            string build1FileName = TempFile.CreateWithContents("<project/>", Path.Combine(TempDirName, filename1));
            string build2FileName = TempFile.CreateWithContents("<project/>", Path.Combine(TempDirName, filename2));

            using (ConsoleCapture c = new ConsoleCapture()) {
                bool errors = false;
                try {

                    //check that error message is not generated always.
                    Assertion.Assert("Using filepath failed", 0 == ConsoleDriver.Main(new string[] {"-buildfile:" + build1FileName}));

                    //check absolute
                    Assertion.Assert("Using absolute filepath failed", 0 == ConsoleDriver.Main(new string[] {"-buildfile:" + build1FileName +" -buildfile:" + build2FileName}));
                }
                catch (Exception e) {
                    e.ToString();
                    errors = true;
                    throw;
                }
                finally {
                    string results = c.Close();
                    if(errors)
                        Console.Write(results);
                }
            }
        }
 */
        [Test]
        public void Test_GetBuildFileName() {
            try {
                ConsoleDriver.GetBuildFileName(null, null, false);
                Assertion.Fail("Exception not thrown.");
            } catch {
            }

            string baseDirectory = TempDir.Create(Path.Combine(TempDirName,  "GetBuildFileName"));
            string build1FileName = Path.Combine(baseDirectory, "file1.build");
            string build2FileName = Path.Combine(baseDirectory, "file2.build");

            try {
                ConsoleDriver.GetBuildFileName(baseDirectory, null, false);
                Assertion.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }

            TempFile.Create(build1FileName);

            Assertion.AssertEquals(build1FileName, ConsoleDriver.GetBuildFileName(Path.GetDirectoryName(build1FileName), null, false));

            // create a second build file in same directory
            TempFile.Create(build2FileName);
            Assertion.AssertEquals(Path.GetDirectoryName(build1FileName), Path.GetDirectoryName(build2FileName));

            try {
                ConsoleDriver.GetBuildFileName(Path.GetDirectoryName(build1FileName), null, false);
                Assertion.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }
        }

        [Test]
        public void Test_FindInParentOption() {
            string baseDirectory = TempDir.Create(Path.Combine(TempDirName,"Find"));
            string buildFileName = Path.Combine(baseDirectory, "file.build");
            string subDirectory = TempDir.Create(Path.Combine(baseDirectory, "SubDirectory"));

            // create a build file
            TempFile.Create(buildFileName);

            // find the build file from the sub directory
            Assertion.AssertEquals(buildFileName, ConsoleDriver.GetBuildFileName(subDirectory, null, true));

            // create a second build file
            string secondBuildFileName = Path.Combine(baseDirectory, "file2.build");
            TempFile.Create(secondBuildFileName);

            // try to find build file in sub directory
            // expect an exception - multiple *.build files found
            try {
                ConsoleDriver.GetBuildFileName(subDirectory, null, true);
                Assertion.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }

            // try to find a build file that doesn't exist
            // expect an exception - build file not found
            try {
                ConsoleDriver.GetBuildFileName(subDirectory, "foobar.xml", true);
                Assertion.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }

            // try to find a build file with a bad pattern
            try {
                // buildFileName has a full path while GetBuildFileName will only accept a filename/pattern or null.
                ConsoleDriver.GetBuildFileName(subDirectory, buildFileName, true);
                Assertion.Fail("Exception not thrown.");
            } catch {
            }

            // try to find specific build file in sub directory (expect success)
            Assertion.AssertEquals(buildFileName, ConsoleDriver.GetBuildFileName(subDirectory, Path.GetFileName(buildFileName), true));
        }
        
        [Test]
        public void Test_ShowHelp() {
            string[] args = { "-help" };

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression look for a plausible version number and valid copyright date
            string expression = @"^NAnt version (?<major>[0-9]+).(?<minor>[0-9]+).(?<build>[0-9]+).(?<revision>[0-9]+) Copyright \(C\) 2001-(?<year>200[0-9]) Gerry Shaw";

            Match match = Regex.Match(result, expression);
            Assertion.Assert("Help text does not appear to be valid.", match.Success);
            int major = Int32.Parse(match.Groups["major"].Value);
            int minor = Int32.Parse(match.Groups["minor"].Value);
            int build = Int32.Parse(match.Groups["build"].Value);
            int revision = Int32.Parse(match.Groups["revision"].Value);
            int year  = Int32.Parse(match.Groups["year"].Value);
            Assertion.Assert("Version numbers must be positive.", major >= 0);
            Assertion.Assert("Version numbers must be positive.", minor >= 0);
            Assertion.Assert("Version numbers must be positive.", build >= 0);
            Assertion.Assert("Version numbers must be positive.", revision >= 0);
            Assertion.AssertEquals(DateTime.Now.Year, year);
        }

        [Test]
        public void Test_BadArgument() {
            string[] args = { "-asdf", "-help", "-verbose" };

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression look for a plausible version number and valid copyright date
            string expression = @"Unknown argument '-asdf'";

            Match match = Regex.Match(result, expression);
            Assertion.Assert("Argument did not cause an error.", match.Success);
        }

        [Test]
        public void Test_DefineProperty() {
            string buildFileContents = @"<?xml version='1.0' ?>
                <project name='Test' default='test' basedir='.'>
                    <target name='test'>
                        <property name='project.name' value='Foo.Bar'/>
                        <echo message='project.name = ${project.name}'/>
                    </target>
                </project>";

            // write build file to temp file
            string buildFileName = TempFile.CreateWithContents(buildFileContents);
            Assertion.Assert(buildFileName + " does not exists.", File.Exists(buildFileName));

            string[] args = {
                "-D:project.name=MyCompany.MyProject",
                String.Format("-buildfile:{0}", buildFileName),
            };

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // regular expression to look for expected output
            string expression = @"project.name = MyCompany.MyProject";
            Match match = Regex.Match(result, expression);
            Assertion.Assert("Property 'project.name' appears to have been overridden by <property> task.\n" + result, match.Success);

            // delete the build file
            File.Delete(buildFileName);
            Assertion.Assert(buildFileName + " exists.", !File.Exists(buildFileName));
        }

        [Test]
        public void Test_ShowProjectHelp() {
            string buildFileContents = @"<?xml version='1.0' ?>
                <project name='Hello World' default='build' basedir='.'>

                    <property name='basename' value='HelloWorld'/>
                    <target name='init'/> <!-- fake subtarget for unit test -->

                    <target name='clean' description='cleans build directory'>
                        <delete file='${basename}.exe' failonerror='false'/>
                    </target>

                    <target name='build' description='compiles the source code'>
                        <csc target='exe' output='${basename}.exe'>
                            <sources>
                                <includes name='${basename}.cs'/>
                            </sources>
                        </csc>
                    </target>

                    <target name='test' depends='build' description='run the program'>
                        <exec program='${basename}.exe'/>
                    </target>
                </project>";

            // write build file to temp file
            string buildFileName = TempFile.CreateWithContents(buildFileContents);
            Assertion.Assert(buildFileName + " does not exists.", File.Exists(buildFileName));

            string[] args = {
                "-projecthelp",
                String.Format("-buildfile:{0}", buildFileName),
            };

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            /* expecting output in the form of"

                Default Target:

                 build         compiles the source code

                Main Targets:

                 clean         cleans build directory
                 build         compiles the source code
                 test          run the program

                Sub Targets:

                 init
            */

            // using a regular expression look for a plausible version number and valid copyright date
            // expression created by RegEx http://www.organicbit.com/regex/
            string expression = @"Default Target:[\s\S]*(?<default>build)\s*compiles the source code[\s\S]*Main Targets:[\s\S]*(?<main1>build)\s*compiles the source code[\s\S]*(?<main2>clean)\s*cleans build directory[\s\S]*(?<main3>test)\s*run the program[\s\S]*Sub Targets:[\s\S]*(?<subtarget1>init)";

            Match match = Regex.Match(result, expression);
            if (match.Success) {
                Assertion.AssertEquals("build", match.Groups["default"].Value);
                Assertion.AssertEquals("build", match.Groups["main1"].Value);
                Assertion.AssertEquals("clean", match.Groups["main2"].Value);
                Assertion.AssertEquals("test", match.Groups["main3"].Value);
                Assertion.AssertEquals("init", match.Groups["subtarget1"].Value);
            } else {
                Assertion.Fail("Project help text does not appear to be valid, see results for details:\n" + result);
            }

            // delete the build file
            File.Delete(buildFileName);
            Assertion.Assert(buildFileName + " exists.", !File.Exists(buildFileName));
        }

        [Test]
        public void Test_CreateLogger() {
            string xmlLogger = "NAnt.Core.XmlLogger";
            string defaultLogger = "NAnt.Core.DefaultLogger";
            string badLogger = "NAnt.Core.LoggerThatDoesNotExistUnlessSomeJerkCreatedIt";
            string notLogger = "NAnt.Core.Task";

            IBuildLogger logger;

            logger = ConsoleDriver.CreateLogger(xmlLogger);
            Assertion.AssertEquals(typeof(XmlLogger), logger.GetType());

            logger = ConsoleDriver.CreateLogger(defaultLogger);
            Assertion.AssertEquals(typeof(DefaultLogger), logger.GetType());

            try {
                logger = ConsoleDriver.CreateLogger(badLogger);
                Assertion.Fail("Test_CreateLogger did not throw an exception.");
            } catch(Exception e) {
                Assertion.AssertEquals(typeof(TypeLoadException), e.GetType());
            }

            try {
                logger = ConsoleDriver.CreateLogger(notLogger);
                Assertion.Fail("Test_CreateLogger did not throw an exception.");
            } catch(Exception e) {
                Assertion.AssertEquals(typeof(MemberAccessException), e.GetType());
            }
        }

        [Test]
        public void Test_CreateLoggerWithFile() {
            string xmlLogger = "NAnt.Core.XmlLogger";
            string consoleLogger = "NAnt.Core.DefaultLogger";

            IBuildLogger logger;

            string streamFileName = TempFile.Create();
            StreamWriter instanceFileStream = new StreamWriter(File.OpenWrite(streamFileName));

            try {
                logger = ConsoleDriver.CreateLogger(xmlLogger);
                Assertion.AssertEquals(typeof(XmlLogger), logger.GetType());

                try {
                    logger = ConsoleDriver.CreateLogger(consoleLogger);
                    logger.OutputWriter = instanceFileStream;
                } catch(Exception e) {
                    Assertion.AssertEquals(typeof(MissingMethodException), e.GetType());
                }
            } finally {
                instanceFileStream.Close();
                File.Delete(streamFileName);
            }
        }
    }
}
#endif