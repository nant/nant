using System;

using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core;

namespace Tests.NAnt.SourceControl.Tasks {
    [TestFixture]
    public class CvsTaskTest : BuildTestBase {
        private string destination;

        private readonly string MODULE = "sharpcvslib";
        private readonly string OVERRIDE_DIR = "sharpcvslib-new";
        private readonly string CHECK_FILE = "SharpCvsLib.sln";

        private readonly string CVSROOT = 
            ":pserver:anonymous@goliath.sporadicism.com:/cvsroot/sharpcvslib";

        private readonly string CHECKOUT = @"<?xml version='1.0'?>
            <project>
                <cvs            command='{0}'
                                cvsroot='{1}'
                                module='{2}' 
                                destination='{3}'
                                usesharpcvslib='{4}' />
            </project>";

        private readonly string CHECKOUT_WITH_COMMAND_OPTIONS = @"<?xml version='1.0'?>
            <project>
                <cvs            command='{0}'
                                cvsroot='{1}'
                                module='{2}' 
                                destination='{3}'
                                usesharpcvslib='{4}'>
                    <commandoptions>
                        <option name='-d' value='{5}' />
                        <option name='-r' value='{6}' />
                    </commandoptions>
                </cvs>
            </project>";

        private readonly string GENERIC_COMMANDLINE = @"<?xml version='1.0'?>
            <project>
                <cvs            command='{0}'
                                cvsroot='{1}'
                                module='{2}' 
                                destination='{3}'
                                usesharpcvslib='{4}' />
            </project>";

        #region Override implementation of BuildTestBase

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

        #endregion Override implementation of BuildTestBase

        /// <summary>
        /// Test that the checkout command executes successfully.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestCheckout () {
            this.destination = this.TempDirName;
            string checkoutPath = Path.Combine(this.destination, this.MODULE);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            object[] args = {"co", CVSROOT, MODULE, this.destination, true};
            this.RunBuild(FormatBuildFile(CHECKOUT, args), Level.Info);

            Assert.IsTrue(File.Exists(checkFilePath), "The check file should not be there.");
        }

        /// <summary>
        /// Test that the checkout command executes successfully with non-sharpcvslib binary.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestCheckout_NotSharpcvslib () {
            this.destination = this.TempDirName;
            string checkoutPath = Path.Combine(this.destination, this.MODULE);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            object[] args = {"co", CVSROOT, MODULE, this.destination, false};
            this.RunBuild(FormatBuildFile(CHECKOUT, args), Level.Info);

            Assert.IsTrue(File.Exists(checkFilePath), "The check file should not be there.");
        }

        /// <summary>
        /// Test that the checkout command executes successfully with non-sharpcvslib binary.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestCheckout_CommandOptions_NoSharpCvsLib () {
            this.destination = this.TempDirName;
            string checkoutPath = Path.Combine(this.destination, this.OVERRIDE_DIR);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            object[] args = {"co", CVSROOT, MODULE, this.destination, false, this.OVERRIDE_DIR, "HEAD"};
            this.RunBuild(FormatBuildFile(CHECKOUT_WITH_COMMAND_OPTIONS, args), Level.Info);

            Assert.IsTrue(File.Exists(checkFilePath), "The check file should not be there.");
        }

        /// <summary>
        /// Test that the checkout command executes successfully with non-sharpcvslib binary.
        /// </summary>
        // TODO: Get this unit test working
        /*        public void TestCheckout_CommandOptions_SharpCvsLib () {
                    this.destination = this.TempDirName;
                    string checkoutPath = Path.Combine(this.destination, this.OVERRIDE_DIR);
                    string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

                    object[] args = {"co", CVSROOT, MODULE, this.destination, true, this.OVERRIDE_DIR, "HEAD"};
                    string result = 
                        this.RunBuild(FormatBuildFile(CHECKOUT_WITH_COMMAND_OPTIONS, args), Level.Debug);

                    System.Console.WriteLine(result);
                    Assertion.Assert("The check file should not be there.", 
                        File.Exists(checkFilePath));

                }
        */
        [Test]
        [Category("InetAccess")]
        public void TestCheckout_CommandLine_Sharpcvslib () {
            this.destination = this.TempDirName;
            string checkoutPath = Path.Combine(this.destination, this.OVERRIDE_DIR);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            object[] checkoutArgs = {"co", CVSROOT, MODULE, this.destination, true};
            string resultCheckout = 
                this.RunBuild(FormatBuildFile(GENERIC_COMMANDLINE, checkoutArgs), Level.Info);
            System.Console.WriteLine(resultCheckout);

            Directory.Delete(Path.Combine(this.destination, "build"));

            object[] updateArgs = {"update -dP", CVSROOT, MODULE, this.destination, true};
            this.RunBuild(FormatBuildFile(GENERIC_COMMANDLINE, updateArgs), Level.Info);

            Assert.IsTrue(File.Exists(checkFilePath), "The check file should not be there.");
        }

        /// <summary>
        /// Test that the time necessary to perform a checkout with both binaries 
        ///     is equal.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestTimeCheckout () {
            long sharpCvsLibTime = DoCheckout(true);
            long cvsPathTime = DoCheckout(false);

            Assert.IsTrue(sharpCvsLibTime < cvsPathTime, "Sharpcvslib time: " 
                + sharpCvsLibTime + "; time for the cvs executable in the path"
                + " variable: " + cvsPathTime);
        }

        #region Private Instance Methods

        private string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        private long DoCheckout (bool useSharpCvsLib) {
            DateTime start = DateTime.Now;
            this.destination = this.TempDirName;

            object[] checkoutArgs = {"co", CVSROOT, MODULE, this.destination, useSharpCvsLib};
            string resultCheckout = 
                this.RunBuild(FormatBuildFile(GENERIC_COMMANDLINE, checkoutArgs), Level.Info);
            System.Console.WriteLine(resultCheckout);

            DateTime end = DateTime.Now;

            // cleanup for next checkout test
            Directory.Delete(Path.Combine(this.destination, MODULE), true);

            return end.Subtract(start).Ticks;
        }

        #endregion Private Instance Methods


    }
}
