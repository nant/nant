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
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {

    [TestFixture]
    public class IncludeTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0'?>
           <project basedir='{0}' default='includedTarget'>
                <include buildfile='include.xml'/>
            </project>";

        const string _includedBuildFile = @"<?xml version='1.0'?>
            <project>
                <echo message='Task executed'/>
                <target name='includedTarget'>
                    <echo message='Target executed'/>
                </target>
            </project>";

        string _includeFileName;

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _includeFileName = Path.Combine(TempDirName, "include.xml");
            TempFile.CreateWithContents(_includedBuildFile, _includeFileName);
        }

        [Test]
        public void Test_Simple() {
            string result = RunBuild(FormatBuildFile(_format));
            Assert.IsTrue(result.IndexOf("Task executed") != -1, "Global task should have executed." + Environment.NewLine + result);
            Assert.IsTrue(result.IndexOf("Target executed") != -1, "Target should have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_NestedTask() {
            const string formatNestedTask = @"<?xml version='1.0' ?>
               <project basedir='{0}' default='test'>
                    <target name='test'>
                        <include buildfile='include.xml'/>
                    </target>
                </project>";

            try {
                RunBuild(formatNestedTask);
                Assert.Fail("Task appears in target element but BuildException not thrown.");
            } catch (TestBuildException e) {
                Assert.IsTrue(e.InnerException.Message.IndexOf("Task not allowed in targets.") != -1,
                    "Build exception should have been because of a nested task." + Environment.NewLine + e.ToString());
            }
        }

        [Test]
        public void Test_RecursiveInclude() {
            // modify included build file to recursively include itself
            string recursiveIncludedBuildFile = @"<?xml version='1.0'?><project><include buildfile='include.xml'/></project>";
            File.Delete(_includeFileName);
            TempFile.CreateWithContents(recursiveIncludedBuildFile, _includeFileName);

            try {
                RunBuild(FormatBuildFile(_format));
                Assert.Fail("Task appears in target element but BuildException not thrown.");
            } catch (TestBuildException e) {
                Assert.IsTrue(e.InnerException.Message.IndexOf("Recursive includes are not allowed.") != -1,
                    "Build exception should have been because of a recursive include." + Environment.NewLine + e.ToString());
            }
        }


        private string FormatBuildFile(string format) {
            return String.Format(CultureInfo.InvariantCulture, format, TempDirName);
        }
    }
}
