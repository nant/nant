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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    [TestFixture]		
    public class TargetTest : BuildTestBase {
        #region Private Static Fields

        private const string BuildFragment = @"
            <project default='{0}'>
                <property name='a' value='{1}'/>
                <property name='b' value='{2}'/>

                <target name='Target1' depends='Target2 Target3'>
                    <echo message='Target1 executed'/>
                </target>
                <target name='Target2' if='${{a}}'>
                    <echo message='Target2 executed'/>
                </target>
                <target name='Target3' unless='${{b}}' depends='{3}'>
                    <echo message='Target3 executed'/>
                </target>
            </project>";

        #endregion Private Static Fields

        #region Public Instance Methods

        [Test]
        public void Test_Normal() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "false", "true", string.Empty), listener);

            Assertion.Assert("Target1 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target1") == 1);
            Assertion.Assert("Target2 should not have executed.\n" + result, listener.GetTargetExecutionCount("Target2") == 0);
            Assertion.Assert("Target3 should not have executed.\n" + result, listener.GetTargetExecutionCount("Target3") == 0);
        }

        [Test]
        public void Test_If() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "true", "true", string.Empty), listener);

            Assertion.Assert("Target1 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target1") == 1);
            Assertion.Assert("Target2 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target2") == 1);
            Assertion.Assert("Target3 should not have executed.\n" + result, listener.GetTargetExecutionCount("Target3") == 0);
        }

        [Test]
        public void Test_Unless() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "false", "false", string.Empty), listener);

            Assertion.Assert("Target1 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target1") == 1);
            Assertion.Assert("Target2 should not have executed.\n" + result, listener.GetTargetExecutionCount("Target2") == 0);
            Assertion.Assert("Target3 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target3") == 1);
        }

        [Test]
        public void Test_Depends() {
            // create new listener that allows us to track build events
            TestBuildListener listener = new TestBuildListener();

            // run the build
            string result = RunBuild(FormatBuildFile("Target1", "true", "false", string.Empty), listener);

            Assertion.Assert("Target1 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target1") == 1);
            Assertion.Assert("Target2 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target2") == 1);
            Assertion.Assert("Target3 should have executed once.\n" + result, listener.GetTargetExecutionCount("Target3") == 1);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_CircularDependency() {
            // run the build with Target1 dependent on Target3 and vice versa
            string result = RunBuild(FormatBuildFile("Target1", "true", "false", "Target1"));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_UnknowDependentTarget() {
            // run the build with an unknown dependent target
            string result = RunBuild(FormatBuildFile("Target1", "true", "false", "Unknown"));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_UnknowDefaultTarget() {
            // run the build with an unknown default target
            string result = RunBuild(FormatBuildFile("Unknown", "true", "false", string.Empty));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string defaultTarget, string a, string b, string target3Depends) {
            return string.Format(CultureInfo.InvariantCulture, BuildFragment, 
                defaultTarget, a, b, target3Depends);
        }

        #endregion Private Instance Methods

        private class TestBuildListener : IBuildListener {
            #region Public Instance Constructors
        
            public TestBuildListener() {
                _executedTargets = new Hashtable();
                _executedTasks = new Hashtable();
            }

            #endregion Public Instance Constructors

            #region Implementation of IBuildListener

            public void BuildStarted(object sender, BuildEventArgs e) {
            }

            public void BuildFinished(object sender, BuildEventArgs e) {
            }

            public void TargetStarted(object sender, BuildEventArgs e) {
                if (e.Target != null) {
                    if (_executedTargets.ContainsKey(e.Target.Name)) {
                        _executedTargets[e.Target.Name] = ((int) _executedTargets[e.Target.Name]) + 1;
                    } else {
                        _executedTargets.Add(e.Target.Name, 1);
                    }
                }
            }

            public void TargetFinished(object sender, BuildEventArgs e) {
            }

            public void MessageLogged(object sender, BuildEventArgs e) {
            }

            public void TaskStarted(object sender, BuildEventArgs e) {
                if (e.Task != null) {
                    if (_executedTasks.ContainsKey(e.Task.Name)) {
                        _executedTasks[e.Task.Name] = ((int) _executedTasks[e.Task.Name]) + 1;
                    } else {
                        _executedTasks.Add(e.Task.Name, 1);
                    }
                }
            }

            public void TaskFinished(object sender, BuildEventArgs e) {
            }

            #endregion Implementation of IBuildListener

            #region Public Instance Methods

            public int GetTargetExecutionCount(string target) {
                if (_executedTargets.ContainsKey(target)) {
                    return (int) _executedTargets[target];
                } else {
                    return 0;
                }
            }

            public int GetTaskExecutionCount(string task) {
                if (_executedTasks.ContainsKey(task)) {
                    return (int) _executedTasks[task];
                } else {
                    return 0;
                }
            }

            #endregion Public Instance Methods

            #region Private Instance Fields

            private Hashtable _executedTargets;
            private Hashtable _executedTasks;

            #endregion Private Instance Fields
        }
    }
}
