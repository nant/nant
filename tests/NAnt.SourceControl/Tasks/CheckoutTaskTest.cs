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
    /// Test that the checkout command brings down the master.build
    /// file from the nant repository to the specified directory.
    /// </summary>
    [TestFixture]
    public class CheckoutTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string destination;

        private readonly string MODULE = "sharpcvslib-test-repository";
        private readonly string CHECK_FILE = "test-file.txt";

        private readonly string CVSROOT = 
            ":pserver:anonymous@linux.sporadicism.com:/home/cvs/src";

        private readonly string _projectXML = @"<?xml version='1.0'?>
            <project>
                <cvs-checkout   module='{0}' 
                                cvsroot='{1}'
                                destination='{2}'
                                password='{3}'
                                tag='{4}' />
            </project>";

/*		private readonly string _useSharpCvsLibProjectXML = @"<?xml version='1.0'?>
            <project>
				<property name='sourcecontrol.usesharpcvslib' value='{0}'/>
                <cvs-checkout   module='{1}' 
                                cvsroot='{2}'
                                destination='{3}'
                                password='{4}' />
            </project>";
*/
        #endregion Private Instance Fields

        #region Override implementation of BuildTestBase

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

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Tests that the directory for the cvs checkout gets created and
        /// that at least the master.build file comes down from the 
        /// repository.
        /// </summary>
        [Test]
        public void Test_CvsCheckout () {
            object[] args = 
                {MODULE, CVSROOT, this.destination, string.Empty, string.Empty};

            string checkoutPath = Path.Combine(this.destination, this.MODULE);
            string checkFilePath = Path.Combine(checkoutPath, this.CHECK_FILE);

            string result = 
                this.RunBuild(FormatBuildFile(_projectXML, args), Level.Debug);
            Assertion.Assert("File does not exist, checkout probably did not work.", 
                File.Exists(checkFilePath));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        #endregion Private Instance Methods
    }
}
