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

//Test
namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the TabsToSpaces classes.
    /// </summary>
    [TestFixture]
    public class TabsToSpaces : FilterTestBase {
        const string _tagName = "tabstospaces";


        /// <summary>
        /// Empty input file
        /// </summary>
        [Test]
        public void EmptyFileBasicTest () {
            base.TestFilter(@"<" + _tagName + @" order=""0""/>", "", "");
        }


        /// <summary>
        /// Test default parameters
        /// </summary>
        [Test]
        public void DefaultParam() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" order=""0""/>";

            string input = "\t\tTEST\t\t";
            string expectedOutput = @"        TEST        ";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test specify replacement character
        /// </summary>
        [Test]
        public void SpecityCharacter() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" replacementchar=""*"" order=""0""/>";

            string input = "\t\tTEST\t\t";
            string expectedOutput = @"********TEST********";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }


        /// <summary>
        /// Test specity replacement character and replacement spaces
        /// </summary>
        [Test]
        public void SpecityAll() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" replacementchar=""*"" replacementspaces=""6"" order=""0""/>";

            string input = "\t\tTEST\t\t";
            string expectedOutput = @"************TEST************";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test with no tabs
        /// </summary>
        [Test]
        public void SpecityAllNoTabs() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" replacementchar=""*"" replacementspaces=""6"" order=""0""/>";

            string input = "NO TABS ARE PRESENT";
            string expectedOutput = @"NO TABS ARE PRESENT";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test using the filter tag instead of <tabstospaces>
        /// </summary>
        [Test]
        public void UsingFilterTag() {
            string prologueXml = null;

            string filterXml = @"<filter assembly=""NAnt.Core"" class=""NAnt.Core.Filters.TabsToSpaces"" order=""0"">
                               <param name=""replacementchar"" value=""*""/>
                               <param name=""replacementspaces"" value=""6""/>
                               </filter>";

            string input = "\t\tTEST\t\t";
            string expectedOutput = @"************TEST************";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }


        /// <summary>
        /// Test mixing tabs and characters
        /// </summary>
        [Test]
        public void Scattered() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" replacementchar=""*"" replacementspaces=""1"" order=""0""/>";

            string input = "aaaa\tbb\t\tb\tzzzz\tz\tz\tffff";
            string expectedOutput = @"aaaa*bb**b*zzzz*z*z*ffff";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }


    }
}
