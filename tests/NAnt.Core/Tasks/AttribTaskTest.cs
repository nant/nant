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

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class AttribTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0'?>
            <project>
                <attrib file='{0}' {1}>{2}</attrib>
            </project>";

        string _tempFileName;
        FileAttributes _normalFileAttributes;

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _tempFileName = Path.Combine(TempDirName, "myfile.txt");
            TempFile.Create(_tempFileName);
            File.SetAttributes(_tempFileName, FileAttributes.Normal);
            // Handle case when temp folder is compressed
            _normalFileAttributes = File.GetAttributes(_tempFileName) & (FileAttributes.Normal | FileAttributes.Compressed);
        }

        [Test]
        public void Test_Normal() {
            File.SetAttributes(_tempFileName, FileAttributes.Archive|FileAttributes.Hidden|FileAttributes.ReadOnly|FileAttributes.System);
            Assertion.Assert(_tempFileName + " should have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) != 0);
            Assertion.Assert(_tempFileName + " should have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) != 0);
            Assertion.Assert(_tempFileName + " should have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) != 0);
            Assertion.Assert(_tempFileName + " should have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) != 0);
            RunBuild(FormatBuildFile("normal='true'"));
            Assertion.Assert(_tempFileName + " should not have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) == 0);
            Assertion.Assert(_tempFileName + " should not have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) == 0);
            Assertion.Assert(_tempFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) == 0);
            Assertion.Assert(_tempFileName + " should not have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) == 0);
            Assertion.Assert(_tempFileName + " should have Normal file attribute.", (File.GetAttributes(_tempFileName) & _normalFileAttributes) != 0);
        }

        /// <summary>
        /// Ensure that an invalid path causes a <see cref="BuildException" />
        /// to be thrown.
        /// </summary>
        [Test]
        public void Test_InvalidFilePath() {
            try {
                // execute build with invalid file path
                RunBuild(string.Format(CultureInfo.InvariantCulture, _format, "abc#?-}", "", ""));
                // have the test fail
                Assertion.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assertion.Assert((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
            }
        }

        [Test]
        public void Test_Archive() {
            Assertion.Assert(_tempFileName + " should not have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) == 0);
            RunBuild(FormatBuildFile("archive='true'"));
            Assertion.Assert(_tempFileName + " should have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) != 0);
        }
        
        [Test]
        public void Test_Hidden() {
            Assertion.Assert(_tempFileName + " should not have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) == 0);
            RunBuild(FormatBuildFile("hidden='true'"));
            Assertion.Assert(_tempFileName + " should have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) != 0);
        }

        [Test]
        public void Test_ReadOnly() {
            Assertion.Assert(_tempFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) == 0);
            RunBuild(FormatBuildFile("readonly='true'"));
            Assertion.Assert(_tempFileName + " should have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) != 0);
        }

        [Test]
        public void Test_System() {
            Assertion.Assert(_tempFileName + " should not have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) == 0);
            RunBuild(FormatBuildFile("system='true'"));
            Assertion.Assert(_tempFileName + " should have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) != 0);
        }
        
        [Test]
        public void Test_Multiple() {
            for (int i = 0; i < 10; i++) {
                string fileName = Path.Combine(TempDirName, "myfile" + i + ".txt");
                TempFile.Create(fileName);
            }

            // pick any of the just created files for testing
            string testFileName = Path.Combine(TempDirName, "myfile8.txt");

            Assertion.Assert(testFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(testFileName) & FileAttributes.ReadOnly) == 0);
            string result = RunBuild(FormatBuildFile("verbose='true' readonly='true'", "<fileset basedir='" + TempDirName + "'><includes name='**/*.txt'/></fileset>"));
            Assertion.Assert(testFileName + " should have ReadOnly file attribute.", (File.GetAttributes(testFileName) & FileAttributes.ReadOnly) != 0);

            // check for valid output
            Assertion.Assert("Build output should include names of all files changed." + Environment.NewLine + result, result.IndexOf("myfile8.txt") != 0);
            Assertion.Assert("Build output should include count of all files changed." + Environment.NewLine + result, result.IndexOf("11 files") != 0);
            Assertion.Assert("Build output should include file attributes set." + Environment.NewLine + result, result.IndexOf("ReadOnly") != 0);
            Assertion.Assert("Build output should name specified in file attribute." + Environment.NewLine + result, result.IndexOf("myfile.txt") != 0);
        }

        private string FormatBuildFile(string attributes) {
            return FormatBuildFile(attributes, "");
        }

        private string FormatBuildFile(string attributes, string nestedElements) {
            return string.Format(CultureInfo.InvariantCulture, _format, _tempFileName, attributes, nestedElements);
        }
    }
}
