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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class PropertyTest : BuildTestBase {
        [Test]
        public void Test_PropCreate() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value not set.\n" + result, result.IndexOf("I Love you") != -1);
        }

        [Test]
        public void Test_PropReset() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <echo message='I Love ${foo}'/>
                        <property name='foo' value='me'/>
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value not re-set.\n" + result, result.IndexOf("I Love me") != -1);
        }
 
        [Test]
        public void Test_ROSet() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='nant.filename' value='you'/>
                        <echo message='nant.filename=${nant.filename}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("RO Property value was set.\n" + result, result.IndexOf("nant.filename=you") == -1);
        }

        [Test]
        public void Test_NoOverwriteProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <property name='foo' value='me' overwrite='false' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value should not have been overwritten.\n" + result, result.IndexOf("I Love me") == -1);
        }

        [Test]
        public void Test_OverwriteProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <property name='foo' value='me' overwrite='true' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value should have been overwritten.\n" + result, result.IndexOf("I Love me") != -1);
        }

        [Test]
        public void Test_OverwriteReadOnlyProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you' readonly='true'/>
                        <property name='foo' value='me' overwrite='true' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Read-only property should not have been overwritten.\n" + result, result.IndexOf("I Love me") == -1);
        }

        [Test]
        public void Test_DynamicProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='me' />
                        <property name='foo2' value='I Love ${foo}' dynamic='true' />
                        <property name='foo' value='you' />
                        <echo message='${foo2}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Value of dynamic property should have reflected change in referenced property.\n" + result, result.IndexOf("I Love you") != -1);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_DynamicPropertyNotExisting() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo2' value='I love ${foo}' dynamic='true' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_DynamicPropertyCircularReference() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='${foo2}' dynamic='true' />
                        <property name='foo2' value='${foo}' dynamic='true' />
                        <echo message='${foo}' />
                    </project>";
            string result = RunBuild(_xml);
        }

        [Test]
        public void Test_ChangeStaticPropertyToDynamic() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='me' />
                        <property name='foo2' value='test' />
                        <property name='foo2' value='I Love ${foo}' dynamic='true' />
                        <property name='foo' value='you' />
                        <echo message='${foo2}' />
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Static property should be upgraded to dynamic property.\n" + result, result.IndexOf("I Love you") != -1);
        }

        [Test]
        public void Test_ReadOnlyDynamicProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='me' />
                        <property name='foo2' value='I Love ${foo}' dynamic='true' readonly='true' />
                        <property name='foo' value='you' />
                        <echo message='${foo2}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Value of read-only dynamic property should have reflected change in referenced property.\n" + result, result.IndexOf("I Love you") != -1);
        }
    }
}