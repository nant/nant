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

// Matthew Mastracci (mmastrac@canada.com)

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Resources;
using System.Collections;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {

    [TestFixture]
    public class ResxTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0'?>
            <project>
                <resx input=""{1}\{0}"" output=""{1}\{2}"" />
            </project>";

        const string _sourceCode = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
		<root>
			<xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
				<xsd:element name=""root"" msdata:IsDataSet=""true"">
					<xsd:complexType>
						<xsd:choice maxOccurs=""unbounded"">
							<xsd:element name=""data"">
								<xsd:complexType>
									<xsd:sequence>
										<xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
										<xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
									</xsd:sequence>
									<xsd:attribute name=""name"" type=""xsd:string"" />
									<xsd:attribute name=""type"" type=""xsd:string"" />
									<xsd:attribute name=""mimetype"" type=""xsd:string"" />
								</xsd:complexType>
							</xsd:element>
							<xsd:element name=""resheader"">
								<xsd:complexType>
									<xsd:sequence>
										<xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
									</xsd:sequence>
									<xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
								</xsd:complexType>
							</xsd:element>
						</xsd:choice>
					</xsd:complexType>
				</xsd:element>
			</xsd:schema>
			<resheader name=""ResMimeType"">
				<value>text/microsoft-resx</value>
			</resheader>
			<resheader name=""Version"">
				<value>1.0.0.0</value>
			</resheader>
			<resheader name=""Reader"">
				<value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
			</resheader>
			<resheader name=""Writer"">
				<value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
			</resheader>
			<data name=""A"">
				<value>Test string</value>
			</data>
			<data name=""B"" type=""System.Drawing.Icon, System.Drawing, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"" mimetype=""application/x-microsoft.net.object.bytearray.base64"">
				<value>
					AAABAAEAEBAQAAAAAAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAAAAAAgAAAAAAAAAAAAAAAEAAAABAA
					AAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//
					AAD///8AAAAAAAAAAAAAAAAAAAAAcAAHiIiIiIBwAAeIiIiIgHAAB/94SEiAcAAH+IeHiIBwAAeEiEhI
					gHAAd4RIh4iAcABERHSISIBwBGdnZ2iIgHB0iIZ2+EiAcGeH92//+IBwdHf2/0gAAABnd////3hwAHAH
					////dwAAAAd3d3dwAAD//wAA4AEAAOABAADgAQAA4AEAAOABAADgAQAAwAEAAMABAACAAQAAAAEAAAAB
					AAAAAwAAAAcAAGAPAADgHwAA
				</value>
			</data>
			<data name=""C"">
				<value>Test string #2</value>
			</data>
		</root>
        ";
            
        string _sourceFileName;

        [SetUp]
        protected override void SetUp()
        {
        	base.SetUp();
        	_sourceFileName = Path.Combine(TempDirName, "test.resx");
        	TempFile.CreateWithContents(_sourceCode, _sourceFileName);
        	string result = RunBuild(FormatBuildFile(""));
        }
        
        [Test]
        public void TestResources()
        {
        	Hashtable htRes = new Hashtable();
        	
		using ( ResourceReader rr = new ResourceReader( Path.ChangeExtension( _sourceFileName, "resources" ) ) )
		{
			foreach ( DictionaryEntry de in rr )
			{
				htRes[ de.Key ] = de.Value;
			}
		}
		
		Assertion.AssertEquals( "Failed to read proper number of resources", htRes.Count, 3 );		
		Assertion.Assert( "Failed to find resource A", htRes.ContainsKey( "A" ) );		
		Assertion.Assert( "Failed to find resource B", htRes.ContainsKey( "B" ) );		
		Assertion.Assert( "Failed to find resource C", htRes.ContainsKey( "C" ) );		
		
		Assertion.AssertEquals( "Failed to read resource A value", htRes["A"].ToString(), "Test string" );		
		Assertion.AssertEquals( "Failed to read resource B", htRes["B"].GetType().ToString(), "System.Drawing.Icon" );		
		Assertion.AssertEquals( "Failed to read resource C value", htRes["C"].ToString(), "Test string #2" );		
        }
        
        private string FormatBuildFile(string attributes) {
            return String.Format(_format, Path.GetFileName(_sourceFileName), Path.GetDirectoryName(_sourceFileName), Path.GetFileName(Path.ChangeExtension( _sourceFileName, "resources" )), attributes);
        }
   }
}

