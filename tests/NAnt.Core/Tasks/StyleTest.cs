// NAnt - A .NET build tool
// Copyright (C) 2004 Gert Driesen (gert.driesen@ardatis.com)
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class StyleTest : BuildTestBase {
        #region Private Instance Fields

        private string _projectXml = @"<?xml version='1.0'?>
                <project>
                    <style in='{0}' out='{1}' style='{2}' />
                </project>";

        #endregion Private Instance Fields
        
        #region Public Instance Methods

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

        #endregion Public Instance Methods
   }
}