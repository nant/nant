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
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {

    public class CscTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0'?>
            <project>
                <csc target='exe' output='{0}.exe' {2}>
                    <sources basedir='{1}'>
                        <includes name='{0}'/>
                    </sources>
                </csc>
            </project>";

        const string _sourceCode = @"
            public class HelloWorld { 
                static void Main() { 
                    System.Console.WriteLine(""Hello World using C#""); 
                }
            }";

        string _sourceFileName;

        public CscTaskTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();
			_sourceFileName = Path.Combine(TempDirName, "HelloWorld.cs");
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);
		}

        /// <summary>Test to make sure debug option works.</summary>
        public void Test_DebugBuild() {
            string result = RunBuild(FormatBuildFile("debug='true'"));
            Assert(_sourceFileName + ".exe does not exists, program did compile.", File.Exists(_sourceFileName + ".exe"));
            Assert(_sourceFileName + ".pdb does not exists, program did compile with debug switch.", File.Exists(_sourceFileName + ".pdb"));
        }

        /// <summary>Test to make sure debug option works.</summary>
        public void Test_ReleaseBuild() {
            string result = RunBuild(FormatBuildFile("debug='false'"));
            Assert(_sourceFileName + ".exe does not exists, program did compile.", File.Exists(_sourceFileName + ".exe"));
            Assert(_sourceFileName + ".pdb does exists, program did compiled with debug switch.", !File.Exists(_sourceFileName + ".pdb"));
        }

        private string FormatBuildFile(string attributes) {
            return String.Format(_format, Path.GetFileName(_sourceFileName), Path.GetDirectoryName(_sourceFileName), attributes);
        }
    }
}
