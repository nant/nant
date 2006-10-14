// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.IO;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Functions {
    [TestFixture]
    public class FileFunctionsTest : BuildTestBase {
        #region Public Instance Methods

        [Test]
        public void IsAssembly_OK() {
            string buildFragment =
                "<project>" +
                "   <if test=\"${not file::is-assembly(path::combine(nant::get-base-directory(), 'NAnt.Core.dll'))}\">" +
                "       <fail>#1</fail>" +
                "   </if>" +
                "</project>";

            RunBuild(buildFragment);
        }

        [Test]
        public void IsAssembly_InvalidAssembly() {
            string buildFragment =
                "<project>" +
                "   <echo file=\"assembly.dll\">test</echo>" +
                "   <if test=\"${file::is-assembly('assembly.dll')}\">" +
                "       <fail>#1</fail>" +
                "   </if>" +
                "</project>";

            RunBuild(buildFragment);
        }

        [Test]
        public void IsAssembly_NoAssembly() {
            string buildFragment1 =
                "<project>" +
                "   <echo file=\"noassembly1.msi\">test</echo>" +
                "   <if test=\"${file::is-assembly('noassembly1.msi')}\">" +
                "       <fail>#1</fail>" +
                "   </if>" +
                "</project>";

            RunBuild(buildFragment1);

            string buildFragment2 =
                "<project>" +
                "   <if test=\"${file::is-assembly('noassembly1.msi')}\">" +
                "       <fail>#1</fail>" +
                "   </if>" +
                "</project>";

            RunBuild(buildFragment2);
        }

        [Test]
        public void IsAssembly_AssemblyDoesNotExist() {
            string buildFragment =
                "<project>" +
                "   <if test=\"${file::is-assembly('doesnotexist.dll')}\">" +
                "       <fail>#1</fail>" +
                "   </if>" +
                "</project>";

            try {
                RunBuild(buildFragment);
                Assert.Fail ("#1");
            } catch (TestBuildException ex) {
                Assert.IsNotNull (ex.InnerException, "#2");
                Assert.AreEqual (typeof (BuildException), ex.InnerException.GetType(), "#3");
                Assert.IsNotNull (ex.InnerException.InnerException, "#4");
                Assert.AreEqual (typeof (FileNotFoundException), ex.InnerException.InnerException.GetType(), "#5");
            }
        }

        #endregion Public Instance Methods
    }
}
