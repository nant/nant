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
//
// Bernard Vander Beken

using System;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {

    [TestFixture]
    public class SleepTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0' ?>
            <project>
                <sleep {0}/>
            </project>";

        [Test]
        public void Test_Normal() {
            string result = RunBuild(String.Format(_format, " milliseconds='20'"));
            Assertion.Assert("Task should have executed." + Environment.NewLine + result, result.IndexOf("[sleep]") != -1);
        }

        [Test]
        public void Test_NoDurationSpecified() {
            string result = RunBuild(String.Format(_format, ""));
            Assertion.Assert("Task should have executed." + Environment.NewLine + result, result.IndexOf("[sleep]") != -1);
        }

        [Test]
        public void Test_NegativeDurationFails() {
            try {
                RunBuild(String.Format(_format, " milliseconds='-1'"));
                Assertion.Fail("A BuildException must be thrown for negative durations.");
            } catch (TestBuildException) {
                // This is expected.
            }
        }

        [Test]
        public void Test_NegativePartialDurationFails() {
            try {
                RunBuild(String.Format(_format, " seconds='1' milliseconds='-1'"));
                Assertion.Fail("A BuildException must be thrown for negative partial durations.");
            } catch (TestBuildException) {
                // This is expected.
            }
        }
    }
}
