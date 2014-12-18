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
        
        [Test]
        public void Test_BuildXmlns() {
            const string buildXmlnsFile = @"<?xml version='1.0'?>
                        <project default='includeXmlnsTarget' basedir='{0}' xmlns='http://nant.sf.net/release/0.85/nant.xsd'>
                          <include buildfile='example1.xml' />
                        </project>";
                        
            const string includeNonXmlnsFile = @"<?xml version='1.0'?>
                        <project>
                        <target name='includeXmlnsTarget'>
                          <echo message='Target executed'/>
                        </target>
                        </project>";
                        
            
            string includeNonXmlnsFileName = Path.Combine(TempDirName, "example1.xml");
            TempFile.CreateWithContents(includeNonXmlnsFile, includeNonXmlnsFileName);
            
            string result = RunBuild(FormatBuildFile(buildXmlnsFile));
            Assert.IsTrue(result.IndexOf("Target executed") != -1, "Include target without xmlns did not execute." + Environment.NewLine + result);
            
        }
        
        [Test]
        public void Test_IncludeXmlns() {
            const string buildNonXmlnsFile = @"<?xml version='1.0'?>
                        <project default='includeXmlnsTarget' basedir='{0}'>
                          <include buildfile='example2.xml' />
                        </project>";
                        
            const string includeXmlnsFile = @"<?xml version='1.0'?>
                        <project xmlns='http://nant.sf.net/release/0.85/nant.xsd'>
                        <target name='includeXmlnsTarget'>
                          <echo message='Target executed'/>
                        </target>
                        </project>";
                        
            
            string includeXmlnsFileName = Path.Combine(TempDirName, "example2.xml");
            TempFile.CreateWithContents(includeXmlnsFile, includeXmlnsFileName);
            
            string result = RunBuild(FormatBuildFile(buildNonXmlnsFile));
            Assert.IsTrue(result.IndexOf("Target executed") != -1, "Include target with xmlns did not execute." + Environment.NewLine + result);
            
        }
        
        [Test]
        public void Test_IncludeDuplicateFile() {
            const string mainBuildFile = @"<project name='ot' default='targets.coverage' basedir='.' >
                          <include buildfile='includes.include'/>
                          <include buildfile='targets.include'/>
                        </project>";
                        
            const string includeIncludeFile = @"<project name='includes' basedir='.'>
                          <include buildfile='targets.include'/>
                        </project>";
                        
            const string targetIncludeFile = @"<?xml version='1.0' encoding='ISO-8859-1'?>
                        <project name='ect.nant.targets' default='' basedir='.'>
                          <target name='targets.coverage'>
                            <echo message='Message from targets' />
                          </target>
                        </project>";
            
            string includeIncludeFileName = Path.Combine(TempDirName, "includes.include");
            TempFile.CreateWithContents(includeIncludeFile, includeIncludeFileName);
            
            string targetIncludeFileName = Path.Combine(TempDirName, "targets.include");
            TempFile.CreateWithContents(targetIncludeFile, targetIncludeFileName);
            
            string result = RunBuild(FormatBuildFile(mainBuildFile));
            Assert.IsTrue(result.IndexOf("Message from targets") != -1, "Include target with duplicate include did not execute." + Environment.NewLine + result);
            
        }

        private string FormatBuildFile(string format) {
            return String.Format(CultureInfo.InvariantCulture, format, TempDirName);
        }
    }
}
