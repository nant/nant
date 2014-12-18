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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace Tests.NAnt.VisualCpp.Tasks {
    [TestFixture]
    public class RcTaskTest : VisualCppTestBase {
        private string _resInputFile;
        private string _resDir;

        private const string _projectXml = @"<?xml version='1.0'?>
                <project>
                    <rc rcfile=""{0}"" {1} />
                </project>";

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _resDir = CreateTempDir("res");
            _resInputFile = CreateTempFile(Path.Combine(_resDir, "test.rc"), 
                string.Empty);
        }

        /// <summary>
        /// Test to make sure simple compile works.
        /// </summary>
        [Test]
        public void Test_Compile_DefaultOutputFile() {
            if (!ResourceCompilerPresent) {
                Assert.Ignore("The Resource Compiler (rc.exe) is not available on the PATH.");
            }

            RunBuild(FormatBuildFile(_resInputFile, ""));
            Assert.IsTrue(File.Exists(Path.ChangeExtension(_resInputFile, "RES")),
                "Default compiled resource not created.");
        }

        /// <summary>
        /// Test to make sure an up-to-date res file is not recompiled.
        /// </summary>
        [Test]
        public void Test_Output_UpToDate() {
            if (!ResourceCompilerPresent) {
                Assert.Ignore("The Resource Compiler (rc.exe) is not available on the PATH.");
            }

            string outputFile = CreateTempFile("output.res");
            DateTime orgLastModified = File.GetLastWriteTime(outputFile);

            // wait for a second to make sure we would get another lastwritetime
            // if the output file is rebuilt
            System.Threading.Thread.Sleep(1000);

            RunBuild(FormatBuildFile(_resInputFile, "output=\"" + outputFile + "\""));

            DateTime newLastModified = File.GetLastWriteTime(outputFile);
            Assert.AreEqual(orgLastModified, newLastModified, 
                "output file should not have been rebuilt");
        }

        /// <summary>
        /// Test to make sure that a modification of an external file that is 
        /// referenced by a rc file caused the rc file to be rebuilt (bug #1195320).
        /// </summary>
        [Test]
        public void Test_External_Files() {
            if (!ResourceCompilerPresent) {
                Assert.Ignore("The Resource Compiler (rc.exe) is not available on the PATH.");
            }

            string xmlFile = CreateTempFile(Path.Combine(_resDir, "description.xml"), 
                "<root/>");
            string resFile = CreateTempFile(Path.Combine(_resDir, "test-external.rc"), 
                "IDR_XML_DESCRIPTION     XML               \"description.xml\"");
            string outputFile = CreateTempFile(Path.Combine(_resDir, "output.res"));

            RunBuild(FormatBuildFile(resFile, "output=\"" + outputFile + "\""));

            DateTime orgLastModified = File.GetLastWriteTime(outputFile);

            // wait for a second to make sure we would get another lastwritetime
            // if the output file is rebuilt
            System.Threading.Thread.Sleep(1000);

            RunBuild(FormatBuildFile(resFile, "output=\"" + outputFile + "\""));

            DateTime newLastModified = File.GetLastWriteTime(outputFile);
            Assert.AreEqual(orgLastModified, newLastModified, 
                "output file should not have been rebuilt");

            // "modify" xml file
            File.SetLastWriteTime(xmlFile, DateTime.Now);

            // rc file should now be recompiled
            RunBuild(FormatBuildFile(resFile, "output=\"" + outputFile + "\""));

            // verify whether rc file was rebuilt
            newLastModified = File.GetLastWriteTime(outputFile);
            Assert.IsTrue(orgLastModified != newLastModified, 
                "output file should have been rebuilt");
        }

        private string FormatBuildFile(string inputFile, string extra) {
            return string.Format(CultureInfo.InvariantCulture, _projectXml, inputFile, extra);
        }
    }
}
