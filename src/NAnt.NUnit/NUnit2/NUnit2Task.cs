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
// Mike Two (2@thoughtworks.com or mike2@nunit.org)
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using NUnit.Core;
using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.NUnit.Types;
using NAnt.NUnit2.Types;

namespace NAnt.NUnit2.Tasks {
    /// <summary>
    /// Runs tests using the NUnit V2.0 framework.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   See the <a href="http://nunit.sf.net">NUnit home page</a> for more 
    ///   information.
    ///   </para>
    ///   <para>
    ///   The <see cref="HaltOnFailure" /> or <see cref="HaltOnError" /> 
    ///   attributes are only used to stop more than one test suite to stop 
    ///   running.  If any test suite fails, a build error will be thrown.  
    ///   Set <see cref="Task.FailOnError" /> to <see langword="false" /> to 
    ///   ignore test errors and continue the build.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Run tests in the <c>MyProject.Tests.dll</c> assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nunit2>
    ///     <formatter type="Plain" />
    ///     <test assemblyname="MyProject.Tests.dll" appconfig="MyProject.Tests.dll.config" />
    /// </nunit2>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Run all tests in files listed in the <c>tests.txt</c> file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nunit2>
    ///     <formatter type="Xml" usefile="true" extension=".xml" />
    ///     <test>
    ///         <assemblies>
    ///             <includesList name="tests.txt" />
    ///         </assemblies>
    ///     </test>
    /// </nunit2>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nunit2")]
    public class NUnit2Task : Task {
        #region Private Instance Fields

        private bool _haltOnFailure = false;
        private bool _haltOnError = true;
        private NUnit2TestCollection _tests = new NUnit2TestCollection();
        private FormatterElementCollection _formatterElements = new FormatterElementCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties
       
        /// <summary>
        /// Stop the build process if a test fails. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure {
            get { return _haltOnFailure; }
            set { _haltOnFailure = value; }
        }

        /// <summary>
        /// Build fails on error. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("haltonerror")]
        [BooleanValidator()]
        public bool HaltOnError {
            get { return _haltOnError; }
            set { _haltOnError = value; }
        }

        /// <summary>
        /// Tests to run.
        /// </summary>
        [BuildElementArray("test")]
        public NUnit2TestCollection Tests {
            get { return _tests; }
        }

