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

// Jay Turpin (jayturpin@hotmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Collections.Specialized;
using System.IO;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core.Tasks;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class TouchTaskTest : BuildTestBase {
        const string _format = @"<?xml version='1.0' ?>
            <project>
                <touch {0}>{1}</touch>
            </project>";

        StringCollection _fileList = new StringCollection();

        /// <summary>Create the text fixture.</summary>
        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            // add 3 directories
            Directory.CreateDirectory(Path.Combine(TempDirName, "dir1"));
            Directory.CreateDirectory(Path.Combine(TempDirName, "dir2"));
            Directory.CreateDirectory(Path.Combine(TempDirName, "dir3"));

            // add file names
            _fileList.Add(Path.Combine(TempDirName, "file1.txt"));
            _fileList.Add(Path.Combine(TempDirName, "file2.txt"));
            _fileList.Add(Path.Combine(TempDirName, Path.Combine("dir1" ,"file3.tab")));
            _fileList.Add(Path.Combine(TempDirName, Path.Combine("dir1" ,"file4.txt")));
            _fileList.Add(Path.Combine(TempDirName, Path.Combine("dir2" ,"file5.tab")));
            _fileList.Add(Path.Combine(TempDirName, Path.Combine("dir2" ,"file6.txt")));


            // add some text to each file, just for fun ;)
            for (int i = 0; i < _fileList.Count; i++) {
                TempFile.Create(_fileList[i]);
            }
        }

        [Test]
        public void Test_File_DateTime() {

            DateTime newTouchDate = DateTime.Parse("01/01/1980");
            string fileName = _fileList[0];
            RunBuild(FormatBuildFile("file='" + fileName + "' datetime='" + newTouchDate.ToString(CultureInfo.InvariantCulture) + "'"));

            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;

            Assertion.Assert("File not touched", newTouchDate.Equals(lastTouchDate));

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assertion.Assert("Wrong file was touched", !newTouchDate.Equals(lastTouchDate));
        }

        [Test]
        public void Test_File_Millis() {
            // <touch file='myfile' millis='1000000000'/>

            string fileName = _fileList[0];
            int milliSeconds = 1000000000;
            RunBuild(FormatBuildFile("file='" + fileName + "' millis='" + milliSeconds.ToString() + "'"));
            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;
            DateTime newTouchDate = DateTime.Parse("01/01/1970").AddMilliseconds(milliSeconds);

            Assertion.Assert("Wrong touch date", newTouchDate.Equals(lastTouchDate));

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assertion.Assert("Wrong file touched", !newTouchDate.Equals(lastTouchDate));
        }

        [Test]
       public void Test_File_Default() {
            // sleep for a bit or this test will intermittently fail on fast machines
            System.Threading.Thread.Sleep(2000);
            // <touch file='myfile' />

            string fileName = _fileList[0];
            DateTime newTouchDate = DateTime.Now;
            RunBuild(FormatBuildFile("file='" + fileName + "'"));


            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;

            // Can only ensure that Now() is greater or equal to the file date
            Assertion.Assert("Touch date incorrect", lastTouchDate.CompareTo(newTouchDate) >= 0);

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assertion.Assert("Wrong file touched", !newTouchDate.Equals(lastTouchDate));
        }

        [Test]
       public void Test_Same_File_Twice() {
            // <touch file='myfile' />
            // <touch file='myfile' />
            // Old code used to lock the file - shouldn't now, allowing us to mess with it between runs

            string fileName = _fileList[0];

            // Get rid of the file first
            File.Delete(fileName);

            RunBuild(FormatBuildFile("file='" + fileName + "'"));
            Assertion.Assert("File doesn't exist!", File.Exists(fileName));
            File.Delete(fileName);

            RunBuild(FormatBuildFile("file='" + fileName + "'"));
            Assertion.Assert("File doesn't exist!", File.Exists(fileName));
            File.Delete(fileName);
        }


        [Test]
        public void Test_FileSet_DateTime() {
            // <touch datetime="01/01/1980 00:00">
            //   <fileset dir="src_dir"/>
            // </touch>

            DateTime newTouchDate = DateTime.Parse("01/01/1980");          
            RunBuild(FormatBuildFile("datetime='" + newTouchDate.ToString(CultureInfo.InvariantCulture) + "'","<fileset basedir='" + TempDirName + "'><include name='**' /></fileset>"));

            for (int i = 0; i < _fileList.Count; i++) {
                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assertion.Assert("Touch: fileset, datetime, " + _fileList[i], newTouchDate.Equals(lastTouchDate));
            }
        }

        [Test]
        public void Test_FileSet_Millis() {
            // <touch millis="100000">
            //   <fileset dir="src_dir"/>
            //</touch>

            int milliSeconds = 1000000000;
            DateTime newTouchDate = DateTime.Parse("01/01/1970").AddMilliseconds(milliSeconds);
          
            RunBuild(FormatBuildFile("datetime='" + newTouchDate.ToString(CultureInfo.InvariantCulture) + "'","<fileset basedir='" + TempDirName + "'><include name='**' /></fileset>"));

            for (int i = 0; i < _fileList.Count; i++) {

                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assertion.Assert("Touch: fileset, millis, " + _fileList[i], newTouchDate.Equals(lastTouchDate));
            }

        }

        [Test]
        public void Test_FileSet_Default() {
            // <touch>
            //  <fileset dir="src_dir"/>
            // </touch>
            DateTime newTouchDate = DateTime.Now;
          
            RunBuild(FormatBuildFile("","<fileset basedir='" + TempDirName + "'><include name='**' /></fileset>"));

            for (int i = 0; i < _fileList.Count; i++) {
                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assertion.Assert("Touch: fileset ONLY, " + _fileList[i], lastTouchDate.CompareTo(newTouchDate) >= 0);
            }
        }

        private string FormatBuildFile(string options) {
            return FormatBuildFile(options, "");
        }

        private string FormatBuildFile(string options, string nestedElements) {
            return String.Format(CultureInfo.InvariantCulture, _format, options, nestedElements);
        }
    }
}

