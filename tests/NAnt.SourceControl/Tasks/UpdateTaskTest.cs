using System;

using System.IO;

using NUnit.Framework;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.SourceControl.Tasks {
    /// <summary>
    /// Tests that the update task performs correctly and does
    ///     not returen errors.
    /// </summary>
    [TestFixture]
    public class UpdateTaskTest : BuildTestBase {
        private static readonly String cvsTempPath = 
            Path.Combine (Path.GetTempPath (), "cvscheckout-test");
        private static readonly String TEST_FILE = 
            Path.Combine (cvsTempPath, "nant/NAnt.build");

        /// <summary>
        /// Project to checkout the file initially before
        ///     the update test is done.
        /// </summary>
        private readonly String _checkoutXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='NAnt' 
                                cvsroot=':pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant'
                                destination='" + cvsTempPath + @"'
                                password='' />
            </project>";


        /// <summary>
        /// Project to update the working directory.
        /// </summary>
        private readonly String _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-update   module='NAnt' 
                                cvsroot=':pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant'
                                destination='" + cvsTempPath + @"'
                                password='' />
            </project>";


        /// <summary>
        /// Run the checkout command so we have something to update.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            //System.Console.WriteLine (this.RunBuild (_checkoutXML));
        }

        /// <summary>
        /// Test that a file deleted from the local working directory
        ///     is retrieved from the cvs repository during an update.
        /// </summary>
        [Test]
        public void Test_CvsUpdate () {            

            if (File.Exists (TEST_FILE)) {
                // Delete the file.
                File.Delete (TEST_FILE);
            }

            // Make sure the file exists before we start the test.
            Assertion.Assert ("The master.build file was not where I expected it.", 
                !File.Exists (TEST_FILE));

            // Run the update to bring the file back down.
            bool good = false;
            int run = 0;
            while (!good && run < 100) {
                try {
                    String result = this.RunBuild (_projectXML);
                    good = true;
                } catch (Exception) {
                    run++;
                }
            }

            Assertion.Assert ("Still not good.", good);


            // Check that the file is back.
            Assertion.Assert ("File does not exist, update probably did not work.", 
                File.Exists (TEST_FILE));
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown ();
            //Directory.Delete ("c:/temp/cvscheckout-test", true);
        }
    }
}
