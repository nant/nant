// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Types;

namespace Tests.NAnt.Core {

    [TestFixture]
    public class TaskContainerTest : BuildTestBase {
        #region Private Static Fields

        private const string _format1 = @"<?xml version='1.0' ?>
           <project name='testing' default='test'>
                <target name='test'>
                    <if test='{0}'>
                        <fileset id='foo'>
                            <include name='bar' />
                        </fileset>
                    </if>
                </target>
            </project>";
        private const string _format2 = @"<?xml version='1.0' ?>
           <project name='testing' default='test'>
                <target name='test'>
                    <fileset id='foo'>
                        <include name='abc' />
                    </fileset>
                    <if test='{0}'>
                        <fileset id='foo'>
                            <include name='bar' />
                        </fileset>
                    </if>
                </target>
            </project>";
        private const string _format3 = @"<?xml version='1.0' ?>
           <project name='testing' default='test'>
                <target name='test'>
                    <if test='{0}'>
                        <foo />
                    </if>
                </target>
            </project>";

        #endregion Private Static Fields

        #region Public Instance Methods
        
        [Test]
        public void Test_IfFilesetDefine1() {
            string buildxml = FormatBuildFile(_format1,"${1==1}");
            Project project = CreateFilebasedProject(buildxml, Level.Info);
            ExecuteProject(project);
            DataTypeBase foo = project.DataTypeReferences["foo"];
            Assert.IsNotNull(foo);
            Assert.IsTrue(foo is FileSet);

            FileSet fs = (FileSet)foo;
            Assert.AreEqual(1,fs.Includes.Count);
            Assert.AreEqual("bar",fs.Includes[0]);
        }

        [Test]
        public void Test_IfFilesetDefine2() {
            string buildxml = FormatBuildFile(_format1,"${1==2}");
            Project project = CreateFilebasedProject(buildxml, Level.Info);
            ExecuteProject(project);
            DataTypeBase foo = project.DataTypeReferences["foo"];
            Assert.IsNull(foo);
        }

        [Test]
        public void Test_IfFilesetRedefine1() {
            string buildxml = FormatBuildFile(_format2,"${1==1}");
            Project project = CreateFilebasedProject(buildxml, Level.Info);
            ExecuteProject(project);
            DataTypeBase foo = project.DataTypeReferences["foo"];
            Assert.IsNotNull(foo);
            Assert.IsTrue(foo is FileSet);

            FileSet fs = (FileSet)foo;
            Assert.AreEqual(1,fs.Includes.Count);
            Assert.AreEqual("bar",fs.Includes[0]);
        }

        [Test]
        public void Test_IfFilesetRedefine2() {
            string buildxml = FormatBuildFile(_format2,"${1==2}");
            Project project = CreateFilebasedProject(buildxml, Level.Info);
            ExecuteProject(project);
            DataTypeBase foo = project.DataTypeReferences["foo"];
            Assert.IsNotNull(foo);
            Assert.IsTrue(foo is FileSet);

            FileSet fs = (FileSet)foo;
            Assert.AreEqual(1,fs.Includes.Count);
            Assert.AreEqual("abc",fs.Includes[0]);
        }

        [Test]
        public void Test_IfUnknownNode1() {
            string buildxml = FormatBuildFile(_format3,"${1==2}");
            RunBuild(buildxml);
        }

        [Test]
        public void Test_IfUnknownNode2() {
            string buildxml = FormatBuildFile(_format3,"${1==1}");

            try {
                RunBuild(buildxml);
                Assert.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                Assert.IsNotNull(ex);
                Assert.IsTrue(ex.InnerException is BuildException);
                BuildException be = (BuildException) ex.InnerException;
                Assert.AreEqual(5, be.Location.LineNumber);
                Assert.IsTrue(be.RawMessage.IndexOf("foo") != -1);
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string fmt,params object[] pars) {
            return string.Format(CultureInfo.InvariantCulture, fmt, pars);
        }

        #endregion Private Instance Methods
    }
}
