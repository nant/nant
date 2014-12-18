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
        public void SetSingleEnv() {
            string _xml1 = @"
                    <project>
                        <setenv name='FooA' value='ValueA' />
                        <setenv name='FooB' value='ValueB' />
                        <setenv name='FooC' value='ValueC' />
                    </project>";
            RunBuild(_xml1);
            Assert.AreEqual ("ValueA", Environment.GetEnvironmentVariable("FooA"), "#A1");
            Assert.AreEqual ("ValueB", Environment.GetEnvironmentVariable("FooB"), "#A2");
            Assert.AreEqual ("ValueC", Environment.GetEnvironmentVariable("FooC"), "#A3");

            string _xml2 = @"
                    <project>
                        <setenv name='FooA' value='' />
                        <setenv name='FooB' />
                        <setenv name='FooC' value=' ' />
                        <setenv name='FooD' value='' />
                        <setenv name='FooE' />
                        <setenv name='FooF' value=' ' />
                    </project>";
            RunBuild(_xml2);
            Assert.IsNull (Environment.GetEnvironmentVariable("FooA"), "#B1");
            Assert.IsNull (Environment.GetEnvironmentVariable("FooB"), "#B2");
            Assert.AreEqual (" ", Environment.GetEnvironmentVariable("FooC"), "#B3");
            Assert.IsNull (Environment.GetEnvironmentVariable("FooD"), "#B4");
            Assert.IsNull (Environment.GetEnvironmentVariable("FooE"), "#B5");
            Assert.AreEqual (" ", Environment.GetEnvironmentVariable("FooF"), "#B6");
        }

        [Test]
        public void Test_SetMultipleEnvVars() {
            string _xml = @"
                    <project>
                        <setenv>
                            <variable name='var1' value='value1' />
                            <variable name='var2' value='value2' unless='${1==2}' />
                            <variable name='var3' value='value3' />
                            <variable name='var4' value='value4' />
                            <variable name='var5' value='' />
                            <variable name='var6' />
                            <variable name='var7' value=' ' />
                            <variable name='var8' value='value8' unless='${1==1}' />
                            <variable name='var9' value='value9' if='${1==1}' />
                        </setenv>
                        <setenv>
                            <variable name='var3' value='' />
                            <variable name='var4' />
                        </setenv>
                    </project>";
            RunBuild(_xml);
            Assert.AreEqual ("value1", Environment.GetEnvironmentVariable("var1"), "#1");
            Assert.AreEqual ("value2", Environment.GetEnvironmentVariable("var2"), "#2");
            Assert.IsNull (Environment.GetEnvironmentVariable("var3"), "#3");
            Assert.IsNull (Environment.GetEnvironmentVariable("var4"), "#4");
            Assert.IsNull (Environment.GetEnvironmentVariable("var5"), "#5");
            Assert.IsNull (Environment.GetEnvironmentVariable("var6"), "#6");
            Assert.AreEqual (" ", Environment.GetEnvironmentVariable("var7"), "#7");
            Assert.IsNull (Environment.GetEnvironmentVariable("var8"), "#8");
            Assert.AreEqual ("value9", Environment.GetEnvironmentVariable("var9"), "#9");
        }

        [Test]
        public void Test_ExpandEnvStrings() {
            string _xml1 = @"
                    <project>
                        <setenv >
                            <variable name='var1' />
                            <variable name='var2' value='value2' />
                            <variable name='var3' value='value3:%var2%:%var1%' />
                        </setenv>
                    </project>";
            string _xml2 = @"
                    <project>
                        <setenv name='var4' />
                        <setenv name='var5' value='value5' />
                        <setenv name='var6' value='value6:%var5%:%var4%' />
                    </project>";

            RunBuild(_xml1);
            Assert.IsNull (Environment.GetEnvironmentVariable ("var1"), "#A1");
            Assert.AreEqual ("value2", Environment.GetEnvironmentVariable("var2"), "#A2");
            Assert.AreEqual("value3:value2:%var1%", Environment.GetEnvironmentVariable("var3"), "#A3");

            RunBuild (_xml2);
            Assert.IsNull (Environment.GetEnvironmentVariable ("var4"), "#B1");
            Assert.AreEqual ("value5", Environment.GetEnvironmentVariable ("var5"), "#B2");
            Assert.AreEqual ("value6:value5:%var4%", Environment.GetEnvironmentVariable ("var6"), "#B3");
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
