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

    public class TargetTest : BuildTestBase {

        const string _format = @"
            <project default='Target1'>
                <property name='a' value='{0}'/>
                <property name='b' value='{1}'/>

                <target name='Target1' depends='Target2 Target3'>
                    <echo message='Target1 executed'/>
                </target>
                <target name='Target2' if='${{a}}'>
                    <echo message='Target2 executed'/>
                </target>
                <target name='Target3' unless='${{b}}' depends='Target1'> <!-- check for infinite loop -->
                    <echo message='Target3 executed'/>
                </target>
            </project>";

		public TargetTest(String name) : base(name) {
        }

        public void Test_Normal() {
            string result = RunBuild(FormatBuildFile("false", "true"));
            Assert("Target1 should have executed.\n" + result, result.IndexOf("Target1 executed") != -1);
            Assert("Target2 should not have executed.\n" + result, result.IndexOf("Target2 executed") == -1);
            Assert("Target3 should not have executed.\n" + result, result.IndexOf("Target3 executed") == -1);
        }

        public void Test_If() {
            string result = RunBuild(FormatBuildFile("true", "true"));
            Assert("Target1 should have executed.\n" + result, result.IndexOf("Target1 executed") != -1);
            Assert("Target2 should have executed.\n" + result, result.IndexOf("Target2 executed") != -1);
            Assert("Target3 should not have executed.\n" + result, result.IndexOf("Target3 executed") == -1);
        }

        public void Test_Unless() {
            string result = RunBuild(FormatBuildFile("false", "false"));
            Assert("Target1 should have executed.\n" + result, result.IndexOf("Target1 executed") != -1);
            Assert("Target2 should not have executed.\n" + result, result.IndexOf("Target2 executed") == -1);
            Assert("Target3 should have executed.\n" + result, result.IndexOf("Target3 executed") != -1);
        }

        public void Test_Depends() {
            string result = RunBuild(FormatBuildFile("true", "false"));
            Assert("Target1 should have executed.\n" + result, result.IndexOf("Target1 executed") != -1);
            Assert("Target2 should have executed.\n" + result, result.IndexOf("Target2 executed") != -1);
            Assert("Target3 should have executed.\n" + result, result.IndexOf("Target3 executed") != -1);
        }

        private string FormatBuildFile(string a, string b) {
            return String.Format(_format, a, b);
        }
    }
}
