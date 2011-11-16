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
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class SysInfoTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0' ?>
            <project>
                <sysinfo {0} failonerror='false' />
            </project>";

        [Test]
        public void Test_Normal() {
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _format, ""));
            Assert.IsTrue(result.IndexOf("[sysinfo]") != -1, "Task should have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_Verbose() {
            string result = RunBuild(String.Format(CultureInfo.InvariantCulture, _format, "verbose='true'"));
            Assert.IsTrue(result.IndexOf("[sysinfo]") != -1, "Task should have executed." + Environment.NewLine + result);
        }

        [Test]
        public void Test_DuplicateTasks() {
            //
            // ensure we can call sysinfo twice in during a buildfile
            //
            string xml = @"<?xml version='1.0' ?>
            <project>
                <sysinfo failonerror='false' />
                <sysinfo failonerror='false' />
            </project>";

           try {
               RunBuild(xml);
           } catch(BuildException e) {
              Assert.Fail("Duplicate sysinfo tasks should've worked" + Environment.NewLine + e.ToString());
           }
        }

        [Test]
        public void Test_PropertiesNotReadOnly() {
            //
            // ensure properties set by sysinfo task are not readonly
            //
            string xml = @"<?xml version='1.0' ?>
            <project>
                <sysinfo prefix='test' failonerror='false' />
                <property name='test.clr.version' value='44.32.23'/>
                <echo message='test.clr.version = ${test.clr.version}'/>
            </project>";

            string result = RunBuild(xml);
            string expression = @"test.clr.version = 44.32.23";
            Match match = Regex.Match(result, expression);
            Assert.IsTrue(match.Success, "SysInfo property should've been modified!" + Environment.NewLine + result);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_FailOnIllegalPropertyNames() {
            //
            // ensure properties with parentheses will fail like ProgramFiles(x86)
            // SetEnvironmentVariable is first available on .Net 2.0
            //
            string xml = @"<?xml version='1.0' ?>
            <project>
                <sysinfo />
            </project>";
            if (AssignEnvironmentVariable("AnIllegalPropertyWithPa(rens)", "AnIllegalPropertyWithParens")) {
                RunBuild(xml);
            } else {
                throw new TestBuildException();
            }

        }

#if (ONLY_1_0 || ONLY_1_1)
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError=true)]
        static extern bool SetEnvironmentVariable(string lpName, string lpValue);
#endif

        private bool AssignEnvironmentVariable(string propName, string propValue) {

            Environment.SetEnvironmentVariable(propName, propValue);
            return true;
        }

    }
}
