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

// Mike Two (2@thoughtworks.com or mike2@nunit.org)


using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Reflection;
using System.Resources;
using System.Collections;
using System.Text;
using SourceForge.NAnt.Attributes;
using SourceForge.NAnt.Tasks.NUnit.Formatters;
using NUnit.Framework;
using NUnit.Core;

namespace SourceForge.NAnt.Tasks.NUnit2 {
    /// <summary>Runs tests using the NUnit V2.0 framework.</summary>
    /// <remarks>
    ///   <para>See the <a href="http://nunit.sf.net">NUnit home page</a> for more information.</para>
    ///   <para>The <c>haltonfailure</c> or <c>haltonerror</c> are only used to stop more than one test suite to stop running.  If any test suite fails a build error will be thrown.  Use <c>failonerror="false"</c> to ignore test errors and continue build.</para>
    /// </remarks>
    /// <example>
    ///   <para>Run tests in the <c>MyProject.Tests.dll</c> assembly.</para>
    ///   <code>
    /// <![CDATA[
    /// <nunit2>
    ///     <test assemblyname="MyProject.Tests.dll" outfile="results.xml"/>
    /// </nunit2>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("nunit2")]
    public class NUnit2Task : Task {
        private bool _haltOnFailure;
        private ArrayList tests = new ArrayList();
		
        /// <summary>Stop the build process if a test fails.</summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure       { get { return _haltOnFailure; } set { _haltOnFailure = value; }}
	    
        FormatterElementCollection _formatterElements = new FormatterElementCollection();
	    
        protected override void InitializeTask(XmlNode taskNode) {
            foreach (XmlNode testNode in taskNode) {
                if(testNode.Name.Equals("test")) {
                    NUnit2Test test = new NUnit2Test();
                    test.Project = Project; 
                    test.Initialize(testNode);
                    tests.Add(test);
                }
            }
			
            // now get formatters
            foreach (XmlNode formatterNode in taskNode) {
                if(formatterNode.Name.Equals("formatter")) {
                    FormatterElement formatter = new FormatterElement();
                    formatter.Project = Project;
                    formatter.Initialize(formatterNode);
                    _formatterElements.Add(formatter);
                }
            }
            
            FormatterElement defaultFormatter = new FormatterElement();
            defaultFormatter.Project = Project;
            defaultFormatter.Type = FormatterType.Plain;
            defaultFormatter.UseFile = false;
            _formatterElements.Add(defaultFormatter);
        }
        
        protected override void ExecuteTask() {
            foreach (NUnit2Test test in tests) {
                EventListener listener = new NullListener();
                TestResult result = RunRemoteTest(test, listener);
	    			
                string xmlResultFile = test.AssemblyName + "-results.xml";	    			
							
                XmlResultVisitor resultVisitor = new XmlResultVisitor(xmlResultFile, result);
                result.Accept(resultVisitor);
                resultVisitor.Write();	
				
                foreach (FormatterElement formatter in _formatterElements) {
                    if (formatter.Type == FormatterType.Xml) {
                        if (!formatter.UseFile) {
                            using (StreamReader reader = new StreamReader(xmlResultFile)) {
                                // strip off the xml header
                                reader.ReadLine();
                                StringBuilder builder = new StringBuilder();
                                while (reader.Peek() > -1)
                                    builder.Append(reader.ReadLine().Trim()).Append("\n");
                                Log.WriteLine(builder.ToString());
                            }
                        }
                    } else if (formatter.Type == FormatterType.Plain) {
                        TextWriter writer;
                        if (formatter.UseFile) {
                            writer = new StreamWriter(test.AssemblyName + "-results" + formatter.Extension);
                        } else {
                            writer = new LogWriter();
                        }
                        CreateSummaryDocument(xmlResultFile, writer, test);
                        writer.Close();
                    }
                }
                if (result.IsFailure && (test.HaltOnFailure || HaltOnFailure)) {
                    throw new BuildException("Tests Failed");
                }		
            }
        }
	    
        private TestResult RunRemoteTest(NUnit2Test test, EventListener listener) {
            LogWriter writer = new LogWriter();
            NUnit2TestDomain domain = new NUnit2TestDomain(writer, writer);
            if ( test.TestName != null ) {
                return domain.RunTest(test.TestName, test.AssemblyName, test.AppConfigFile, listener);
            } else {
                return domain.Run(test.AssemblyName, test.AppConfigFile, listener);
            }
        }
	    
        private void CreateSummaryDocument(string resultFile, TextWriter writer, NUnit2Test test) {
            XPathDocument originalXPathDocument = new XPathDocument (resultFile);
            XslTransform summaryXslTransform = new XslTransform();
            XmlTextReader transformReader = GetTransformReader(test);
            summaryXslTransform.Load(transformReader);
			
            summaryXslTransform.Transform(originalXPathDocument,null,writer);
        }
		
		
        private XmlTextReader GetTransformReader(NUnit2Test test) {
            XmlTextReader transformReader;
            if(test.TransformFile == null) {
                Assembly assembly = Assembly.GetAssembly(typeof(XmlResultVisitor));
                ResourceManager resourceManager = new ResourceManager("NUnit.Framework.Transform",assembly);
                string xmlData = (string)resourceManager.GetObject("Summary.xslt");

                transformReader = new XmlTextReader(new StringReader(xmlData));
            }
            else {
                FileInfo xsltInfo = new FileInfo(test.TransformFile);
                if(!xsltInfo.Exists) {
                    throw new BuildException(String.Format("Transform file: {0} does not exist", xsltInfo.FullName));
                }

                transformReader = new XmlTextReader(xsltInfo.FullName);
            }
			
            return transformReader;
        }
		
        private class LogWriter : TextWriter {
			
            public override Encoding Encoding { get { return Encoding.UTF8; } }
			
			
            public override void Write(char[] chars) {
                Log.WriteLine(new String(chars, 0, chars.Length -1));
            }
			
            public override void WriteLine(string line) {
                Log.WriteLine(line);
            }
			
            public override void WriteLine(string line, params object[] args) {
                Log.WriteLine(line, args);
            }				
        }
    }
}