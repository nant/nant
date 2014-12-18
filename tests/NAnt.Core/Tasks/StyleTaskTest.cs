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
using System.Text;
using System.Xml;
using System.Globalization;
using NAnt.Core;
using NUnit.Framework;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class StyleTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string _projectXml = @"<?xml version='1.0'?>
                <project>
                    <style in='{0}' out='{1}' style='{2}' />
                </project>";

        private string _xmlSrcFileName = "source";
        private string _xmlSrcFile2Name = "source2";
        private string _xmlSingletonFileName = "singleton";
        private string _xslSrcFileName = "transform";
        private string _xslPassthroughSrcFileName = "passthrough";

        private string _xmlSrcFileExtension = "xml";
        private string _xslSrcFileExtension = "xsl";
        private string _outputFileExtension = "html";

        private string _xmlSrcFileNameFull;
        private string _xmlSrcFile2NameFull;
        private string _xmlSingletonSrcFileNameFull;
        private string _xslSrcFileNameFull;
        private string _xslPassthroughSrcFileNameFull;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _xmlSrcFile = @"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?> 
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

        private const string _xslSrcFile = 
        @"<?xml version=""1.0"" encoding=""ISO-8859-1""?>
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

        /// <summary>
        /// A simple xml input file that includes a singleton tag for passthrough testing (see XSL below)
        /// </summary>
        private const string _xmlSingletonSrcFile = 
        @"<roleManager defaultProvider=""sampleprovider"">
            <providers>
                <clear />
                <add name=""samplename"" type=""sampletype"" />
            </providers>
        </roleManager>";

        /// <summary>
        /// A passthrough transform, similar to those used for config file transforms (e.g., XDT)
        /// This is to make sure that MS-sensitive singleton elements are properly maintained during passthrough
        /// </summary>
        private const string _xslPassthroughSrcFile = @"
        <xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
            <xsl:output method=""xml"" indent=""yes"" encoding=""UTF-8""/>
            <!-- copy entire source -->
            <xsl:template match=""@* | node()"">
                <xsl:copy>
                    <xsl:apply-templates select=""@* | node()""/>
                </xsl:copy> 
            </xsl:template>
            <xsl:template match=""roleManager/providers/add[@name='samplename']/@type"">
                <xsl:attribute name=""type"">xslttype</xsl:attribute>
            </xsl:template>
        </xsl:stylesheet>";
        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _xmlSrcFileNameFull = Path.Combine(TempDirName, _xmlSrcFileName + "." + _xmlSrcFileExtension);
            TempFile.CreateWithContents(_xmlSrcFile, _xmlSrcFileNameFull);
            _xmlSrcFile2NameFull = Path.Combine(TempDirName, _xmlSrcFile2Name + "." + _xmlSrcFileExtension);
            TempFile.CreateWithContents(_xmlSrcFile, _xmlSrcFile2NameFull);
            _xmlSingletonSrcFileNameFull = Path.Combine(TempDirName, _xmlSingletonFileName + "." + _xmlSrcFileExtension);
            TempFile.CreateWithContents(_xmlSingletonSrcFile, _xmlSingletonSrcFileNameFull);
            _xslSrcFileNameFull = Path.Combine(TempDirName, _xslSrcFileName + "." + _xslSrcFileExtension);
            TempFile.CreateWithContents(_xslSrcFile, _xslSrcFileNameFull);
            _xslPassthroughSrcFileNameFull = Path.Combine(TempDirName, _xslPassthroughSrcFileName + "." + _xslSrcFileExtension);
            TempFile.CreateWithContents(_xslPassthroughSrcFile, _xslPassthroughSrcFileNameFull);
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        [Test]
        public void Test_Simple() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";
            
            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);

            RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, outputFN));

            Assert.IsTrue(File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0, "Output file not created.");
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
            
            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);

            RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, outputFN));

            Assert.IsTrue(File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0, "Output file not created.");
        }

        [Test]
        public void Test_Extension() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' extension='{2}' />
                </project>";
           
            RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, _outputFileExtension));

            Assert.IsTrue(File.Exists(Path.Combine(TempDirName, _xmlSrcFileName + "." +
                _outputFileExtension)), "Output file not created.");
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
            
            RunBuild(String.Format(CultureInfo.InvariantCulture,
                _xml, _xslSrcFileNameFull, _outputFileExtension,_xmlSrcFileNameFull,
                _xmlSrcFile2NameFull ));

            string outputFN = Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension);
            Assert.IsTrue(File.Exists(outputFN) && (new FileInfo(outputFN)).Length > 0, "Output file not created.");
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_NoSrcfiles() {
            string _xml = @"
                <project>
                    <style style='{0}' />
                </project>";
            
            RunBuild(String.Format(CultureInfo.InvariantCulture,
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
           
            RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_XslFileMissing() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";
           
            
            File.Delete(_xslSrcFileNameFull);
            RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_SourceFileMissing() {
            string _xml = @"
                <project>
                    <style style='{0}' in='{1}' out='{2}' />
                </project>";
                       
            File.Delete(_xmlSrcFileNameFull);

            RunBuild(String.Format(CultureInfo.InvariantCulture, _xml, _xslSrcFileNameFull, _xmlSrcFileNameFull, Path.Combine(TempDirName, _xmlSrcFileName + "." + _outputFileExtension)));
        }

        /// <summary>
        /// Ensures paths specifies as argument to document() function are 
        /// resolved relative to the stylesheet directory.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   A path specified in the document() function is only resolved when
        ///   the transformation is performed, not when loading the XSL
        ///   transform.
        ///   </para>
        ///   <para>
        ///   Failure to find the file specified as argument to the document()
        ///   function will not result in a failure, so we need to verify the
        ///   content of the transformed XML file.
        ///   </para>
        /// </remarks>
        [Test]
        public void Test_DocumentFunction() {
            string tempDir1 = CreateTempDir("dir1");
            string tempDir1SubDir1 = Path.Combine(tempDir1, "subdir1");
            Directory.CreateDirectory(tempDir1SubDir1);
            string inputXmlFile = Path.Combine(tempDir1SubDir1, "input.xml");
            string xslFile = Path.Combine(tempDir1, "style.xsl");
            string commonXmlFile = Path.Combine(tempDir1, "common.xml");

            // create input xml file
            XmlWriter writer = new XmlTextWriter(inputXmlFile, Encoding.UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("root");
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            // create XSL file
            writer = new XmlTextWriter(xslFile, Encoding .UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("xsl", "stylesheet", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteStartElement("xsl", "variable", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("name", "common");
            writer.WriteAttributeString("select", "document('common.xml')");
            writer.WriteEndElement();
            writer.WriteStartElement("xsl", "template", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("match", "/");
            writer.WriteStartElement("a");
            writer.WriteStartElement("xsl", "value-of", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("select", "$common/root/data");
            writer.WriteEndElement();
            // end xsl:template elemet
            writer.WriteEndElement();
            // end xsl:stylesheet element
            writer.WriteEndElement();
            writer.Close();

            // create common xml file
            writer = new XmlTextWriter(commonXmlFile, Encoding.UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("root");
            writer.WriteElementString("data", "xxx");
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            // determine filename for output file
            string outputFile = Path.Combine(TempDirName, "out.xml");

            // execute the transformation
            RunBuild(string.Format(CultureInfo.InvariantCulture, _projectXml,
                inputXmlFile, outputFile, xslFile));

            // ensure output file was created
            Assert.IsTrue(File.Exists(outputFile), "Output file \"{0}\" was not created.", outputFile);

            // ensure output file contains expected content
            using (FileStream fs = File.OpenRead(outputFile)) {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fs);
                XmlNode textNode = xmlDoc.SelectSingleNode("//a/text()");
                Assert.IsNotNull(textNode, "XPath expression \"//a/text()\" did not result in a matching node: " + xmlDoc.OuterXml);
                Assert.AreEqual("xxx", textNode.Value);
            }
        }

        /// <summary>
        /// Ensures includes are resolved relative to the directory of the 
        /// containing stylesheet.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   A path specified using the &lt;xsl:include&gt; element is resolved
        ///   when the transform is loaded.
        ///   </para>
        ///   <para>
        ///   Failure to find the included file will result in a failure, but
        ///   we still verify the expected result.
        ///   </para>
        /// </remarks>
        [Test]
        public void Test_Include() {
            string styleDir = CreateTempDir("style");
            string styleIncludeDir = Path.Combine(styleDir, "include");
            Directory.CreateDirectory(styleIncludeDir);
            string inputXmlFile = Path.Combine(TempDirName, "input.xml");
            string mainXslFile = Path.Combine(styleDir, "style.xsl");
            string includeXslFile = Path.Combine(styleIncludeDir, "include.xsl");

            // create input xml file
            XmlWriter writer = new XmlTextWriter(inputXmlFile, Encoding.UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("test");
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            // create main XSL file
            writer = new XmlTextWriter(mainXslFile, Encoding .UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("xsl", "stylesheet", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteStartElement("xsl", "include", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("href", "include/include.xsl");
            writer.WriteEndElement();
            writer.WriteStartElement("xsl", "template", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("match", "test");
            writer.WriteStartElement("xsl", "call-template", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("name", "included");
            writer.WriteEndElement();
            // end xsl:template elemet
            writer.WriteEndElement();
            // end xsl:stylesheet element
            writer.WriteEndElement();
            writer.Close();

            // create include XSL file
            writer = new XmlTextWriter(includeXslFile, Encoding .UTF8);
            writer.WriteStartDocument(false);
            writer.WriteStartElement("xsl", "stylesheet", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteStartElement("xsl", "template", "http://www.w3.org/1999/XSL/Transform");
            writer.WriteAttributeString("name", "included");
            writer.WriteElementString("text", "This is written by included template.");
            // end xsl:template elemet
            writer.WriteEndElement();
            // end xsl:stylesheet element
            writer.WriteEndElement();
            writer.Close();

            // determine filename for output file
            string outputFile = Path.Combine(TempDirName, "out.xml");

            // execute the transformation
            RunBuild(string.Format(CultureInfo.InvariantCulture, _projectXml,
                inputXmlFile, outputFile, mainXslFile));

            // ensure output file was created
            Assert.IsTrue(File.Exists(outputFile), "Output file \"{0}\" was not created.", outputFile);

            // ensure output file contains expected content
            using (FileStream fs = File.OpenRead(outputFile)) {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fs);
                XmlNode textNode = xmlDoc.SelectSingleNode("//text/text()");
                Assert.IsNotNull(textNode, "XPath expression \"//text/text()\" did not result in a matching node: " + xmlDoc.OuterXml);
                Assert.AreEqual("This is written by included template.", textNode.Value);
            }
        }

        [Test]
        public void TransformEngineTests() {
            // Mono apparently still outputs "<clear></clear> regardless if
            // XslCompiledTransform is used or not.
            if (PlatformHelper.IsMono) 
            {
                Assert.Ignore("Mono's XslCompiledTransform behaves the same way as XslTransform when handling start/end xml pairs");
            }
            
            // Old engine (XslTransform) would inappropriately convert singleton tags to start/end pairs.
            // This would cause errors with sensitive readers like ConfigurationManager.AppSettings
            // With new engine (XslCompiledTransform), singleton tag is preserved
            // See Github Issue 17 for details (https://github.com/nant/nant/issues/17)
            string expected = @"<clear />";

            string outputFile = Path.Combine(TempDirName, string.Format(@"{0}.{1}", _xmlSingletonFileName, _outputFileExtension));
            RunBuild(String.Format(CultureInfo.InvariantCulture, _projectXml, _xmlSingletonSrcFileNameFull, outputFile, _xslPassthroughSrcFileNameFull));

            // ensure output file contains expected content
            using (StreamReader sr = new StreamReader(File.OpenRead(outputFile))) {
                string result = sr.ReadToEnd();
                string msg = string.Format(@"Output file {0} must contain '{1}', contents: {2}", outputFile, expected, result);
                Assert.IsTrue(result.Contains(expected), msg);
            }
        }

        #endregion Public Instance Methods

    }
}