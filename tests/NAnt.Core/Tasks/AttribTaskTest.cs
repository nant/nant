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

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {

    public class AttribTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0'?>
            <project>
                <attrib file='{0}' {1}>{2}</attrib>
            </project>";

        string _tempFileName;

		public AttribTaskTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
			_tempFileName = Path.Combine(TempDirName, "myfile.txt");
            TempFile.Create(_tempFileName);
            File.SetAttributes(_tempFileName, FileAttributes.Normal);
		}

        public void Test_Normal() {
            File.SetAttributes(_tempFileName, FileAttributes.Archive|FileAttributes.Hidden|FileAttributes.ReadOnly|FileAttributes.System);
            Assert(_tempFileName + " should have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) != 0);
            Assert(_tempFileName + " should have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) != 0);
            Assert(_tempFileName + " should have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) != 0);
            Assert(_tempFileName + " should have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) != 0);
            RunBuild(FormatBuildFile("normal='true'"));
            Assert(_tempFileName + " should not have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) == 0);
            Assert(_tempFileName + " should not have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) == 0);
            Assert(_tempFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) == 0);
            Assert(_tempFileName + " should not have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) == 0);
            Assert(_tempFileName + " should have Normal file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Normal) != 0);
        }

        public void Test_Archive() {
            Assert(_tempFileName + " should not have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) == 0);
            RunBuild(FormatBuildFile("archive='true'"));
            Assert(_tempFileName + " should have Archive file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Archive) != 0);
        }

        public void Test_Hidden() {
            Assert(_tempFileName + " should not have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) == 0);
            RunBuild(FormatBuildFile("hidden='true'"));
            Assert(_tempFileName + " should have Hidden file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.Hidden) != 0);
        }

        public void Test_ReadOnly() {
            Assert(_tempFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) == 0);
            RunBuild(FormatBuildFile("readonly='true'"));
            Assert(_tempFileName + " should have ReadOnly file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.ReadOnly) != 0);
        }

        public void Test_System() {
            Assert(_tempFileName + " should not have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) == 0);
            RunBuild(FormatBuildFile("system='true'"));
            Assert(_tempFileName + " should have System file attribute.", (File.GetAttributes(_tempFileName) & FileAttributes.System) != 0);
        }

        public void Test_Multiple() {
            for (int i = 0; i < 10; i++) {
                string fileName = Path.Combine(TempDirName, "myfile" + i + ".txt");
                TempFile.Create(fileName);
            }

            // pick any of the just created files for testing
            string testFileName = Path.Combine(TempDirName, "myfile8.txt");

            Assert(testFileName + " should not have ReadOnly file attribute.", (File.GetAttributes(testFileName) & FileAttributes.ReadOnly) == 0);
            string result = RunBuild(FormatBuildFile("verbose='true' readonly='true'", "<fileset basedir='" + TempDirName + "'><includes name='**/*.txt'/></fileset>"));
            Assert(testFileName + " should have ReadOnly file attribute.", (File.GetAttributes(testFileName) & FileAttributes.ReadOnly) != 0);

            // check for valid output
            Assert("Build output should include names of all files changed.\n" + result, result.IndexOf("myfile8.txt") != 0);
            Assert("Build output should include count of all files changed.\n" + result, result.IndexOf("11 files") != 0);
            Assert("Build output should include file attributes set.\n" + result, result.IndexOf("ReadOnly") != 0);
            Assert("Build output should name specified in file attribute.\n" + result, result.IndexOf("myfile.txt") != 0);
        }

        private string FormatBuildFile(string attributes) {
            return FormatBuildFile(attributes, "");
        }

        private string FormatBuildFile(string attributes, string nestedElements) {
            return String.Format(_format, _tempFileName, attributes, nestedElements);
        }
    }
}
