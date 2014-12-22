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

using NUnit.Framework;
using Tests.NAnt.Core;

namespace Tests.NAnt.Win32.Tasks {
    /// <summary>
    /// Tests the ReadRegistryTask.
    /// </summary>
    [TestFixture]
    public class ReadRegistryTest : BuildTestBase {

        [Test]
        public void Test_Read_Defaults() {
            string _xml = string.Empty;
            switch (Environment.OSVersion.Platform) {
                case PlatformID.Win32Windows:
                    // String to use for win9x system
                    _xml = @"
                        <project name='PropTests'>
                            <readregistry property='windows.id' key='SOFTWARE\Microsoft\Windows\CurrentVersion\ProductName'/>
                            <echo message='productID=${windows.id};'/>
                        </project>";
                    break;
                case PlatformID.Win32NT:
                    // String to use for winnt system
                    _xml = @"
                        <project name='PropTests'>
                            <readregistry property='windows.id' key='SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProductName'/>
                            <echo message='productID=${windows.id};'/>
                        </project>";
                    break;
                default:
                    Assert.Fail("Unsupported Windows version detected.");
                    break;
            }

            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("productID=;") == -1,
                "Fail message missing:" + Environment.NewLine + result);
        }
        /// <summary>
        /// Makes sure invalid path throws and exception
        /// </summary>
        [Test]
        public void Test_BadPath() {
            string _xml = @"
                    <project name='foo'>
                        <readregistry property='foo' key='noway\do\I\EXIst'/>
                    </project>";
            try {
                string result = RunBuild(_xml);
                Assert.Fail("Invalid key did not generate an exception:" + result);
            } catch (TestBuildException be) {
                //no op, good.
                if(be.InnerException.ToString().IndexOf("missing") != -1)
                    Assert.Fail("Wrong type of exception; does not contain word 'missing'!" + Environment.NewLine + be.ToString());
            }
        }
    }
}
