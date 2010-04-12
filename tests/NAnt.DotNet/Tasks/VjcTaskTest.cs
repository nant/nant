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
// Ian MacLean ( ian_maclean@another.com )

using System.Globalization;
using System.IO;

using NUnit.Framework;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;
using NAnt.DotNet.Tasks;

namespace Tests.NAnt.DotNet.Tasks 
{
    [TestFixture]
    public class VjcTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string _sourceFileName;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _format = @"<?xml version='1.0'?>
            <project>
                <vjc target='exe' output='{0}.exe' {1}>
                    <sources basedir='{2}'>
                        <includes name='{3}'/>
                    </sources>
                </vjc>
            </project>";

        private const string _sourceCode = @"
            public class HelloWorld { 
                 public static void main(String[] args) { 
                    System.Console.WriteLine(""Hello World using J#""); 
                }
            }";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _sourceFileName = Path.Combine(TempDirName, "HelloWorld.jsl");
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
            Assertion.Assert(_sourceFileName + ".pdb does not exists, program did compile with debug switch.", File.Exists(_sourceFileName + ".pdb"));
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

        /// <summary>
        /// Test to make sure output can be created, even if the path does not exist yet.
        /// </summary>		
        [Test]
        public void Test_CreateParentDirectory() {
            _sourceFileName = Path.Combine(TempDirName, 
                Path.Combine("bin", "HelloWorld.jsl"));
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);            

            RunBuild(FormatBuildFile(
                Path.Combine("bin", "HelloWorld.jsl"), null, null, null));
            Assertion.Assert(_sourceFileName + ".exe does not exists, program did compile.", File.Exists(_sourceFileName + ".exe"));
        }
         
        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string attributes) {
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
        
        /// <summary>
        /// Unit tests for ResourceLinkage
        /// </summary>
        [TestFixture]
        public class TestResourceLinkage {
            /// <summary>
            /// Uses a representative sampling of classname inputs to verify that the classname line can be found
            /// </summary>
            [Test]
            public void TestFindClassname()  {
                // Positive test cases - classname should be found
                VerifyFindClassname( "public abstract class CompilerBase\r\n{} \r\n}", "CompilerBase" );
                VerifyFindClassname( "public abstract class Conference \r\n{}", "Conference" );
                VerifyFindClassname( "public class AssemblyAttributeEnumerator implements IEnumerator {\r\n", "AssemblyAttributeEnumerator" );
                VerifyFindClassname( "private class FolderCollection implements IFolderCollection\r\n{}", "FolderCollection" );
                VerifyFindClassname( "class InstallTool\r\n{}", "InstallTool" );
                VerifyFindClassname( " abstract class FSObject\r\n{}", "FSObject" );
                VerifyFindClassname( "private class Enumerator implements IEnumerator, ILevelCollectionEnumerator\r\n{}", "Enumerator" );
                VerifyFindClassname( "private class Enumerator implements IEnumerator, ILevelCollectionEnumerator\r\n{}", "Enumerator" );
                VerifyFindClassname( "private class Enumerator implements IEnumerator, ILevelCollectionEnumerator\r\n{}", "Enumerator" );
                VerifyFindClassname( "public class FrameworkInfoDictionary implements IDictionary, ICollection, IEnumerable, ICloneable {\r\n}", "FrameworkInfoDictionary" );
                VerifyFindClassname( "\tclass InstallTool\r\n{}", "InstallTool" );
                VerifyFindClassname( " class InstallTool\r\n{}", "InstallTool" );
                VerifyFindClassname( " abstract class FSObject\r\n{}", "FSObject" );
        
                // Negative test cases - no classname should be found
                VerifyFindClassname( "// this is some class here\r\n", "" );
                //VerifyFindClassname( "/* this is some class here\r\n", null );
            }
                
            /// <summary>
            /// Parses the input, ensuring the class name is found
            /// </summary>
            public void VerifyFindClassname( string input, string expectedClassname )   {
                VjcTask vjcTask = new VjcTask();
                StringReader reader = new StringReader( input );
                CompilerBase.ResourceLinkage linkage = vjcTask.PerformSearchForResourceLinkage( reader );
                
                Assertion.AssertNotNull("no resourcelinkage found for " + input, linkage);
                string message = string.Format( "Failed to find expected class name {0}. Found {1} instead.", linkage.ClassName, expectedClassname ); 
                Assertion.Assert( message, (expectedClassname == linkage.ClassName ) );
            }
        }
    }
}
