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
using System.IO;

using NUnit.Framework;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Base class for running input through filters and checking results.
    /// </summary>
    public abstract class FilterTestBase : BuildTestBase {
        const string _testFileName = "filterTest.txt";
        const string _destDirName = "copy";
        const string _projectXmlFormat = @"<?xml version='1.0'?>
            <project>
                {0}
                <copy todir=""" + _destDirName + @""" verbose=""true"">
                    <filterchain>{1}</filterchain>
                    <fileset basedir=""."">
                        <include name=""" + _testFileName + @""" />
                    </fileset>
                </copy>
            </project>";

        protected void FilterTest(string filterXml, string input, string expectedOutput) {
            FilterTest(filterXml, input, expectedOutput, String.Empty);
        }

        protected void FilterTest(string filterXml, string input, string expectedOutput, string prologueXml) {
            base.CreateTempFile(_testFileName, input);
            base.RunBuild(string.Format(_projectXmlFormat, prologueXml, filterXml));
            
            string actualOutput;
            TextReader outputFile = File.OpenText(Path.Combine(Path.Combine(base.TempDirName, _destDirName), _testFileName));
            try {
                actualOutput = outputFile.ReadToEnd();
            }
            finally {
                outputFile.Close();
            }

            Assert.AreEqual(expectedOutput, actualOutput, "Filter's actual output does not match expected output!");
        }

        protected override void TearDown() {
            base.TearDown ();
        }
    }
}
