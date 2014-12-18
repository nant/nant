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
using System.Collections;
using System.IO;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using Tests.NAnt.Core.Util;

using NUnit.Framework;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class ExpressionEvaluatorTest : BuildTestBase {
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

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _buildFileName = Path.Combine(TempDirName, "test.build");
            TempFile.CreateWithContents(FormatBuildFile("", ""), _buildFileName);

            _project = new Project(_buildFileName, Level.Info, 0);
            _project.Properties["prop1"] = "asdf";
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        [Test]
        public void TestRelationOperators() {
            // string & string
            AssertExpression("'a' == 'a'", true);
            AssertExpression("'a' == 'b'", false);
            AssertExpression("'a' != 'b'", true);
            AssertExpression("'a' != 'a'", false);
            AssertExpression("'a' > 'b'", false);
            AssertExpression("'a' > 'a'", false);
            AssertExpression("'b' > 'a'", true);
            AssertExpression("'a' >= 'b'", false);
            AssertExpression("'a' >= 'a'", true);
            AssertExpression("'b' >= 'a'", true);
            AssertExpression("'a' < 'b'", true);
            AssertExpression("'a' < 'a'", false);
            AssertExpression("'b' < 'a'", false);
            AssertExpression("'a' <= 'b'", true);
            AssertExpression("'a' <= 'a'", true);
            AssertExpression("'b' <= 'a'", false);

            // bool & bool
            AssertExpression("false == false", true);
            AssertExpression("false == true", false);
            AssertExpression("false != true", true);
            AssertExpression("false != false", false);
            AssertExpression("false > true", false);
            AssertExpression("false > false", false);
            AssertExpression("true > false", true);
            AssertExpression("false >= true", false);
            AssertExpression("false >= false", true);
            AssertExpression("true >= false", true);
            AssertExpression("false < true", true);
            AssertExpression("false < false", false);
            AssertExpression("true < false", false);
            AssertExpression("false <= true", true);
            AssertExpression("false <= false", true);
            AssertExpression("true <= false", false);
            
            // int & int
            AssertExpression("1 == 1", true);
            AssertExpression("1 == 2", false);
            AssertExpression("1 != 2", true);
            AssertExpression("1 != 1", false);
            AssertExpression("1 > 2", false);
            AssertExpression("1 > 1", false);
            AssertExpression("2 > 1", true);
            AssertExpression("1 >= 2", false);
            AssertExpression("1 >= 1", true);
            AssertExpression("2 >= 1", true);
            AssertExpression("1 < 2", true);
            AssertExpression("1 < 1", false);
            AssertExpression("2 < 1", false);
            AssertExpression("1 <= 2", true);
            AssertExpression("1 <= 1", true);
            AssertExpression("2 <= 1", false);

            // int & long
            AssertExpression("1 == long::parse('1')", true);
            AssertExpression("1 == long::parse('66666666666666')", false);
            AssertExpression("1 != long::parse('66666666666666')", true);
            AssertExpression("1 != long::parse('1')", false);
            AssertExpression("1 > long::parse('2')", false);
            AssertExpression("1 > long::parse('1')", false);
            AssertExpression("2 > long::parse('1')", true);
            AssertExpression("1 >= long::parse('66666666666666')", false);
            AssertExpression("1 >= long::parse('1')", true);
            AssertExpression("2 >= long::parse('1')", true);
            AssertExpression("1 < long::parse('66666666666666')", true);
            AssertExpression("1 < long::parse('1')", false);
            AssertExpression("2 < long::parse('1')", false);
            AssertExpression("1 <= long::parse('66666666666666')", true);
            AssertExpression("1 <= long::parse('1')", true);
            AssertExpression("2 <= long::parse('1')", false);

            // int & double
            AssertExpression("1 == 1.0", true);
            AssertExpression("1 == 1.5", false);
            AssertExpression("1 != 1.5", true);
            AssertExpression("1 != 1.0", false);
            AssertExpression("1 > 1.5", false);
            AssertExpression("2 > 1.5", true);
            AssertExpression("1 > 0.0", true);
            AssertExpression("1 >= 1.5", false);
            AssertExpression("1 >= 1.0", true);
            AssertExpression("2 >= 1.5", true);
            AssertExpression("1 < 1.5", true);
            AssertExpression("1 < 1.0", false);
            AssertExpression("2 < 1", false);
            AssertExpression("1 <= 1.5", true);
            AssertExpression("1 <= 1.0", true);
            AssertExpression("2 <= 1.5", false);

            // long & long
            AssertExpression("66666666666666 == 66666666666666", true);
            AssertExpression("66666666666666 == 77777777777777", false);
            AssertExpression("66666666666666 != 77777777777777", true);
            AssertExpression("66666666666666 != 66666666666666", false);
            AssertExpression("66666666666666 > 77777777777777", false);
            AssertExpression("66666666666666 > 66666666666666", false);
            AssertExpression("77777777777777 > 66666666666666", true);
            AssertExpression("66666666666666 >= 77777777777777", false);
            AssertExpression("66666666666666 >= 66666666666666", true);
            AssertExpression("77777777777777 >= 66666666666666", true);
            AssertExpression("66666666666666 < 77777777777777", true);
            AssertExpression("66666666666666 < 66666666666666", false);
            AssertExpression("77777777777777 < 66666666666666", false);
            AssertExpression("66666666666666 <= 77777777777777", true);
            AssertExpression("66666666666666 <= 66666666666666", true);
            AssertExpression("77777777777777 <= 66666666666666", false);

            // long & int
            AssertExpression("long::parse('1') == 1", true);
            AssertExpression("long::parse('1') == 2", false);
            AssertExpression("long::parse('1') != 2", true);
            AssertExpression("long::parse('1') != 1", false);
            AssertExpression("long::parse('1') > 2", false);
            AssertExpression("long::parse('1') > 1", false);
            AssertExpression("long::parse('2') > 1", true);
            AssertExpression("long::parse('1') >= 2", false);
            AssertExpression("long::parse('1') >= 1", true);
            AssertExpression("long::parse('2') >= 1", true);
            AssertExpression("long::parse('1') < 2", true);
            AssertExpression("long::parse('1') < 1", false);
            AssertExpression("long::parse('2') < 1", false);
            AssertExpression("long::parse('1') <= 2", true);
            AssertExpression("long::parse('1') <= 1", true);
            AssertExpression("long::parse('2') <= 1", false);

            // long & double
            AssertExpression("long::parse('1') == 1.0", true);
            AssertExpression("long::parse('1') == 2.0", false);
            AssertExpression("long::parse('1') != 2.0", true);
            AssertExpression("long::parse('1') != 1.0", false);
            AssertExpression("long::parse('1') > 2.0", false);
            AssertExpression("long::parse('1') > 1.0", false);
            AssertExpression("long::parse('2') > 1.0", true);
            AssertExpression("long::parse('1') >= 2.0", false);
            AssertExpression("long::parse('1') >= 1.0", true);
            AssertExpression("long::parse('2') >= 1.0", true);
            AssertExpression("long::parse('1') < 2.0", true);
            AssertExpression("long::parse('1') < 1.0", false);
            AssertExpression("long::parse('2') < 1.0", false);
            AssertExpression("long::parse('1') <= 2.0", true);
            AssertExpression("long::parse('1') <= 1.0", true);
            AssertExpression("long::parse('2') <= 1.0", false);

            // double & double
            AssertExpression("1.0 == 1.0", true);
            AssertExpression("1.0 == 2.0", false);
            AssertExpression("1.0 != 2.0", true);
            AssertExpression("1.0 != 1.0", false);
            AssertExpression("1.0 > 2.0", false);
            AssertExpression("1.0 > 1.0", false);
            AssertExpression("2.0 > 1.0", true);
            AssertExpression("1.0 >= 2.0", false);
            AssertExpression("1.0 >= 1.0", true);
            AssertExpression("2.0 >= 1.0", true);
            AssertExpression("1.0 < 2.0", true);
            AssertExpression("1.0 < 1.0", false);
            AssertExpression("2.0 < 1.0", false);
            AssertExpression("1.0 <= 2.0", true);
            AssertExpression("1.0 <= 1.0", true);
            AssertExpression("2.0 <= 1.0", false);

            // double & int
            AssertExpression("1.0 == 1", true);
            AssertExpression("1.0 == 2", false);
            AssertExpression("1.0 != 2", true);
            AssertExpression("1.0 != 1", false);
            AssertExpression("1.0 > 2", false);
            AssertExpression("1.0 > 1", false);
            AssertExpression("2.0 > 1", true);
            AssertExpression("1.0 >= 2", false);
            AssertExpression("1.0 >= 1", true);
            AssertExpression("2.0 >= 1", true);
            AssertExpression("1.0 < 2", true);
            AssertExpression("1.0 < 1", false);
            AssertExpression("2.0 < 1", false);
            AssertExpression("1.0 <= 2", true);
            AssertExpression("1.0 <= 1", true);
            AssertExpression("2.0 <= 1", false);

            // double & long
            AssertExpression("1.0 == long::parse('1')", true);
            AssertExpression("1.0 == 66666666666666", false);
            AssertExpression("1.0 != 66666666666666", true);
            AssertExpression("1.0 != long::parse('1')", false);
            AssertExpression("1.0 > 66666666666666", false);
            AssertExpression("1.0 > long::parse('1')", false);
            AssertExpression("2.0 > long::parse('1')", true);
            AssertExpression("1.0 >= 66666666666666", false);
            AssertExpression("1.0 >= long::parse('1')", true);
            AssertExpression("2.0 >= long::parse('1')", true);
            AssertExpression("1.0 < 66666666666666", true);
            AssertExpression("1.0 < long::parse('1')", false);
            AssertExpression("2.0 < long::parse('1')", false);
            AssertExpression("1.0 <= 66666666666666", true);
            AssertExpression("1.0 <= long::parse('1')", true);
            AssertExpression("2.0 <= long::parse('1')", false);

            // datetime & datetime
            // TO-DO !!!!

            // timespan & timespan
            AssertExpression("timespan::from-days(1.0) == timespan::from-days(1.0)", true);
            AssertExpression("timespan::from-days(1.0) == timespan::from-days(2.0)", false);
            AssertExpression("timespan::from-days(1.0) != timespan::from-days(2.0)", true);
            AssertExpression("timespan::from-days(1.0) != timespan::from-days(1.0)", false);
            AssertExpression("timespan::from-days(1.0) > timespan::from-days(2.0)", false);
            AssertExpression("timespan::from-days(1.0) > timespan::from-days(1.0)", false);
            AssertExpression("timespan::from-days(2.0) > timespan::from-days(1.0)", true);
            AssertExpression("timespan::from-days(1.0) >= timespan::from-days(2.0)", false);
            AssertExpression("timespan::from-days(1.0) >= timespan::from-days(1.0)", true);
            AssertExpression("timespan::from-days(2.0) >= timespan::from-days(1.0)", true);
            AssertExpression("timespan::from-days(1.0) < timespan::from-days(2.0)", true);
            AssertExpression("timespan::from-days(1.0) < timespan::from-days(1.0)", false);
            AssertExpression("timespan::from-days(2.0) < timespan::from-days(1.0)", false);
            AssertExpression("timespan::from-days(1.0) <= timespan::from-days(2.0)", true);
            AssertExpression("timespan::from-days(1.0) <= timespan::from-days(1.0)", true);
            AssertExpression("timespan::from-days(2.0) <= timespan::from-days(1.0)", false);

            // version & version
            AssertExpression("version::parse('1.0') == version::parse('1.0')", true);
            AssertExpression("version::parse('1.0') == version::parse('1.0.0')", false);
            AssertExpression("version::parse('1.0') == version::parse('1.0.0.1')", false);
            AssertExpression("version::parse('1.0') == version::parse('2.0')", false);
            AssertExpression("version::parse('1.0') != version::parse('1.0')", false);
            AssertExpression("version::parse('1.0') != version::parse('1.0.0')", true);
            AssertExpression("version::parse('1.0') != version::parse('1.0.0.1')", true);
            AssertExpression("version::parse('1.0') != version::parse('2.0')", true);
            AssertExpression("version::parse('1.0') > version::parse('1.0')", false);
            AssertExpression("version::parse('1.0') > version::parse('1.0.0')", false);
            AssertExpression("version::parse('1.0') > version::parse('1.0.0.1')", false);
            AssertExpression("version::parse('1.0') > version::parse('2.0')", false);
            AssertExpression("version::parse('1.0.0') > version::parse('1.0')", true);
            AssertExpression("version::parse('1.0.1') > version::parse('1.0')", true);
            AssertExpression("version::parse('2.0') > version::parse('1.0')", true);
            AssertExpression("version::parse('1.0') >= version::parse('1.0')", true);
            AssertExpression("version::parse('1.0') >= version::parse('1.0.0')", false);
            AssertExpression("version::parse('1.0') >= version::parse('1.0.0.1')", false);
            AssertExpression("version::parse('1.0') >= version::parse('2.0')", false);
            AssertExpression("version::parse('1.0.1') >= version::parse('1.0')", true);
            AssertExpression("version::parse('1.0') < version::parse('1.0')", false);
            AssertExpression("version::parse('1.0') < version::parse('1.0.0')", true);
            AssertExpression("version::parse('1.0') < version::parse('1.0.0.1')", true);
            AssertExpression("version::parse('1.0') < version::parse('2.0')", true);
            AssertExpression("version::parse('1.0.1') < version::parse('1.0')", false);
            AssertExpression("version::parse('2.0') < version::parse('1.0')", false);
            AssertExpression("version::parse('1.0') <= version::parse('1.0')", true);
            AssertExpression("version::parse('1.0') <= version::parse('1.0.0')", true);
            AssertExpression("version::parse('1.0') <= version::parse('1.0.0.1')", true);
            AssertExpression("version::parse('1.0') <= version::parse('2.0')", true);
            AssertExpression("version::parse('1.0.1') <= version::parse('1.0')", false);
            AssertExpression("version::parse('2.0') <= version::parse('1.0')", false);
        }

        [Test]
        public void TestCoreOperations() {
            AssertExpression("1 + 2", 3);
            AssertExpression("1 + 2 + 3", 6);
            AssertExpression("1 + 2 * 3", 7);
            AssertExpression("2 * 1 * 3", 6);
            AssertExpression("1 / 2 + 3", 3);
            AssertExpression("5.0 / (2 + 8)", 0.5);
            AssertExpression("((((1))))", 1);
            AssertExpression("((((1 + 2))))", 3);
            AssertExpression("((((1 + 2)+(2 + 1))))", 6);
            AssertExpression("((((1 + 2)/(2 + 1))))", 1);
            AssertExpression("-1", -1);
            AssertExpression("--1", 1);
            AssertExpression("10 % 3", 1);
            AssertExpression("10 % 3 % 5", 1);
            AssertExpression("-1 == 1 - 2", true);
            AssertExpression("--1.0 == 1.0", true);
            AssertExpression("1 != 1", false);
            AssertExpression("1 == 2", false);
            AssertExpression("10.0 - 1.0 >= 8.9", true);
            AssertExpression("10.0 + 1 <= 11.1", true);
            AssertExpression("1 * 1.0 == 1.0", true);
            AssertFailure("1.aaaa"); // fractional part expected
            AssertFailure("(1 1)");
            AssertFailure("aaaa::1");
            AssertFailure("aaaa::bbbb 1");
        }
        
        [Test]
        public void TestCoreOperationFailures() {
            AssertFailure("1 + aaaa");
            AssertFailure("1 + ");
            AssertFailure("*3");
            AssertFailure("2 */ 1 * 3");
            AssertFailure("1 // 2  + 3");
            AssertFailure("double::tostring(5)/(2 + 8)");
            AssertFailure("double::to-string(1 / 2 + 3");
            AssertFailure("-'aaa'");
            AssertFailure("true + true");
            AssertFailure("true - true");
            AssertFailure("true * true");
            AssertFailure("true / true");
            AssertFailure("true % true");
            AssertFailure("((((1)))");
            AssertFailure("((1 + 2))))");
            AssertFailure("((((1 + 2) + (2 + 1)))");
            AssertFailure("5 / 0");
            AssertFailure("5 % 0");
        }
        
        [Test]
        public void TestRelationalOperators() {
            AssertExpression("'a' + 'b' == 'ab'", true);
            AssertExpression("true", true);
            AssertExpression("false", false);
        }
        
        [Test]
        public void TestLogicalOperators() {
            AssertExpression("true or false or false", true);
            AssertExpression("false or false or false", false);
            AssertExpression("false or true", true);
            AssertExpression("true and false", false);
            AssertExpression("true and true and false", false);
            AssertExpression("true and true and true", true);
            AssertExpression("false and true and true", false);
            AssertExpression("not true", false);
            AssertExpression("not false", true);
            AssertExpression("not (1==1)", false);
            AssertExpression("true or not (1 == 1)", true);
            AssertExpression("true or not (--1 == 1)", true);
        }

        [Test]
        public void TestConversionFunctions() {
            // string to bool
            AssertExpression("bool::parse('True')", true);
            AssertExpression("bool::parse('true')", true);
            AssertExpression("bool::parse('False')", false);
            AssertExpression("bool::parse('false')", false);
            AssertFailure("bool::parse('aaafalse')");

            // bool to string
            AssertExpression("bool::to-string(false)", bool.FalseString);
            AssertExpression("bool::to-string(true)", bool.TrueString);
            AssertFailure("bool::to-string('aaafalse')");
            AssertFailure("bool::to-string(1)");

            // string to int
            AssertExpression("int::parse('123' + '45')", 12345);
            AssertFailure("int::parse('12345.66666')");

            // int to string
            AssertExpression("int::to-string(12345)", "12345");

            // string to long
            AssertExpression("long::parse('6667778888' + '666777')", 6667778888666777);

            // long to string
            AssertExpression("long::to-string(6667778888666777)", "6667778888666777");

            // string to double
            AssertExpression("double::parse('5') / (2 + 8)", 0.5);
            AssertExpression("double::parse('1') / 2 + 3", 3.5);
            AssertFailure("double::parse('aaaaaaaaa')");

            // double to string
            AssertExpression("double::to-string(5.56)", "5.56");
            AssertExpression("double::to-string(5.0)", "5");
            AssertFailure("double::to-string(5#0)");

            // string to datetime
            AssertExpression("datetime::parse('12/31/1999 01:23:34')", new DateTime(1999,12,31,1,23,34));
            AssertFailure("datetime::parse('B')");

            // coercion
            AssertExpression("coercion::get-name(coercion::create-bar())", "default bar");
            AssertExpression("coercion::get-name(coercion::create-bar('a'))", "a");
            AssertExpression("coercion::get-name(coercion::create-boo())", "default boo");
            AssertExpression("coercion::get-name(coercion::create-boo('b'))", "b");
            AssertExpression("coercion::get-name(coercion::create-zoo())", "default zoo");
            AssertExpression("coercion::get-name(coercion::create-zoo('c'))", "c");
            AssertFailure("coercion::get-name(coercion::create-foo())");
            AssertFailure("coercion::get-name(coercion::create-foo('d'))");
        }

        [Test]
        public void TestStringFunctions() {
            AssertExpression("string::get-length('')", 0);
            AssertExpression("string::get-length('') == 0", true);
            AssertExpression("string::get-length('') == 1", false);
            AssertExpression("string::get-length('test')", 4);
            AssertExpression("string::get-length('test') == 4", true);
            AssertExpression("string::get-length('test') == 5", false);
            AssertExpression("string::get-length(prop1)", 4);
            AssertExpression("string::get-length('d''Artagnan')", 10);
            AssertExpression("string::get-length('d''Artagnan') == 10", true);
            AssertExpression("string::get-length('d''Artagnan') == 11", false);
            AssertExpression("string::substring('abcde',1,2) == 'bc'", true);
            AssertExpression("string::trim('  ab  ') == 'ab'", true);
            AssertExpression("string::trim-start('  ab  ') == 'ab  '", true);
            AssertExpression("string::trim-end('  ab  ') == '  ab'", true);
            AssertExpression("string::pad-left('ab',5,'.') == '...ab'", true);
            AssertExpression("string::pad-right('ab',5,'.') == 'ab...'", true);
            AssertExpression("string::index-of('abc','c') == 2", true);
            AssertExpression("string::index-of('abc','d') == -1", true);
            AssertExpression("string::index-of('abc','d') == -1", true);
        }
        
        [Test]
        public void TestDateTimeFunctions() {
            AssertFailure("datetime::now(111)");
            AssertFailure("datetime::add()");
            AssertFailure("datetime::now(");
        }
        
        [Test]
        public void TestMathFunctions() {
            AssertExpression("math::round(0.1)", 0.0);
            AssertExpression("math::round(0.7)", 1.0);
            AssertExpression("math::floor(0.1)", 0.0);
            AssertExpression("math::floor(0.7)", 0.0);
            AssertExpression("math::ceiling(0.1)", 1.0);
            AssertExpression("math::ceiling(0.7)", 1.0);
            AssertExpression("math::abs(1)", 1.0);
            AssertExpression("math::abs(-1)", 1.0);
        }
        
        [Test]
        public void TestConditional() {
            AssertExpression("if(true,1,2)", 1);
            AssertExpression("if(true,'a','b')", "a");
            AssertExpression("if(false,'a','b')", "b");
            AssertFailure("if(1,2,3)");
            AssertFailure("if(true 2,3)");
            AssertFailure("if(true,2,3 3");
            AssertFailure("if(true,2 2,3)");
            AssertFailure("if [ true, 1, 0 ]");
        }
        
        [Test]
        public void TestFileFunctions() {
            AssertExpression("file::exists('c:\\i_am_not_there.txt')", false);
            AssertFailure("file::get-last-write-time('c:/no-such-file.txt')");
        }
        
        [Test]
        public void TestDirectoryFunctions() {
            AssertExpression("directory::exists('c:\\i_am_not_there')", false);
            AssertExpression("directory::exists('" + Directory.GetCurrentDirectory() + "')", true);
        }
        
        [Test]
        public void TestNAntFunctions() {
            AssertExpression("property::get-value('prop1')", "asdf");
            AssertExpression("property::exists('prop1')", true);
            AssertExpression("property::exists('prop1a')", false);
            //AssertExpression("target::exists('i_am_not_there')", false);
            //AssertExpression("target::exists('test')", true);
        }

        [Test]
        public void TestStandaloneEvaluator() {
            ExpressionEvaluator eval = 
                new ExpressionEvaluator(_project, 
                _project.Properties,
                new Hashtable(),
                new Stack());
            
            Assert.AreEqual(eval.Evaluate("1 + 2 * 3"), 7);
            eval.CheckSyntax("1 + 2 * 3");
        }
        
        [Test]
        [ExpectedException(typeof(ExpressionParseException))]
        public void TestStandaloneEvaluatorFailure() {
            ExpressionEvaluator eval = new ExpressionEvaluator(_project, 
                _project.Properties,
                new Hashtable(), 
                new Stack());

            eval.Evaluate("1 + 2 * datetime::now(");
        }
        
        [Test]
        [ExpectedException(typeof(ExpressionParseException))]
        public void TestStandaloneEvaluatorFailure2() {
            ExpressionEvaluator eval = new ExpressionEvaluator(_project, 
                _project.Properties,
                new Hashtable(),
                new Stack());

            eval.Evaluate("1 1");
        }
        
        [Test]
        [ExpectedException(typeof(ExpressionParseException))]
        public void TestStandaloneEvaluatorSyntaxCheckFailure() {
            ExpressionEvaluator eval = new ExpressionEvaluator(_project, 
                _project.Properties,
                new Hashtable(),
                new Stack());

            eval.CheckSyntax("1 + 2 * 3 1");
        }

        #endregion Public Instance Methods

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
                _project.Log(Level.Debug, "Got expected failure on ${" + expression + "}: " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                // ok - this one should have been thrown
            } catch (Exception ex) {
                // some other exception has been thrown - fail
                Assert.Fail("Expected BuildException while evaluating ${" + expression + "}, but " + ex.GetType().FullName + " was thrown.");
            }
        }

        private string FormatBuildFile(string globalTasks, string targetTasks) {
            return string.Format(CultureInfo.InvariantCulture, _format, TempDirName, globalTasks, targetTasks);
        }

        #endregion Private Instance Methods
    }

    [FunctionSet("coercion", "Test Function Extensibility")]
    public class CoercionFunctions : FunctionSetBase {
        public CoercionFunctions(Project project, PropertyDictionary propDict ) :
            base(project, propDict) {
        }

        [Function("create-foo")]
        public static Foo CreateFoo() {
            return new Foo();
        }

        [Function("create-foo")]
        public static Foo CreateFoo(string name) {
            return new Foo(name);
        }

        [Function("create-bar")]
        public static Bar CreateBar() {
            return new Bar();
        }

        [Function("create-bar")]
        public static Bar CreateBar(string name) {
            return new Bar(name);
        }

        [Function("create-zoo")]
        public static Zoo CreateZoo() {
            return new Zoo();
        }

        [Function("create-zoo")]
        public static Zoo CreateZoo(string name) {
            return new Zoo(name);
        }

        [Function("create-boo")]
        public static Bar CreateBoo() {
            return new Boo();
        }

        [Function("create-boo")]
        public static Boo CreateBoo(string name) {
            return new Boo(name);
        }

        [Function("get-name")]
        public static string GetName(Bar bar) {
            return bar.Name;
        }
    }

    public class Foo {
        private readonly string name;

        public Foo() : this("default foo") {
        }

        public Foo(String name) {
            this.name = name;
        }

        public string Name {
            get { return name; }
        }
    }

    public class Bar : Foo {
        public Bar() : this("default bar") {
        }

        public Bar(String name) : base(name) {
        }
    }

    public class Zoo : Bar {
        public Zoo() : this("default zoo") {
        }

        public Zoo(String name) : base(name) {
        }
    }

    public class Boo : Bar, IConvertible {
        public Boo() : this("default boo") {
        }

        public Boo(String name) : base(name) {
        }

        #region IConvertible Members

        [CLSCompliant(false)]
        public ulong ToUInt64(IFormatProvider provider) {
            return 0;
        }

        [CLSCompliant(false)]
        public sbyte ToSByte(IFormatProvider provider) {
            return 0;
        }

        public double ToDouble(IFormatProvider provider) {
            return 0;
        }

        public DateTime ToDateTime(IFormatProvider provider) {
            return new DateTime ();
        }

        public float ToSingle(IFormatProvider provider) {
            return 0;
        }

        public bool ToBoolean(IFormatProvider provider) {
            return false;
        }

        public int ToInt32(IFormatProvider provider) {
            return 0;
        }

        [CLSCompliant(false)]
        public ushort ToUInt16(IFormatProvider provider) {
            return 0;
        }

        public short ToInt16(IFormatProvider provider) {
            return 0;
        }

        public string ToString(IFormatProvider provider) {
            return null;
        }

        public byte ToByte(IFormatProvider provider) {
            return 0;
        }

        public char ToChar(IFormatProvider provider) {
            return '\0';
        }

        public long ToInt64(IFormatProvider provider) {
            return 0;
        }

        public System.TypeCode GetTypeCode() {
            return new System.TypeCode ();
        }

        public decimal ToDecimal(IFormatProvider provider) {
            return 0;
        }

        public object ToType(Type conversionType, IFormatProvider provider) {
            if (conversionType == typeof(Foo))
                return new Foo(Name + " => foo");
            if (conversionType == typeof(Bar))
                return new Bar(Name + " => bar");
            throw new InvalidCastException();
        }

        [CLSCompliant(false)]
        public uint ToUInt32(IFormatProvider provider) {
            return 0;
        }

        #endregion
    }
}
