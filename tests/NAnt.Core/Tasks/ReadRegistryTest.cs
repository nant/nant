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
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {
    /// <summary>
    /// Tests the ReadRegistryTask.
    /// </summary>
    [TestFixture]
    public class ReadRegistryTest : BuildTestBase {
        
		[Test]
        public void Test_Read_Defaults() {
            string _xml = @"
                    <project name='PropTests'>
                        <readregistry property='windows.id' key='SOFTWARE\Microsoft\Windows\CurrentVersion\ProductId'/>
                        <echo message='productID=${windows.id};'/>
                    </project>";
            try {
                string result = RunBuild(_xml);
                Assertion.Assert("Fail message missing:\n" + result, result.IndexOf("productID=;") == -1);
            }
            catch (TestBuildException be) {
                Assertion.Fail("\n" + be.ToString());
            }
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
                Console.WriteLine(result);
                Assertion.Fail("Invalid key did not generate an exception");
            }
            catch (TestBuildException be) {
                //no op, good.
                if(be.InnerException.ToString().IndexOf("missing") != -1)
                    Assertion.Fail("Wrong type of exception; does not contain word 'missing'!\n" + be.ToString());
            }
            catch {
                Assertion.Fail("Other exception!");
            }
        }
    }
}
