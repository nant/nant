using System;
using System.IO;

using NUnit.Framework;
using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;


namespace Tests.NAnt.SourceControl.Tasks {

    /// <summary>
    /// Test that the checkout command brings down the master.build
    ///     file from the nant repository to the specified directory.
    /// </summary>
    [TestFixture]
    public class CheckoutTaskTest : BuildTestBase {
        private String destination;

        private readonly String MODULE = "sharpcvslib-test-repository";
        private readonly String CHECK_FILE = "test-file.txt";

        private readonly String CVSROOT = 
            ":pserver:anonymous@linux.sporadicism.com:/home/cvs/src";

        private readonly String _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                password='{3}'
                                tag='{4}' />
            </project>";

        /// <summary>
        /// Create the directory needed for the test if it does not exist.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            this.destination = this.TempDirName;
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown ();
        }

        /// <summary>
        /// Test that the directory for the cvs checkout gets created and
        ///     that at least the master.build file comes down from the 
        ///     repository.
        /// </summary>
        [Test]
        public void Test_CvsCheckout () {
            object[] args = 
                {MODULE, CVSROOT, this.destination, String.Empty, String.Empty};

            String checkoutPath = Path.Combine (this.destination, this.MODULE);
            String checkFilePath = Path.Combine (checkoutPath, this.CHECK_FILE);

            String result = 
                this.RunBuild (FormatBuildFile (_projectXML, args), Level.Debug);
            Assertion.Assert ("File does not exist, checkout probably did not work.", 
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
