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
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;

using NUnit.Core;

namespace NAnt.NUnit2.Tasks {
    /// <summary>
    /// Custom TestDomain, similar to the one included with NUnit, in order 
    /// to workaround some limitations in it.
    /// </summary>
    internal class NUnit2TestDomain {
        #region Public Instance Constructors

        public NUnit2TestDomain(TextWriter outStream, TextWriter errorStream) {
            _outStream = outStream;
            _errorStream = errorStream;
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Runs a single testcase.
        /// </summary>
        /// <param name="testcase">The test to run, or <see langword="null" /> if running all tests.</param>
        /// <param name="assemblyFile">The test assembly.</param>
        /// <param name="configFilePath">The application configuration file for the test domain.</param>
        /// <param name="listener">An <see cref="EventListener" />.</param>
        /// <returns>
        /// The result of the test.
        /// </returns>
        public TestResult RunTest(string testcase, string assemblyFile, string configFilePath, EventListener listener) {
            // get full path to directory containing test assembly
            string assemblyDir = Path.GetFullPath(Path.GetDirectoryName(assemblyFile));

            // create test domain
            AppDomain domain = CreateDomain(assemblyDir, configFilePath);

            // store current directory
            string currentDir = Directory.GetCurrentDirectory();

            // change current dir to directory containing test assembly
            Directory.SetCurrentDirectory(assemblyDir);
            assemblyFile = Path.GetFileName(assemblyFile);

            try {
                RemoteTestRunner runner = CreateTestRunner(domain);
                runner.TestFileName = assemblyFile;
                if (testcase != null) {
                    runner.TestName = testcase;
                }
                runner.BuildSuite(); 
                return runner.Run(listener, _outStream, _errorStream);
            } finally {
                // restore original current directory
                Directory.SetCurrentDirectory(currentDir);

                // unload test domain
                AppDomain.Unload(domain);
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private AppDomain CreateDomain(string basedir, string configFilePath) {
            // spawn new domain in specified directory
            AppDomainSetup domSetup = new AppDomainSetup();
            domSetup.ApplicationBase = basedir;
            domSetup.ConfigurationFile = configFilePath;
            domSetup.ApplicationName = "NAnt NUnit2.1 Remote Domain";
         
            return AppDomain.CreateDomain( 
                domSetup.ApplicationName, 
                AppDomain.CurrentDomain.Evidence, 
                domSetup
                );
        }

        private RemoteTestRunner CreateTestRunner(AppDomain domain) {
            ObjectHandle oh;
            Type rtrType = typeof(RemoteTestRunner);

            oh = domain.CreateInstance(
                rtrType.Assembly.FullName,
                rtrType.FullName,
                false, 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                null,
                CultureInfo.InvariantCulture,
                null,
                AppDomain.CurrentDomain.Evidence);
            return (RemoteTestRunner) oh.Unwrap();
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private TextWriter _outStream;
        private TextWriter _errorStream;

        #endregion Private Instance Fields
    }
}
