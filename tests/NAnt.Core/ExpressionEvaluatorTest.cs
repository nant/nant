// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    [TestFixture] public class ExpressionEvaluatorTest : BuildTestBase {
        #region Private Instance Fields

        private string _format = @"<?xml version='1.0'?>
            <project name='ProjectTest' default='test' basedir='{0}'>
                {1}
                <target name='test'>
                    {2}
                </target>
            </project>";

        private string _buildFileName;
        private Project _project;

        #endregion Private Instance Fields

        [SetUp] protected override void SetUp() {
            base.SetUp();
            _buildFileName = Path.Combine(TempDirName, "test.build");
            TempFile.CreateWithContents(FormatBuildFile("", ""), _buildFileName);

            //_project = new Project(_buildFileName, Level.Debug);
            _project = new Project(_buildFileName, Level.Info);
            _project.Properties["prop1"] = "asdf";
        }

        [TearDown] protected override void TearDown() {
        }
        #region Public Instance Methods
        
        [Test] public void TestCoreOperations() {
            AssertExpression("1+2", 3);
            AssertExpression("1+2+3", 6);
            AssertExpression("1+2*3", 7);
            AssertExpression("2*1*3", 6);
            AssertExpression("1/2+3", 3);
            AssertExpression("5.0/(2+8)", 0.5);
            AssertExpression("convert::to-double(5)/(2+8)", 0.5);
            AssertExpression("convert::to-double(1)/2+3", 3.5);
            AssertExpression("((((1))))", 1);
            AssertExpression("((((1+2))))", 3);
            AssertExpression("((((1+2)+(2+1))))", 6);
            AssertExpression("((((1+2)/(2+1))))", 1);
            AssertExpression("-1", -1);
            AssertExpression("--1", 1);
            AssertExpression("'a' = 'a'", true);
            AssertExpression("'a' = 'b'", false);
            AssertExpression("'a' <> 'a'", false);
            AssertExpression("'a' <> 'b'", true);
            AssertExpression("'a' + 'b' = 'ab'", true);
            AssertExpression("1 = 1", true);
            AssertExpression("1 <> 1", false);
            AssertExpression("1 = 2", false);
            AssertExpression("1 <> 2", true);
            AssertExpression("1.0 = 1.0", true);
            AssertExpression("1.0 <> 1.0", false);
            AssertExpression("1.0 = 2.0", false);
            AssertExpression("1.0 <> 2.0", true);
            AssertExpression("true", true);
            AssertExpression("false", false);
            AssertExpression("true==true", true);
            AssertExpression("true==false", false);
            AssertExpression("true<>false", true);
            AssertExpression("true<>true", false);
            AssertExpression("not true", false);
            AssertExpression("not false", true);
            AssertExpression("not (1=1)", false);
        }
        
        [Test] public void TestCoreOperationFailures() {
            AssertFailure("1+aaaa");
            AssertFailure("1+");
            AssertFailure("*3");
            AssertFailure("2*/1*3");
            AssertFailure("1//2+3");
            AssertFailure("convert::todouble(5)/(2+8)");
            AssertFailure("convert::to-double(1/2+3");
            //AssertFailure("((((1)))");
            //AssertFailure("((1+2))))");
            //AssertFailure("((((1+2)+(2+1))))");
            //AssertFailure("5.0/0.0");
        }
        
        [Test] public void TestStringFunctions() {
            AssertExpression("string::get-length('')", 0);
            AssertExpression("string::get-length('')=0", true);
            AssertExpression("string::get-length('')=1", false);
            AssertExpression("string::get-length('test')", 4);
            AssertExpression("string::get-length('test')=4", true);
            AssertExpression("string::get-length('test')=5", false);
            AssertExpression("string::get-length(prop1)", 4);
            AssertExpression("string::get-length('d''Artagnan')", 10);
            AssertExpression("string::get-length('d''Artagnan')=10", true);
            AssertExpression("string::get-length('d''Artagnan')=11", false);
            AssertExpression("string::substring('abcde',1,2)='bc'", true);
            AssertExpression("string::trim('  ab  ')='ab'", true);
            AssertExpression("string::trim-start('  ab  ')='ab  '", true);
            AssertExpression("string::trim-end('  ab  ')='  ab'", true);
            AssertExpression("string::pad-left('ab',5,'.')='...ab'", true);
            AssertExpression("string::pad-right('ab',5,'.')='ab...'", true);
            AssertExpression("string::index-of('abc','c')=2", true);
            AssertExpression("string::index-of('abc','d')=-1", true);
            AssertExpression("string::index-of('abc','d')=-1", true);
        }
        
        [Test] public void TestMathFunctions() {
            AssertExpression("math::round(0.1)", 0.0);
            AssertExpression("math::round(0.7)", 1.0);
            AssertExpression("math::floor(0.1)", 0.0);
            AssertExpression("math::floor(0.7)", 0.0);
            AssertExpression("math::ceiling(0.1)", 1.0);
            AssertExpression("math::ceiling(0.7)", 1.0);
            AssertExpression("math::abs(1)", 1.0);
            AssertExpression("math::abs(-1)", 1.0);
        }
        
        [Test] public void TestConditional() {
            AssertExpression("if(true,1,2)", 1);
            AssertExpression("if(true,'a','b')", "a");
            AssertExpression("if(false,'a','b')", "b");
        }
        
        [Test] public void TestFileFunctions() {
            AssertExpression("file::exists('c:\\i_am_not_there.txt')", false);
        }
        
        [Test] public void TestDirectoryFunctions() {
            AssertExpression("directory::exists('c:\\i_am_not_there')", false);
            AssertExpression("directory::exists('" + Directory.GetCurrentDirectory() + "')", true);
        }
        
        [Test] public void TestNAntFunctions() {
            AssertExpression("nant::get-property-value('prop1')", "asdf");
            AssertExpression("nant::property-exists('prop1')", true);
            AssertExpression("nant::property-exists('prop1a')", false);
            //AssertExpression("nant::target-exists('i_am_not_there')", false);
            //AssertExpression("nant::target-exists('test')", true);
        }
        #endregion
        
        #region Private Instance Methods

        private void AssertExpression(string expression, object expectedReturnValue) {
            string value = _project.ExpandProperties("${" + expression + "}", Location.UnknownLocation);
            string expectedStringValue = Convert.ToString(expectedReturnValue, CultureInfo.InvariantCulture);

            _project.Log(Level.Debug, "expression: " + expression);
            _project.Log(Level.Debug, "value: " + value + ", expected: " + expectedStringValue);
            Assert.AreEqual(expectedStringValue, value, expression);
        }

        private void AssertFailure(string expression) {
            try {
                string value = _project.ExpandProperties("${" + expression + "}", Location.UnknownLocation);
                // we shouldn't get here
                Assert.Fail("Expected BuildException while evaluating ${" + expression + "}, nothing was thrown. The returned value was " + value);
            } catch (BuildException ex) {
                _project.Log(Level.Debug, "Got expected failure on ${" + expression + "}: " + ex.InnerException.Message);
                // ok - this one should have been thrown
            } catch (Exception ex) {
                // some other exception has been thrown - fail
                Assert.Fail("Expected BuildException while evaluating ${" + expression + "}, but " + ex.GetType().FullName + " was thrown.");
            }
        }

        private string FormatBuildFile(string globalTasks, string targetTasks) {
            return string.Format(CultureInfo.InvariantCulture, _format, TempDirName, globalTasks, targetTasks);
        }

        #endregion

    }
}

