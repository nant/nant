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

using NUnit.Framework;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tests {

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

		public IncludeTaskTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
			_includeFileName = Path.Combine(TempDirName, "include.xml");
            TempFile.CreateWithContents(_includedBuildFile, _includeFileName);
		}

        public void Test_Simple() {
            string result = RunBuild(FormatBuildFile(_format));
            Assert("Global task should have executed.\n" + result, result.IndexOf("Task executed") != -1);
            Assert("Target should have executed.\n" + result, result.IndexOf("Target executed") != -1);
        }

        public void Test_NestedTask() {
            const string formatNestedTask = @"<?xml version='1.0' ?>
               <project basedir='{0}' default='test'>
                    <target name='test'>
                        <include buildfile='include.xml'/>
                    </target>
                </project>";

            try {
                RunBuild(formatNestedTask);
                Fail("Task appears in target element but BuildException not thrown.");
            } catch (BuildException e) {
                Assert("Build exception should have been because of a nested task.\n" + e.Message, e.Message.IndexOf("Task not allowed in targets.") != -1);
            }
        }

        public void Test_RecursiveInclude() {
            // modify included build file to recursively include itself
            string recursiveIncludedBuildFile = @"<?xml version='1.0'?><project><include buildfile='include.xml'/></project>";
            File.Delete(_includeFileName);
            TempFile.CreateWithContents(recursiveIncludedBuildFile, _includeFileName);

            try {
                RunBuild(FormatBuildFile(_format));
                Fail("Task appears in target element but BuildException not thrown.");
            } catch (BuildException e) {
                Assert("Build exception should have been because of a recursive include.\n" + e.Message, e.Message.IndexOf("Recursive includes are not allowed.") != -1);
            }
        }


        private string FormatBuildFile(string format) {
            return String.Format(format, TempDirName);
        }
    }
}
