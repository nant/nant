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

// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;
using System.Globalization;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {
    /// <summary>
    /// Tests available Task.
    /// </summary>
    [TestFixture]
    public class AvailableTest : BuildTestBase {

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            tempFile = CreateTempFile("a.b");
            tempDir = CreateTempDir("foo");

            // create a temporary file
            notExistingTempFile = CreateTempFile("b.c");
            // delete it to make sure it definitely not exists
            if (notExistingTempFile != null && File.Exists(notExistingTempFile)) {
                File.Delete(notExistingTempFile);
            }

            // create a temporary directory
            notExistingTempDir = CreateTempDir("test");
            // delete it to make sure it definitely not exists
            if (notExistingTempDir != null && Directory.Exists(notExistingTempDir)) {
                Directory.Delete(notExistingTempDir);
            }
        }

        [Test]
        public void Test_ExistingFile() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='file.exists'/>
                <echo message='file.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.File.ToString(CultureInfo.InvariantCulture), tempFile, "${file.exists}"));
            
            Assertion.Assert("File resource should have existed:" + result, result.ToLower().IndexOf("file.exists=true") != -1);
        }

        [Test]
        public void Test_NotExistingFile() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='file.exists'/>
                <echo message='file.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.File.ToString(CultureInfo.InvariantCulture), notExistingTempFile, "${file.exists}"));
            
            Assertion.Assert("File resource not should have existed:" + result, result.ToLower().IndexOf("file.exists=false") != -1);
        }

        [Test]
        public void Test_InvalidFile() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='file.exists'/>
                <echo message='file.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), @"\\#(){/}.dddd", "${file.exists}"));
            
            Assertion.Assert("File resource not should have existed:" + result, result.ToLower().IndexOf("file.exists=false") != -1);
        }

        [Test]
        public void Test_ExistingDirectory() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='dir.exists'/>
                <echo message='dir.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), tempDir, "${dir.exists}"));
            
            Assertion.Assert("Directtory resource should have existed:" + result, result.ToLower().IndexOf("dir.exists=true") != -1);
        }

        [Test]
        public void Test_NotExistingDirectory() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='dir.exists'/>
                <echo message='dir.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), notExistingTempDir, "${dir.exists}"));
            
            Assertion.Assert("Directory resource not should have existed:" + result, result.ToLower().IndexOf("dir.exists=false") != -1);
        }

        [Test]
        public void Test_InvalidDirectory() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='dir.exists'/>
                <echo message='dir.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), "#(){/}", "${dir.exists}"));
            
            Assertion.Assert("Directory resource not should have existed:" + result, result.ToLower().IndexOf("dir.exists=false") != -1);
        }

        [Test]
        [ExpectedException(typeof(BuildException))]
        public void Test_InvalidResourceType() {
            string _xml= @"
            <project>
                <available type='{0}' resource='{1}' property='file.exists'/>
                <echo message='file.exists={2}'/>
            </project>";
            
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, "InvalidResourceType", @"\\#(){/}.dddd", "${file.exists}"));

            Assertion.Fail("A buildexception should have been thrown because of the invalid resource type:" + result);
        }

        #region Private Instance Fields

        string tempFile;
        string tempDir;
        string notExistingTempFile;
        string notExistingTempDir;

        #endregion Private Instance Fields
    }
}
