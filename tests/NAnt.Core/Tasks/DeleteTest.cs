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
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tests {
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
    public class DeleteTest : BuildTestBase {
        const string _xmlProjectTemplate = @"
            <project>
                <delete file='{0}'/>
                <delete dir='{1}'/>
                <delete dir='{2}'/>
            </project>";

        string tempFile;
        string tempDir;
        string tempFileInTempDirDir;

        public DeleteTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
            tempFile = CreateTempFile("a.b");
            tempDir = CreateTempDir("foo");
            tempFileInTempDirDir = CreateTempDir("goo");
            CreateTempFile(Path.Combine(tempFileInTempDirDir, "ha.he"));
        }

        public void Test_Delete() {
            Assert("File should have been created:" + tempFile, File.Exists(tempFile));
            Assert("Dir should have been created:" + tempDir, Directory.Exists(tempDir));
            Assert("Dir should have been created:" + tempFileInTempDirDir, Directory.Exists(tempFileInTempDirDir));

            string result = RunBuild(String.Format(_xmlProjectTemplate, tempFile, tempDir, tempFileInTempDirDir));
            
            Assert("File should have been deleted:" + result, !File.Exists(tempFile));
            Assert("Dir should have been deleted:" + result, !Directory.Exists(tempDir));
            Assert("Dir should have been deleted:" + result, !Directory.Exists(tempFileInTempDirDir));
        }
    }
}
