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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace NAnt.NUnit1.Types {
    /// <summary>
    /// Prints information about running tests in plain text.
    /// </summary>
    public class PlainTextFormatter : IResultFormatter {
        #region Public Instance Constructors

        public PlainTextFormatter() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public TextWriter Writer {
            get { return _writer; }
            set { _writer = value; }
        }

        #endregion Public Instance Properties

        #region Implementation of IResultFormatter

        /// <summary>Sets the Writer the formatter is supposed to write its results to.</summary>
        public void SetOutput(TextWriter writer) {
            Writer = writer;
        }

        /// <summary>Called when the whole test suite has started.</summary>
        public void StartTestSuite(NUnitTestData suite) {
        }

        /// <summary>Called when the whole test suite has ended.</summary>
        public void EndTestSuite(TestResultExtra result) {
            Writer.WriteLine("------------------------------------------");
            if (result.WasSuccessful) {
                Writer.WriteLine("{0} tests: ALL SUCCESSFUL", result.RunCount);
            } else {
                Writer.WriteLine("{0} tests: FAILURES: {1} ERRORS: {2}",
                    result.RunCount, result.FailureCount, result.ErrorCount);
            }
            Writer.Flush();
            Writer.Close();
        }

        #endregion Implementation of IResultFormatter

        #region Implementation of ITestListener

        public void AddError(ITest test, Exception e) {
            Writer.WriteLine("ERROR: " + test.ToString());
            Writer.WriteLine(FormatError(e.StackTrace, e.Message));
        }

        public void AddFailure(ITest test, AssertionFailedError e) {
            Writer.WriteLine("FAILURE: " + test.ToString());
            Writer.WriteLine(FormatError(e.StackTrace, e.Message));
        }

        public void StartTest(ITest test) {
            // TODO: the output from ToString is hard to read, change this to output ClassName.TestName
            Writer.WriteLine(test.ToString());
        }

        public void EndTest(ITest test) {
        }

        #endregion Implementation of ITestListener

        #region Private Static Methods

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

        TextWriter _writer = null;

        #endregion Private Instance Fields
    }
}
