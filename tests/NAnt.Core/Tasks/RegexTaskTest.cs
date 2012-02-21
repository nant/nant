// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez (ScottHernandez_at_hotmail.com)
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
// Michael Aird (mike@airdian.com)

using System;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class RegexTest : BuildTestBase {
        [Test]
        public void Test_RegexMatch() {
            string _xml = @"
                    <project name='RegexTests'>
                        <regex pattern=""(?'lastword'\w+)$"" input='This is a test sentence' />
                        <echo message='${lastword}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.AreNotEqual(-1, result.IndexOf("sentence"),
                String.Concat("Regex did not match properly.\n", result));
        }

        [Test]
        public void Test_RegexMatchMultiple() {
            string _xml = @"
                    <project name='RegexTests'>
                        <regex pattern=""^(?'path'.*(\\|/)|(/|\\))(?'file'.*)$"" input=""d:\Temp\SomeDir\SomeDir\bla.xml"" />
                        <echo message='path=${path}'/>
                        <echo message='file=${file}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assert.AreNotEqual(-1, result.IndexOf(@"path=d:\Temp\SomeDir\SomeDir\"),
                String.Concat("Regex did not set first property correcty.\n", result));
            Assert.AreNotEqual(-1, result.IndexOf(@"file=bla.xml"),
                String.Concat("Regex did not set second property correcty.\n", result));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_RegexNoMatch() {
            string _xml = @"
                    <project name='RegexTests'>
                        <regex pattern=""(?'digit'\d)$"" input='This is a test sentence' />
                        <echo message='${digit}'/>
                    </project>";
            RunBuild(_xml);
        }
    }
}
