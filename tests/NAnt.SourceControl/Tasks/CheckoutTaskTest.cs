using System;
using System.IO;

using NUnit.Framework;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;


namespace Tests.NAnt.SourceControl.Tasks {

    /// <summary>
    /// Test that the checkout command brings down the master.build
    ///     file from the nant repository to the specified directory.
    /// </summary>
    [TestFixture]
    public class CheckoutTaskTest : BuildTestBase {
        private static readonly String cvsTempPath = 
            Path.Combine (Path.GetTempPath (), "cvscheckout-test");
        private static readonly String TEST_FILE = 
            Path.Combine (cvsTempPath, "Sporadicism.Blogger/blogger.build");

        private readonly String _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='Sporadicism.Blogger' 
                                cvsroot=':pserver:anonymous@linux.sporadicism.com:/home/cvs/src'
                                destination='" + cvsTempPath.Replace ("\\", "/") + @"'
                                password='' />
            </project>";

        /// <summary>
        /// Test that the directory for the cvs checkout gets created and
        ///     that at least the master.build file comes down from the 
        ///     repository.
        /// </summary>
        [Test]
        public void Test_CvsCheckout () {
            String result = this.RunBuild (_projectXML);
            Assertion.Assert ("File does not exist, checkout probably did not work.", 
                File.Exists (TEST_FILE));
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown ();

            Directory.Delete (cvsTempPath, true);
        }
    }
}
