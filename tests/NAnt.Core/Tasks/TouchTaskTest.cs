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

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {

    public class TouchTaskTest : TestCase {

        public TouchTaskTest(String name): base(name) {}

        public void Test_Foobar() {
            TouchTask touch = new TouchTask();
        }

        StringCollection _fileList = new StringCollection();
        string _baseDirectory = null;

        /// <summary>Create the text fixture.</summary>
        protected override void SetUp() {
            // create test directory structure
            _baseDirectory = TempDir.Create("NAnt.Tests.TouchTest");

            // add 3 directories
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "dir1"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "dir2"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "dir3"));

            // add file names
            _fileList.Add(Path.Combine(_baseDirectory, "file1.txt"));
            _fileList.Add(Path.Combine(_baseDirectory, "file2.txt"));
            _fileList.Add(Path.Combine(_baseDirectory, Path.Combine("dir1" ,"file3.tab")));
            _fileList.Add(Path.Combine(_baseDirectory, Path.Combine("dir1" ,"file4.txt")));
            _fileList.Add(Path.Combine(_baseDirectory, Path.Combine("dir2" ,"file5.tab")));
            _fileList.Add(Path.Combine(_baseDirectory, Path.Combine("dir2" ,"file6.txt")));


            // add some text to each file, just for fun ;)
            for (int i = 0; i < _fileList.Count; i++) {
                TempFile.Create(_fileList[i]);
            }
        }

        /// <summary>Destroy the text fixture.</summary>
        protected override void TearDown() {
            TempDir.Delete(_baseDirectory);
        }

        public void Test_File_DateTime() {

            TouchTask touch = new TouchTask();
            touch.Project = CreateEmptyProject();

            // <touch file='myfile' datetime='06/28/2000 2:02 pm'/>

            DateTime newTouchDate = DateTime.Parse("01/01/1980");
            string fileName = _fileList[0];
            string xmlString = "<touch file='" + fileName + "' datetime='" + newTouchDate.ToString() + "'/>";

            touch.FileName = fileName;
            touch.Datetime = newTouchDate.ToString();
            touch.Execute();

            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;

            Assert(xmlString, newTouchDate.Equals(lastTouchDate));

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assert(xmlString, !newTouchDate.Equals(lastTouchDate));
        }

        public void Test_File_Millis() {
            TouchTask touch = new TouchTask();
            touch.Project = CreateEmptyProject();

            // <touch file='myfile' millis='100000'/>

            string fileName = _fileList[0];
            int milliSeconds = 100000;
            string xmlString = "<touch file='" + fileName + "' millis='" + milliSeconds.ToString() + "'/>";

            touch.FileName = fileName;
            touch.Millis = milliSeconds.ToString();
            touch.Execute();

            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;
            DateTime newTouchDate = DateTime.Parse("01/01/1970").AddMilliseconds(milliSeconds);

            Assert(xmlString, newTouchDate.Equals(lastTouchDate));

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assert(xmlString, !newTouchDate.Equals(lastTouchDate));
        }

       public void Test_File_Default() {
            // sleep for a bit or this test will intermittently fail on fast machines
            System.Threading.Thread.Sleep(2000);

            TouchTask touch = new TouchTask();
            touch.Project =  CreateEmptyProject();

            // <touch file='myfile' />

            string fileName = _fileList[0];
            DateTime newTouchDate = DateTime.Now;
            string xmlString = "<touch file='" + fileName + "'/>";

            touch.FileName = fileName;
            touch.Execute();

            FileInfo file = new FileInfo(fileName);
            DateTime lastTouchDate = file.LastWriteTime;

            // Can only ensure that Now() is greater or equal to the file date
            Assert(xmlString, lastTouchDate.CompareTo(newTouchDate) >= 0);

            // Make sure another file is NOT touched
            fileName = _fileList[1];
            file = new FileInfo(fileName);
            lastTouchDate = file.LastWriteTime;

            Assert(xmlString, !newTouchDate.Equals(lastTouchDate));
        }

        public void Test_FileSet_DateTime() {
            TouchTask touch = new TouchTask();
            touch.Project = CreateEmptyProject();

            // <touch datetime="01/01/1980 00:00">
            //   <fileset dir="src_dir"/>
            // </touch>

            DateTime newTouchDate = DateTime.Parse("01/01/1980");          
            touch.TouchFileSet.BaseDirectory = _baseDirectory;
            touch.TouchFileSet.Includes.Add("**");
            touch.TouchFileSet.Excludes.Add("");

            touch.Datetime = newTouchDate.ToString();
            touch.Execute();

            for (int i = 0; i < _fileList.Count; i++) {
                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assert("Touch: fileset, datetime, " + _fileList[i], newTouchDate.Equals(lastTouchDate));
            }
        }

        public void Test_FileSet_Millis() {

            TouchTask touch = new TouchTask();
            touch.Project = CreateEmptyProject();

            // <touch millis="100000">
            //   <fileset dir="src_dir"/>
            //</touch>

            int milliSeconds = 100000;
            DateTime newTouchDate = DateTime.Parse("01/01/1970").AddMilliseconds(milliSeconds);
          
            touch.TouchFileSet.BaseDirectory = _baseDirectory;
            touch.TouchFileSet.Includes.Add("**");
            touch.TouchFileSet.Excludes.Add("");

            touch.Millis = milliSeconds.ToString();
            touch.Execute();

            for (int i = 0; i < _fileList.Count; i++) {

                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assert("Touch: fileset, millis, " + _fileList[i], newTouchDate.Equals(lastTouchDate));
            }

        }

        public void Test_FileSet_Default() {
            TouchTask touch = new TouchTask();
            touch.Project = CreateEmptyProject();

            // <touch>
            //  <fileset dir="src_dir"/>
            // </touch>
            DateTime newTouchDate = DateTime.Now;
          
            touch.TouchFileSet.BaseDirectory = _baseDirectory;
            touch.TouchFileSet.Includes.Add("**");
            touch.TouchFileSet.Excludes.Add("");

            touch.Execute();

            for (int i = 0; i < _fileList.Count; i++) {
                FileInfo file = new FileInfo(_fileList[i]);
                DateTime lastTouchDate = file.LastWriteTime;

                Assert("Touch: fileset ONLY, " + _fileList[i], lastTouchDate.CompareTo(newTouchDate) >= 0);
            }
        }

		protected Project CreateEmptyProject() {
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			doc.AppendChild(doc.CreateElement("project"));
			return new Project(doc);
		}
    }
}

