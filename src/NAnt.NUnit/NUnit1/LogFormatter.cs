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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
//
// TO-DO : replace Console.WriteLine methods with logging method calls

using System;
using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace NAnt.NUnit1.Types {
    /// <summary>
    /// Prints information about running tests directly to the build log.
    /// </summary>
    public class LogFormatter : IResultFormatter {
        #region Public Instance Constructors

        public LogFormatter(string prefix, bool verbose) {
            if (prefix != null) {
                _prefix = prefix;
            } else {
                _prefix = String.Empty;
            }
            _verbose = verbose;
        }

        #endregion Public Instance Constructors

        #region Protected Instance Properties

        protected bool Verbose {
            get { return _verbose; }
        }

        protected string Prefix {
            get { return _prefix; }
        }

        #endregion Protected Instance Properties

        #region Implementation of IResultFormatter

        /// <summary>Not used, all output goes to Log class.</summary>
        public void SetOutput(TextWriter writer) {
        }

        /// <summary>Called when the whole test suite has started.</summary>
        public void StartTestSuite(NUnitTestData suite) {
            if (Verbose) {
                Console.WriteLine(Prefix + "------------------------------------------");
            }
        }

        /// <summary>Called when the whole test suite has ended.</summary>
        public void EndTestSuite(TestResultExtra result) {
            if (Verbose) {
                Console.WriteLine(Prefix + "------------------------------------------");
            }
            if (result.WasSuccessful) {
                Console.WriteLine(Prefix + "{0} tests: ALL SUCCESSFUL", result.RunCount);
            } else {
                Console.WriteLine(Prefix + "{0} tests: FAILURES: {1} ERRORS: {2}", result.RunCount, result.FailureCount, result.ErrorCount);
            }
        }

        #endregion Implementation of IResultFormatter

        #region Implementation of ITestListener

        public void AddError(ITest test, Exception e) {
            Console.WriteLine(Prefix + "ERROR: " + GetTestSummary(test));
            Console.WriteLine(FormatError(e.StackTrace, e.Message));
            if (Verbose) {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void AddFailure(ITest test, AssertionFailedError e) {
            Console.WriteLine(Prefix + "FAILURE: " + GetTestSummary(test));
            Console.WriteLine(FormatError(e.StackTrace, e.Message));
            if (Verbose) {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void StartTest(ITest test) {
            if (Verbose) {
                Console.WriteLine(Prefix + GetTestSummary(test));
            }
        }

        public void EndTest(ITest test) {
        }

        #endregion Implementation of ITestListener

        #region Private Static Methods

        // NOTE: When test.ToString() displays something less stupid than 
        // MethodName(Namespace.ClassName) think about changing to that.  As it 
        // is now its impossible to sort the test output.
        // The workaround is to use this method.
        private static string GetTestSummary(ITest test) {
            string nunitInfo = test.ToString();
            return test.GetType().Name + "." + nunitInfo.Substring(0, nunitInfo.IndexOf('('));
        }


        /// <summary>Convert a stack trace line into something that can be clicked on in an IDE output window.</summary>
        /// <param name="trace">The StackTrace string, see <see cref="Exception.StackTrace"/>.</param>
        /// <param name="message">The string that gets appended to the end of file(line): portion.</param>
        private static string FormatError(string trace, string message) {
            // if we can't find a filename(line#) string, then at least display the message
            string line = message;

            string[] lines = trace.Split(new char[] {'\n'});

            // search the stack trace for the first filename(linenumber) like string
            Regex r = new Regex(@"^\s+at (?<method>.+) in (?<file>.+):line (?<line>\d+)$");
            foreach (string str in lines) {
                Match match = r.Match(str);
                if (match.Success) {
                    line = match.Result("${file}(${line}): ") + message; 
                    break; // no need to continue
                }
            }

            return line;
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private string _prefix;
        private bool _verbose;

        #endregion Private Instance Fields
    }
}
