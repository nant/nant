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
using System.Reflection;
using System.Text;
using System.Xml;
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
                        <includes name='{1}' />
                    </fileset>
                </copy>
            </project>";
        
        const string _xmlProjectTemplate2 = @"
            <project>
                <mkdir dir='{0}/destination' />
                <mkdir dir='{0}/source/test' />
                <copy verbose='true' todir='{0}/destination' {1}>
                    <fileset basedir='{0}'>
                        <includes name='{0}/source/*' />
                    </fileset>
                </copy>
            </project>
        ";

        const string _xmlProjectTemplate3 = @"
            <project>
                <copy verbose='true' file='{0}' tofile='{1}' />
            </project>
        ";

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
            string results;
            string dest = CreateTempDir("a.99");
            
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1, string.Empty));

            Assertion.Assert("File should not have been created:" + tempFile1, !File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should not have been created:" + tempFile2, !File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should not have been created:" + tempFile3, !File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should not have been created:" + tempFile4, !File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should not have been created:" + tempFile5, !File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should not have been created:" + tempFile6, !File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should not have been created:" + tempFile7, !File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should not have been created:" + tempDir2, !Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should not have been created:" + tempDir3, !Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should not have been created:" + tempDir4, !Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should not have been created:" + tempDir5, !Directory.Exists(GetPath(dest,tempDir1,tempDir5)));
        }

        /// <summary>
        /// Ensure that an invalid path for destination directory causes a 
        /// <see cref="BuildException" /> to be thrown.
        /// </summary>
        [Test]
        public void Test_Copy_InvalidDestinationDirectory() {
            if (! PlatformHelper.IsUnix ) {
                try {
                    RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "abc#?-{", tempDir1, string.Empty));
                    // have the test fail
                    Assertion.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assertion.Assert((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                }   
            }
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists.
        /// </summary>
        [Test]
        public void Test_Copy_Structure() {
            string results;
            string dest = CreateTempDir("a.xx");
            
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", string.Empty));

            Assertion.Assert("File should have been created:" + tempFile1, File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should have been created:" + tempFile3, File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should have been created:" + tempFile4, File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should have been created:" + tempFile5, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should have been created:" + tempFile6, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should have been created:" + tempFile7, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should have been created:" + tempDir2, Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should have been created:" + tempDir3, Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should have been created:" + tempDir4, Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should have been created:" + tempDir5, Directory.Exists(GetPath(dest,tempDir1,tempDir5)));
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists.
        /// </summary>
        [Test]
        public void Test_Copy_Structure_IncludeEmptyDirs() {
            string results;
            string dest = CreateTempDir("a.xx");
            
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", " includeemptydirs='true' "));

            Assertion.Assert("File should have been created:" + tempFile1, File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should have been created:" + tempFile3, File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should have been created:" + tempFile4, File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should have been created:" + tempFile5, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should have been created:" + tempFile6, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should have been created:" + tempFile7, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should have been created:" + tempDir2, Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should have been created:" + tempDir3, Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should have been created:" + tempDir4, Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should have been created:" + tempDir5, Directory.Exists(GetPath(dest,tempDir1,tempDir5)));
        }

        /// <summary>
        /// Copy everything from under tempDir1 to a new temp directory and 
        /// ensure it exists. Do NOT copy empty dirs.
        /// </summary>
        [Test]
        public void Test_Copy_Structure_ExcludeEmptyDirs() {
            string results;
            string dest = CreateTempDir("a.xx");
            
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "\\**\\*", " includeemptydirs='false' "));

            Assertion.Assert("File should have been created:" + tempFile1, File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should have been created:" + tempFile3, File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should have been created:" + tempFile4, File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should have been created:" + tempFile5, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should have been created:" + tempFile6, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should have been created:" + tempFile7, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should have been created:" + tempDir2, Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should have been created:" + tempDir3, Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should have been created:" + tempDir4, Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should not have been created:" + tempDir5, !Directory.Exists(GetPath(dest, tempDir1, tempDir5)));
        }

        [Test]
        public void Test_Copy_Structure_Overwrite() {
            string results;
            string dest = CreateTempDir("a.c");
            
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "/**/*", string.Empty));

            Assertion.Assert("File should have been created:" + tempFile1, File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should have been created:" + tempFile3, File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should have been created:" + tempFile4, File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should have been created:" + tempFile5, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should have been created:" + tempFile6, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should have been created:" + tempFile7, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should have been created:" + tempDir2, Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should have been created:" + tempDir3, Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should have been created:" + tempDir4, Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should have been created:" + tempDir5, Directory.Exists(GetPath(dest,tempDir1,tempDir5)));

            // Set some read-only attributes
            File.SetAttributes(GetPath(dest,tempDir1,tempDir3,tempFile5), FileAttributes.ReadOnly);
            File.SetAttributes(GetPath(dest,tempDir1,tempDir2), FileAttributes.ReadOnly);

            // Delete some files and directories
            File.Delete(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4));
            File.Delete(GetPath(dest,tempDir1,tempDir2,tempFile3));
            Directory.Delete(GetPath(dest,tempDir1,tempDir5));

            // Run it again to overwrite
            results = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, dest, tempDir1 + "/**/*", string.Empty));

            Assertion.Assert("File should have been created:" + tempFile1, File.Exists(GetPath(dest,tempDir1,tempFile1)));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(GetPath(dest,tempDir1,tempFile2)));
            Assertion.Assert("File should have been created:" + tempFile3, File.Exists(GetPath(dest,tempDir1,tempDir2,tempFile3)));
            Assertion.Assert("File should have been created:" + tempFile4, File.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4,tempFile4)));
            Assertion.Assert("File should have been created:" + tempFile5, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile5)));
            Assertion.Assert("File should have been created:" + tempFile6, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile6)));
            Assertion.Assert("File should have been created:" + tempFile7, File.Exists(GetPath(dest,tempDir1,tempDir3,tempFile7)));

            Assertion.Assert("Dir should have been created:" + tempDir1, Directory.Exists(GetPath(dest,tempDir1)));
            Assertion.Assert("Dir should have been created:" + tempDir2, Directory.Exists(GetPath(dest,tempDir1,tempDir2)));
            Assertion.Assert("Dir should have been created:" + tempDir3, Directory.Exists(GetPath(dest,tempDir1,tempDir3)));
            Assertion.Assert("Dir should have been created:" + tempDir4, Directory.Exists(GetPath(dest,tempDir1,tempDir3,tempDir4)));
            Assertion.Assert("Dir should have been created:" + tempDir5, Directory.Exists(GetPath(dest,tempDir1,tempDir5)));
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
                        "abc#?-{", tempFile2));
                    // have the test fail
                    Assertion.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assertion.Assert((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
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
                        tempFile1, "abc#?-{"));
                    // have the test fail
                    Assertion.Fail("Build should have failed.");
                } catch (TestBuildException ex) {
                    // assert that a BuildException was the cause of the TestBuildException
                    Assertion.Assert((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                }
            }
        }

        [Test]
        public void Test_Copy_Files_No_Overwrite() {
            string results;

            File.Delete(tempFile2);
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(tempFile2));
        }

        [Test]
        public void Test_Copy_Files_Overwrite() {
            string results;

            results = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(tempFile2));
        }

        [Test]
        public void Test_Copy_Files_Overwrite_Readonly() {
            string results;

            File.SetAttributes(tempFile2, FileAttributes.ReadOnly);
            results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate3, tempFile1, tempFile2));
            Assertion.Assert("File should have been created:" + tempFile2, File.Exists(tempFile2));
        }

        /// <summary>
        /// <para>
        /// This test suggested by gert.driesen@pandora.be.  Tests copying subdirectories only.
        /// </para>
        /// <para>
        /// Empty directories should be copied when <c>includeemptydirs</c> 
        /// is not specified (default is <see langword="true" />).
        /// </para>
        /// </summary>
        [Test]
        public void Test_Copy_Structure_Directories() {
            string results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate2, tempDir1, string.Empty));
            Assertion.Assert("Dir should have been created:" + GetPath(tempDir1, "destination"), Directory.Exists(GetPath(tempDir1, "destination")));
            Assertion.Assert("Dir should have been created:" + GetPath(tempDir1, "source", "test"), Directory.Exists(GetPath(tempDir1, "source", "test")));
            Assertion.Assert("Dir should have been created:" + GetPath(tempDir1, "destination", "source","test"), Directory.Exists(GetPath(tempDir1, "destination", "source", "test")));
        }

        /// <summary>
        /// <para>
        /// This test suggested by gert.driesen@pandora.be.  Tests copying subdirectories only.
        /// </para>
        /// <para>
        /// Empty directories should not be copied when <c>includeemptydirs</c> 
        /// is <see langword="false" />.
        /// </para>
        /// </summary>
        [Test]
        public void Test_Copy_Structure_Directories_ExcludeEmptyDirs() {
            string results = RunBuild(string.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate2, tempDir1, " includeemptydirs='false' "));
            Assertion.Assert("Dir should have been created:" + GetPath(tempDir1, "destination"), Directory.Exists(GetPath(tempDir1, "destination")));
            Assertion.Assert("Dir should have been created:" + GetPath(tempDir1, "source", "test"), Directory.Exists(GetPath(tempDir1, "source", "test")));
            Assertion.Assert("Dir should not have been created:" + GetPath(tempDir1, "destination", "source","test"), !Directory.Exists(GetPath(tempDir1, "destination", "source", "test")));
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
                        <includes name='*.txt' />
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
                        <includes name='*.txt' />
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
                        <includes name='*.txt' />
                    </fileset>
                </copy>
            </project>";

            RunBuild(xmlProjectTemplate);
        }
    }
}
