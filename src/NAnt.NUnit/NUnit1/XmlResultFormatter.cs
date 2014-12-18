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

// Ian MacLean (ian_maclean@another.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;
using System.Xml;

using NUnit.Framework;

namespace NAnt.NUnit1.Types {
    /// <summary>
    /// Prints detailed information about running tests in XML format.
    /// </summary>
    public class XmlResultFormatter : IResultFormatter {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlResultFormatter" />
        /// class.
        /// </summary>
        public XmlResultFormatter() {
            _document = new XmlDocument();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public TextWriter Writer {
            get { return _writer; }
            set { _writer = value; }
        }

        #endregion Public Instance Properties

        #region Implemenation of IResultFormatter

        /// <summary>
        /// Sets the <see cref="TextWriter" /> the formatter is supposed to 
        /// write its results to.
        /// </summary>
        public void SetOutput(TextWriter writer) {
            Writer = writer;
        }

        /// <summary>
        /// Called when the whole test suite has started.
        /// </summary>
        public void StartTestSuite(NUnitTestData suite) {
            XmlDeclaration decl = _document.CreateXmlDeclaration("1.0", null, null);
            _document.AppendChild(decl);
            _suiteElement = _document.CreateElement(ElementTestSuite);
            //
            // if this is a testsuite, use it's name
            //
            string suiteName = suite.Suite.ToString();
            if (String.IsNullOrEmpty(suiteName)) {
                suiteName = "test"; 
            }
            _suiteElement.SetAttribute(AttributeName, suiteName );
        }

        /// <summary>
        /// Called when the whole test suite has ended.
        /// </summary>
        public void EndTestSuite(TestResultExtra result) {
            _suiteElement.SetAttribute(AttributeTests , result.RunCount.ToString(NumberFormatInfo.InvariantInfo));
            double time = result.RunTime;
            time /= 1000D; 
            _suiteElement.SetAttribute(AttributeTime, time.ToString("#####0.000", NumberFormatInfo.InvariantInfo));
            _document.AppendChild(_suiteElement);

            _suiteElement.SetAttribute(AttributeErrors , result.ErrorCount.ToString(NumberFormatInfo.InvariantInfo)); 
            _suiteElement.SetAttribute(AttributeFailures , result.FailureCount.ToString(NumberFormatInfo.InvariantInfo));
            
            // Send all output to here
            _document.Save(Writer);
            Writer.Flush();
            Writer.Close();
        }

        #endregion Implemenation of IResultFormatter

        #region Implemenation of ITestListener

        public void AddError(ITest test, Exception t) {
            FormatError(ElementError, test, t);
        }

        public void AddFailure(ITest test, AssertionFailedError t) {
            FormatError(ElementFailure, test, (Exception)t);
        }

        public void StartTest(ITest test) {
            _testStartTime =  DateTime.Now;
            _currentTest = _document.CreateElement(ElementTestCase);
            _currentTest.SetAttribute(AttributeName, ((TestCase ) test).ToString());
            
            string className = test.GetType().FullName;
            _currentTest.SetAttribute(AttributeClassname, className);

            _suiteElement.AppendChild(_currentTest);
        }

        public void EndTest(ITest test) {
            TimeSpan elapsedTime = DateTime.Now - _testStartTime;
            double time = elapsedTime.Milliseconds;
            time /= 1000D;
            _currentTest.SetAttribute(AttributeTime, time.ToString("#####0.000", NumberFormatInfo.InvariantInfo));
        }

        #endregion Implemenation of ITestListener

        #region Private Instance Methods

        private void FormatError(string type, ITest test, Exception t) {
            if (test != null) {
                EndTest(test);
            }

            XmlElement nested = _document.CreateElement(type);
            if (test != null) {
                _currentTest.AppendChild(nested);
            } else {
                _suiteElement.AppendChild(nested);
            }

            string message = t.Message;
            if (message != null && message.Length > 0) {
                nested.SetAttribute( AttributeMessage, message );
            }
            nested.SetAttribute(AttributeType, t.GetType().FullName);

            XmlText traceElement = _document.CreateTextNode(t.StackTrace);
            nested.AppendChild(traceElement);
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        TextWriter _writer;

        XmlDocument _document;
        XmlElement _suiteElement;
        XmlElement _currentTest;

        DateTime _testStartTime;

        #endregion Private Instance Fields

        #region Private Static Fields

        const string ElementTestSuite = "testsuite";
        const string ElementTestCase = "testcase";
        const string ElementError = "error";
        const string ElementFailure = "failure";

        const string AttributeName = "name";
        const string AttributeTime = "time";
        const string AttributeErrors = "errors";
        const string AttributeFailures = "failures";
        const string AttributeTests = "tests";
        const string AttributeType = "type";
        const string AttributeMessage = "message";
        const string AttributeClassname = "classname";

        #endregion Private Static Fields
    }
}
