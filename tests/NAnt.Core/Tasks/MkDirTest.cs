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
    /// Tests mkdir Tasks. <para>Creates a temp directory and deletes it.</para>
    /// </summary>
    public class MkDirTest : BuildTestBase {
        public MkDirTest(String name) : base(name) {
        }

        public void Test_CreateDirectory() {
            string _xml= @"
            <project>
                <mkdir dir='{0}'/>
            </project>";
            
            string tempDir = Path.Combine(TempDirName, "goo");
            string result = RunBuild(String.Format(_xml, tempDir));
            
            Assert("Dir should have been created:" + result, Directory.Exists(tempDir));
            
        }
    }
}