        /// <summary>
        /// Formatters to output results of unit tests.
        /// </summary>
        [BuildElementArray("formatter")]
        public FormatterElementCollection FormatterElements {
            get { return _formatterElements; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Runs the tests and sets up the formatters.
        /// </summary>
        protected override void ExecuteTask() {
            if (FormatterElements.Count == 0) {
                FormatterElement defaultFormatter = new FormatterElement();
                defaultFormatter.Project = Project;
                defaultFormatter.Type = FormatterType.Plain;
                defaultFormatter.UseFile = false;
                FormatterElements.Add(defaultFormatter);

                Log(Level.Warning, LogPrefix + "No <formatter .../> element was specified." +
                    " A plain-text formatter was added to prevent loosing output of the" +
                    " test results.");

                Log(Level.Warning, LogPrefix + "Add a <formatter .../> element to the" +
                    " <nunit2> task to prevent this warning from being output and" +
                    " to ensure forward compatibility with future revisions of NAnt.");
            }

            foreach (NUnit2Test test in Tests) {
                EventListener listener = new NullListener();
                TestResult[] results = RunRemoteTest(test, listener);

                // no tests results. An error might have occurred
                if (results == null || results.Length == 0) {
                    continue;
                }

                StringCollection assemblies = test.TestAssemblies;
                for (int i = 0; i < results.Length; i++) {
                    string assemblyFile = assemblies[i];
                    TestResult result = results[i];

                    // temp file for storing test results
                    string xmlResultFile = Path.GetTempFileName();

                    try {
                        XmlResultVisitor resultVisitor = new XmlResultVisitor(xmlResultFile, result);
                        result.Accept(resultVisitor);
                        resultVisitor.Write();

                        foreach (FormatterElement formatter in _formatterElements) {
                            if (formatter.Type == FormatterType.Xml) {
                                if (formatter.UseFile) {
                                    File.Copy(xmlResultFile, result.Name + "-results" + formatter.Extension, true);
                                } else {
                                    using (StreamReader reader = new StreamReader(xmlResultFile)) {
                                        // strip off the xml header
                                        reader.ReadLine();
                                        StringBuilder builder = new StringBuilder();
                                        while (reader.Peek() > -1) {
                                            builder.Append(reader.ReadLine().Trim()).Append("\n");
                                        }
                                        Log(Level.Info, LogPrefix + builder.ToString());
                                    }
                                }
                            }  else if (formatter.Type == FormatterType.Plain) {
                                TextWriter writer;
                                if (formatter.UseFile) {
                                    writer = new StreamWriter(result.Name + "-results" + formatter.Extension);
                                } else {
                                    writer = new LogWriter(this, LogPrefix, CultureInfo.InvariantCulture);
                                }
                                CreateSummaryDocument(xmlResultFile, writer, test);
                                writer.Close();
                            }
                        }
                    } finally {
                        // make sure temp file with test results is removed
                        File.Delete(xmlResultFile);
                    }

                    if (result.IsFailure && (test.HaltOnFailure || HaltOnFailure)) {
                        throw new BuildException("Tests Failed");
                    }
                }
            }
        }
        
        #endregion Override implementation of Task

        #region Private Instance Methods

        private TestResult[] RunRemoteTest(NUnit2Test test, EventListener listener) {
            StringCollection assemblies = test.TestAssemblies;
            ArrayList results = new ArrayList();

            foreach (string assembly in assemblies) {
                TestResult res = RunSingleRemoteTest(test, assembly, listener);
                if (res != null) {
                    results.Add(res);
                }
            }

            return (TestResult[]) results.ToArray(typeof(TestResult));
        }

        private TestResult RunSingleRemoteTest(NUnit2Test test, string testAssembly, EventListener listener) {
            try {
                LogWriter writer = new LogWriter(this, LogPrefix, CultureInfo.InvariantCulture);
                NUnit2TestDomain domain = new NUnit2TestDomain(writer, writer);
                return domain.RunTest(test.TestName, testAssembly, test.AppConfigFile, listener);
            } catch (Exception ex) {
                if (HaltOnError) {
                    throw new BuildException("NUnit 2.0 Error: ", ex);
                }
                Log(Level.Error, LogPrefix + "NUnit 2.0 Error: " + ex.ToString());
                return null;
            }
        }

        private void CreateSummaryDocument(string resultFile, TextWriter writer, NUnit2Test test) {
            XPathDocument originalXPathDocument = new XPathDocument(resultFile);
            XslTransform summaryXslTransform = new XslTransform();
            XmlTextReader transformReader = GetTransformReader(test);
            summaryXslTransform.Load(transformReader);
            summaryXslTransform.Transform(originalXPathDocument, null, writer);
        }
        
        private XmlTextReader GetTransformReader(NUnit2Test test) {
            XmlTextReader transformReader;
            if (test.TransformFile == null) {
                Assembly assembly = Assembly.GetAssembly(typeof(XmlResultVisitor));
                ResourceManager resourceManager = new ResourceManager("NUnit.Framework.Transform", assembly);
                string xmlData = (string)resourceManager.GetObject("Summary.xslt", CultureInfo.InvariantCulture);
                transformReader = new XmlTextReader(new StringReader(xmlData));
            } else {
                FileInfo xsltInfo = new FileInfo(test.TransformFile);
                if (!xsltInfo.Exists) {
                    throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Transform file: {0} does not exist", xsltInfo.FullName));
                }
                transformReader = new XmlTextReader(xsltInfo.FullName);
            }
            
            return transformReader;
        }
        
        #endregion Private Instance Methods

        /// <summary>
        /// Implements a <see cref="TextWriter" /> for writing information to 
        /// the NAnt logging infrastructure.
        /// </summary>
        private class LogWriter : TextWriter {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="LogWriter" /> class 
            /// with the specified prefix and format provider.
            /// </summary>
            /// <param name="task">Determines the indentation level.</param>
            /// <param name="logPrefix">The prefix for written messages.</param>
            /// <param name="formatProvider">An <see cref="IFormatProvider" /> object that controls formatting.</param>
            public LogWriter(Task task, string logPrefix, IFormatProvider formatProvider) : base(formatProvider) {
                _task = task;
                _logPrefix = logPrefix;
            }

            #endregion Public Instance Constructors

            #region Override implementation of TextWriter

            /// <summary>
            /// Gets the <see cref="Encoding" /> in which the output is written.
            /// </summary>
            /// <value>
            /// The <see cref="LogWriter" /> always writes output in UTF8 
            /// encoding.
            /// </value>
            public override Encoding Encoding {
                get { return Encoding.UTF8; }
            }

            /// <summary>
            /// Writes a character array to the text stream, while adding a 
            /// prefix if its the first output on the current line.
            /// </summary>
            /// <param name="chars">The character array to write to the text stream.</param>
            public override void Write(char[] chars) {
                if (_needPrefix) {
                    _message = _logPrefix;
                }
                _message += new string(chars, 0, chars.Length -1);
            }

            /// <summary>
            /// Writes a string followed by a line terminator to the text stream.
            /// </summary>
            /// <param name="value">The string to write. If <paramref name="value" /> is a null reference, only the line termination characters are written.</param>
            public override void WriteLine(string value) {
                string message = "";
                if (_needPrefix) {
                    message = _logPrefix;
                }
                _task.Log(Level.Info, message + value);
                _needPrefix = true;
            }

            /// <summary>
            /// Writes out a formatted string with prefix and a new line, using the same 
            /// semantics as <see cref="string.Format" />.
            /// </summary>
            /// <param name="line">The formatting string.</param>
            /// <param name="args">The object array to write into format string.</param>
            public override void WriteLine(string line, params object[] args) {
                string message = "";
                if (_needPrefix) {
                    message = _logPrefix;
                }
                _task.Log(Level.Info, message + line, args);
                _needPrefix = true;
            }   


            public override void Close() {
                if (!StringUtils.IsNullOrEmpty(_message)) {
                    _task.Log(Level.Info, _message);
                }
                base.Close ();
            }


            #endregion Override implementation of TextWriter

            #region Private Instance Fields

            private Task _task = null;
            private bool _needPrefix = true;
            private string _logPrefix;
            private string _message = "";

            #endregion Private Instance Fields
        }
    }
}