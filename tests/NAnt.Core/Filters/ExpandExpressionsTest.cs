// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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

using System;
using System.Text;
using NAnt.Core.Filters;
using NUnit.Framework;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the ExpandExpressions class.
    /// </summary>
    [TestFixture]
    public class ExpandExpressionsTest : FilterTestBase {
        const string _tagName = "expandexpressions";
        const char _stringChar = 'A';
        const int _minStringExpressionLength = 5;

        [Test]
        public void InstantiationTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", " ", " ");
        }

        [Test]
        public void EmptyFileTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", "", "");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoOrderTest () {
            base.TestFilter(@"<" + _tagName + @" />", " ", " ");
        }

        [Test]
        public void ExpressionTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", "${'la' + 'la'}", "lala");
        }

        [Test]
        public void MaxSafeExpressionTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", GetString(4090) + GetStringExpression(2048), GetString(4090) + GetString(2048 - _minStringExpressionLength));
        }

        [Test]
        public void UnsafeButExpandedExpressionTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", GetStringExpression(4095), GetString(4095 - _minStringExpressionLength));
        }

        [Test]
        public void UnsafeAndIgnoredExpressionTest () {
            string temp = GetString(1) + GetStringExpression(4095);
            base.TestFilter(@"<" + _tagName + @" order=""0"" />", temp, temp);
        }

        private string GetStringExpression (int expressionLength) {
            if (expressionLength < _minStringExpressionLength) {
                throw new ArgumentException("A string expression can't be shorter than \"${''}\"!");
            }

            StringBuilder bldr = new StringBuilder(expressionLength);
            bldr.Append("${'");
            bldr.Append(_stringChar, expressionLength - _minStringExpressionLength);
            bldr.Append("'}");
            return bldr.ToString();
        }

        private string GetString (int length) {
            if (length < 0) {
                throw new ArgumentException("A string can't be shorter than \"\"!");
            }

            StringBuilder bldr = new StringBuilder(length);
            bldr.Append(_stringChar, length);

            return bldr.ToString();
        }
    }
}
