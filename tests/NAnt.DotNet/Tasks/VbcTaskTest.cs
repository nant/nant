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
using NAnt.DotNet.Tasks;
using NUnit.Framework;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class VbcTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string _sourceFileName;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _format = @"<?xml version='1.0'?>
            <project>
                <vbc target='exe' output='{0}.exe' {2}>
                    <sources basedir='{1}'>
                        <include name='{0}'/>
                    </sources>
                </vbc>
            </project>";

        private const string _sourceCode = @"
            imports System

            public class MainApp
                shared sub Main()
                    Console.WriteLine(""Hello World using VB.NET"")
                        return
                    end sub
            end class";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _sourceFileName = Path.Combine(TempDirName, "HelloWorld.vb");
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
            Assertion.Assert(_sourceFileName + ".exe does not exists, program did compile.", File.Exists(_sourceFileName + ".exe"));
            // Comment this for now as its hard to know which framework was used to compile and it was mono there will be no pdb file.
            //Assertion.Assert(_sourceFileName + ".pdb does not exists, program did compile with debug switch.", File.Exists(_sourceFileName + ".pdb"));
        }

        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        public void Test_ReleaseBuild() {   
            string result = RunBuild(FormatBuildFile("debug='false'"));
            Assertion.Assert(_sourceFileName + ".exe does not exists, program did compile.", File.Exists(_sourceFileName + ".exe"));
            Assertion.Assert(_sourceFileName + ".pdb does exists, program did compiled with debug switch.", !File.Exists(_sourceFileName + ".pdb"));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string attributes) {
            return string.Format(CultureInfo.InvariantCulture, _format, 
                Path.GetFileName(_sourceFileName), 
                Path.GetDirectoryName(_sourceFileName), 
                attributes);
        }

        #endregion Private Instance Methods
        /// <summary>
        /// Unit tests for FileParser
        /// </summary>
        [TestFixture]
            public class TestResourceLinkage 
        {
            /// <summary>
            /// Uses a representative sampling of classname inputs to verify that the classname line can be found
            /// </summary>
            [Test]
            public void TestFindClassname() 
            {
                // Positive test cases - classname should be found
                VerifyFindClassname( "Public Abstract Class CompilerBase\r\n{} \r\n}", "CompilerBase" );
                VerifyFindClassname( "Public Abstract Class Conference \r\n{}", "Conference" );              
        
                // Negative test cases - no classname should be found
                VerifyFindClassname( "' this is some Class here\r\n", "" );           
            }
                
            /// <summary>
            /// Parses the input, ensuring the class name is found
            /// </summary>
            public void VerifyFindClassname( string input, string expectedClassname ) {
                VbcTask vbTask = new VbcTask();
                StringReader reader = new StringReader( input );
                CompilerBase.ResourceLinkage linkage = vbTask.PerformSearchForResourceLinkage( reader );
                
                Assertion.AssertNotNull("no resourcelinkage found for " + input, linkage);
                string message = string.Format( "Failed to find expected class name {0}. Found {1} instead.", linkage.ClassName, expectedClassname ); 
                Assertion.Assert( message, (expectedClassname == linkage.ClassName ) );
            }
        }
    }
}
