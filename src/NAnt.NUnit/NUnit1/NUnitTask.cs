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
using System.IO;
using System.Xml;
using SourceForge.NAnt.Attributes;
using SourceForge.NAnt.Tasks.NUnit.Formatters;
using NUnit.Framework;
using NUnit.Runner;
using System.Reflection;
using System.Runtime.Remoting;

namespace SourceForge.NAnt.Tasks.NUnit {

    /// <summary>Runs tests using the NUnit framework.</summary>
    /// <remarks>
    ///   <para>See the <a href="http://nunit.sf.net">NUnit home page</a> for more information.</para>
    ///   <para>The <c>haltonfailure</c> or <c>haltonerror</c> are only used to stop more than one test suite to stop running.  If any test suite fails a build error will be thrown.  Use <c>failonerror="false"</c> to ignore test errors and continue build.</para>
    /// </remarks>
    /// <example>
    ///   <para>Run tests in the <c>MyProject.Tests.dll</c> assembly.</para>
    ///   <para>The test results are logged in <c>results.xml</c> and <c>results.txt</c> using the <c>Xml</c> and <c>Plain</c> formatters, respectively.</para>
    ///   <code>
    /// <![CDATA[
    /// <nunit basedir="build" verbose="false" haltonerror="true" haltonfailure="true">
    ///     <formatter type="Xml"/>
    ///     <formatter type="Plain"/>
    ///     <test name="MyProject.Tests.AllTests" assembly="MyProject.Tests.dll" outfile="results"/>
    /// </nunit>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("nunit")]
    public class NUnitTask : Task {

        bool _haltOnError = false;
        bool _haltOnFailure = false;
        int _timeout = 0;

        // If NUnit integrates the change that I posted this should be uncommented out
        // See NUnit discussion board for post by gerry_shaw on October 10, 2001
        // <summary>Break in the debugger whenever a test fails.</summary>
        //[TaskAttribute("breakindebugger")]
        //[BooleanValidator()]
        //string _breakindebugger = Boolean.FalseString;
        //public bool BreakInDebugger     { get { return Convert.ToBoolean(_breakindebugger); } }

        // Attribute properties

        /// <summary>Stops running tests when a test causes an error.  Default is "false".</summary>
        /// <remarks>Implies haltonfailure.</remarks>
        [TaskAttribute("haltonerror")]
        [BooleanValidator()]
        public bool HaltOnError         { get { return _haltOnError; }set { _haltOnError = value; } }

        /// <summary>Stops running tests if a test fails (errors are considered failures as well).  Default is "false".</summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure       { get { return _haltOnFailure; } set { _haltOnFailure = value; }}

        /// <summary>Cancel the individual tests if they do not finish in the specified time (measured in milliseconds). Ignored if fork is disabled.</summary>
        [TaskAttribute("timeout")]
        public int  Timeout             { get { return _timeout; } set { _timeout = value; } }

        // child element collections
        NUnitTestCollection _tests = new NUnitTestCollection(); // TODO make a type safe collection
        FormatterElementCollection _formatterElements = new FormatterElementCollection();

        bool _failuresPresent = false;
        bool _errorsPresent = false;

        void ExecuteTest(NUnitTest test) {
            // Set Defaults
            RunnerResult result = RunnerResult.Success;

            if (test.ToDir == null) {
                test.ToDir = Project.BaseDirectory;
            }
            if (test.OutFile == null) {
                test.OutFile = "TEST-" + test.Class;
            }
            NUnitTestData testData = test.GetTestData();
            mergeFormatters(testData);

            if (testData.Fork == true) {
                result = ExecuteInAppDomain(testData);
            } else {
                result = ExecuteInProc(testData);
            }

            // Handle return code:
            // If there is an error/failure and that it should halt, stop
            // everything otherwise just log a statement.
            bool errorOccurred   = (result == RunnerResult.Errors);
            bool failureOccurred = (result != RunnerResult.Success);

            if ((errorOccurred && test.HaltOnError) || (failureOccurred && test.HaltOnFailure)) {
                // Only thrown if this test should halt as soon as the first
                // error/failure is detected.  In most cases all tests will
                // be run to get a full list of problems.
                throw new BuildException("Test " + testData.Class + " Failed" , Location);
            }

            // Used for reporting the final result from the task.
            if (errorOccurred) {
                _errorsPresent = true;
            }
            if (failureOccurred) {
                _failuresPresent = true;
            }
        }

