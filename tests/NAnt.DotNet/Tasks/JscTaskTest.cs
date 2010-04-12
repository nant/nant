// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System.Globalization;
using System.IO;

using NUnit.Framework;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class JscTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string _sourceFileName;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _format = @"<?xml version='1.0'?>
            <project>
                <jsc target='exe' output='{0}.exe' {1}>
                    <sources basedir='{2}'>
                        <include name='{3}'/>
                    </sources>
                    <resources basedir='{2}'>
                        <include name='**/*.resx' />
                    </resources>
                </jsc>
            </project>";

        private const string _sourceCode = @"print(""Hello World using JScript.NET"");";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _sourceFileName = Path.Combine(TempDirName, "HelloWorld.js");
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        public void Test_DebugBuild() {
            string result = RunBuild(FormatBuildFile("debug='true'"));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");
            Assert.IsTrue(File.Exists(_sourceFileName + ".pdb"), _sourceFileName + ".pdb does not exists, program did compile with debug switch.");
        }

        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        public void Test_ReleaseBuild() {
            string result = RunBuild(FormatBuildFile("debug='false'"));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");
            Assert.IsFalse(File.Exists(_sourceFileName + ".pdb"), _sourceFileName + ".pdb does exists, program did compiled with debug switch.");
        }

        /// <summary>
        /// Test to make sure output can be created, even if the path does not exist yet.
        /// </summary>
        [Test] 
        public void Test_CreateParentDirectory() {
            _sourceFileName = Path.Combine(TempDirName, 
                Path.Combine("bin", "HelloWorld.js"));
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);            

            RunBuild(FormatBuildFile(
                Path.Combine("bin", "HelloWorld.js"), null, null, null));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");            // Comment this for now as its hard to know which framework was used to compile and it was mono there will be no pdb file.
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile (string attributes)
        {
            return FormatBuildFile(
                null,
                attributes,
                null,
                null);
        }

        private string FormatBuildFile(
            string output, 
            string attributes, 
            string basedir,
            string includefiles) {
            return string.Format(CultureInfo.InvariantCulture, _format, 
                output       != null ? output : Path.GetFileName(_sourceFileName), 
                attributes   != null ? attributes : "",
                basedir      != null ? basedir : Path.GetDirectoryName(_sourceFileName), 
                includefiles != null ? includefiles : Path.GetFileName(_sourceFileName));
        }
        #endregion Private Instance Methods
    }
}
