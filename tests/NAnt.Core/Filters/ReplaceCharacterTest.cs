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
    /// Tests the ReplaceCharacter class.
    /// </summary>
    [TestFixture]
    public class ReplaceCharacterTest : FilterTestBase {
        const string _tagName = "replacecharacter";
        const char _stringChar = 'A';
        const int _minStringExpressionLength = 5;

        [Test]
        public void InstantiationTest () {
            base.TestFilter(@"<" + _tagName + @" chartoreplace=""^"" replacementchar=""$"" order=""0"" />", " ", " ");
        }

        [Test]
        public void EmptyFileTest () {
            base.TestFilter(@"<" + _tagName + @" chartoreplace=""^"" replacementchar=""$"" order=""0"" />", " ", " ");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoCharToReplaceTest () {
            base.TestFilter(@"<" + _tagName + @" replacementchar=""$"" order=""0"" />", " ", " ");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoReplacementCharTest () {
            base.TestFilter(@"<" + _tagName + @" chartoreplace=""^"" order=""0"" />", " ", " ");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoOrderTest () {
            base.TestFilter(@"<" + _tagName + @" chartoreplace=""^"" replacementchar=""$"" />", " ", " ");
        }

        [Test]
        public void BasicTest () {
            base.TestFilter(@"<" + _tagName + @" chartoreplace=""^"" replacementchar=""$"" order=""0"" />", "hello!\n^\n^^\ngoodbye!", "hello!\n$\n$$\ngoodbye!");
        }
    }
}
