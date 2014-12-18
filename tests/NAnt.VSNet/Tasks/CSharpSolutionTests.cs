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

using System;
using System.IO;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Test that c# projects are built successfully.
    /// </summary>
    [TestFixture]
    public class CSharpSolutionTests : SolutionTestBase {
        #region Private Static Fields

        private const string SharpSchedule = "sharpschedule";

        #endregion Private Static Fields

        #region Protected Instance Properties

        /// <summary>
        /// Gets the language that is being tested.
        /// </summary>
        protected override LanguageType CurrentLanguage {
            get {return LanguageType.cs;}
        }

        #endregion Protected Instance Properties

        #region Override implementation of SolutionTestBase

        /// <summary>
        /// Initialize example directory.
        /// </summary>
        public CSharpSolutionTests () {
        }

        /// <summary>
        /// Run the checkout command so we have something to update.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown();
        }

        #endregion Override implementation of SolutionTestBase

        #region Public Instance Methods

        /// <summary>
        /// Checks whether solution task fails if solution file does not exist.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void TestSolutionFileDoesNotExist() {
            string build = FormatBuildFile(SolutionProject, new object[]{"doesnotexist.sln", "Debug", ""});
            RunBuild(build, Level.Info);
        }

        /// <summary>
        /// Checks whether no failure is reported if solution file does not exist,
        /// but task is not executed.
        /// </summary>
        [Test]
        public void TestSolutionFileDoesNotExistNotExecuted() {
            string build = FormatBuildFile(SolutionProject, new object[] {"doesnotexist.sln", "Debug", "if=\"${true==false}\""});
            RunBuild(build, Level.Info);
        }

        [Test]
        [Ignore("Relies on cvs, and solution fails to build during nightly build.")]
        public void TestSharpscheduleBuild () {
            DirectoryInfo destination = new DirectoryInfo(this.TempDirName);

            try {
                GetProject(destination);
            } catch (Exception) {
                // do not let test fail if cvs checkout fails, users might not
                // have cvs installed
                return;
            }

            string gentlePath = Path.Combine(destination.FullName, SharpSchedule);
            object[] args = {Path.Combine(gentlePath, "schedule.sln"), "release", ""};

            string build = FormatBuildFile(SolutionProject, args);
            RunBuild(build, Level.Info);
        }

        /// <summary>
        /// Tests that the winforms solution builds using the nant solution task.  Ensures that
        /// the outputs are generated correctly.
        /// </summary>
        [Test]
        public void TestWinForm () {
            this.GetCurrentSolutionFile("WinForms");
            this.RunTestPlain();
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void GetProject (DirectoryInfo destination) {
            string cvsroot = ":pserver:anonymous@cvs.sourceforge.net:/cvsroot/sharpschedule";
            string module = SharpSchedule;
            string password = "";

            // use if the build is breaking often
            DateTime date = DateTime.Now;
            this.CheckoutFiles (cvsroot, module, destination.FullName, 
                password, date);
        }

        #endregion Private Instance Methods
    }
}
