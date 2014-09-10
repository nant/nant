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
using Tests.NAnt.Core;

namespace Tests.NAnt.SourceControl.Tasks {
    /// <summary>
    /// Test that the checkout command brings down the master.build
    /// file from the nant repository to the specified directory.
    /// </summary>
    [TestFixture]
    public class CheckoutTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string destination;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string TestModule = "sharpcvslib";
        private const string CheckFile = "lib/ICSharpCode.SharpZipLib.dll";

        private const string TestCvsRoot = 
            ":pserver:anonymous@cvs.sourceforge.net:/cvsroot/sharpcvslib";

        private const string NAntModule = "nant";
        private const string NAntCvsRoot = ":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant";

        private const string _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                revision='{3}'
                                usesharpcvslib='false'/>
            </project>";

        private const string _checkoutByDateProjectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                date='{3}'
                                overridedir='{4}'
                                usesharpcvslib='false' />
            </project>";

        private const string _testReadonly = @"<?xml version='1.0'?>
            <project name='Test checkout' default='checkout'>
                <property name='sourcecontrol.usesharpcvslib' value='{0}' />
                <target name='checkout'>
                    <cvs-checkout   module='{1}'
                                    cvsroot='{2}' 
                                    destination='{3}'
                                    readonly='true'
                                    quiet='true'
                                    commandline='-n'
                    />
                </target>
            </project>";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        /// <summary>
        /// Create the directory needed for the test if it does not exist.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
            destination = TempDirName;
        }

        /// <summary>
        /// Remove the directory created by the checkout/ update.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown ();
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Tests that the directory for the cvs checkout gets created and
        /// that at least the master.build file comes down from the 
        /// repository.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void Test_CvsCheckout_HEAD () {
            object[] args = {NAntModule, NAntCvsRoot, TempDirName, string.Empty};

            string checkoutPath = Path.Combine(TempDirName, NAntModule);
            string checkFilePath = Path.Combine(checkoutPath, "src/NAnt.Compression/Tasks/TarTask.cs");

            RunBuild(FormatBuildFile(_projectXML, args));
            Assert.IsTrue(File.Exists(checkFilePath), "File does not exist, checkout probably did not work.");
        }

        [Test]
        [Category("InetAccess")]
        public void Test_CvsCheckout_Revision () {
            object[] args = {NAntModule, NAntCvsRoot, TempDirName, "EE-patches"};

            string checkoutPath = Path.Combine(TempDirName, NAntModule);
            string checkFilePath = Path.Combine(checkoutPath, "src/NAnt.Compression/Tasks/TarTask.cs");

            RunBuild(FormatBuildFile(_projectXML, args));
            Assert.IsFalse(File.Exists(checkFilePath), "File '" + checkFilePath 
                + "' should not exist.");
            Assert.IsTrue(File.Exists(Path.Combine(checkoutPath, "NAnt.build")), 
                "File does not exist, checkout probably did not work.");
        }

        /// <summary>
        /// Test that a checkout is performed for the given date.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestCheckoutDate () {
            object[] args = { 
                 TestModule, TestCvsRoot, destination, "2003/08/16", "2003_08_16"};

            string checkoutPath = Path.Combine(destination, "2003_08_16");
            string checkFilePath = Path.Combine(checkoutPath, CheckFile);

            RunBuild(FormatBuildFile(_checkoutByDateProjectXML, 
                args), Level.Info);
            Assert.IsTrue(File.Exists(checkFilePath), "File \"{0}\" does not exist.", checkFilePath);
        }

        /// <summary>
        /// Test that a checkout is performed for the given date.
        /// </summary>
        [Test]
        [Category("InetAccess")]
        public void TestCheckoutReadonly () {
            object[] args = { 
                false, TestModule, TestCvsRoot, destination, "2003/08/16", "2003_08_16"};

            string checkoutPath = Path.Combine(destination, TestModule);
            string checkFilePath = Path.Combine(checkoutPath, CheckFile);

            RunBuild(FormatBuildFile(_testReadonly, args), 
                Level.Info);
            Assert.IsTrue(File.Exists(checkFilePath), "File \"{0}\" does not exist.", checkFilePath);

            FileAttributes attributes = File.GetAttributes(checkFilePath);
            Assert.IsTrue(attributes.CompareTo(FileAttributes.ReadOnly) > 0);
        }

        /// <summary>
        /// Test that the validations for the module attribute are carried out
        /// correctly.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void TestModuleValidation_Bad() {
            object[] args = { string.Format("{0}/bad/module", TestModule), 
                                TestCvsRoot, destination, "2003/08/16", "2003_08_16"};

            RunBuild(FormatBuildFile(_checkoutByDateProjectXML, args), Level.Info);
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        #endregion Private Instance Methods
    }
}
