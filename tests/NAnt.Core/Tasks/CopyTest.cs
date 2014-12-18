// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez (ScottHernandez@hotmail.com)
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// <para>Tests the deletion of the following:</para>
    /// <para> 
    ///     <list type="test">
    ///         <item> file</item>
    ///         <item> folder</item>
    ///         <item> folder with a file (recursive)</item>
    ///     </list>
    /// </para>
    /// </summary>
    /// <remarks>This test should also test for failures, like permission errors, and filesets</remarks>
    [TestFixture]
    public class CopyTest : BuildTestBase {
        const string _xmlProjectTemplate = @"
            <project>
                <copy verbose='true' todir='{0}' {2}>
                    <fileset>
                        <include name='{1}' />
                    </fileset>
                </copy>
            </project>";
        
        const string _xmlProjectTemplate2 = @"
            <project>
                <mkdir dir='{0}/destination' />
                <mkdir dir='{0}/source/test' />
                <copy verbose='true' todir='{0}/destination' {1}>
                    <fileset basedir='{0}'>
                        <include name='{0}/source/*' />
                    </fileset>
                </copy>
            </project>
        ";

        const string _xmlProjectTemplate3 = @"
            <project>
                <copy verbose='true' file='{0}' tofile='{1}' />
            </project>
        ";

        const string _xmlProjectTemplate4 = @"
            <project>
                <copy todir='{0}'>
                    <fileset basedir='{1}'/>
                </copy>
            </project>
        ";
        
        const string _xmlProjectTemplate5 = @"
            <project>
                <mkdir dir='{0}' />
                <copy verbose='true' todir='{0}'>
                    <fileset basedir='{1}'>
                        <include name='{2}/**/*' />
                    </fileset>
                </copy>
            </project>
        ";

        const string _xmlProjectTemplate6 = @"
            <project basedir='{0}'>
                <mkdir dir='{1}' />
                <copy verbose='true' todir='{1}'>
                    <fileset>
                        <include name='{2}'/>
                        <include name='{3}'/>
                    </fileset>
                </copy>
            </project>";

        string tempFile1, tempFile2, tempFile3, tempFile4, tempFile5, tempFile6, tempFile7;
        string tempDir1, tempDir2, tempDir3, tempDir4, tempDir5;

        /// <summary>
        /// Creates a structure like so:
        /// a.b\
        ///     a.bb
        ///     a.bc
        ///     foo\*
        ///         x.x
        ///     goo\*           
        ///         x\
        ///             y.y
        ///         ha.he
        ///         ha.he2*
        ///         ha.he3*
        ///     empty\          -- note: empty directory
        /// </summary>
        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            tempDir1 = CreateTempDir("a.b");
            tempDir2 = CreateTempDir(Path.Combine(tempDir1, "foo"));
            tempDir3 = CreateTempDir(Path.Combine(tempDir1, "goo"));
            tempDir4 = CreateTempDir(Path.Combine(tempDir1, Path.Combine(tempDir3, "x")));
            tempDir5 = CreateTempDir(Path.Combine(tempDir1, "empty"));

            tempFile1 = CreateTempFile(Path.Combine(tempDir1, "a.bb"));
            tempFile2 = CreateTempFile(Path.Combine(tempDir1, "a.bc"));
            tempFile3 = CreateTempFile(Path.Combine(tempDir2, "x.x"));
            tempFile4 = CreateTempFile(Path.Combine(tempDir4, "y.y"));
            tempFile5 = CreateTempFile(Path.Combine(tempDir3, "ha.he"));
            tempFile6 = CreateTempFile(Path.Combine(tempDir3, "ha.he2"));
            tempFile7 = CreateTempFile(Path.Combine(tempDir3, "ha.he3"));

            File.SetAttributes(tempDir2, FileAttributes.ReadOnly);
            File.SetAttributes(tempDir3, FileAttributes.ReadOnly);
            File.SetAttributes(Path.Combine(tempDir3, "ha.he3"), FileAttributes.ReadOnly);
            File.SetAttributes(Path.Combine(tempDir3, "ha.he2"), FileAttributes.ReadOnly);
        }

        private string GetPath(string rootPath, params string[] fileParts) {
            string path = rootPath;
            foreach (string filePart in fileParts) {
                path = Path.Combine(path, Path.GetFileName(filePart));
            }
            return path;
        }

        /// <summary>
        /// Copy only the directory given.
        /// </summary>
        [Test]
        public void Test_Copy_Only_Directory() {            
            string dest = CreateTempDir("a.99");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1, string.Empty));

            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should not have been created:" + tempFile1);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should not have been created:" + tempFile2);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should not have been created:" + tempFile3);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should not have been created:" + tempFile4);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should not have been created:" + tempFile5);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should not have been created:" + tempFile6);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should not have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should not have been created:" + tempDir2);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should not have been created:" + tempDir3);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should not have been created:" + tempDir4);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should not have been created:" + tempDir5);
        }

        /// <summary>
        /// Checks to make sure that only specified subdirectories are copied when specified
        /// in the include statement. Addresses Issue 85.
        /// </summary>
        [Test]
        public void Test_Copy_Only_SubDirectory()
        {
            string dest = CreateTempDir("a.85");
            string destDir  = GetPath(dest, tempDir1);
            string instructions = String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate5, destDir, tempDir1, "goo");
            RunBuild(instructions);
            
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should not have been created:" + tempFile1);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should not have been created:" + tempFile2);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should not have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should not have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should not have been created:" + tempDir5);
        }

        /// <summary>
        /// Test to make sure that the copy task copies relative file paths when the basedir of the fileset
        /// is not specified. Addresses Issue 88.
        /// </summary>
        [Test]
        public void Test_Copy_Unspecified_Fileset_Basedir()
        {
            string dest = CreateTempDir("a.88");
            string destDir  = GetPath(dest, tempDir1, tempDir3);
            string instructions = String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate6, tempDir2, destDir, 
                "../goo/ha.he", "../goo/ha.he2");
            RunBuild(instructions);

            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should not have been created:" + tempFile1);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should not have been created:" + tempFile2);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should not have been created:" + tempFile3);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should not have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsFalse(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should not have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should not have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsFalse(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should not have been created:" + tempDir5);
        }

        /// <summary>
        /// Ensure that an invalid path for destination directory causes a 
        /// <see cref="BuildException" /> to be thrown.
        /// </summary>
        [Test]
        public void Test_Copy_InvalidDestinationDirectory() {
            if (!PlatformHelper.IsUnix) {
                try {
                    RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "|", tempDir1, string.Empty));
                    // have the test fail
                    Assert.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                }   
            }
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists.
        /// </summary>
        [Test]
        public void Test_Copy_Structure() {
            string dest = CreateTempDir("a.xx");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", string.Empty));

            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should have been created:" + tempDir5);
        }

        /// <summary>
        /// Simple directory copy test.
        /// </summary>
        [Test]
        public void Test_Copy_Dir_Structure()
        {
            string dest = Path.Combine(TempDirName, "a.c");
            RunBuild(String.Format(_xmlProjectTemplate4, dest, tempDir1));

            Assert.IsTrue(Directory.Exists(dest),
                String.Format("Directory was not copied: {0}", tempDir1));
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists.
        /// </summary>
        [Test]
        public void Test_Copy_Structure_IncludeEmptyDirs() {            
            string dest = CreateTempDir("a.xx");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", " includeemptydirs='true' "));

            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should have been created:" + tempDir5);
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists. Do NOT copy empty dirs.
        /// </summary>
        [Test]
        public void Test_Copy_Structure_ExcludeEmptyDirs() {            
            string dest = CreateTempDir("a.xx");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", " includeemptydirs='false' "));

            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsFalse(Directory.Exists(GetPath(dest, tempDir1, tempDir5)), "Dir should not have been created:" + tempDir5);
        }

        [Test]
        public void Test_Copy_Structure_Overwrite() {           
            string dest = CreateTempDir("a.c");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "/**/*", string.Empty));

            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should have been created:" + tempDir5);

            // delete some files and directories
            File.Delete(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4));
            File.Delete(GetPath(dest,tempDir1,tempDir2,tempFile3));
            Directory.Delete(GetPath(dest,tempDir1,tempDir5));

            // ensure file is outdated
            File.SetLastWriteTime(GetPath(dest,tempDir1,tempDir3,tempFile5), new DateTime(2000, 1,1));

            // set some read-only attributes
            File.SetAttributes(GetPath(dest,tempDir1,tempDir3,tempFile5), FileAttributes.ReadOnly);

            // run it again to overwrite
            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "/**/*", string.Empty));

            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1)), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir2)), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3)), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)), "Dir should have been created:" + tempDir4);
            Assert.IsTrue(Directory.Exists(GetPath(dest,tempDir1,tempDir5)), "Dir should have been created:" + tempDir5);

            // check whether readonly file was overwritten (no longer readonly)
            Assert.IsTrue((File.GetAttributes(GetPath(dest,tempDir1,tempDir3,tempFile5)) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly);
        }

        /// <summary>
        /// Ensure that nested files are in fact flattened, nested directories
        /// are not created and up-to-date checking compares the flattened files.
        /// </summary>
        [Test]
        public void Test_Copy_Files_Flatten() {
            string dest = CreateTempDir("a.f");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _xmlProjectTemplate, dest, tempDir1 + "/**/*", "flatten=\"true\""));

            Assert.IsTrue(File.Exists(GetPath(dest, tempFile1)), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile2)), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile3)), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile4)), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile5)), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile6)), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(GetPath(dest, tempFile7)), "File should have been created:" + tempFile7);

            Assert.IsFalse(Directory.Exists(GetPath(dest, tempDir1)), "Dir should not have been created:" + tempDir1);

            // make a file read-only
            File.SetAttributes(GetPath(dest, tempFile1), FileAttributes.ReadOnly);

            // run build again
            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "/**/*", "flatten=\"true\""));

            // if up-to-date check works, build should not have failed and 
            // read-only file should still be read-only
            Assert.IsTrue((File.GetAttributes(GetPath(dest, tempFile1)) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
        }

        /// <summary>
        /// When multiple source files match the same destination file, then 
        /// only the last updated file should actually be copied.
        /// </summary>
        [Test]
        public void Test_Copy_Files_Flatten_UpToDate() {
            const string buildXml = @"
                <project>
                    <mkdir dir='dest' />
                    <mkdir dir='src/dir1' />
                    <mkdir dir='src/dir2' />
                    <mkdir dir='src/dir3' />
                    <echo file='dest/uptodate.txt'>dest</echo>
                    <touch file='dest/uptodate.txt' datetime='02/01/2000' />
                    <echo file='src/dir1/uptodate.txt'>dir1</echo>
                    <touch file='src/dir1/uptodate.txt' datetime='05/01/2000' />
                    <echo file='src/dir2/uptodate.txt'>dir2</echo>
                    <touch file='src/dir2/uptodate.txt' datetime='03/01/2000' />
                    <echo file='src/dir3/uptodate.txt'>dir3</echo>
                    <touch file='src/dir3/uptodate.txt' datetime='04/01/2000' />
                    <copy todir='dest' flatten='true'>
                        <fileset>
                            <include name='src/**' />
                        </fileset>
                    </copy>
                    <loadfile file='dest/uptodate.txt' property='file.content' />
                    <fail unless=""${string::trim(file.content) == 'dir1'}"">File content was '${file.content}'.</fail>
                </project>";

            RunBuild(buildXml, Level.Debug);
        }

        /// <summary>
        /// Ensure that an invalid path for source file causes a <see cref="BuildException" />
        /// to be thrown.
        /// </summary>
        [Test]
        public void Test_Copy_Files_InvalidSourceFilePath() {
            File.Delete(tempFile2);
            if (! PlatformHelper.IsUnix ) {
                try {
                    RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, 
                        "|", tempFile2));
                    // have the test fail
                    Assert.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                }
            }
        }

        /// <summary>
        /// Ensure that an invalid path for destination file causes a 
        /// <see cref="BuildException" /> to be thrown.
        /// </summary>
        [Test]
        public void Test_Copy_Files_InvalidDestinationFilePath() {
            if (! PlatformHelper.IsUnix ) {
                try {
                    RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, 
                        tempFile1, "|"));
                    // have the test fail
                    Assert.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                }
            }
        }

        [Test]
        public void Test_Copy_Files_No_Overwrite() {            

            File.Delete(tempFile2);
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assert.IsTrue(File.Exists(tempFile2), "File should have been created:" + tempFile2);
        }

        [Test]
        public void Test_Copy_Files_Overwrite() {            

            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assert.IsTrue(File.Exists(tempFile2), "File should have been created:" + tempFile2);
        }

        [Test]
        public void Test_Copy_Files_Overwrite_Readonly() {            

            File.SetAttributes(tempFile2, FileAttributes.ReadOnly);
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assert.IsTrue(File.Exists(tempFile2), "File should have been created:" + tempFile2);
        }

        /// <summary>
        /// <para>
        /// This test suggested by drieseng@users.sourceforge.net.  Tests copying subdirectories only.
        /// </para>
        /// <para>
        /// Empty directories should be copied when <c>includeemptydirs</c> 
        /// is not specified (default is <see langword="true" />).
        /// </para>
        /// </summary>
        [Test]
        public void Test_Copy_Structure_Directories() {
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate2, tempDir1, string.Empty));
            Assert.IsTrue(Directory.Exists(GetPath(tempDir1, "destination")), "Dir should have been created:" + GetPath(tempDir1, "destination"));
            Assert.IsTrue(Directory.Exists(GetPath(tempDir1, "source", "test")), "Dir should have been created:" + GetPath(tempDir1, "source", "test"));
            Assert.IsTrue(Directory.Exists(GetPath(tempDir1, "destination", "source", "test")), "Dir should have been created:" + GetPath(tempDir1, "destination", "source","test"));
        }

        /// <summary>
        /// <para>
        /// This test suggested by drieseng@users.sourceforge.net.  Tests copying subdirectories only.
        /// </para>
        /// <para>
        /// Empty directories should not be copied when <c>includeemptydirs</c> 
        /// is <see langword="false" />.
        /// </para>
        /// </summary>
        [Test]
        public void Test_Copy_Structure_Directories_ExcludeEmptyDirs() {
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate2, tempDir1, " includeemptydirs='false' "));
            Assert.IsTrue(Directory.Exists(GetPath(tempDir1, "destination")), "Dir should have been created:" + GetPath(tempDir1, "destination"));
            Assert.IsTrue(Directory.Exists(GetPath(tempDir1, "source", "test")), "Dir should have been created:" + GetPath(tempDir1, "source", "test"));
            Assert.IsFalse(Directory.Exists(GetPath(tempDir1, "destination", "source", "test")), "Dir should not have been created:" + GetPath(tempDir1, "destination", "source","test"));
        }
        
        
        [Test]
        public void Test_Copy_Subdirectories()
        {
            string newRootDir = CreateTempDir("Q.z");
            string destDir = GetPath(newRootDir, "destination");
            string expectedDir1 = GetPath(destDir, "goo");
            string expectedFile1 = GetPath(expectedDir1, "ha.he");
            string expectedFile2 = GetPath(expectedDir1, "ha.he2");
            string expectedFile3 = GetPath(expectedDir1, "ha.he3");
            string expectedDir2 = GetPath(expectedDir1, "x");
            string expectedFile4 = GetPath(expectedDir2, "y.y");
            string badSubDir1 = GetPath(destDir, "a.b");
            string badSubDir2 = GetPath(destDir, "foo");
            string badSubDir3 = GetPath(destDir, "empty");
            
            RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate5, destDir, tempDir1, "goo"));
            
            // Test to make sure that entire directory was not wasn't copied instead
            Assert.IsFalse(Directory.Exists(badSubDir1), "Directory should not have been created", badSubDir1);
            Assert.IsFalse(Directory.Exists(badSubDir2), "Directory should not have been created", badSubDir2);
            Assert.IsFalse(Directory.Exists(badSubDir3), "Directory should not have been created", badSubDir3);
            
            // Test the existance of copied files
            Assert.IsTrue(File.Exists(expectedFile1), "File should have been created: {0}", expectedFile1);
            Assert.IsTrue(File.Exists(expectedFile2), "File should have been created: {0}", expectedFile2);
            Assert.IsTrue(File.Exists(expectedFile3), "File should have been created: {0}", expectedFile3);
            Assert.IsTrue(File.Exists(expectedFile4), "File should have been created: {0}", expectedFile4);
            
            // Test the existance of copied directories
            Assert.IsTrue(Directory.Exists(expectedDir1), "Directory should have been created: {0}", expectedDir1);
            Assert.IsTrue(Directory.Exists(expectedDir2), "Directory should have been created: {0}", expectedDir2);
        }

        /// <summary>
        /// The <c>todir</c> and <c>tofile</c> attribute of the <c>&lt;copy&gt;</c>
        /// task should not be combined.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ToFile_ToDir() {
            const string xmlProjectTemplate = @"
            <project>
                <copy todir='test' tofile='test.file'>
                    <fileset>
                        <include name='*.txt' />
                    </fileset>
                </copy>
            </project>";

            RunBuild(xmlProjectTemplate);
        }

        /// <summary>
        /// The <c>tofile</c> attribute of the <c>&lt;copy&gt;</c> task cannot
        /// be combined with a <c>&lt;fileset&gt;</c> element.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ToFile_FileSet() {
            const string xmlProjectTemplate = @"
            <project>
                <copy tofile='test.file'>
                    <fileset>
                        <include name='*.txt' />
                    </fileset>
                </copy>
            </project>";

            RunBuild(xmlProjectTemplate);
        }

        /// <summary>
        /// The <c>file</c> attribute of the <c>&lt;copy&gt;</c> task cannot
        /// be combined with a <c>&lt;fileset&gt;</c> element.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_File_FileSet() {
            const string xmlProjectTemplate = @"
            <project>
                <copy file='test.file' todir='test'>
                    <fileset>
                        <include name='*.txt' />
                    </fileset>
                </copy>
            </project>";

            RunBuild(xmlProjectTemplate);
        }
    }
}
