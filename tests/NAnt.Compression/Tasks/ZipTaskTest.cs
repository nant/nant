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

using Tests.NAnt.Core;

namespace Tests.NAnt.Compression.Tasks {
    [TestFixture]
    public class ZipTaskTest : BuildTestBase {
        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        public void Test_ReleaseBuild() {
            string projectXML = @"<?xml version='1.0'?>
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
            string projectXML = @"<?xml version='1.0'?>
            <project>
                <zip zipfile='test.zip' />
            </project>";
            //do the build            
            RunBuild(projectXML);
            Assert.IsTrue(File.Exists(Path.Combine(TempDirName, "test.zip")),
                "Zip File not created.");
        }
    }
}
