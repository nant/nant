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
    /// <para>Tests move task.</para>
    /// </summary>
    public class MoveTest : BuildTestBase {
        const string _xmlProjectTemplate = @"
            <project>
                <move file='{1}' tofile='{0}'/>
            </project>";

        string tempDirDest;
        string tempFileSrc;

        public MoveTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
            tempDirDest = CreateTempDir("foob");
            tempFileSrc = CreateTempFile("foo.xml");
        }

        public void Test_Move() {
            Assert("File should have been created:" + tempDirDest, File.Exists(tempFileSrc));
            Assert("Dir should have been created:" + tempDirDest, Directory.Exists(tempDirDest));

            string result = RunBuild(String.Format(_xmlProjectTemplate, Path.Combine(tempDirDest,"foo.xml"), tempFileSrc));
            
            Assert("File should have been removed (during move):" + result, !File.Exists(tempFileSrc));
            Assert("File should have been added (during move):" + result, File.Exists(Path.Combine(tempDirDest, "foo.xml")));

            //hopefully this file won't be there, if things worked
            File.Delete(tempFileSrc);
            TempDir.Delete(tempDirDest);
        }
    }
}
