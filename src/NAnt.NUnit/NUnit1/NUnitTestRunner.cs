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
// Ian MacLean (ian_maclean@another.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;
using System.Xml;

using NUnit.Framework;
using NUnit.Runner;

using NAnt.NUnit.Types;
using NAnt.NUnit1.Types;

namespace NAnt.NUnit1.Tasks {
    public enum RunnerResult {
        Success,
        Failures,
        Errors,
    }

    public class NUnitTestRunner : BaseTestRunner {
        #region Public Instance Constructors

        public NUnitTestRunner(NUnitTestData testData) {
            _nunittest = testData;
            string nunitsuite = testData.Class + "," + testData.Assembly;
            _suite = GetSuite(nunitsuite);
            testData.Suite = _suite;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the collection of registered formatters.
        /// </summary>
        /// <value>Collection of registered formatters.</value>
        public IResultFormatterCollection Formatters {
            get { return _formatters; }
        }

        /// <summary>
        /// Gets the result of the test.
        /// </summary>
        /// <value>The result of the test.</value>
        public RunnerResult ResultCode {
            get { return _resultCode; }
        }

        #endregion Public Instance Properties

        #region Override implementation of BaseTestRunner

        protected override void RunFailed(string message) {
        }

        #endregion Override implementation of BaseTestRunner

        #region Override implementation of IListener

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

        #endregion Override implementation of IListener

        #region Public Instance Methods

        /// <summary>
        /// Determines if the unit test needs running.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if unit test needs running, otherwise,
        /// <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   Determines if the test needs running by looking at the date stamp 
        ///   of the test assembly and the test results log.
        ///   </para>
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
                        int errors   = Convert.ToInt32(doc.DocumentElement.Attributes["errors"].Value, NumberFormatInfo.InvariantInfo);
                        int failuers = Convert.ToInt32(doc.DocumentElement.Attributes["failures"].Value, NumberFormatInfo.InvariantInfo);
                        if (errors == 0 && failuers == 0) {
                            // no previous errors or failures and the assembly date stamp is older
                            // than the log so it should be safe to skip running the tests this time.
                            needsRunning = false;
                        }
                    } catch {
                        // some sort of error parsing xml, so just run the tests again
                    }
                }
            }
            return needsRunning;
        }

        /// <summary>
        /// Runs a Suite extracted from a TestCase subclass.
        /// </summary>
        public void Run(string logPrefix, bool verbose) {
            CreateFormatters(_nunittest, logPrefix, verbose);

            _result = new TestResultExtra();

            _result.AddListener(this);
            long startTime = System.DateTime.Now.Ticks;

            // Handle start
            OnStartTestSuite();

            _suite.Run(_result);

            // finished test
            long endTime = System.DateTime.Now.Ticks;
            long runTime = (endTime-startTime) / 10000;

            _result.RunTime = runTime;

            // Handle completion
            OnEndTestSuite();

            if (_result.WasSuccessful == false) {
                if (_result.ErrorCount != 0) {
                    _resultCode = RunnerResult.Errors;
                } else if (_result.FailureCount !=0) {
                    _resultCode = RunnerResult.Failures;
                }
            }
           
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Creates the formatters to be used when running this test.
        /// </summary>
        protected void CreateFormatters(NUnitTestData testData, string logPrefix, bool verbose) {
            // Now add the specified formatters
            foreach (FormatterData formatterData in testData.Formatters) {
                // determine file
                FileInfo outFile = GetOutput(formatterData, testData);
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

        /// <summary>
        /// Returns the output file or null if does not use a file.
        /// </summary>
        protected FileInfo GetOutput(FormatterData formatterData, NUnitTestData test) {
            if (formatterData.UseFile) {
                string filename = test.OutFile + formatterData.Extension;
                string absFilename = Path.Combine(test.ToDir, filename);
                return new FileInfo(absFilename);
            }
            return null;
        }

        protected IResultFormatter CreateFormatter(FormatterType type, FileInfo outfile) {
            IResultFormatter retFormatter = null;

            switch (type) {
                case FormatterType.Plain:
                    retFormatter = (IResultFormatter) new PlainTextFormatter();
                    break;
                case FormatterType.Xml:
                    retFormatter = (IResultFormatter) new XmlResultFormatter();
                    break;
                default:
                    break;
            }

            if (outfile == null) {
                // TO-DO : find solution for creating LogWriter without access to Task or Project 
                // for dispatching logging events
                // retFormatter.SetOutput(new LogWriter());
                retFormatter.SetOutput(Console.Out);
            } else {
                retFormatter.SetOutput(new StreamWriter(outfile.Create()));
            }
            return retFormatter;
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Returns the test suite from a given class.
        /// </summary>
        /// <remarks>
        /// The assemblyQualifiedName parameter needs to be in form:
        /// "full.qualified.class.name,Assembly"
        /// </remarks>
        private ITest GetSuite(string assemblyQualifiedName) {
            // Don't worry about catching exceptions in this method.  The
            // NUnitTask will catch them and throw a BuildException back to
            // NAnt with the correct location in the build file. [gs]

            StandardLoader loader = new StandardLoader();
            ITest test = loader.LoadTest(assemblyQualifiedName);
            return test;
        }

        private void OnStartTestSuite() {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.StartTestSuite(_nunittest);
            }
        }

        private void OnEndTestSuite() {
            foreach (IResultFormatter formatter in Formatters) {
                formatter.EndTestSuite(_result);
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        IResultFormatterCollection _formatters = new IResultFormatterCollection();
        NUnitTestData _nunittest = null;
        ITest _suite = null;
        TestResultExtra _result = null;
        RunnerResult _resultCode = RunnerResult.Success;

        #endregion Private Instance Fields
    }
}
