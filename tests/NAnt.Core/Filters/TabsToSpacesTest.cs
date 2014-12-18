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

using NUnit.Framework;
using NAnt.Core.Filters;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the <see cref="TabsToSpaces" /> filter.
    /// </summary>
    [TestFixture]
    public class TabsToSpacesTest : FilterTestBase {
        const string _tagName = "tabstospaces";

        /// <summary>
        /// Empty input file
        /// </summary>
        [Test]
        public void EmptyFileBasicTest () {
            base.FilterTest(@"<" + _tagName + @" />", "", "");
        }

        /// <summary>
        /// Test default parameters
        /// </summary>
        [Test]
        public void DefaultParam() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" />";

            string input = "\tTEST\t";
            string expectedOutput = @"        TEST        ";
            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test with tab length that is lower than minimum length (which is 1)
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void TabLengthLow() {
            string filterXml = @"<" + _tagName + @" tablength=""0"" />";
            base.FilterTest(filterXml, " ", " ");
        }

        /// <summary>
        /// Test with tab length that is higher than maximum length (which is 100)
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void TabLengthHigh() {
            string filterXml = @"<" + _tagName + @" tablength=""101"" />";
            base.FilterTest(filterXml, " ", " ");
        }

        /// <summary>
        /// Test with no tabs
        /// </summary>
        [Test]
        public void SpecityAllNoTabs() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" />";

            string input = "NO TABS ARE PRESENT";
            string expectedOutput = @"NO TABS ARE PRESENT";
            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test mixing tabs and characters
        /// </summary>
        [Test]
        public void Scattered() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" tablength=""1"" />";

            string input = "aaaa\tbb\t\tb\tzzzz\tz\tz\tffff";
            string expectedOutput = @"aaaa bb  b zzzz z z ffff";
            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }
    }
}
