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
using System.Xml;

using NUnit.Framework;

using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {
    /// <summary>
    /// Tests the Echo test.
    /// </summary>
    public class FailTest : BuildTestBase {
        
        public FailTest(String name) : base(name) {
        }

        public void Test_Fail() {
            string _xml = @"
                    <project>
                        <fail message='Death Sucks!'/>
                    </project>";
            try {
                string result = RunBuild(_xml);            
                Assert("Fail message missing:" + result, result.IndexOf("Death Sucks!") != -1);
            }
            catch (BuildException be) {
                Assert("Did not fail from Test!", be.ToString().IndexOf("Death Sucks!") != -1);
            }
        }
    }
}
