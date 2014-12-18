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
using System.Globalization;

using NUnit.Framework;

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
    public class DeleteTest : BuildTestBase {
        const string _xmlProjectTemplate = @"
            <project>
                <delete verbose='true' {0}='{1}'/>
            </project>";
        
        string tempFile1, tempFile2, tempFile3, tempFile4, tempFile5, tempFile6, tempFile7, tempFile8, tempFile9, tempFile10;
        string tempDir1, tempDir2, tempDir3, tempDir4, tempDir5, tempDir6, tempDir7, tempDir8;

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
        ///         a.boo
        ///         boo
        /// a.c\
        /// a.d\
        ///     boo\
        /// a.e\
        ///     a.ee
        /// </summary>
        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            tempDir1 = CreateTempDir("a.b");
            tempDir2 = CreateTempDir(Path.Combine(tempDir1, "foo"));
            tempDir3 = CreateTempDir(Path.Combine(tempDir1, "goo"));
            tempDir4 = CreateTempDir(Path.Combine(tempDir1, Path.Combine(tempDir3, "x")));
            tempDir5 = CreateTempDir("a.c");
            tempDir6 = CreateTempDir("a.d");
            tempDir7 = CreateTempDir(Path.Combine(tempDir6, "boo"));
            tempDir8 = CreateTempDir("a.e");

            tempFile1 = CreateTempFile(Path.Combine(tempDir1, "a.bb"));
            tempFile2 = CreateTempFile(Path.Combine(tempDir1, "a.bc"));
            tempFile3 = CreateTempFile(Path.Combine(tempDir2, "x.x"));
            tempFile4 = CreateTempFile(Path.Combine(tempDir4, "y.y"));
            tempFile5 = CreateTempFile(Path.Combine(tempDir3, "ha.he"));
            tempFile6 = CreateTempFile(Path.Combine(tempDir3, "ha.he2"));
            tempFile7 = CreateTempFile(Path.Combine(tempDir3, "ha.he3"));
            tempFile8 = CreateTempFile(Path.Combine(tempDir8, "a.ee"));
            tempFile9 = CreateTempFile(Path.Combine(tempDir3, "a.boo"));
            tempFile10 = CreateTempFile(Path.Combine(tempDir3, "boo"));

            /*
            File.SetAttributes(tempDir2, FileAttributes.ReadOnly);
            File.SetAttributes(tempDir3, FileAttributes.ReadOnly); 
            */
            File.SetAttributes(Path.Combine(tempDir3, "ha.he3"), FileAttributes.ReadOnly);
            File.SetAttributes(Path.Combine(tempDir3, "ha.he2"), FileAttributes.ReadOnly);
        }

        [Test]
        public void Test_Delete() {            

            Assert.IsTrue(File.Exists(tempFile1), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should have been created:" + tempDir4);

            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "file", tempFile6 ));
            
            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);

            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "dir", tempDir2));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);

            RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "file", tempFile1 ));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
        }

        /// <summary>
        /// Checks whether an include pattern (without wildcards) that matches 
        /// a directory containing both files an directories, will NOT cause 
        /// that directory to be removed.
        /// </summary>
        [Test]
        public void Test_DeleteFileSet2() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.b"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        /// <summary>
        /// Checks whether an include pattern (without wildcards) that matches 
        /// a directory containing subdirectories, will NOT cause that directory 
        /// to be removed.
        /// </summary>
        [Test]
        public void Test_DeleteFileSet3() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.d"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        /// <summary>
        /// Checks whether an include pattern (without wildcards) that matches 
        /// a directory containing files, will NOT cause that directory to be 
        /// removed.
        /// </summary>
        [Test]
        public void Test_DeleteFileSet4() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            /// Checks whether an include pattern (without wildcards) that matches 
            /// a directory containing files, will NOT cause that directory to be 
            /// removed.

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.e"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);

            /// Checks whether removing the last file from a directory, with a 
            /// pattern that does not match the directory itself, will not CAUSE
            /// that directory to be removed.

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.e/a.ee"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);

            /// Checks whether an include pattern that matches all files in an 
            /// empty directory, will NOT cause that directory to be removed.

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.e/**/*"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted in previous step:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);

            /// Checks whether an include pattern that matches all files in a
            /// empty directory and the directory itself, will cause that 
            /// directory to be removed.

            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                xmlProjectTemplateFileSet, TempDirName, "a.e/**"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted in previous step:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsFalse(Directory.Exists(tempDir8), "Dir should have been deleted:" + tempDir8);
        }

        /// <summary>
        /// Checks whether an include pattern that matches all files in a 
        /// non-empty directory (and its subdirectories) and that directory 
        /// itself, will cause that directory to be removed.
        /// </summary>
        [Test]
        public void Test_DeleteFileSet5() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName, "a.e/**"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsFalse(Directory.Exists(tempDir8), "Dir should have been deleted:" + tempDir8);
        }

        /// <summary>
        /// Checks whether an include pattern that matches all files in the base 
        /// directory (and its subdirectories), will NOT cause that directory 
        /// to be removed.
        /// </summary>
        [Test]
        public void Test_DeleteFileSet6() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, tempDir1, "**/*"));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsFalse(Directory.Exists(tempDir3), "Dir should have been deleted:" + tempDir3);
            Assert.IsFalse(Directory.Exists(tempDir4), "Dir should have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_DeleteFileSet7() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='{1}' />
                        </fileset>
                    </delete>
                </project>";

            // pattern should match both directories and files named "boo"
            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName, "**/boo"));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should not have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);
            Assert.IsTrue(File.Exists(tempFile9), "File should not have been deleted:" + tempFile9);
            Assert.IsFalse(File.Exists(tempFile10), "File should have been deleted:" + tempFile10);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsFalse(Directory.Exists(tempDir7), "Dir should have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern1() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='goo/**' />
                            <exclude name='goo/**/ha.he3' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, tempDir1));

            Assert.IsTrue(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsFalse(Directory.Exists(tempDir4), "Dir should have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern1_NoIncludeEmptyDirs() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true' includeemptydirs='false'>
                        <fileset basedir='{0}'>
                            <include name='goo/**' />
                            <exclude name='goo/**/ha.he3' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, tempDir1));

            Assert.IsTrue(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern2() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='**/*' />
                            <exclude name='**/a.e/*' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsFalse(Directory.Exists(tempDir1), "Dir should have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsFalse(Directory.Exists(tempDir3), "Dir should have been deleted:" + tempDir3);
            Assert.IsFalse(Directory.Exists(tempDir4), "Dir should have been deleted:" + tempDir4);
            Assert.IsFalse(Directory.Exists(tempDir5), "Dir should have been deleted:" + tempDir5);
            Assert.IsFalse(Directory.Exists(tempDir6), "Dir should have been deleted:" + tempDir6);
            Assert.IsFalse(Directory.Exists(tempDir7), "Dir should have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern2_NoIncludeEmptyDirs() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true' includeemptydirs='false'>
                        <fileset basedir='{0}'>
                            <include name='**/*' />
                            <exclude name='**/a.e/*' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);
            Assert.IsTrue(File.Exists(tempFile8), "File should not have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern3() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true'>
                        <fileset basedir='{0}'>
                            <include name='**/*' />
                            <exclude name='**/a.e/*.whatever' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted:" + tempFile8);

            Assert.IsFalse(Directory.Exists(tempDir1), "Dir should have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsFalse(Directory.Exists(tempDir3), "Dir should have been deleted:" + tempDir3);
            Assert.IsFalse(Directory.Exists(tempDir4), "Dir should have been deleted:" + tempDir4);
            Assert.IsFalse(Directory.Exists(tempDir5), "Dir should have been deleted:" + tempDir5);
            Assert.IsFalse(Directory.Exists(tempDir6), "Dir should have been deleted:" + tempDir6);
            Assert.IsFalse(Directory.Exists(tempDir7), "Dir should have been deleted:" + tempDir7);
            Assert.IsFalse(Directory.Exists(tempDir8), "Dir should have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_ExcludePattern3_NoIncludeEmptyDirs() {
            string xmlProjectTemplateFileSet = @"
                <project>
                    <delete verbose='true' includeemptydirs='false'>
                        <fileset basedir='{0}'>
                            <include name='**/*' />
                            <exclude name='**/a.e/*.whatever' />
                        </fileset>
                    </delete>
                </project>";

            RunBuild(string.Format(CultureInfo.InvariantCulture,
                xmlProjectTemplateFileSet, TempDirName));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);
            Assert.IsFalse(File.Exists(tempFile8), "File should have been deleted:" + tempFile8);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);
            Assert.IsTrue(Directory.Exists(tempDir5), "Dir should not have been deleted:" + tempDir5);
            Assert.IsTrue(Directory.Exists(tempDir6), "Dir should not have been deleted:" + tempDir6);
            Assert.IsTrue(Directory.Exists(tempDir7), "Dir should not have been deleted:" + tempDir7);
            Assert.IsTrue(Directory.Exists(tempDir8), "Dir should not have been deleted:" + tempDir8);
        }

        [Test]
        public void Test_NonExistingFile() {
            string xmlProject = @"
                <project>
                    <delete file='nonexistingfile.txt' />
                </project>";
            RunBuild(xmlProject);
        }

        [Test]
        public void Test_NonExistingDirectory() {
            string xmlProject = @"
                <project>
                    <delete dir='nonexistingdir' />
                </project>";
            RunBuild(xmlProject);
        }
    }
}
