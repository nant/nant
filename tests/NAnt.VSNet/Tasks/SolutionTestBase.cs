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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;
using Tests.NAnt.VSNet;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for LanguageType.
    /// </summary>
    public enum LanguageType {
        cpp,
        cs,
        vb,
        vjs
    }

    /// <summary>
    /// Available configurations.
    /// </summary>
    public enum ConfigurationType {
        Release,
        Debug
    }

    /// <summary>
    /// Type of binary that was built.
    /// </summary>
    public enum OutputType {
        exe,
        dll
    }
    /// <summary>
    /// Test that c# projects are built successfully.
    /// </summary>
    [TestFixture]
    public abstract class SolutionTestBase : BuildTestBase {
        private readonly string _checkoutXML = @"<?xml version='1.0'?>
            <project default='checkout'>
                <target name='checkout'>
                    <cvs-checkout   module='{0}' 
                                    cvsroot='{1}'
                                    destination='{2}'
                                    password='{3}'
                                    date='{4}' 
                                    usesharpcvslib='false'/>
                </target>
            </project>";

        private string _copy = 
            @"<?xml version='1.0'?>
                <project>
                    <mkdir dir='{0}'/>
                    <copy todir='{1}' verbose='true'>
                        <fileset basedir='{2}'>
                            <include name='**' />
                        </fileset>
                    </copy>
                </project>";

        private DirectoryInfo _currentBaseDir;
        private DirectoryInfo CurrentBaseDir {
            get {
                if (null == this._currentBaseDir) {
                    string examplesDir = this.GetExampleBaseDir().FullName;
                    string currentLanguage = this.CurrentLanguage.ToString();
                    string fullPath = Path.Combine(examplesDir, currentLanguage);
                    this._currentBaseDir = new DirectoryInfo(fullPath);
                }
                return this._currentBaseDir;
            }
        }


        /// <summary>
        /// Get a string that represents the NAnt build file used to build a solution.
        /// </summary>
        protected string SolutionProject =
            @"<?xml version='1.0'?>
                <project default='solution'>
                    <target name='solution' description='Build the project using the solution file.'>
                        <solution solutionfile='{0}' configuration='{1}' verbose='true' />
                    </target>
                </project>";

        /// <summary>
        /// Constructor.
        /// </summary>
        public SolutionTestBase () {
        }

        /// <summary>
        /// The <see cref="Tests.NAnt.VSNet.Tasks.LanguageType"/> that the test is targetting.
        /// </summary>
        protected abstract LanguageType CurrentLanguage {get;}

        /// <summary>
        /// Constructs the path to the solution file using the following substitutions:
        ///     ${examples.dir}/${CurrentLanguage}/${name}/${name}.sln
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected FileInfo GetCurrentSolutionFile (string name) {
            string solutionDir = name;
            string solutionFile = name + ".sln";

            string fullPath;

            fullPath = Path.Combine(CurrentBaseDir.FullName, solutionDir);
            fullPath = Path.Combine(fullPath, solutionFile);

            return new FileInfo(fullPath);
        }

        /// <summary>
        /// Execute any default setup.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            CopyExampleToTemp(this.CurrentBaseDir, this.TempDirectory);
        }

        /// <summary>
        /// Execute any default tear down.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
//            base.TearDown();
        }

        #region Protected Instance Methods

        /// <summary>
        /// Checkout the project to a temporary path.
        /// </summary>
        /// <param name="cvsroot">Cvsroot used to checkout the project.</param>
        /// <param name="module">Module to checkout.</param>
        /// <param name="destination">Place to put the files checkout out.</param>
        /// <param name="password">Password, or <code>String.Empty</code> if no passord.</param>
        /// <param name="date">The date tag to use when checking out the project (used to seperate
        ///     a failing test from a failing build, and most times this is the current date 
        ///     unless a project has been failing consistently.</param>
        protected void CheckoutFiles (string cvsroot, string module, string destination, 
            string password, DateTime date) {
            object[] args = { 
                 module, cvsroot, destination, string.Empty, DateTime.Now};

            string build = FormatBuildFile(_checkoutXML, args);
            string result = RunBuild(build, Level.Info);
        }

        /// <summary>
        /// Get the solution example base directory.
        /// </summary>
        /// <returns></returns>
        protected DirectoryInfo GetExampleBaseDir () {
            string path = Path.Combine(System.Environment.CurrentDirectory, "..");
            path = Path.Combine(path, "examples");
            path = Path.Combine(path, "Solution");

            return new DirectoryInfo(path);
        }

        protected string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        /// <summary>
        /// Assert that the output directory and output file exist.  If they do not exist then 
        /// an assertion exception is thrown.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="output"></param>
        /// <param name="solutionName"></param>
        protected void AssertOutputExists (ConfigurationType configType, OutputType outputType, string solutionName) {
            string tempDir = Path.Combine(this.TempDirectory.FullName, solutionName);
            string binDir = Path.Combine(tempDir, "bin");
            string outputDir = Path.Combine(binDir, configType.ToString());
            string outputFile = Path.Combine(outputDir, 
                String.Format("{0}.{1}", solutionName, outputType.ToString()));

            DirectoryInfo od = new DirectoryInfo(outputDir);
            FileInfo of = new FileInfo(outputFile);

            Assert.IsTrue(od.Exists, String.Format("Output directory does not exist: {0}.", od.FullName));
            Assert.IsTrue(of.Exists, String.Format("Output file does not exist: {0}.", of.FullName));
        }

        /// <summary>
        /// Run a plain build using the given solution file and output type.
        /// </summary>
        /// <param name="solutionFile"></param>
        /// <param name="outputType"></param>
        protected void RunTestPlain(FileInfo solutionFile, OutputType outputType) {
            this.RunTestPlainImpl(ConfigurationType.Release, outputType, solutionFile);
            this.RunTestPlainImpl(ConfigurationType.Debug, outputType, solutionFile);
        }

        #endregion Protected Instance Methods

        #region "Private Instance Methods"

        /// <summary>
        /// Copy the example directory to the temp directory to build.
        /// </summary>
        /// <param name="tempDir"></param>
        /// <param name="exampleDir"></param>
        private void CopyExampleToTemp (DirectoryInfo exampleDir, DirectoryInfo tempDir) {
            object[] args = {tempDir.FullName, tempDir.FullName, exampleDir.FullName};

            string build = FormatBuildFile(_copy, args);
            string result = RunBuild(build, Level.Info);
        }

        private void RunTestPlainImpl(ConfigurationType configType, OutputType outputType, FileInfo solutionFile) {
            // run Release build
            object[] args = {solutionFile.FullName, configType.ToString()};
            string build = FormatBuildFile(SolutionProject, args);
            string result = RunBuild(build, Level.Info);
            AssertOutputExists (configType, outputType, Path.GetFileNameWithoutExtension(solutionFile.FullName));
        }
        #endregion "Private Instance Methods"
    }
}
