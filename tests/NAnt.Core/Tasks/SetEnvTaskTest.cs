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
//
// Ian MacLean (imaclean@gmail.com)

using System;
using NAnt.Core;
using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the Echo task.
    /// </summary>
    [TestFixture]
    public class SetEnvTaskTest : BuildTestBase {
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }
        
        [Test]
        public void Test_SetSingleEnv() {
            string _xml = @"
                    <project>
                        <setenv name='Foo' value='Some Value'/>
                    </project>";
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("Foo") != null, 
                "Environment variable Foo should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("Foo") == "Some Value", 
                "Environment variable Foo should have been set to 'Some Value'" );
        }
        
        [Test]
        public void Test_SetMultipleEnvVars() {
            string _xml = @"
                    <project>
                        <setenv >
                            <environment>
                                <option name='var1' value='value1' />
                                <option name='var2' value='value2' />
                            </environment>
                        </setenv>
                    </project>";
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("var1") != null, 
                "Environment variable var1 should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("var1") == "value1", 
                "Environment variable var1 should have been set to 'Some Value'" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("var2") != null, 
                "Environment variable var2 should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("var2") == "value2", 
                "Environment variable var2 should have been set to 'value2'" );
        }

        [Test]
        public void Test_ExpandEnvStrings() {
            string platformSpecVarPart = "";
            if ( PlatformHelper.IsWin32 ) {
                platformSpecVarPart = "%var2%";
            } else {
                platformSpecVarPart = "$var2";
            }
            string _xml = string.Format(@"
                    <project>
                        <setenv >
                            <environment>
                                <option name='var2' value='value2' />
                                <option name='var3' value='value3:{0}' />
                            </environment>
                        </setenv>
                    </project>", platformSpecVarPart);
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("var3") != null, 
                "Environment variable var3 should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("var3") == "value3:value2", 
                "Environment variable var3 should have been set to 'value3:value2'" );
        }
    }
}