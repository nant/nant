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
using System.Xml;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Types;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {

    [TestFixture]
    public class FileSetTest : BuildTestBase {

        FileSet _fileSet;

        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            // create the file set
            _fileSet = new FileSet();
            _fileSet.BaseDirectory = TempDirectory;

            // create some test files to match against
            TempFile.CreateWithContents( 
@"world.peace
world.war
reefer.maddness",
            Path.Combine(TempDirName, "include.list")
            );
            TempFile.Create(Path.Combine(TempDirName, "world.peace"));
            TempFile.Create(Path.Combine(TempDirName, "world.war"));
            TempFile.Create(Path.Combine(TempDirName, "reefer.maddness"));
            TempFile.Create(Path.Combine(TempDirName, "reefer.saddness"));

            string sub1Path = Path.Combine(TempDirName, "sub1");
            Directory.CreateDirectory(sub1Path);
            TempFile.Create(Path.Combine(sub1Path, "sub.one"));
            string sub2Path = Path.Combine(TempDirName, "sub2");
            Directory.CreateDirectory(sub2Path);
        }

        [Test]
        public void CaseSensitive () {
            FileSet fileSet = new FileSet ();
            Assert.AreEqual (PlatformHelper.IsUnix, fileSet.CaseSensitive, "#1");
            fileSet.CaseSensitive = true;
            Assert.IsTrue (fileSet.CaseSensitive, "#2");
            fileSet.CaseSensitive = false;
            Assert.IsFalse (fileSet.CaseSensitive, "#3");
        }

        [Test]
        public void Test_AsIs() {
            _fileSet.AsIs.Add("foo");
            _fileSet.AsIs.Add("bar");
            AssertMatch("foo", false);
            AssertMatch("bar", false);
            Assert.AreEqual(2, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_IncludesAndAsIs() {
            _fileSet.Includes.Add("foo");
            _fileSet.AsIs.Add("foo");
            _fileSet.AsIs.Add("bar");
            AssertMatch("foo", false);
            AssertMatch("bar", false);
            Assert.AreEqual(2, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_All() {
            _fileSet.Includes.Add("**/*");
            AssertMatch("sub1" + Path.DirectorySeparatorChar + "sub.one");
            AssertMatch("world.peace");
            AssertMatch("world.war");
            AssertMatch("reefer.maddness");
            AssertMatch("reefer.saddness");
            // Expect 6 - not including directory
            Assert.AreEqual(6, _fileSet.FileNames.Count);
            // Two directories, including one empty one
            Assert.AreEqual(2, _fileSet.DirectoryNames.Count);
        }

        [Test]
        public void Test_Includes_All_Excludes_Some() {
            _fileSet.Includes.Add("**/*");
            _fileSet.Excludes.Add("**/*reefer*");
            
            AssertMatch("sub1" + Path.DirectorySeparatorChar + "sub.one");
            AssertMatch("world.peace");
            AssertMatch("world.war");
            // Expect 4 - not including directory
            Assert.AreEqual(4, _fileSet.FileNames.Count);
            // Two directories, including one empty one
            Assert.AreEqual(2, _fileSet.DirectoryNames.Count);
        }

        [Test]
        public void Test_Includes_Wildcards1() {
            _fileSet.Includes.Add("world.*");
            AssertMatch("world.peace");
            AssertMatch("world.war");
            Assert.AreEqual(2, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_Wildcards2() {
            _fileSet.Includes.Add("*.?addness");
            AssertMatch("reefer.maddness");
            AssertMatch("reefer.saddness");
            Assert.AreEqual(2, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_Sub1() {
            _fileSet.Includes.Add("sub?/sub*");
            AssertMatch("sub1" + Path.DirectorySeparatorChar + "sub.one");
            Assert.AreEqual(1, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_Sub2() {
            _fileSet.Includes.Add("sub2/**/*");
            Assert.AreEqual(0, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_List() {
            FileSet.IncludesFile elem = new FileSet.IncludesFile();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( "<includesList name=\"" + Path.Combine(_fileSet.BaseDirectory.FullName, "include.list") + "\" />" );
            elem.Project = CreateFilebasedProject("<project/>" );
            elem.Initialize(doc.DocumentElement);
            _fileSet.IncludesFiles = new FileSet.IncludesFile[] { elem };
            Assert.AreEqual(3, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_Includes_File() {
            FileSet.IncludesFile elem = new FileSet.IncludesFile();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( "<includesfile name=\"" + Path.Combine(_fileSet.BaseDirectory.FullName, "include.list") + "\" />" );
            elem.Project = CreateFilebasedProject("<project/>" );
            elem.Initialize(doc.DocumentElement);
            _fileSet.IncludesFiles = new FileSet.IncludesFile[] { elem };
            Assert.AreEqual(3, _fileSet.FileNames.Count);
        }

        [Test]
        public void Test_NewestFile() {
            string file1 = this.CreateTempFile("testfile1.txt", "hellow");
            string file2 = this.CreateTempFile("testfile2.txt", "hellow");
            string file3 = this.CreateTempFile("testfile3.txt", "hellow");
            string file4 = this.CreateTempFile("testfile4.txt", "hellow");

            //file1 was created first, but we will set the time in the future.
            FileInfo f1 = new FileInfo(file1);
            f1.LastWriteTime = DateTime.Now.AddDays(2);

            FileSet fs = new FileSet();
            fs.Includes.Add(file1);
            fs.Includes.Add(file2);
            fs.Includes.Add(file3);
            fs.Includes.Add(file4);

            FileInfo newestfile = fs.MostRecentLastWriteTimeFile;

            Assert.IsTrue(f1.FullName == newestfile.FullName, "Most Recent File should be '{0}', but was '{1}'", f1.Name, newestfile.Name);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_IncludesFile_NotExists() {
            const string buildXML = @"
                <project name=""fileset-test"">
                    <fileset id=""whatever"">
                        <includesfile name=""notexists.txt"" />
                    </fileset>
                </project>";

            RunBuild(buildXML);
        }

        [Test]
        public void Test_IncludesFile_NotExists_Conditional() {
            const string buildXML = @"
                <project name=""fileset-test"">
                    <fileset id=""whatever"">
                        <includesfile name=""notexists.txt"" if=""${file::exists('notexists.txt')}"" />
                    </fileset>
                </project>";

            RunBuild(buildXML);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ExcludesFile_NotExists() {
            const string buildXML = @"
                <project name=""fileset-test"">
                    <fileset id=""whatever"">
                        <excludesfile name=""notexists.txt"" />
                    </fileset>
                </project>";

            RunBuild(buildXML);
        }

        [Test]
        public void Test_ExcludesFile_NotExists_Conditional() {
            const string buildXML = @"
                <project name=""fileset-test"">
                    <fileset id=""whatever"">
                    <excludesfile name=""notexists.txt"" if=""${file::exists('notexists.txt')}"" />
                    </fileset>
                </project>";

            RunBuild(buildXML);
        }

        [Test]
        public void Matching_CaseInsensitive () {
            _fileSet.CaseSensitive = false;
            _fileSet.Includes.Add ("WoRLD.*");
            _fileSet.Includes.Add ("Sub*/*.OnE");
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#1");

            _fileSet.Excludes.Add ("WoRLD.peacE");
            _fileSet.Scan ();
            Assert.AreEqual(2, _fileSet.FileNames.Count, "#2");

            _fileSet.Excludes.Add ("SuB*/**");
            _fileSet.Scan ();
            Assert.AreEqual(1, _fileSet.FileNames.Count, "#3");
        }

        [Test]
        public void Matching_CaseSensitive () {
            _fileSet.CaseSensitive = true;
            _fileSet.Includes.Add ("WoRLD.*");
            _fileSet.Includes.Add ("Sub*/*.OnE");
            Assert.AreEqual(0, _fileSet.FileNames.Count, "#1");

            _fileSet.Includes.Add ("world.*");
            _fileSet.Includes.Add ("sub1/*.one");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#2");

            _fileSet.Excludes.Add ("WoRLD.peacE");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#3");

            _fileSet.Excludes.Add ("SuB*/**");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#4");
        }

        [Test]
        public void Matching_Unix () {
            if (!PlatformHelper.IsUnix) {
                return;
            }

            _fileSet.Includes.Add ("WoRLD.*");
            _fileSet.Includes.Add ("Sub1/*.OnE");
            Assert.AreEqual(0, _fileSet.FileNames.Count, "#1");

            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("world.*");
            _fileSet.Includes.Add ("sub1/*.one");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#2");

            _fileSet.Excludes.Add ("WoRLD.peacE");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#3");

            _fileSet.Excludes.Add ("SuB1/**");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#4");

            TempFile.Create(Path.Combine(TempDirName, "world.peacE"));

            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("world.*");
            _fileSet.Excludes.Clear ();
            _fileSet.Scan ();
            Assert.AreEqual (3, _fileSet.FileNames.Count, "#5");

            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("su*/**");
            _fileSet.Scan ();
            Assert.AreEqual (1, _fileSet.FileNames.Count, "#6");

            string sub1Path = Path.Combine(TempDirName, "suB1");
            Directory.CreateDirectory(sub1Path);
            TempFile.Create(Path.Combine(sub1Path, "sub.one"));

            _fileSet.Scan ();
            Assert.AreEqual (2, _fileSet.FileNames.Count, "#7");

            _fileSet.CaseSensitive = false;
            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("SuB1/*.one");
            _fileSet.Excludes.Clear ();
            Assert.AreEqual(0, _fileSet.FileNames.Count, "#8");
        }

        [Test]
        public void Matching_Windows () {
            if (PlatformHelper.IsUnix) {
                return;
            }

            _fileSet.Includes.Add ("WoRLD.*");
            _fileSet.Includes.Add ("Sub1/*.OnE");
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#1");

            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("world.*");
            _fileSet.Includes.Add ("sub1/*.one");
            _fileSet.Scan ();
            Assert.AreEqual(3, _fileSet.FileNames.Count, "#2");

            _fileSet.Excludes.Add ("WoRLD.peacE");
            _fileSet.Scan ();
            Assert.AreEqual(2, _fileSet.FileNames.Count, "#3");

            _fileSet.Excludes.Add ("SuB1/**");
            _fileSet.Scan ();
            Assert.AreEqual(1, _fileSet.FileNames.Count, "#4");

            _fileSet.CaseSensitive = true;
            _fileSet.Includes.Clear ();
            _fileSet.Includes.Add ("SuB1/*.one");
            _fileSet.Excludes.Clear ();
            Assert.AreEqual(1, _fileSet.FileNames.Count, "#5");
        }

        [Test]
        public void Bug1776101 () {
            if (PlatformHelper.IsUnix) {
                Assert.Ignore ("Only valid on non-Unix platform");
            }

            const string buildXML = @"
                <project name=""fileset-test"">
                    <mkdir dir=""Src/Api/test/bug1776101"" />
                    <mkdir dir=""Src/Api/release/whatever"" />
                    <touch file=""Src/Api/test/bug1776101/temp.tlb"" />
                    <touch file=""Src/Api/release/whatever/temp.txt"" />
                    <delete>
                        <fileset>
                            <include name=""Src/Api/**/*.tlb""/>
                            <include name=""src/Api/**/Release/**"" />
                        </fileset>
                    </delete>
                    <fail if=""${directory::exists('Src/Api/release/whatever')}"">#1</fail>
                </project>";

            RunBuild(buildXML);
        }

        void AssertMatch(string fileName) {
            AssertMatch(fileName, true);
        }

        void AssertMatch(string fileName, bool prefixBaseDir) {
            if (prefixBaseDir && !Path.IsPathRooted(fileName)) {
                fileName = Path.Combine(_fileSet.BaseDirectory.FullName, fileName);
            }
            Assert.IsTrue(_fileSet.FileNames.IndexOf(fileName) != -1,
                fileName + " should have been in file set.");
        }
    }
}
