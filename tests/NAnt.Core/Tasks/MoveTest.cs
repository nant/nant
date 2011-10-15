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

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests <see cref="MoveTask" />.
    /// </summary>
    [TestFixture]
    public class MoveTest : BuildTestBase {
        #region Private Instance Fields

        private string _tempDirDest;
        private string _tempFileSrc;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _xmlProjectTemplate = 
            "<project>" 
                + "<move file=\"{0}\" tofile=\"{1}\" overwrite=\"{2}\" />"
            + "</project>";
        
        #endregion Private Static Fields

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _tempDirDest = CreateTempDir("foob");
            _tempFileSrc = CreateTempFile("foo.xml", "SRC");
        }

        [Test]
        public void NoOverwrite_Destination_DoesNotExist() {
            string tempFileDest = Path.Combine(_tempDirDest, "foo.xml");

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "false"));

            Assert.IsFalse(File.Exists(_tempFileSrc), "#1:" + result);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDirDest, "foo.xml")), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void NoOverwrite_Destination_Newer() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            // ensure destination file is more recent than source file
            File.SetLastWriteTime(tempFileDest, DateTime.Now.AddDays(1));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "false"));

            Assert.IsTrue(File.Exists(_tempFileSrc), "#1:" + result);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDirDest, "foo.xml")), "#2:" + result);

            using (StreamReader sr = new StreamReader (_tempFileSrc, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3:" + result);
            }

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("DEST", sr.ReadToEnd (), "#4:" + result);
            }
        }

        [Test]
        public void NoOverwrite_Destination_Older() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            // ensure source file is more recent than destination file
            File.SetLastWriteTime(_tempFileSrc, DateTime.Now.AddDays(1));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "false"));

            Assert.IsFalse(File.Exists(_tempFileSrc), "#1:" + result);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDirDest, "foo.xml")), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void NoOverwrite_Destination_Same() {
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, _tempFileSrc, "false"));

            Assert.IsTrue(File.Exists(_tempFileSrc), "#1:" + result);

            using (StreamReader sr = new StreamReader (_tempFileSrc, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#2");
            }
        }

        [Test]
        public void NoOverwrite_Destination_UpToDate() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            // ensure destination file has same write time than source file
            File.SetLastWriteTime(tempFileDest, File.GetLastWriteTime (_tempFileSrc));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "false"));

            Assert.IsTrue (File.Exists (_tempFileSrc), "#1:" + result);
            Assert.IsTrue (File.Exists (tempFileDest), "#2:" + result);

            using (StreamReader sr = new StreamReader (_tempFileSrc, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("DEST", sr.ReadToEnd (), "#4");
            }
        }

        [Test]
        public void NoOverwrite_Source_DoesNotExist() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            File.Delete (_tempFileSrc);

            try {
                string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                    _xmlProjectTemplate, _tempFileSrc, tempFileDest, "false"));
                Assert.Fail (result);
            } catch (TestBuildException) {
                // just catch the exception
            }

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1");
            Assert.IsTrue (File.Exists (tempFileDest), "#2");

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("DEST", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void Overwrite_Destination_DoesNotExist() {
            string tempFileDest = Path.Combine(_tempDirDest, "foo.xml");

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "true"));

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1:" + result);
            Assert.IsTrue (File.Exists (tempFileDest), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void Overwrite_Destination_Newer() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            // ensure destination file is more recent than source file
            File.SetLastWriteTime(tempFileDest, DateTime.Now.AddDays(1));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "true"));

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1:" + result);
            Assert.IsTrue (File.Exists (tempFileDest), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void Overwrite_Destination_Older() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"));
            // ensure source file is more recent than destination file
            File.SetLastWriteTime(_tempFileSrc, DateTime.Now.AddDays(1));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "true"));

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1:" + result);
            Assert.IsTrue (File.Exists (tempFileDest), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#3");
            }
        }

        [Test]
        public void Overwrite_Destination_Same() {
            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, _tempFileSrc, "true"));

            Assert.IsTrue(File.Exists(_tempFileSrc), "#1:" + result);

            using (StreamReader sr = new StreamReader (_tempFileSrc, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#2");
            }
        }

        [Test]
        public void Overwrite_Destination_UpToDate() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            // ensure destination file has same write time than source file
            File.SetLastWriteTime(tempFileDest, File.GetLastWriteTime (_tempFileSrc));

            string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                _xmlProjectTemplate, _tempFileSrc, tempFileDest, "true"));

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1:" + result);
            Assert.IsTrue (File.Exists (tempFileDest), "#2:" + result);

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("SRC", sr.ReadToEnd (), "#4");
            }
        }

        [Test]
        public void Overwrite_Source_DoesNotExist() {
            string tempFileDest = CreateTempFile(Path.Combine(_tempDirDest, "foo.xml"), "DEST");
            File.Delete (_tempFileSrc);

            try {
                string result = RunBuild(string.Format(CultureInfo.InvariantCulture,
                    _xmlProjectTemplate, _tempFileSrc, tempFileDest, "true"));
                Assert.Fail (result);
            } catch (TestBuildException) {
                // just catch the exception
            }

            Assert.IsFalse (File.Exists (_tempFileSrc), "#1");
            Assert.IsTrue (File.Exists (tempFileDest), "#2");

            using (StreamReader sr = new StreamReader (tempFileDest, Encoding.UTF8, true)) {
                Assert.AreEqual ("DEST", sr.ReadToEnd (), "#3");
            }
        }

        /// <summary>
        /// When multiple source files match the same destination file, then 
        /// only the last updated file should actually be moved.
        /// </summary>
        [Test]
        public void Test_Move_Files_Flatten() {
            const string buildXml = @"
                <project>
                    <mkdir dir='dest' />
                    <mkdir dir='src/dir1' />
                    <mkdir dir='src/dir2' />
                    <mkdir dir='src/dir3' />
                    <touch file='dest/uptodate.txt' datetime='02/01/2000' />
                    <touch file='src/uptodate.txt' datetime='03/01/2000' />
                    <touch file='src/dir1/uptodate.txt' datetime='05/01/2000' />
                    <touch file='src/dir2/uptodate.txt' datetime='03/01/2000' />
                    <touch file='src/dir3/uptodate.txt' datetime='04/01/2000' />
                    <move todir='dest' flatten='true' overwrite='true'>
                        <fileset>
                            <include name='src/**' />
                        </fileset>
                    </move>
                    <fail unless=""${datetime::get-day(file::get-last-write-time('dest/uptodate.txt')) == 1}"">#1</fail>
                    <fail unless=""${datetime::get-month(file::get-last-write-time('dest/uptodate.txt')) == 5}"">#2</fail>
                    <fail unless=""${datetime::get-year(file::get-last-write-time('dest/uptodate.txt')) == 2000}"">#3</fail>
                    <fail unless=""${file::exists('src/uptodate.txt')}"">#4</fail>
                    <fail if=""${file::exists('src/dir1/uptodate.txt')}"">#5</fail>
                    <fail unless=""${file::exists('src/dir2/uptodate.txt')}"">#6</fail>
                    <fail unless=""${file::exists('src/dir3/uptodate.txt')}"">#7</fail>
                </project>";

            RunBuild(buildXml);
        }
    }
}
