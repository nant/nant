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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text;
using System.Globalization;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {

    [TestFixture]
    public class ExecTaskTest : BuildTestBase {
        const string _format = @"<?xml version='1.0' ?>
            <project>
                <exec {0}>{1}</exec>
            </project>";


        /// <summary>Test <arg> option.</summary>
        [Test]
        public void Test_ArgOption() {
            string result = "";
            if (PlatformHelper.IsWin32) {
                result = RunBuild(FormatBuildFile("program='cmd.exe'", "<arg value='/c echo Hello, World!'/>"));
            } else {
                result = RunBuild(FormatBuildFile("program='echo'", "<arg value='Hello, World!'/>"));
            }
            Assert.IsTrue(result.IndexOf("Hello, World!") != -1, "Could not find expected text from external program, <arg> element is not working correctly.");
        }

        /// <summary>Regression test for bug #461732 - ExternalProgramBase.ExecuteTask() hanging</summary>
        /// <remarks>
        /// http://sourceforge.net/tracker/index.php?func=detail&aid=461732&group_id=31650&atid=402868
        /// </remarks>
        [Test]
        public void Test_ReadLargeAmountFromStdout() {

            // create a text file with A LOT of data
            string line = "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + Environment.NewLine;
            StringBuilder contents = new StringBuilder("You can delete this file" + Environment.NewLine);
            for (int i = 0; i < 250; i++) {
                contents.Append(line);
            }
            string tempFileName = Path.Combine(TempDirName, "bigfile.txt");
            TempFile.Create(tempFileName);

            if (PlatformHelper.IsWin32) {
                RunBuild(FormatBuildFile("program='cmd.exe' commandline='/c type &quot;" + tempFileName + "&quot;'", ""));
            } else {
                RunBuild(FormatBuildFile("program='cat' commandline=' &quot;" + tempFileName + "&quot;'", ""));
            }
            // if we get here then we passed, ie, no hang = bug fixed
        }

        private string FormatBuildFile(string attributes, string nestedElements) {
            return String.Format(CultureInfo.InvariantCulture, _format, attributes, nestedElements);
        }
    }
}