        // TODO implement launching in a seperate App Domain
        RunnerResult ExecuteInAppDomain(NUnitTestData test) {
 
            // spawn new domain in specified directory
            AppDomainSetup domSetup = new AppDomainSetup();
            domSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            domSetup.ConfigurationFile = Project.GetFullPath(test.AppConfigFile);
            domSetup.ApplicationName = "NAnt Remote Domain";
            AppDomain newDomain =  AppDomain.CreateDomain(domSetup.ApplicationName, null, domSetup);

            // instantiate subclassed test runner in new domain
            Type runnerType = typeof(RemoteNUnitTestRunner);
            ObjectHandle oh = newDomain.CreateInstance ( 
                                       runnerType.Assembly.FullName,
                                       runnerType.FullName,
                                       false, 0, null, 
                                       new object[] { test },
                                       null, null, null
                                    );
            RemoteNUnitTestRunner runner = (RemoteNUnitTestRunner)(oh.Unwrap());
            Log.WriteLine(LogPrefix + "Running {0} ", test.Class);


            runner.Run(LogPrefix, Verbose);
            return runner.ResultCode;
        }

        RunnerResult ExecuteInProc(NUnitTestData test) {
            try {
                NUnitTestRunner runner = new NUnitTestRunner(test);

                if (runner.NeedsRunning()) {
                    Log.WriteLine(LogPrefix + "Running {0}", test.Class);
                    runner.Run(LogPrefix, Verbose);
                } else {
                    Log.WriteLine(LogPrefix + "Skipping {0} because tests haven't changed.", test.Class);
                }
                return runner.ResultCode;

            } catch (Exception e) {
                throw new BuildException("Error running unit test.", Location, e);
            }
        }

        /// <param name="taskNode">Xml node used to initialize this task instance.</param>
        protected override void InitializeTask(XmlNode taskNode) {

            // get all child tests
            foreach (XmlNode testNode in taskNode) {
                if(testNode.Name.Equals("test"))
                {
                    NUnitTest test = new NUnitTest();
                    test.Project = Project; 
                    test.Initialize(testNode);
                    _tests.Add(test);
                }
            }

            // now get formatters
            foreach (XmlNode formatterNode in taskNode) {
                if(formatterNode.Name.Equals("formatter"))
                {
                    FormatterElement formatter = new FormatterElement();
                    formatter.Project = Project;
                    formatter.Initialize(formatterNode);
                    _formatterElements.Add(formatter);
                }
            }
        }

        protected override void ExecuteTask() {
            // If NUnit integrates the change that I posted this should be uncommented out
            // See NUnit discussion board for post by gerry_shaw on October 10, 2001
            //Assertion.BreakInDebugger = BreakInDebugger;

            foreach (NUnitTest test in _tests) {
                //test.AutoExpandAttributes();
                ExecuteTest(test);
            }

            // always throw a buildexception if tests failed (use failonerror="false" to continue building with failed tests).
            if (_failuresPresent) {
                throw new BuildException("Unit test failed, see build log.", Location);
            }
            if (_errorsPresent) {
                throw new BuildException("Unit test had errors, see build log.", Location);
            }
        }


        protected void mergeFormatters(NUnitTestData test){
           foreach ( FormatterElement element in _formatterElements ) {
              test.Formatters.Add(element.Data);
           }
        }
    }
}
