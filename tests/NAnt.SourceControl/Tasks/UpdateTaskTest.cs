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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.SourceControl.Tasks {
    /// <summary>
    /// Tests that the update task performs correctly and does
    /// not return errors.
    /// </summary>
    [TestFixture]
    public class UpdateTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string destination;

        private readonly string MODULE = "sharpcvslib-test-repository";
        private readonly string CHECK_FILE = "test-file.txt";

        private readonly string CVSROOT = 
            ":pserver:anonymous@linux.sporadicism.com:/home/cvs/src";

        private readonly string _checkoutXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                password='{3}'
                                tag='{4}' />
            </project>";

        /// <summary>
        /// Project to update the working directory.
        /// </summary>
        private readonly string _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-update   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                password='{3}'
                                tag='{4}' />
            </project>";

        #endregion Private Instance Fields

        #region Override implementation of BuildTestBase

        /// <summary>
        /// Run the checkout command so we have something to update.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            this.destination = this.TempDirName;

            object[] args = { 
                 MODULE, CVSROOT, this.destination, string.Empty, string.Empty};
            string result = 
                this.RunBuild(FormatBuildFile(_checkoutXML, args), Level.Debug);
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown();
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Test that a file deleted from the local working directory
        /// is retrieved from the cvs repository during an update.
        /// </summary>
        [Test]
        public void Test_CvsUpdate () {
            string checkoutPath = Path.Combine(this.destination, this.MODULE);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            if (File.Exists(checkFilePath)) {
                // Delete the file.
                File.Delete(checkFilePath);
            }

            // Make sure the file does not exist before we start the test.
            Assertion.Assert("The check file should not be there.", 
                !File.Exists(checkFilePath));

            // Run the update to bring the file back down.
            object[] args = {MODULE, CVSROOT, this.destination, string.Empty, 
                                string.Empty};
            string result = this.RunBuild(FormatBuildFile(_projectXML, args), 
                Level.Debug);

            // Check that the file is back.
            Assertion.Assert("File does not exist, update probably did not work.", 
                File.Exists (checkFilePath));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        #endregion Private Instance Methods
    }
}
