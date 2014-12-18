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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Runtime.Remoting;

using NAnt.Core;
using NAnt.Core.Attributes;

using NAnt.NUnit.Types;
using NAnt.NUnit1.Types;

using System.Security;
using System.Security.Permissions;

namespace NAnt.NUnit1.Tasks {
    /// <summary>
    /// Runs tests using the NUnit V1.0 framework.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   See the <see href="http://nunit.sf.net">NUnit home page</see> for more 
    ///   information.
    ///   </para>
    ///   <para>
    ///   The <see cref="HaltOnFailure" /> or <see cref="HaltOnError" /> 
    ///   attributes are only used to stop more than one test suite to stop 
    ///   running.  If any test suite fails a build error will be thrown.  
    ///   Set <see cref="Task.FailOnError" /> to <see langword="false" /> to 
    ///   ignore test errors and continue build.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Run tests in the <c>MyProject.Tests.dll</c> assembly.
    ///   </para>
    ///   <para>
    ///   The test results are logged in <c>results.xml</c> and <c>results.txt</c> 
    ///   using the <see cref="FormatterType.Xml" /> and <see cref="FormatterType.Plain" /> 
    ///   formatters, respectively.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nunit basedir="build" verbose="false" haltonerror="true" haltonfailure="true">
    ///     <formatter type="Xml" />
    ///     <formatter type="Plain" />
    ///     <test name="MyProject.Tests.AllTests" assembly="MyProject.Tests.dll" outfile="results"/>
    /// </nunit>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nunit")]
    [Obsolete("In a future release, this task will be moved to NAntContrib. However, we strongly advise you to upgrade to NUnit 2.x.")]
    public class NUnitTask : Task {
        #region Private Instance Fields

        private bool _haltOnError = false;
        private bool _haltOnFailure = false;
        private int _timeout = 0;
        private bool _failuresPresent = false;
        private bool _errorsPresent = false;
        private NUnitTestCollection _tests = new NUnitTestCollection();
        private FormatterElementCollection _formatterElements = new FormatterElementCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Stops running tests when a test causes an error. The default is 
        /// <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// Implies haltonfailure.
        /// </remarks>
        [TaskAttribute("haltonerror")]
        [BooleanValidator()]
        public bool HaltOnError {
            get { return _haltOnError; }
            set { _haltOnError = value; }
        }

        /// <summary>
        /// Stops running tests if a test fails (errors are considered failures 
        /// as well). The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure {
            get { return _haltOnFailure; }
            set { _haltOnFailure = value; }
        }

        /// <summary>
        /// Cancel the individual tests if they do not finish in the specified 
        /// time (measured in milliseconds). Ignored if fork is disabled.
        /// </summary>
        [TaskAttribute("timeout")]
        public int Timeout {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Tests to run.
        /// </summary>
        [BuildElementArray("test")]
        public NUnitTestCollection Tests {
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

        protected override void ExecuteTask() {
            foreach (NUnitTest test in _tests) {
                ExecuteTest(test);
            }

            if (_failuresPresent) {
                throw new BuildException("Unit test failed, see build log.", Location);
            }
            if (_errorsPresent) {
                throw new BuildException("Unit test had errors, see build log.", Location);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private void ExecuteTest(NUnitTest test) {
            // Set Defaults
            RunnerResult result = RunnerResult.Success;

            if (test.ToDir == null) {
                test.ToDir = Project.BaseDirectory;
            }
            if (test.OutFile == null) {
                test.OutFile = "TEST-" + test.Class;    
            }

            NUnitTestData testData = test.GetTestData();

            foreach (FormatterElement element in FormatterElements) {
                testData.Formatters.Add(element.Data);
            }

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
        private RunnerResult ExecuteInAppDomain(NUnitTestData test) {
 
            // spawn new domain in specified directory
            AppDomainSetup domSetup = new AppDomainSetup();
            domSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            domSetup.ConfigurationFile = Project.GetFullPath(test.AppConfigFile);
            domSetup.ApplicationName = "NAnt Remote Domain";

            PermissionSet domainPermSet = new PermissionSet(PermissionState.Unrestricted);
            AppDomain newDomain = AppDomain.CreateDomain(domSetup.ApplicationName, AppDomain.CurrentDomain.Evidence, 
                                    domSetup, domainPermSet);

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
            Log(Level.Info, "Running '{0}'.", test.Class);

            runner.Run(string.Empty, Verbose);
            return runner.ResultCode;
        }

        private RunnerResult ExecuteInProc(NUnitTestData test) {
            try {
                NUnitTestRunner runner = new NUnitTestRunner(test);

                if (runner.NeedsRunning()) {
                    Log(Level.Info, "Running '{0}'.", test.Class);
                    runner.Run(string.Empty, Verbose);
                } else {
                    Log(Level.Info, "Skipping '{0}' because tests haven't changed.", test.Class);
                }
                return runner.ResultCode;
            } catch (Exception ex) {
                throw new BuildException("Error running unit test.", Location, ex);
            }
        }

        #endregion Private Instance Methods
    }
}
