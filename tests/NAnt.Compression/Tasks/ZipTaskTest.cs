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
using System.Xml;

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
            CreateTempFile("src\\temp1.file","hello");
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
    }
}
