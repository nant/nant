// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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

using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests available Task.
    /// </summary>
    [TestFixture]
    public class AvailableTest : BuildTestBase {
        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _tempFile = CreateTempFile("a.b");
            _tempDir = CreateTempDir("foo");

            // create a temporary file
            _notExistingTempFile = CreateTempFile("b.c");
            // delete it to make sure it definitely not exists
            if (_notExistingTempFile != null && File.Exists(_notExistingTempFile)) {
                File.Delete(_notExistingTempFile);
            }

            // create a temporary directory
            _notExistingTempDir = CreateTempDir("test");
            // delete it to make sure it definitely does not exist
            if (_notExistingTempDir != null && Directory.Exists(_notExistingTempDir)) {
                Directory.Delete(_notExistingTempDir);
            }
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        [Test]
        public void Test_ExistingFile() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='file.exists'/>
                    <echo message='file.exists={2}'/>
                </project>";
            
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xml, AvailableTask.ResourceType.File.ToString(CultureInfo.InvariantCulture), 
                _tempFile, "${file.exists}"));
            
            Assert.IsTrue(result.ToLower().IndexOf("file.exists=true") != -1,
                "File resource should have existed:" + result);
        }

        [Test]
        public void Test_NotExistingFile() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='file.exists'/>
                    <echo message='file.exists={2}'/>
                </project>";
            
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xml, AvailableTask.ResourceType.File.ToString(CultureInfo.InvariantCulture), 
                _notExistingTempFile, "${file.exists}"));
            
            Assert.IsTrue(result.ToLower().IndexOf("file.exists=false") != -1,
                "File resource not should have existed:" + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidFile() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='file.exists'/>
                    <echo message='file.exists={2}'/>
                </project>";
            // unix accepts most characters ( except / ) so this test won't fail there.
            // mono even on windows acts like unix here.
            if (!(PlatformHelper.IsMono)) {
                RunBuild(string.Format(CultureInfo.InvariantCulture, 
                    xml, AvailableTask.ResourceType.File.ToString(CultureInfo.InvariantCulture), 
                    "###-?", "${file.exists}"));
            } else {
                // throw the exception to keep the test happy
                throw new TestBuildException();                 
            }
        }

        [Test]
        public void Test_ExistingDirectory() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='dir.exists'/>
                    <echo message='dir.exists={2}'/>
                </project>";
            
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), 
                _tempDir, "${dir.exists}"));
            
            Assert.IsTrue(result.ToLower().IndexOf("dir.exists=true") != -1,
                "Directory resource should have existed:" + result);
        }

        [Test]
        public void Test_NotExistingDirectory() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='dir.exists'/>
                    <echo message='dir.exists={2}'/>
                </project>";
            
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), 
                _notExistingTempDir, "${dir.exists}"));
            
            Assert.IsTrue(result.ToLower().IndexOf("dir.exists=false") != -1,
                "Directory resource not should have existed:" + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidDirectory() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='dir.exists'/>
                    <echo message='dir.exists={2}'/>
                </project>";
            // unix accepts most characters ( except / ) so this test won't fail there.
            if (! PlatformHelper.IsUnix ) {
                RunBuild(string.Format(CultureInfo.InvariantCulture, 
                    xml, AvailableTask.ResourceType.Directory.ToString(CultureInfo.InvariantCulture), 
                    "|", "${dir.exists}"));
            }   else {
                throw new TestBuildException();    
            }
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_InvalidResourceType() {
            string xml= @"
                <project>
                    <available type='{0}' resource='{1}' property='file.exists'/>
                    <echo message='file.exists={2}'/>
                </project>";
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xml, "InvalidResourceType", @"\\#(){/}.dddd", "${file.exists}"));
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private string _tempFile;
        private string _tempDir;
        private string _notExistingTempFile;
        private string _notExistingTempDir;

        #endregion Private Instance Fields
    }
}
