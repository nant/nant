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
using System.Globalization;
using System.Threading;
using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class PropertyTest : BuildTestBase {
        private CultureInfo _originalCulture;
        private CultureInfo _originalUICulture;

        protected override void SetUp() {
            base.SetUp();

            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        }

        [TearDown]
        protected override void TearDown() {
            base.TearDown();
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }

        [Test]
        public void Test_PropCreate() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("I Love you") != -1, "Property value not set." + Environment.NewLine + result);
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
            Assert.IsTrue(result.IndexOf("I Love me") != -1, "Property value not re-set." + Environment.NewLine + result);
        }

        /// <summary>
        /// Overwriting a read-only property should result in build error.
        /// </summary>
        [Ignore("For now, we only output a warning message when read-only properties are overwritten.")]
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_ROSet() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='test' value='you' readonly='true' />
                        <property name='test' value='you2' />
                    </project>";
            RunBuild(_xml);
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
            Assert.IsTrue(result.IndexOf("I Love me") == -1, "Property value should not have been overwritten." + Environment.NewLine + result);
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
            Assert.IsTrue(result.IndexOf("I Love me") != -1, "Property value should have been overwritten." + Environment.NewLine + result);
        }

        /// <summary>
        /// Overwriting a read-only property should result in build error.
        /// </summary>
        [Ignore("For now, we only output a warning message when read-only properties are overwritten.")]
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_OverwriteReadOnlyProperty() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you' readonly='true'/>
                        <property name='foo' value='me' overwrite='true' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            RunBuild(_xml);
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
            Assert.IsTrue(result.IndexOf("I Love you") != -1, "Value of dynamic property should have reflected change in referenced property." + Environment.NewLine + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_DynamicPropertyNotExisting() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo2' value='I love ${foo}' dynamic='true' />
                        <echo message='I Love ${foo}'/>
                    </project>";
            RunBuild(_xml);
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
            RunBuild(_xml);
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
            Assert.IsTrue(result.IndexOf("I Love you") != -1, "Static property should be upgraded to dynamic property." + Environment.NewLine + result);
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
            Assert.IsTrue(result.IndexOf("I Love you") != -1, "Value of read-only dynamic property should have reflected change in referenced property." + Environment.NewLine + result);
        }

        [Test]
        public void Test_EscapedDollarPropertyValue()
        {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='test.var' value='some simple value'/>
                        <echo message='$${test.var} = ${test.var}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.IsTrue(result.IndexOf("${test.var} = some simple value") != -1,
                String.Format("Value of escaped property not as expected. - '{0}'",
                result));
            Assert.IsTrue(result.IndexOf("$${test.var} = some simple value") == -1,
                "Extra '$' character was not stripped");
        }

        [Test]
        public void CurrentFramework_NonExisting () {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='nant.settings.currentframework' value='doesnotexist' />
                    </project>";
            try {
                RunBuild(_xml);
                Assert.Fail ("#1");
            } catch (TestBuildException ex) {
                Assert.IsNotNull(ex.InnerException, "#2");

                string expectedError = "Target framework could not be changed. " +
                    "\"doesnotexist\" is not a valid framework identifier.";

                BuildException inner = ex.InnerException as BuildException;
                Assert.IsNotNull(inner, "#3");
                Assert.IsNull(inner.InnerException, "#4");
                Assert.IsNotNull(inner.RawMessage, "#5");
                Assert.IsTrue(inner.RawMessage.StartsWith(expectedError),
                    "#6:" + inner.RawMessage);
            }
        }

        [Test]
        public void CurrentFramework_Invalid () {
            FrameworkInfo invalid = null;

            Project p = CreateEmptyProject();
            foreach (FrameworkInfo framework in p.Frameworks) {
                if (!framework.IsValid) {
                    invalid = framework;
                    break;
                }
            }

            if (invalid == null) {
                Assert.Ignore("Test requires at least one invalid framework.");
            }

            string _xml = @"
                    <project name='PropTests'>
                        <property name='nant.settings.currentframework' value='{0}' />
                    </project>";
            try {
                RunBuild(string.Format(_xml, invalid.Name));
                Assert.Fail ("#1");
            } catch (TestBuildException ex) {
                Assert.IsNotNull(ex.InnerException, "#2");

                // either initialization of the framework failed, or validation
                // failed
                BuildException inner = ex.InnerException as BuildException;
                Assert.IsNotNull(inner, "#3");
                Assert.IsNotNull(inner.InnerException, "#4");
                Assert.IsNotNull(inner.RawMessage, "#5");
            }
        }

        /// <summary>
        /// Test to make sure that the _unless_ xml attribute works as expected for
        /// the property task (and other tasks as well)
        /// </summary>
        [Test]
        public void Test_PropertyUnlessAttribute()
        {
            string xml = "<project name='PropTests'>" +
                    "<property name='myMonth' value='january'/>" +
                    "<property name='myMonth' value='${does.not.exist}' unless=\"${property::exists('myMonth')}\"/>" +
                    "<echo message='${myMonth}'/>" +
                "</project>";
            string result = RunBuild(xml);
            Assert.IsTrue(result.Contains("january"));
        }
    }
}
