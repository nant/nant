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

using NUnit.Framework;

using NAnt.Core.Filters;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the <see cref="ReplaceCharacter" /> filter.
    /// </summary>
    [TestFixture]
    public class ReplaceCharacterTest : FilterTestBase {
        const string _tagName = "replacecharacter";
        const char _stringChar = 'A';
        const int _minStringExpressionLength = 5;

        [Test]
        public void InstantiationTest () {
            base.FilterTest(@"<" + _tagName + @" from=""^"" to=""$"" />", " ", " ");
        }

        [Test]
        public void EmptyFileTest () {
            base.FilterTest(@"<" + _tagName + @" from=""^"" to=""$"" />", " ", " ");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoCharToReplaceTest () {
            base.FilterTest(@"<" + _tagName + @" to=""$"" />", " ", " ");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void NoReplacementCharTest () {
            base.FilterTest(@"<" + _tagName + @" from=""^"" />", " ", " ");
        }

        [Test]
        public void BasicTest () {
            base.FilterTest(@"<" + _tagName + @" from=""^"" to=""$"" />", "hello!\n^\n^^\ngoodbye!", "hello!\n$\n$$\ngoodbye!");
        }
    }
}
