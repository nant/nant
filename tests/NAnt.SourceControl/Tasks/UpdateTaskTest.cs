using System;

using System.IO;

using NUnit.Framework;
using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.SourceControl.Tasks {
    /// <summary>
    /// Tests that the update task performs correctly and does
    ///     not returen errors.
    /// </summary>
    [TestFixture]
    public class UpdateTaskTest : BuildTestBase {
        private String destination;

        private readonly String MODULE = "sharpcvslib-test-repository";
        private readonly String CHECK_FILE = "test-file.txt";

        private readonly String CVSROOT = 
            ":pserver:anonymous@linux.sporadicism.com:/home/cvs/src";

        private readonly String _checkoutXML = @"<?xml version='1.0'?>
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
        private readonly String _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-update   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                password='{3}'
                                tag='{4}' />
            </project>";


        /// <summary>
        /// Run the checkout command so we have something to update.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            this.destination = this.TempDirName;

            object[] args = { 
                 MODULE, CVSROOT, this.destination, String.Empty, String.Empty};
            String result = 
                this.RunBuild (FormatBuildFile (_checkoutXML, args), 
                                              Level.Debug);
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown ();
        }

        /// <summary>
        /// Test that a file deleted from the local working directory
        ///     is retrieved from the cvs repository during an update.
        /// </summary>
        [Test]
        public void Test_CvsUpdate () {
            String checkoutPath = Path.Combine (this.destination, this.MODULE);
            String checkFilePath = Path.Combine (checkoutPath, this.CHECK_FILE);

            if (File.Exists (checkFilePath)) {
                // Delete the file.
                File.Delete (checkFilePath);
            }

            // Make sure the file does not exist before we start the test.
            Assertion.Assert ("The check file should not be there.", 
                !File.Exists (checkFilePath));

            // Run the update to bring the file back down.
            object[] args = { 
                                MODULE, CVSROOT, this.destination, String.Empty, String.Empty};
            String result = 
                this.RunBuild (FormatBuildFile (_projectXML, args), 
                                              Level.Debug);

            // Check that the file is back.
            Assertion.Assert ("File does not exist, update probably did not work.", 
                File.Exists (checkFilePath));
        }

        private String FormatBuildFile (String baseFile, object[] args) {
            String buildFile = 
                String.Format (baseFile, args);

            //System.Console.WriteLine (buildFile);
            return buildFile;
        }
    }
}
