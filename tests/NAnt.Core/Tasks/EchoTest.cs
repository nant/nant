// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez (ScottHernandez@hotmail.com)
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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the Echo task.
    /// </summary>
    [TestFixture]
    public class EchoTest : BuildTestBase {
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }
        
        [Test]
        public void Test_EchoDefaultProjectInfo() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Echo message missing:" + result, result.IndexOf("Go Away!") != -1);
        }

        [Test]
        public void Test_EchoDebugProjectInfo() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Debug' />
                    </project>";
            string result = RunBuild(_xml, Level.Info);
            Assertion.Assert("Debug echo should not be output when Project level is Info.", result.IndexOf("Go Away!") == -1);
        }

        [Test]
        public void Test_EchoWarningProjectInfo() {
            string _xml = @"
                    <project>
                        <echo level='Warning'>Go Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Info);
            Assertion.Assert("Warning echo should be output when Project level is Info.", result.IndexOf("Go Away!") != -1);
        }

        [Test]
        public void Test_EchoWarningProjectError() {
            string _xml = @"
                    <project>
                        <echo level='Warning'>Go Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Error);
            Assertion.Assert("Warning echo should not be output when Project level is Error.", result.IndexOf("Go Away!") == -1);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EchoInvalidLevel() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Invalid' />
                    </project>";
            string result = RunBuild(_xml, Level.Error);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_EchoMessageAndInlineContent() {
            string _xml = @"
                    <project>
                        <echo message='Go Away!' level='Debug'>Go Away!</echo>
                    </project>";
            string result = RunBuild(_xml, Level.Info);
        }
    }
}
