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
    public class TarTaskTest : BuildTestBase {
        /// <summary>
        /// Test to make sure a simple tar file can be created.
        /// </summary>
        [Test]
        public void Test_SimpleTar() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <tar destfile='test.tar'>
                        <fileset basedir='src'>
                            <include name='**'/>
                        </fileset>
                    </tar>
                </project>";

            CreateTempDir("src");
            CreateTempFile(Path.Combine("src", "temp1.file"),"hello");
            RunBuild(projectXML);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirName,"test.tar")),
                "Tar File not created.");
        }

        /// <summary>
        /// Verifies whether Flatten produces a flat tar file by discarding
        /// directory structure.
        /// attribute.
        /// </summary>
        [Test]
        public void Test_FlattenedFile() {
            const string projectXML = @"<?xml version='1.0'?>
                <project>
                    <tar destfile='test.tar' flatten='true'>
                        <fileset basedir='src'>
                            <include name='/**/*' />
                        </fileset>
                    </tar>
                    <untar src='test.tar' dest='extract' />
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
