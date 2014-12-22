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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using NUnit.Core;
using NUnit.Core.Filters;
using TestOutput = NUnit.Core.TestOutput;
using NUnit.Util;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.NUnit.Types;
using NAnt.NUnit2.Types;

namespace NAnt.NUnit2.Tasks {
    /// <summary>
    /// Runs tests using the NUnit V2.6 framework.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The <see cref="HaltOnFailure" /> attribute is only useful when more 
    ///   than one test suite is used, and you want to continue running other 
    ///   test suites although a test failed.
    ///   </para>
    ///   <para>
    ///   Set <see cref="Task.FailOnError" /> to <see langword="false" /> to 
    ///   ignore any errors and continue the build.
    ///   </para>
    ///   <para>
    ///   In order to run a test assembly built with NUnit 2.0 or 2.1 using 
    ///   <see cref="NUnit2Task" />, you must add the following node to your
    ///   test config file :
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <configuration>
    ///     ...
    ///     <runtime>
    ///         <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    ///             <dependentAssembly>
    ///                 <assemblyIdentity name="nunit.framework" publicKeyToken="96d09a1eb7f44a77" culture="Neutral" /> 
    ///                 <bindingRedirect oldVersion="2.0.6.0" newVersion="2.2.8.0" /> 
    ///                 <bindingRedirect oldVersion="2.1.4.0" newVersion="2.2.8.0" /> 
    ///             </dependentAssembly>
    ///         </assemblyBinding>
    ///     </runtime>
    ///     ...
    /// </configuration>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   See the <see href="http://nunit.sf.net">NUnit home page</see> for more 
    ///   information.
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
    /// </example>
    /// <example>
    ///   <para>
    ///   Only run tests that are not known to fail in files listed in the <c>tests.txt</c>
    ///   file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nunit2>
    ///     <formatter type="Xml" usefile="true" extension=".xml" outputdir="${build.dir}/results" />
    ///     <test>
    ///         <assemblies>
    ///             <includesfile name="tests.txt" />
    ///         </assemblies>
    ///         <categories>
    ///             <exclude name="NotWorking" />
    ///         </categories>
    ///         <references basedir="build">
    ///             <include name="Cegeka.Income.Services.dll" />
    ///             <include name="Cegeka.Util.dll" />
    ///         </references>
    ///     </test>
    /// </nunit2>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nunit2")]
    public class NUnit2Task : Task {
        #region Private Instance Fields

        private bool _labels = false;
        private bool _haltOnFailure = false;
        private List<NUnit2Test> _tests = new List<NUnit2Test>();
        private List<FormatterElement> _formatterElements = new List<FormatterElement>();
        //private NUnit2TestCollection _tests = new NUnit2TestCollection();
        //private FormatterElementCollection _formatterElements = new FormatterElementCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties
       
        /// <summary>
        /// Stop the test run if a test fails. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure {
            get { return _haltOnFailure; }
            set { _haltOnFailure = value; }
        }

        /// <summary>
        /// Indicate whether or not to label the text output as the tests run.
        /// </summary>
        [TaskAttribute("labels")]
        [BooleanValidator()]
        public bool Labels {
            get { return _labels; }
            set { _labels = value; }
        }

        /// <summary>
        /// Tests to run.
        /// </summary>
        [BuildElementArray("test")]
        public List<NUnit2Test> Tests {
            get { return _tests; }
        }

        /// <summary>
        /// Formatters to output results of unit tests.
        /// </summary>
        [BuildElementArray("formatter")]
        public List<FormatterElement> FormatterElements {
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
                defaultFormatter.NamespaceManager = NamespaceManager;
                defaultFormatter.Type = FormatterType.Plain;
                defaultFormatter.UseFile = false;
                FormatterElements.Add(defaultFormatter);

                Log(Level.Warning, "No <formatter .../> element was specified." +
                    " A plain-text formatter was added to prevent losing output of the" +
                    " test results.");

                Log(Level.Warning, "Add a <formatter .../> element to the" +
                    " <nunit2> task to prevent this warning from being output and" +
                    " to ensure forward compatibility with future revisions of NAnt.");
            }

            LogWriter logWriter = new LogWriter(this, Level.Info, CultureInfo.InvariantCulture);
            EventListener listener = GetListener(logWriter);

