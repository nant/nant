// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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

// Michael Aird (mike@airdian.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class StyleTaskTest : BuildTestBase {

        const string _xmlSrcFile = @"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?> 
            <catalog>
                <cd>
                    <title>Empire Burlesque</title> 
                    <artist>Bob Dylan</artist> 
                </cd>
                <cd>
                    <title>Hide your heart</title> 
                    <artist>Bonnie Tyler</artist> 
                </cd>
            </catalog>";

        const string _xslSrcFile = @"<?xml version=""1.0"" encoding=""ISO-8859-1""?>
            <xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
            
            <xsl:template match=""/"">
            <xsl:param name=""param1"" />
                <html>
                <body>
                    <table border=""1"">
                        <tr>
                            <th>Title</th>
                            <th>Artist</th>
                        </tr>
                    <xsl:for-each select=""catalog/cd"">
                    <tr>
                        <td><xsl:value-of select=""title""/></td>
                        <td><xsl:value-of select=""artist""/></td>
                    </tr>
                    </xsl:for-each>
                    </table>
                </body>
                </html>
            </xsl:template>
        
            </xsl:stylesheet>";

        string _xmlSrcFileName="source";
        string _xmlSrcFile2Name="source2";
        string _xslSrcFileName="transform";

        string _xmlSrcFileExtension="xml";
        string _xslSrcFileExtension="xsl";
        string _outputFileExtension="html";

        string _xmlSrcFileNameFull;
        string _xmlSrcFile2NameFull;
        string _xslSrcFileNameFull;

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _xmlSrcFileNameFull = Path.Combine(TempDirName, _xmlSrcFileName + "." + _xmlSrcFileExtension);
            TempFile.CreateWithContents(_xmlSrcFile, _xmlSrcFileNameFull);
            _xmlSrcFile2NameFull = Path.Combine(TempDirName, _xmlSrcFile2Name + "." + _xmlSrcFileExtension);
            TempFile.CreateWithContents(_xmlSrcFile, _xmlSrcFile2NameFull);
            _xslSrcFileNameFull = Path.Combine(TempDirName,  _xslSrcFileName + "." + _xslSrcFileExtension);
            TempFile.CreateWithContents(_xslSrcFile, _xslSrcFileNameFull);
        }

        [Test]
        public void Test_Simple() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";

            string result = null;
            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, outputFN));

            Assertion.Assert("Output file not created.",File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0);
        }

        [Test]
        public void Test_Param() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}'>
                        <parameters>
                            <parameter name='param1' namespaceuri='' value='test' />
                        </parameters>
                    </style>
                </project>";

            string result = null;
            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, outputFN));

            Assertion.Assert("Output file not created.",File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0);
        }

        [Test]
        public void Test_Extension() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' extension='{2}' />
                </project>";

            string result = null;
            result = RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, _outputFileExtension));

            Assertion.Assert("Output file not created.",File.Exists(Path.Combine(TempDirName, _xmlSrcFileName + "." +
                                                                                        _outputFileExtension)));
        }

        [Test]
        public void Test_Infiles() {
            string _xml = @"
                <project>
                    <style style='{0}' extension='{1}'>
                        <infiles>
                            <includes name='{2}' />
                            <includes name='{3}' />
                        </infiles>
                    </style>
                </project>";

            string result = null;
            result = RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _outputFileExtension,_xmlSrcFileNameFull,
                _xmlSrcFile2NameFull ));

            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);
            Assertion.Assert("Output file not created.",File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_NoSrcfiles() {
            string _xml = @"
                <project>
                    <style style='{0}' />
                </project>";

            string result = null;
            result = RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileName));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_OutWithInfiles() {
            string _xml = @"
                <project>
                    <style style='{0}' out='{2}'>
                        <infiles>
                            <includes name='{1}' />
                        </infiles>
                    </style>
                </project>";

            string result = null;
            
            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_XslFileMissing() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";

            string result = null;
            
            File.Delete(_xslSrcFileNameFull);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_SourceFileMissing() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";

            string result = null;
            
            File.Delete(_xmlSrcFileNameFull);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }
    }
}