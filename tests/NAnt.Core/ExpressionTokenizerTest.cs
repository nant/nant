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

using NAnt.Core;

using NUnit.Framework;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class ExpressionTokenizerTest {
        [Test]
        public void Keyword() {
            string [] identifiers = new string[] {
                "a.d",
                "a-d",
                "a.d",
                "a\\d",
                "a_d",
                "_ad",
                "ad5"
                };

            for (int i = 0; i < identifiers.Length; i++) {
                string identifier = identifiers[i];

                ExpressionTokenizer et = new ExpressionTokenizer();
                et.InitTokenizer(identifier);
                Assert.AreEqual (identifier, et.TokenText, "#A1");
                Assert.AreEqual (ExpressionTokenizer.TokenType.Keyword, et.CurrentToken, "#A2:" + identifier);
                et.GetNextToken();
                Assert.AreEqual (identifier, et.TokenText, "#B1");
                Assert.AreEqual (ExpressionTokenizer.TokenType.EOF, et.CurrentToken, "#B2:" + identifier);
            }
        }

        [Test]
        public void Keyword_ShouldNotEndWithDash() {
            ExpressionTokenizer et = new ExpressionTokenizer();
            try {
                et.InitTokenizer("abc-");
                Assert.Fail();
            } catch (ExpressionParseException) {
            }
        }

        [Test]
        public void Keyword_ShouldNotEndWithDot() {
            ExpressionTokenizer et = new ExpressionTokenizer();
            try {
                et.InitTokenizer("abc.");
                Assert.Fail();
            } catch (ExpressionParseException) {
            }
        }
    }
}
