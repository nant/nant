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
    /// Tests the FilterChain classes.
    /// </summary>
    [TestFixture]
    public class FilterChainTest : FilterTestBase {
        [Test]
        public void NoFilerTest () {
            base.TestFilter("", " ", " ");
        }

        [Test]
        public void NoFilterEmptyFileTest () {
            base.TestFilter(@"", "", "");
        }

        [Test]
        public void MalformedFilterTest () {
            base.TestFilter(@"<blah />", " ", " ");
        }

        [Test]
        public void FilterOrderTest1a () {
            base.TestFilter(@"<replacecharacter chartoreplace=""^"" replacementchar=""$"" order=""0"" />
                    <expandexpressions order=""1"" />", "^{'la' + 'la'}", "lala");
        }

        [Test]
        public void FilterOrderTest1b () {
            base.TestFilter(@"<expandexpressions order=""1"" />
                    <replacecharacter chartoreplace=""^"" replacementchar=""$"" order=""0"" />", "^{'la' + 'la'}", "lala");
        }

        [Test]
        public void FilterOrderTest2a () {
            base.TestFilter(@"<replacecharacter chartoreplace=""^"" replacementchar=""$"" order=""1"" />
                    <expandexpressions order=""0"" />", "^{'la' + 'la'}", "${'la' + 'la'}");
        }

        [Test]
        public void FilterOrderTest2b () {
            base.TestFilter(@"<expandexpressions order=""0"" />
                    <replacecharacter chartoreplace=""^"" replacementchar=""$"" order=""1"" />", "^{'la' + 'la'}", "${'la' + 'la'}");
        }
    }
}
