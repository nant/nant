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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tests {

    /// <summary>A simple task for testing Task class.</summary>
    [TaskName("test")]
    class TestTask : Task {
        bool _fail = false;

        [TaskAttribute("fail", Required=false)]
        [BooleanValidator()]
        public bool Fail {
            get { return _fail; }
            set { _fail = value; }
        }

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + "TestTask executed");
            Log.WriteLineIf(Verbose, LogPrefix + "Verbose message");
            if (Fail) {
                throw new BuildException("TestTask failed");
            }
        }
    }

    public class TaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0' ?>
           <project name='testing' default='test'>
                <!--<taskdef assembly='{0}'/>-->
                <target name='test'>
                    <test {1}/>
                </target>
            </project>";

		  public TaskTest(String name) : base(name) {
        }
        protected override void SetUp() {
            base.SetUp();
        }
        public void Test_Simple() {
            string result = RunBuild(FormatBuildFile(""));
            Assert("Task should have executed.\n" + result, result.IndexOf("TestTask executed") != -1);
        }

        public void Test_Verbose() {
            string result = RunBuild(FormatBuildFile("verbose='true'"));
            Assert("Verbose message should have been displayed.\n" + result, result.IndexOf("Verbose message") != -1);
        }

        public void Test_FailOnError() {
            string result = RunBuild(FormatBuildFile("fail='true' failonerror='false'"));
            Assert("Task should have failed.\n" + result, result.IndexOf("TestTask failed") != -1);
        }

        public void Test_If_True() {
            string result = RunBuild(FormatBuildFile("if='true'"));
            Assert("Task should have executed.\n" + result, result.IndexOf("TestTask executed") != -1);
        }

        public void Test_If_False() {
            string result = RunBuild(FormatBuildFile("if='false'"));
            Assert("Task should not have executed.\n" + result, result.IndexOf("TestTask executed") == -1);
        }

        public void Test_Unless_False() {
            string result = RunBuild(FormatBuildFile("unless='false'"));
            Assert("Task should have executed.\n" + result, result.IndexOf("TestTask executed") != -1);
        }

        public void Test_Unless_True() {
            string result = RunBuild(FormatBuildFile("unless='true'"));
            Assert("Task should not have executed.\n" + result, result.IndexOf("TestTask executed") == -1);
        }

        public void Test_Mixture() {
            string result = RunBuild(FormatBuildFile("verbose='true' if='true' unless='false'"));
            Assert("Task should have executed.\n" + result, result.IndexOf("TestTask executed") != -1);
        }

/*
        public void Test_UnknownAttribute() {
            try {
                string result = RunBuild(FormatBuildFile("FaIL='false'"));
                Assert("Task should have caused build error.\n" + result, result.IndexOf("BUILD ERROR") != -1);
            } catch (BuildException e) {
                Assert("Task should have caused build error from unknown attribute.\n" + e.Message, e.Message.IndexOf("Unknown attribute 'FaIl'") != -1);
            }
        }
*/
        private string FormatBuildFile(string attributes) {
            return String.Format(_format, Assembly.GetExecutingAssembly().Location, attributes);
        }
    }
}
