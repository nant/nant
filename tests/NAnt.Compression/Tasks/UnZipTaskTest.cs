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

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Compression.Tasks {
    [TestFixture]
    public class UnZipTaskTest : BuildTestBase {
        const string _projectXML = @"<?xml version='1.0'?>
            <project>
                <zip zipfile='docs.zip'>
                    <fileset basedir='doc'>
                        <include name='**/*/**'/>
                    </fileset>
                </zip>
                <unzip zipfile='docs.zip'/>
            </project>";

        [Test]
        public void Test_ReleaseBuild() {
            CreateTempDir("doc");
            CreateTempFile(Path.Combine ("doc", "temp1.file"), "hello");
            RunBuild(_projectXML);
            Assert.IsTrue(Directory.Exists(Path.Combine(TempDirName, "doc")),
                "UnZip dir not created.");
            TempDir.Delete("doc");
        }

        /// <summary>
        /// Checks whether a "normal" build error is reported when build author
        /// attempts to extract a directory instead of a zip file.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void ExtractDirectory() {
            CreateTempDir("doc");

            string projectXML = @"<project>
                <unzip zipfile='doc'/>
            </project>";

            RunBuild(projectXML);
        }
    }
}
