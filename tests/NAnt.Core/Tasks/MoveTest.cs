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
using System.Text;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests <see cref="MoveTask" />.
    /// </summary>
    [TestFixture]
    public class MoveTest : BuildTestBase {
        #region Private Instance Fields

        private string _tempDirDest;
        private string _tempFileSrc;

        private string _tempDirSourceOne;
        private string _tempDirSourceTwo;
        private string _tempDirSourceThree;
        private string _tempDirSourceFour;
        private string _tempFileSourceOne;
        private string _tempFileSourceTwo;
        private string _tempFileSourceThree;
        private string _tempFileSourceFour;
        private string _tempDirTargetOne;
        private string _tempDirTargetTwo;
        private string _tempDirTargetThree;
        private string _tempDirTargetFour;
        private string _tempFileTargetOne;
        private string _tempFileTargetTwo;
        private string _tempFileTargetThree;
        private string _tempFileTargetFour;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _xmlProjectTemplate = @"
            <project>
                <move file='{0}' tofile='{1}' overwrite='{2}' />
            </project>
        ";

        private const string _xmlProjectTemplate2 = @"
            <project>
                <move todir='{0}'>
                    <fileset basedir='{1}' />
                </move>
            </project>
        ";

        private const string _xmlProjectTemplate3 = @"
            <project>
                <move todir='{0}'>
                    <fileset basedir='{1}'>
                        <include name='{2}'/>
                    </fileset>
                </move>
            </project>
        ";

        private const string _xmlProjectTemplate4 = @"
            <project>
                <move verbose='true' file='{0}' todir='{1}' />
            </project>
        ";

        private const string _xmlProjectTemplate5 = @"
            <project>
                <move todir='{0}' includeemptydirs='false'>
                    <fileset basedir='{1}' />
                </move>
            </project>
        ";
        
        #endregion Private Static Fields

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _tempDirDest = CreateTempDir("foob");
            _tempFileSrc = CreateTempFile("foo.xml", "SRC");

            // The following vars are needed for directory moving tests.
            _tempDirSourceOne = CreateTempDir("dirA");
            _tempDirSourceTwo = CreateTempDir(Path.Combine("dirA", "subDir"));
            _tempDirSourceThree = CreateTempDir("dirE");
            _tempDirSourceFour = CreateTempDir(Path.Combine(_tempDirSourceThree, "CVS"));
            _tempFileSourceOne = CreateTempFile(Path.Combine(_tempDirSourceOne, "file.one"));
            _tempFileSourceTwo = CreateTempFile(Path.Combine(_tempDirSourceTwo, "file2.two"));
            _tempFileSourceThree = CreateTempFile(Path.Combine(_tempDirSourceThree, "file3.three"));
            _tempFileSourceFour = CreateTempFile(Path.Combine(_tempDirSourceFour, "file4.four"));
            _tempDirTargetOne = Path.Combine(TempDirName, "dirB");
            _tempDirTargetTwo = Path.Combine(_tempDirTargetOne, "subDir");
            _tempDirTargetThree = Path.Combine(_tempDirTargetOne, "dirX");
            _tempDirTargetFour = Path.Combine(_tempDirTargetThree, "CVS");
            _tempFileTargetOne = Path.Combine(_tempDirTargetOne, "file.one");
            _tempFileTargetTwo = Path.Combine(_tempDirTargetTwo, "file2.two");
            _tempFileTargetThree = Path.Combine(_tempDirTargetThree, "file3.three");
            _tempFileTargetFour = Path.Combine(_tempDirTargetFour, "file4.four");
        }

        /// <summary>
        /// Tests moving a directory using a fileset element.
        /// </summary>
        [Test]
        public void FilesetDirectoryMoveTest()
        {
            RunBuild(string.Format(_xmlProjectTemplate2, _tempDirTargetOne,
                _tempDirSourceOne));

            Assert.IsTrue(Directory.Exists(_tempDirTargetOne),
                string.Format("'{0}' directory does not exist", _tempDirTargetOne));
            Assert.IsTrue(File.Exists(_tempFileTargetOne),
                string.Format("'{0}' file does not exist", _tempFileTargetOne));

            Assert.IsTrue(Directory.Exists(_tempDirTargetTwo),
                string.Format("'{0}' directory does not exist", _tempDirTargetTwo));
            Assert.IsTrue(File.Exists(_tempFileTargetTwo),
                string.Format("'{0}' file does not exist", _tempFileTargetTwo));

            Assert.IsFalse(Directory.Exists(_tempDirSourceOne),
                string.Format("'{0}' directory still exists", _tempDirSourceOne));
            Assert.IsFalse(File.Exists(_tempFileSourceOne),
                string.Format("'{0}' file still exists", _tempFileSourceOne));

            Assert.IsFalse(Directory.Exists(_tempDirSourceTwo),
                string.Format("'{0}' directory still exists", _tempDirSourceTwo));
            Assert.IsFalse(File.Exists(_tempFileSourceTwo),
                string.Format("'{0}' file still exists", _tempFileSourceTwo));
        }

        /// <summary>
        /// Tests moving the contents of a directory using a fileset element.
        /// </summary>
        [Test]
        public void FilesetIncludeDirectoryMoveTest()
        {
            RunBuild(string.Format(_xmlProjectTemplate3, _tempDirTargetOne,
                _tempDirSourceOne, "**/*"));

            Assert.IsTrue(Directory.Exists(_tempDirTargetOne),
                string.Format("'{0}' directory does not exist", _tempDirTargetOne));
            Assert.IsTrue(File.Exists(_tempFileTargetOne),
                string.Format("'{0}' file does not exist", _tempFileTargetOne));

            Assert.IsTrue(Directory.Exists(_tempDirTargetTwo),
                string.Format("'{0}' directory does not exist", _tempDirTargetTwo));
            Assert.IsTrue(File.Exists(_tempFileTargetTwo),
                string.Format("'{0}' file does not exist", _tempFileTargetTwo));

            Assert.IsFalse(Directory.Exists(_tempDirSourceOne),
                string.Format("'{0}' directory still exists", _tempDirSourceOne));
            Assert.IsFalse(File.Exists(_tempFileSourceOne),
                string.Format("'{0}' file still exists", _tempFileSourceOne));

            Assert.IsFalse(Directory.Exists(_tempDirSourceTwo),
                string.Format("'{0}' directory still exists", _tempDirSourceTwo));
            Assert.IsFalse(File.Exists(_tempFileSourceTwo),
                string.Format("'{0}' file still exists", _tempFileSourceTwo));
        }

        /// <summary>
        /// A simple file move test.
        /// </summary>
        [Test]
        public void SimpleFileMoveTest()
        {
            RunBuild(String.Format(_xmlProjectTemplate, _tempFileSourceOne, _tempFileTargetOne, "true"));

            Assert.IsFalse(File.Exists(_tempFileSourceOne),
                string.Format("'{0}' file still exists", _tempFileSourceOne));
            Assert.IsTrue(File.Exists(_tempFileTargetOne),
                string.Format("'{0}' file does not exist", _tempFileTargetOne));
        }

        /// <summary>
        /// Tests moving select contents of a directory using a fileset element.
        /// </summary>
        [Test]
        public void SelectFileMoveTest()
        {
            RunBuild(string.Format(_xmlProjectTemplate3, _tempDirTargetOne,
                _tempDirSourceOne, "**/file.*"));

            Assert.IsTrue(Directory.Exists(_tempDirSourceOne),
                string.Format("'{0}' source directory does not exist", _tempDirSourceOne));
            Assert.IsTrue(Directory.Exists(_tempDirTargetOne),
                string.Format("'{0}' target directory does not exist", _tempDirTargetOne));

            Assert.IsFalse(File.Exists(_tempFileSourceOne),
                string.Format("'{0}' source file still exists", _tempFileSourceOne));
            Assert.IsTrue(File.Exists(_tempFileTargetOne),
                string.Format("'{0}' target file does not exist", _tempFileTargetOne));

            Assert.IsTrue(File.Exists(_tempFileSourceTwo),
                string.Format("'{0}' source file does not exists", _tempFileSourceTwo));
            Assert.IsFalse(File.Exists(_tempFileTargetTwo),
                string.Format("'{0}' target file does exist", _tempFileTargetTwo));
        }

        /// <summary>
        /// Simple file to dir move test.
        /// </summary>
        [Test]
        public void SimpleFileToDirMoveTest()
        {
            RunBuild(String.Format(_xmlProjectTemplate4, _tempFileSourceOne, _tempDirTargetOne));

            Assert.IsFalse(File.Exists(_tempFileSourceOne),
                string.Format("'{0}' file still exists", _tempFileSourceOne));
            Assert.IsTrue(File.Exists(_tempFileTargetOne),
                string.Format("'{0}' file does not exist", _tempFileTargetOne));
        }

        /// <summary>
        /// Tests to ensure that files of a subdirectory are moved without actually
        /// moving the subdirectory itself.
        /// </summary>
        /// <remarks>
        /// This should happen when the fileset base directory has other files/directories
        /// besides the subdirectory being moved.
        /// </remarks>
        [Test]
        public void MoveSubdirectoryOnlyTest()
        {
            string targetDir = CreateTempDir("dirAtarget");
            string targetSubDir = Path.Combine(targetDir, "subDir");
            string targetSubDirFile = Path.Combine(targetSubDir, "file2.two");
            RunBuild(String.Format(_xmlProjectTemplate3, targetDir, _tempDirSourceOne, "subDir/**"));

            Assert.IsTrue(Directory.Exists(targetSubDir),
                string.Format("'{0}' target sub directory does not exist", targetSubDir));

            Assert.IsTrue(File.Exists(targetSubDirFile),
                string.Format("'{0}' target sub directory file does not exist", targetSubDirFile));

            Assert.IsTrue(Directory.Exists(_tempDirSourceTwo),
                string.Format("'{0}' source sub directory does exist", _tempDirSourceTwo));

            Assert.IsFalse(File.Exists(_tempFileSourceTwo),
                string.Format("'{0}' source sub directory file does exist", _tempFileSourceTwo));

        }

        /// <summary>
        /// Renames a directory with the same name but different casing.
        /// </summary>
        [Test]
        public void RenameDirectoryToSameNameDifferenceCasingTest()
        {
            string sameNameSubDir = "Dira";
            string sameNameTarget = Path.Combine(TempDirName, sameNameSubDir);
            RunBuild(String.Format(_xmlProjectTemplate2, sameNameTarget, _tempDirSourceOne));

            // This should be true regardless of underlying OS NAnt is running on.
            Assert.IsTrue(Directory.Exists(sameNameTarget),
                string.Format("'{0}' directory does not exist", sameNameTarget));

            if (PlatformHelper.IsWindows)
            {
                // Because Windows is case-insensitive, need to make sure that 
                // the directory name's casing matches what is on the filesystem
                // after the move.
                DirectoryInfo parent = new DirectoryInfo(TempDirName);
                DirectoryInfo[] subDirs = parent.GetDirectories();
                bool foundCasing = false;

                foreach (DirectoryInfo subDir in subDirs)
                {
                    if (sameNameSubDir.Equals(subDir.Name, 
                        StringComparison.InvariantCulture))
                    {
                        foundCasing = true;
                        break;
                    }
                }

                if (!foundCasing)
                {
                    Assert.Fail("Directory '{0}' may exist but not in the expected casing: '{1}'",
                        _tempDirSourceOne, sameNameTarget);
                }
            }
            else
            {
                Assert.IsFalse(Directory.Exists(_tempDirSourceOne),
                    string.Format("'{0}' directory still exists", _tempDirSourceOne));
            }
        }

        private void PrintDirContents(DirectoryInfo dir)
        {
            if (!dir.Exists) return;
            foreach (FileInfo f in dir.GetFiles())
            {
                System.Console.WriteLine(f.FullName);
            }
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                System.Console.WriteLine(d.FullName);
                PrintDirContents(d);
            }
        }

        /// <summary>
        /// Tests empty directory moves when includeemptydir property is false.
        /// </summary>
        /// <remarks>
        /// Copy tasks for directories with includeemptydir='false' should only
        /// copy the entire directory structure if no empty directories are found
        /// in the source directory tree.  If empty directories are found, don't copy
        /// the directory (since no include files were specified in the fileset element).
        /// </remarks>
        [Test]
        public void DoNotIncludeEmptyDirMoveTest()
        {
            string buildScript1 = String.Format(_xmlProjectTemplate5, _tempDirTargetOne, _tempDirSourceOne);
            string buildScript2 = String.Format(_xmlProjectTemplate5, _tempDirTargetThree, _tempDirSourceThree);
            CreateTempDir(Path.Combine(_tempDirSourceThree, "EmptyOne"));
            CreateTempDir(Path.Combine(_tempDirSourceThree, "EmptyTwo"));

            RunBuild(buildScript1);

            Assert.IsTrue(Directory.Exists(_tempDirTargetOne),
                string.Format("'{0}' target directory does not exist", _tempDirTargetOne));

            Assert.IsFalse(Directory.Exists(_tempDirSourceOne),
                string.Format("'{0}' source directory does exist", _tempDirSourceOne));

            RunBuild(buildScript2);

            Assert.IsTrue(Directory.Exists(_tempDirSourceThree),
                string.Format("'{0}' target directory does not exist", _tempDirSourceThree));

            Assert.IsFalse(Directory.Exists(_tempDirTargetThree),
                string.Format("'{0}' source directory does exist", _tempDirTargetThree));
        }

        /// <summary>
        /// Checks to see if the move task will move
        /// </summary>
        [Test]
        public void FilesetExcludeDirectoryMoveTest()
        {
            RunBuild(String.Format(_xmlProjectTemplate3, _tempDirTargetThree,
                _tempDirSourceThree, "**/*"));

            Assert.IsTrue(Directory.Exists(_tempDirTargetThree),
                string.Format("'{0}' target directory does not exist", _tempDirTargetThree));

            Assert.IsTrue(Directory.Exists(_tempDirSourceThree),
                string.Format("'{0}' source directory does not exist", _tempDirSourceThree));

            Assert.IsTrue(File.Exists(_tempFileTargetThree),
                string.Format("'{0}' target file does not exist", _tempFileTargetThree));

            Assert.IsTrue(File.Exists(_tempFileSourceFour),
                string.Format("'{0}' source file does not exist", _tempFileSourceFour));

            Assert.IsFalse(Directory.Exists(_tempDirTargetFour),
                string.Format("'{0}' target directory does exist", _tempDirTargetFour));

            Assert.IsFalse(File.Exists(_tempFileTargetFour),
                string.Format("'{0}' target file does exist", _tempFileTargetFour));

            Assert.IsFalse(File.Exists(_tempFileSourceThree),
                string.Format("'{0}' source file does exist", _tempFileSourceThree));
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
