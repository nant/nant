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
using System.Text.RegularExpressions;
using X = System.Xml;

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
                    Assert.IsTrue(0 == ConsoleDriver.Main(new string[] {"-buildfile:" + filename}), "Using filepath failed");
                    //check absolute
                    Assert.IsTrue(0 == ConsoleDriver.Main(new string[] {@"-buildfile:" + build1FileName}), "Using absolute filepath failed");
                    //check relative path, should be resolvable via currentdirectory
                    Assert.IsTrue(0 == ConsoleDriver.Main(new string[] {string.Format("-buildfile:.{0}{1}", Path.DirectorySeparatorChar, filename)}), "Using relative filepath failed #1");
                    //check relative path, should be resolvable via currentdirectory
                    Assert.IsTrue(0 == ConsoleDriver.Main(new string[] {string.Format("-buildfile:..{0}foo{0}{1}", Path.DirectorySeparatorChar, filename)}), "Using relative filepath failed #2");
                } catch (Exception e) {
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

        [Test]
        public void Test_GetBuildFileName() {
            try {
                ConsoleDriver.GetBuildFileName(null, null, false);
                Assert.Fail("Exception not thrown.");
            } catch {
            }

            string baseDirectory = TempDir.Create(Path.Combine(TempDirName, "GetBuildFileName"));
            string build1FileName = Path.Combine(baseDirectory, "file1.build");
            string build2FileName = Path.Combine(baseDirectory, "file2.build");

            try {
                ConsoleDriver.GetBuildFileName(baseDirectory, null, false);
                Assert.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }

            TempFile.Create(build1FileName);

            Assert.AreEqual(build1FileName, ConsoleDriver.GetBuildFileName(Path.GetDirectoryName(build1FileName), null, false));

            // create a second build file in same directory
            TempFile.Create(build2FileName);
            Assert.AreEqual(Path.GetDirectoryName(build1FileName), Path.GetDirectoryName(build2FileName));

            try {
                ConsoleDriver.GetBuildFileName(Path.GetDirectoryName(build1FileName), null, false);
                Assert.Fail("ApplicationException not thrown.");
            } catch (ApplicationException) {
            }
        }

        [Test]
        public void Test_FindInParentOption() {
            string baseDirectory = TempDir.Create(Path.Combine(TempDirName, "Find"));
            string buildFileName = Path.Combine(baseDirectory, "file.build");
            string subDirectory = TempDir.Create(Path.Combine(baseDirectory, "SubDirectory"));

            // create a build file
            TempFile.Create(buildFileName);

            // find the build file from the sub directory
            Assert.AreEqual(buildFileName, ConsoleDriver.GetBuildFileName(subDirectory, null, true));

            // create a second build file
            string secondBuildFileName = Path.Combine(baseDirectory, "file2.build");
            TempFile.Create(secondBuildFileName);

            // try to find build file in sub directory
            // expect an exception - multiple *.build files found
            try {
                ConsoleDriver.GetBuildFileName(subDirectory, null, true);
                Assert.Fail("ApplicationException not thrown (#1).");
            } catch (ApplicationException) {
            }

            // try to find a build file that doesn't exist
            // expect an exception - build file not found
            //
            // however, we might find a "default.build" file if there's one in
            // one of the parent directories (eg. the root)
            try {
                string buildFile = ConsoleDriver.GetBuildFileName(subDirectory, "foobarmustnotexist.xml", true);
                if (Path.GetFileName(buildFile) != "default.build") {
                    Assert.Fail("ApplicationException not thrown (#2).");
                }
            } catch (ApplicationException) {
            }

            // try to find a build file with a bad pattern
            try {
                // buildFileName has a full path while GetBuildFileName will only accept a filename/pattern or null.
                ConsoleDriver.GetBuildFileName(subDirectory, buildFileName, true);
                Assert.Fail("Exception not thrown.");
            } catch {
            }

            // try to find specific build file in sub directory (expect success)
            Assert.AreEqual(buildFileName, ConsoleDriver.GetBuildFileName(subDirectory, Path.GetFileName(buildFileName), true));
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
            string expression = @"^NAnt (?<infoMajor>[0-9]+).(?<infoMinor>[0-9]+) "
                 + @"\(Build (?<buildMajor>[0-9]+).(?<buildMinor>[0-9]+).(?<buildBuild>[0-9]+).(?<buildRevision>[0-9]+); "
                 + @"(?<configuration>.*); (?<releasedate>.*)\)"
                 + ".*\n" + @"Copyright \(C\) 2001-(?<year>20[0-9][0-9]) Gerry Shaw";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Help text does not appear to be valid.");
            int infoMajor = Int32.Parse(match.Groups["infoMajor"].Value);
            int infoMinor = Int32.Parse(match.Groups["infoMinor"].Value);
            int buildMajor = Int32.Parse(match.Groups["buildMajor"].Value);
            int buildMinor = Int32.Parse(match.Groups["buildMinor"].Value);
            int buildBuild = Int32.Parse(match.Groups["buildBuild"].Value);
            int buildRevision = Int32.Parse(match.Groups["buildRevision"].Value);
            int year  = Int32.Parse(match.Groups["year"].Value);
            Assert.IsTrue(infoMajor >= 0, "Version numbers must be positive.");
            Assert.IsTrue(infoMinor >= 0, "Version numbers must be positive.");
            Assert.IsTrue(buildMajor >= 0, "Version numbers must be positive.");
            Assert.IsTrue(buildMinor >= 0, "Version numbers must be positive.");
            Assert.IsTrue(buildBuild >= 0, "Version numbers must be positive.");
            Assert.IsTrue(buildRevision >= 0, "Version numbers must be positive.");
            Assert.IsTrue(year <= DateTime.Now.Year, "Copyright year should be equal or less than current year.");
        }

        [Test]
        public void Test_UnknownArgument() {
            string[] args = { "-asdf", "-help", "-verbose" };

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Unknown argument '-asdf'";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_DuplicateArgument() {
            string[] args = {"-buildfile:test", "-buildfile:test"};

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Duplicate command-line argument '-buildfile'.";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_InvalidBoolValue() {
            string[] args = {"-debug:test"};

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Invalid value 'test' for command-line argument '-debug'.";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_DuplicateCollectionValue() {
            string[] args = {"-listener:test", "-listener:test"};

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Duplicate value 'test' for command-line argument '-listener'.";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_MissingValueForNameValuePair() {
            string[] args = {"-D:test", "-D:test"};

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Expected name\/value pair \(<name>=<value>\).";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_MissingNameForNameValuePair() {
            string[] args = {"-D:test=", "-D:test="};

            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.Main(args);
                result = c.Close();
            }

            // using a regular expression to check for correct error message
            string expression = @"Duplicate property named 'test' for command-line argument 'D'.";

            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "Argument did not cause an error: " + result);
        }

        [Test]
        public void Test_DefineProperty() {
            string buildFileContents = @"<?xml version='1.0' ?>
                <project name='Test' default='test' basedir='.'>
                    <target name='test'>
                        <property name='project.name' value='Foo.Bar' overwrite='false' />
                        <echo message='project.name = ${project.name}'/>
                    </target>
                </project>";

            // write build file to temp file
            string buildFileName = CreateTempFile("buildfile.xml", buildFileContents);
            Assert.IsTrue(File.Exists(buildFileName), buildFileName + " does not exist.");

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
            Assert.IsTrue(match.Success, "Property 'project.name' appears to have been overridden by <property> task." + Environment.NewLine + result);

            // delete the build file
            File.Delete(buildFileName);
            Assert.IsFalse(File.Exists(buildFileName), buildFileName + " exists.");
        }

        [Test]
        public void Test_ShowProjectHelp() {
           DoTestShowProjectHelp(string.Empty,string.Empty);
        }
        [Test]
        public void Test_ShowProjectHelpWithNamespace() {
            DoTestShowProjectHelp("urn:nant",string.Empty);
        }
        [Test]
        public void Test_ShowProjectHelpWithNamespacePrefix() {
            DoTestShowProjectHelp("urn:nant","n");
        }
        public void DoTestShowProjectHelp(string nantNamespace, string prefix) {
            string buildFileContents = @"<?xml version='1.0' ?>
                <{1}{2}project {0} name='Hello World' default='build' basedir='.'>

                    <{1}{2}property name='basename' value='HelloWorld'/>
                    <{1}{2}target name='init'/> <!-- fake subtarget for unit test -->

                    <{1}{2}target name='clean' description='cleans build directory'>
                        <{1}{2}delete file='${{basename}}.exe' failonerror='false'/>
                    </{1}{2}target>

                    <{1}{2}target name='build' description='compiles the source code'>
                        <{1}{2}csc target='exe' output='${{basename}}.exe'>
                            <{1}{2}sources>
                                <{1}{2}include name='${{basename}}.cs'/>
                            </{1}{2}sources>
                        </{1}{2}csc>
                    </{1}{2}target>

                    <{1}{2}target name='test' depends='build' description='run the program'>
                        <{1}{2}exec program='${{basename}}.exe'/>
                    </{1}{2}target>
                </{1}{2}project>";

            string colon = prefix.Length == 0? string.Empty: ":";
            string namespaceDecl = nantNamespace.Length == 0? string.Empty:
                string.Format("xmlns{2}{1}=\"{0}\"",nantNamespace,prefix,colon);
            // write build file to temp file
            string buildFileName = CreateTempFile("buildfile.xml", string.Format(buildFileContents,namespaceDecl,prefix,colon));
            Assert.IsTrue(File.Exists(buildFileName), buildFileName + " does not exist.");         

            X.XmlDocument document = new Project(buildFileName,Level.Warning,0).Document;
            Assert.AreEqual(nantNamespace,document.DocumentElement.NamespaceURI);
            string result = null;
            using (ConsoleCapture c = new ConsoleCapture()) {
                ConsoleDriver.ShowProjectHelp(document);
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

            // using a regular expression to look for valid output
            // expression created by RegEx http://www.organicbit.com/regex/
            string expression = @"Default Target:[\s]*(?<default>build)\s*compiles the source code[\s]*Main Targets:[\s]*(?<main1>build)\s*compiles the source code[\s]*(?<main2>clean)\s*cleans build directory[\s]*(?<main3>test)\s*run the program[\s]*Sub Targets:[\s]*(?<subtarget1>init)";

            Match match = Regex.Match(result, expression);
            if (match.Success) {
                Assert.AreEqual("build", match.Groups["default"].Value);
                Assert.AreEqual("build", match.Groups["main1"].Value);
                Assert.AreEqual("clean", match.Groups["main2"].Value);
                Assert.AreEqual("test", match.Groups["main3"].Value);
                Assert.AreEqual("init", match.Groups["subtarget1"].Value);
            } else {
                Assert.Fail("Project help text does not appear to be valid, see results for details:" + Environment.NewLine + result);
            }

            // delete the build file
            File.Delete(buildFileName);
            Assert.IsFalse(File.Exists(buildFileName), buildFileName + " exists.");
        }

        [Test]
        public void Test_CreateLogger() {
            string xmlLogger = "NAnt.Core.XmlLogger";
            string defaultLogger = "NAnt.Core.DefaultLogger";
            string badLogger = "NAnt.Core.LoggerThatDoesNotExistUnlessSomeJerkCreatedIt";
            string notLogger = "NAnt.Core.Task";

            IBuildLogger logger;

            logger = ConsoleDriver.CreateLogger(xmlLogger);
            Assert.AreEqual(typeof(XmlLogger), logger.GetType());

            logger = ConsoleDriver.CreateLogger(defaultLogger);
            Assert.AreEqual(typeof(DefaultLogger), logger.GetType());

            try {
                logger = ConsoleDriver.CreateLogger(badLogger);
                Assert.Fail("Test_CreateLogger did not throw an exception.");
            } catch (Exception e) {
                Assert.AreEqual(typeof(TypeLoadException), e.GetType());
            }

            try {
                logger = ConsoleDriver.CreateLogger(notLogger);
                Assert.Fail("Test_CreateLogger did not throw an exception.");
            } catch (Exception e) {
                // on .NET 2.0 or higher, instantiating an abstract class with
                // cause a MissingMethodException to be thrown
                if (Environment.Version.Major >= 2) {
                    Assert.AreEqual(typeof(MissingMethodException), e.GetType());
                } else {
                    Assert.AreEqual(typeof(MemberAccessException), e.GetType());
                }
            }
        }

        [Test]
        public void Test_CreateLoggerWithFile() {
            string xmlLogger = "NAnt.Core.XmlLogger";
            string consoleLogger = "NAnt.Core.DefaultLogger";

            IBuildLogger logger;

            string streamFileName = CreateTempFile("XmlLog.xml");
            StreamWriter instanceFileStream = new StreamWriter(File.OpenWrite(streamFileName));

            try {
                logger = ConsoleDriver.CreateLogger(xmlLogger);
                Assert.AreEqual(typeof(XmlLogger), logger.GetType());

                logger = ConsoleDriver.CreateLogger(consoleLogger);
                logger.OutputWriter = instanceFileStream;
            } finally {
                instanceFileStream.Close();
                File.Delete(streamFileName);
            }
        }
    }
}
#endif
