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
using System.IO;
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
                        <setenv>
                            <variable name='var1' value='value1' />
                            <variable name='var2' value='value2' />
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
            string _xml = @"
                    <project>
                        <setenv >
                            <variable name='var2' value='value2' />
                            <variable name='var3' value='value3:%var2%' />
                        </setenv>
                    </project>";
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("var3") != null, 
                "Environment variable var3 should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("var3") == "value3:value2", 
                "Environment variable var3 should have been set to 'value3:value2'" );
        }
        [Test]
        public void Test_UsePathAttribute() {
            string _xml = @"
                    <project>
                        <setenv name='test_path' path='/home/foo' />
                    </project>";
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("test_path") != null, 
                "Environment variable test_path should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("test_path") == "/home/foo", 
                "Environment variable test_path should have been set to '/home/foo'" );
        }
        [Test]
        public void Test_NestedPathElement() {
            string expectedPath = string.Format(@"{0}{1}dir1{2}{0}{1}dir2",
                TempDirectory.FullName, Path.DirectorySeparatorChar, Path.PathSeparator);
            string _xml = @"
                    <project>
                        <setenv>
                            <variable name='test_path2'>
                                <path>
                                    <pathelement dir='dir1' />
                                    <pathelement dir='dir2' />
                                </path>
                            </variable>
                        </setenv>
                    </project>";
            RunBuild(_xml);
            Assert.IsTrue( Environment.GetEnvironmentVariable("test_path2") != null, 
                "Environment variable test_path2 should have been set" );
            Assert.IsTrue( Environment.GetEnvironmentVariable("test_path2") == expectedPath, 
                "Environment variable test_path2 should have been set to '{0}' actual value is {1}",
                    expectedPath, Environment.GetEnvironmentVariable("test_path2") );
        }
    }
}