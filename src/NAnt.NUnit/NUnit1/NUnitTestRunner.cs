// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

// Ian MacLean (ian_maclean@another.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using NUnit.Framework;
using NUnit.Runner;
using System.Reflection;
using System.Runtime.Remoting;
using System.IO;
using System.Xml;
using SourceForge.NAnt.Tasks.NUnit.Formatters;

namespace SourceForge.NAnt.Tasks.NUnit {

    using System;
    using System.Reflection;

    public enum RunnerResult {
        Success,
        Failures,
        Errors,
    }

    public class NUnitTestRunner : BaseTestRunner {
        FormatterCollection _formatters = new FormatterCollection();
        NUnitTestData           _nunittest = null;
        ITest               _suite = null;
        TestResultExtra     _result = null;
        RunnerResult        _resultCode = RunnerResult.Success;

        /// <summary>Collection of the registered formatters.</summary>
        public FormatterCollection Formatters { get { return _formatters; } }

        public RunnerResult ResultCode        { get { return _resultCode; } }

        public NUnitTestRunner(NUnitTestData testData) {
            _nunittest = testData;
            string nunitsuite = testData.Class + "," + testData.Assembly;
            _suite = GetSuite(nunitsuite);
            testData.Suite = _suite;
        }

        /// <summary>Returns the test suite from a given class.</summary>
        /// <remarks>
        /// The assemblyQualifiedName parameter needs to be in form:
        /// "full.qualified.class.name,Assembly"
        /// </remarks>
        ITest GetSuite(string assemblyQualifiedName) {
            // Don't worry about catching exceptions in this method.  The
            // NUnitTask will catch them and throw a BuildException back to
            // NAnt with the correct location in the build file. [gs]

            StandardLoader loader = new StandardLoader();
            ITest test = loader.LoadTest(assemblyQualifiedName);
            return test;
        }

        /// <summary>
        /// Determines if the unit test needs running.
        /// </summary>
        /// <returns><c>true</c> if unit test needs running, otherwise returns <c>false</c>.</returns>
        /// <remarks>
        ///   <para>Determines if the test needs running by looking at the data stamp of the test assembly and the test results log.</para>
        /// </remarks>
        public bool NeedsRunning() {
            // assume we need to run unless proven otherwise
            bool needsRunning = true;

            string assemblyFileName = _nunittest.Assembly;
            string logFileName = _nunittest.OutFile + ".xml";

            if (File.Exists(logFileName) && File.Exists(assemblyFileName)) {
                DateTime assemblyDateStamp = File.GetLastWriteTime(assemblyFileName);
                DateTime logDataStamp = File.GetLastWriteTime(logFileName);

                // simple check of datestamps normally works
                if (logDataStamp > assemblyDateStamp) {
                    // date stamps are ok
                    // look inside results to see if there were failures or errors
                    try {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(logFileName);

                        // check for errors or failures
                        // <testsuite name="Tests" tests="3" time="0.050" errors="0" failures="1">
                        int errors   = Convert.ToInt32(doc.DocumentElement.Attributes["errors"].Value);
                        int failuers = Convert.ToInt32(doc.DocumentElement.Attributes["failures"].Value);
                        if (errors == 0 && failuers == 0) {
                            // no previous errors or failures and the assembly date stamp is older
                            // than the log so it should be safe to skip running the tests this time.
                            needsRunning = false;
                        }
                    } catch (Exception) {
                        // some sort of error parsing xml, so just run the tests again
                    }
                }
            }
            return needsRunning;
        }

        /// <summary>Runs a Suite extracted from a TestCase subclass.</summary>
        public void Run(string logPrefix, bool verbose) {

            CreateFormatters(_nunittest, logPrefix, verbose);

            _result = new TestResultExtra();

            _result.AddListener(this);
            long startTime = System.DateTime.Now.Ticks;

            FireStartTestSuite();

            // Fire start events
            _suite.Run(_result);

            // finished test
            long endTime = System.DateTime.Now.Ticks;
            long runTime = (endTime-startTime) / 10000;

            _result.RunTime = runTime;

            // Handle completion
            FireEndTestSuite();

            if (_result.WasSuccessful == false) {
                if (_result.ErrorCount != 0) {
                    _resultCode = RunnerResult.Errors;
                }
                else if (_result.FailureCount !=0) {
                    _resultCode = RunnerResult.Failures;
                }
            }
           
        }


        /// <summary>
        /// Creates the formatters to be used when running
        /// this test.
        /// </summary>
        protected void CreateFormatters(NUnitTestData testData, string logPrefix, bool verbose) {
            // Now add the specified formatters
            foreach (FormatterData formatterData in testData.Formatters) {
                // determine file
                FileInfo outFile = getOutput(formatterData, testData);
                IResultFormatter formatter = CreateFormatter(formatterData.Type, outFile);
                Formatters.Add(formatter);
            }
            // Add default formatter
            // The Log formatter is special in that it always writes to the
            // Log class rather than the TextWriter set in SetOutput().
            // HACK!
            LogFormatter logFormatter = new LogFormatter(logPrefix, verbose);
            Formatters.Add(logFormatter);

        }

        /// <summary>Return the file or null if does not use a file.</summary>
        protected FileInfo getOutput(FormatterData formatterData, NUnitTestData test){
            if ( formatterData.UseFile ) {
                string filename = test.OutFile + formatterData.Extension;
                string absFilename = Path.Combine(test.ToDir, filename);
                return new FileInfo(absFilename);
            }
            return null;
        }

        protected IResultFormatter CreateFormatter(FormatterType type, FileInfo outfile) {
            //Create new element based on ...
            IResultFormatter retFormatter = null;
            switch (type) {
                case FormatterType.Plain:
                    retFormatter = (IResultFormatter) new PlainTextFormatter();
                    break;
                case FormatterType.Xml:
                    retFormatter = (IResultFormatter) new XmlResultFormatter();
                    break;
                case FormatterType.Custom:
                    // Create based on class name
                    break;
                default:
                    //retFormatter = Custom;
                    break;
            }
    
            if (outfile == null) {
                retFormatter.SetOutput(new LogWriter());
            }
            else {
                retFormatter.SetOutput( new StreamWriter( outfile.Create()));
            }
            return retFormatter;
        }

           
        //---------------------------------------------------------
        // BaseTestRunner overrides
        //---------------------------------------------------------
       protected override void RunFailed(string message) {
       }

        //---------------------------------------------------------
        // IListener overrides
        //---------------------------------------------------------
        public override void AddError(ITest test, Exception t) {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.AddError(test, t);
            }

            if (_nunittest.HaltOnError) {
                _result.Stop();
            }
        }

        public override void AddFailure(ITest test, AssertionFailedError t) {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.AddFailure(test, t);
            }

            if (_nunittest.HaltOnFailure) {
                _result.Stop();
            }
        }

        public override void StartTest(ITest test) {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.StartTest(test);
            }
        }

        public override void EndTest(ITest test) {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.EndTest(test);
            }
        }

        //---------------------------------------------------------
        // Formatter notification methods
        //---------------------------------------------------------

        void FireStartTestSuite() {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.StartTestSuite(_nunittest);
            }
        }

        void FireEndTestSuite() {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.EndTestSuite(_result);
            }
        }
    }
}

