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

using System.IO;
using NUnit.Framework;

using NAnt.Core;

using Tests.NAnt.Core;

namespace Tests.NAnt.Compression.Tasks {
    [TestFixture]
    public class ZipTaskTest : BuildTestBase {
        /// <summary>
        /// Test to make sure a simple zip file can be created.
        /// </summary>
        [Test]
        public void Test_SimpleZip() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip'>
                        <fileset basedir='src'>
                            <include name='**'/>
                        </fileset>
                    </zip>
                </project>";

            CreateTempDir("src");
            CreateTempFile(Path.Combine("src", "temp1.file"),"hello");
            RunBuild(projectXML);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirName,"test.zip")),
                "Zip File not created.");
        }

        /// <summary>
        /// Test to make sure an empty zip file can be created.
        /// </summary>
        [Test]
        public void Test_CreateEmptyZipFile() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' />
                </project>";

            RunBuild(projectXML);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirName, "test.zip")),
                "Zip File not created.");
        }

        /// <summary>
        /// Ensures a <see cref="BuildException" /> is thrown when attempting 
        /// to add a non-existing file to the zip file.
        /// </summary>
        [Test]
        public void Test_NonExistingFile() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip'>
                        <fileset prefix='bin'>
                            <include name='whatever/test.txt' asis='true' />
                        </fileset>
                    </zip>
                </project>";

            try {
                RunBuild(projectXML);
                Assert.Fail("#1");
            } catch (TestBuildException ex) {
                Assert.IsNotNull(ex.InnerException, "#2");
                Assert.IsTrue(ex.InnerException is BuildException, "#3");
                Assert.IsNotNull(ex.InnerException.InnerException, "#4");
                Assert.IsTrue(ex.InnerException.InnerException is BuildException, "#5");

                BuildException be = (BuildException) ex.InnerException.InnerException;
                // error message should contain path of file that does not exist
                Assert.IsTrue(be.RawMessage.IndexOf("whatever/test.txt") != -1, "#6");
            }
        }
        
        
        [Test]
        /// <summary>
        /// Ensures a zip file can be created, even if the path does not exist yet.
        /// </summary>        
        public void Test_CreateZipAndParentDirectory() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='dir1/dir2/test.zip'>
                        <fileset prefix='src'>
                            <include name='**' />
                        </fileset>
                    </zip>
                </project>";
            
            CreateTempDir("src");
            CreateTempFile(Path.Combine("src", "temp1.file"),"hello");                
            RunBuild(projectXML);            
            Assert.IsTrue(File.Exists(
                Path.Combine(TempDirName, 
                    Path.Combine("dir1", 
                        Path.Combine("dir2", "test.zip")))),
                "Zip File not created.");
        }
        
        [Test]
        public void Duplicate_Add() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' duplicate='Add'>
                        <fileset basedir='folder1' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                        <fileset basedir='folder2' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                    </zip>
                    <unzip zipfile='test.zip' todir='extract' />
                </project>";

            CreateTempDir("folder1");
            CreateTempFile(Path.Combine("folder1", "test.txt"), "folder1");
            CreateTempDir("folder2");
            CreateTempFile(Path.Combine("folder2", "test.txt"), "folder2");

            RunBuild(projectXML);

            string extractDir = Path.Combine(TempDirName, "extract");
            Assert.IsTrue(Directory.Exists(extractDir), "#1");
            string binDir = Path.Combine(extractDir, "bin");
            Assert.IsTrue(Directory.Exists(binDir), "#2");
            string testFile = Path.Combine(binDir, "test.txt");
            Assert.IsTrue(File.Exists(testFile), "#3");

            // finally check whether second entry (from folder2) was indeed
            // added to zip file (and as such extracted from it)
            using (StreamReader sr = new StreamReader(testFile, true)) {
                Assert.AreEqual("folder2", sr.ReadToEnd(), "#4");
                sr.Close();
            }
        }

        /// <summary>
        /// Verifies whether a build error is reported if an invalid value is
        /// specified for the "duplicate" attribute.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Duplicate_Invalid() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' duplicate='invalid'>
                        <fileset basedir='folder1' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                        <fileset basedir='folder2' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                    </zip>
                    <unzip zipfile='test.zip' todir='extract' />
                </project>";

            CreateTempDir("folder1");
            CreateTempFile(Path.Combine("folder1", "test.txt"), "folder1");
            CreateTempDir("folder2");
            CreateTempFile(Path.Combine("folder2", "test.txt"), "folder2");

            RunBuild(projectXML);
        }

        /// <summary>
        /// Verifies whether Add is the default value for the "duplicate"
        /// attribute.
        /// </summary>
        [Test]
        public void Duplicate_Add_Default() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip'>
                        <fileset basedir='folder1' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                        <fileset basedir='folder2' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                    </zip>
                    <unzip zipfile='test.zip' todir='extract' />
                </project>";

            CreateTempDir("folder1");
            CreateTempFile(Path.Combine("folder1", "test.txt"), "folder1");
            CreateTempDir("folder2");
            CreateTempFile(Path.Combine("folder2", "test.txt"), "folder2");

            RunBuild(projectXML);

            string extractDir = Path.Combine(TempDirName, "extract");
            Assert.IsTrue(Directory.Exists(extractDir), "#1");
            string binDir = Path.Combine(extractDir, "bin");
            Assert.IsTrue(Directory.Exists(binDir), "#2");
            string testFile = Path.Combine(binDir, "test.txt");
            Assert.IsTrue(File.Exists(testFile), "#3");

            // finally check whether second entry (from folder2) was indeed
            // added to zip file (and as such extracted from it)
            using (StreamReader sr = new StreamReader(testFile, true)) {
                Assert.AreEqual("folder2", sr.ReadToEnd(), "#4");
                sr.Close();
            }
        }

        [Test]
        public void Duplicate_Preserve() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' duplicate='Preserve'>
                        <fileset basedir='folder1' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                        <fileset basedir='folder2' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                    </zip>
                    <unzip zipfile='test.zip' todir='extract' />
                </project>";

            CreateTempDir("folder1");
            CreateTempFile(Path.Combine("folder1", "test.txt"), "folder1");
            CreateTempDir("folder2");
            CreateTempFile(Path.Combine("folder2", "test.txt"), "folder2");

            RunBuild(projectXML);

            string extractDir = Path.Combine(TempDirName, "extract");
            Assert.IsTrue(Directory.Exists(extractDir), "#1");
            string binDir = Path.Combine(extractDir, "bin");
            Assert.IsTrue(Directory.Exists(binDir), "#2");
            string testFile = Path.Combine(binDir, "test.txt");
            Assert.IsTrue(File.Exists(testFile), "#3");

            // finally check whether second entry (from folder2) was indeed
            // added to zip file (and as such extracted from it)
            using (StreamReader sr = new StreamReader(testFile, true)) {
                Assert.AreEqual("folder1", sr.ReadToEnd(), "#4");
                sr.Close();
            }
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Duplicate_Fail() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' duplicate='Fail'>
                        <fileset basedir='folder1' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                        <fileset basedir='folder2' prefix='bin'>
                            <include name='test.txt' />
                        </fileset>
                    </zip>
                </project>";

            CreateTempDir("folder1");
            CreateTempFile(Path.Combine("folder1", "test.txt"), "folder1");
            CreateTempDir("folder2");
            CreateTempFile(Path.Combine("folder2", "test.txt"), "folder2");

            RunBuild(projectXML);
        }

        /// <summary>
        /// Verifies whether Flatten produces a flat zip file by discarding
        /// directory structure.
        /// attribute.
        /// </summary>
        [Test]
        public void Test_FlattenedFile() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <zip zipfile='test.zip' flatten='true'>
                        <fileset basedir='src'>
                            <include name='/**/*' />
                        </fileset>
                    </zip>
                    <unzip zipfile='test.zip' todir='extract' />
                </project>";

            // Created nested structure
            CreateTempDir("src");
            string Path1 = Path.Combine("src", "folder1");
            CreateTempDir(Path1);
            CreateTempFile(Path.Combine(Path1, "test1.txt"), "First");
            
            string Path2 = Path.Combine("src", "folder2");
            CreateTempDir(Path2);
            CreateTempFile(Path.Combine(Path2, "test2.txt"), "Second");
            
            // Run code
            RunBuild(projectXML);
            
            // Check both files are in the root directory (flat)
            string extractDir = Path.Combine(TempDirName, "extract");
            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "test1.txt")), "#1");
            Assert.IsTrue(File.Exists(Path.Combine(extractDir, "test2.txt")), "#2");
        }
    }
}