            foreach (NUnit2Test testElement in Tests) {
                // Setup the test filter var to setup include/exclude filters.
                ITestFilter testFilter = null;

                // If the include categories contains values, setup the category
                // filter with the include categories.
                string includes = testElement.Categories.Includes.ToString();
                if (!String.IsNullOrEmpty(includes))
                {
                    testFilter = new CategoryFilter(includes.Split(','));
                }
                else
                {
                    // If the include categories does not have includes but
                    // contains excludes, setup the category filter with the excludes
                    // and use the Not filter to tag the categories for exclude.
                    string excludes = testElement.Categories.Excludes.ToString();
                    if (!String.IsNullOrEmpty(excludes))
                    {
                        ITestFilter excludeFilter = new CategoryFilter(excludes.Split(','));
                        testFilter = new NotFilter(excludeFilter);
                    }
                    else
                    {
                        // If the categories do not contain includes or excludes,
                        // assign the testFilter var with an empty filter.
                        testFilter = TestFilter.Empty;
                    }
                }

                foreach (string testAssembly in testElement.TestAssemblies) {
                    // Setup the NUnit2TestDomain var.
                    NUnit2TestDomain domain = new NUnit2TestDomain();
                    // Setup the TestPackage var to use with the testrunner var
                    TestPackage package = new TestPackage(testAssembly);

                    try {
                        // Create the TestRunner var
                        TestRunner runner = domain.CreateRunner(
                            new FileInfo(testAssembly),
                            testElement.AppConfigFile,
                            testElement.References.FileNames);

                        // If the name of the current test element is not null,
                        // use it for the package test name.
                        if (!String.IsNullOrEmpty(testElement.TestName))
                        {
                            package.TestName = testElement.TestName;
                        }

                        // Bool var containing the result of loading the test package
                        // in the TestRunner var.
                        bool successfulLoad = runner.Load(package);

                        // If the test package load was successful, proceed with the
                        // testing.
                        if (successfulLoad)
                        {
                            // If the runner does not contain any tests, proceed
                            // to the next assembly.
                            if (runner.Test == null) {
                                Log(Level.Warning, "Assembly \"{0}\" contains no tests.",
                                    testAssembly);
                                continue;
                            }

                            // Setup and run tests
                            TestResult result = runner.Run(listener, testFilter,
                                    true, GetLoggingThreshold());

                            // flush test output to log
                            logWriter.Flush();
    
                            // format test results using specified formatters
                            FormatResult(testElement, result);
    
                            if (result.IsFailure &&
                                (testElement.HaltOnFailure || HaltOnFailure)) {
                                throw new BuildException("Tests Failed.", Location);
                            }
                        }
                        else
                        {
                            // If the package load failed, throw a build exception.
                            throw new BuildException("Test Package Load Failed.", Location);
                        }
                    } catch (BuildException) {
                        // re-throw build exceptions
                        throw;
                    } catch (Exception ex) {
                        if (!FailOnError) {
                            // just log error and continue with next test

                            // TODO: (RMB) Need to make sure that this is the correct way to proceed with displaying NUnit errors.
                            string logMessage =
                                String.Concat("[", Name, "] NUnit Error: ", ex.ToString());

                            Log(Level.Error, logMessage.PadLeft(Project.IndentationSize));
                            continue;
                        }

                        Version nunitVersion = typeof(TestResult).Assembly.GetName().Version;

                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Failure executing test(s). If your assembly is not built using"
                            + " NUnit version {0}, then ensure you have redirected assembly"
                            + " bindings. Consult the documentation of the <nunit2> task"
                            + " for more information.", nunitVersion), Location, ex);
                    } finally {
                        domain.Unload();

                        // flush test output to log
                        logWriter.Flush();
                    }
                }
            }
        }
        
        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Gets a new EventListener to use for the unit tests.
        /// </summary>
        /// <returns>
        /// A new EventListener created with a new EventCollector that
        /// is initialized with <paramref name="logWriter"/>.
        /// </returns>
        /// <param name='logWriter'>
        /// Log writer to send test output to.
        /// </param>
        protected virtual EventListener GetListener(LogWriter logWriter)
        {
            return new EventCollector(logWriter, logWriter, _labels);
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Gets the logging threshold to use for a test runner based on
        /// the current threshold of this task.
        /// </summary>
        /// <returns>
        /// The logging threshold to use when running a test runner.
        /// </returns>
        private LoggingThreshold GetLoggingThreshold()
        {
            LoggingThreshold result;

            // TODO: (RMB) May need to re-evaluate these mappings
            switch (Threshold) {
                case Level.None:
                    result = LoggingThreshold.Fatal;
                    break;
                case Level.Info:
                    result = LoggingThreshold.Info;
                    break;
                case Level.Verbose:
                    result = LoggingThreshold.All;
                    break;
                case Level.Debug:
                    result = LoggingThreshold.Debug;
                    break;
                case Level.Warning:
                    result = LoggingThreshold.Warn;
                    break;
                case Level.Error:
                    result = LoggingThreshold.Error;
                    break;
                default:
                    result = LoggingThreshold.Off;
                    break;
            }
            return result;
        }

        private void FormatResult(NUnit2Test testElement, TestResult result) {
            // temp file for storing test results
            string xmlResultFile = Path.GetTempFileName();

            try {
                XmlResultWriter resultWriter = new XmlResultWriter(xmlResultFile);
                resultWriter.SaveTestResult(result);

                foreach (FormatterElement formatter in FormatterElements) {
                    // permanent file for storing test results
                    string outputFile = result.Name + "-results" + formatter.Extension;

                    if (formatter.Type == FormatterType.Xml) {
                        if (formatter.UseFile) {
                                        
                            if (formatter.OutputDirectory != null) {
                                // ensure output directory exists
                                if (!formatter.OutputDirectory.Exists) {
                                    formatter.OutputDirectory.Create();
                                }

                                // combine output directory and result filename
                                outputFile = Path.Combine(formatter.OutputDirectory.FullName, 
                                    Path.GetFileName(outputFile));
                            }

                            // copy the temp result file to permanent location
                            File.Copy(xmlResultFile, outputFile, true);
                        } else {
                            using (StreamReader reader = new StreamReader(xmlResultFile)) {
                                // strip off the xml header
                                reader.ReadLine();
                                StringBuilder builder = new StringBuilder();
                                while (reader.Peek() > -1) {
                                    builder.Append(reader.ReadLine().Trim()).Append(
                                        Environment.NewLine);
                                }
                                Log(Level.Info, builder.ToString());
                            }
                        }
                    } else if (formatter.Type == FormatterType.Plain) {
                        TextWriter writer;
                        if (formatter.UseFile) {

                            if (formatter.OutputDirectory != null) {
                                // ensure output directory exists
                                if (!formatter.OutputDirectory.Exists) {
                                    formatter.OutputDirectory.Create();
                                }

                                // combine output directory and result filename
                                outputFile = Path.Combine(formatter.OutputDirectory.FullName, 
                                    Path.GetFileName(outputFile));
                            }

                            writer = new StreamWriter(outputFile);
                        } else {
                            writer = new LogWriter(this, Level.Info, CultureInfo.InvariantCulture);
                        }
                        CreateSummaryDocument(xmlResultFile, writer, testElement);
                        writer.Close();
                    }
                }
            } catch (Exception ex) {
                throw new BuildException("Test results could not be formatted.", 
                    Location, ex);
            } finally {
                // make sure temp file with test results is removed
                File.Delete(xmlResultFile);
            }
        }

        private void CreateSummaryDocument(string resultFile, TextWriter writer, NUnit2Test test) {
            XPathDocument originalXPathDocument = new XPathDocument(resultFile);
            // Using XslTransform instead of XslCompiledTransform because the latter
            // does not display nunit output for unknown reasons.
            XslTransform summaryXslTransform = new XslTransform();
            XmlTextReader transformReader = GetTransformReader(test);
            summaryXslTransform.Load(transformReader);
            summaryXslTransform.Transform(originalXPathDocument, null, writer);
        }

        private XmlTextReader GetTransformReader(NUnit2Test test) {
            XmlTextReader transformReader;
            if (test.XsltFile == null) {
                Assembly assembly = Assembly.GetAssembly(typeof(XmlResultWriter));
                ResourceManager resourceManager = new ResourceManager("NUnit.Util.Transform", assembly);
                string xmlData = (string) resourceManager.GetObject("Summary.xslt", CultureInfo.InvariantCulture);
                transformReader = new XmlTextReader(new StringReader(xmlData));
            } else {
                if (!test.XsltFile.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Transform file '{0}' does not exist.", test.XsltFile.FullName), 
                        Location);
                }
                transformReader = new XmlTextReader(test.XsltFile.FullName);
            }
            
            return transformReader;
        }
        
        #endregion Private Instance Methods

        private class EventCollector : MarshalByRefObject, EventListener {
            private TextWriter outWriter;
            private TextWriter errorWriter;
            private string currentTestName;
            private bool _printLabel;

            public EventCollector(TextWriter outWriter, TextWriter errorWriter)
                : this(outWriter, errorWriter, false) {}

            public EventCollector(TextWriter outWriter, TextWriter errorWriter, bool labels) {
                this.outWriter = outWriter;
                this.errorWriter = errorWriter;
                this.currentTestName = string.Empty;
                this._printLabel = labels;
            }

            public void RunStarted( string name, int testCount ) {
            }

            public void RunFinished(TestResult results) {
            }

            public void RunFinished(Exception exception) {
            }

            public void TestFinished(TestResult result) {
                currentTestName = string.Empty;
            }

            public void TestStarted(TestName testName) {
                currentTestName = testName.FullName;

                if (this._printLabel) {
                    outWriter.WriteLine("***** {0}", currentTestName );
                }
            }

            public void SuiteStarted(TestName testName) {
            }

            public void SuiteFinished(TestResult result) {
            }

            public void UnhandledException( Exception exception ) {
                string msg = string.Format("##### Unhandled Exception while running {0}", currentTestName);
                errorWriter.WriteLine(msg);
                errorWriter.WriteLine(exception.ToString());
            }

            public void TestOutput(TestOutput output) {
                switch (output.Type) {
                    case TestOutputType.Out:
                        outWriter.Write(output.Text);
                        break;
                    case TestOutputType.Error:
                        errorWriter.Write(output.Text);
                        break;
                }
            }

            public override object InitializeLifetimeService() {
                return null;
            }
        }
    }
}
