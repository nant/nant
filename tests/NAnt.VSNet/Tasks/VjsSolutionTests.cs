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

using NUnit.Framework;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Test that cpp projects are built successfully.
    /// </summary>
    [TestFixture]
    public class VjsSolutionTests : SolutionTestBase {
        #region Private Static Fields
        // add fields here
        #endregion Private Static Fields

        #region Protected Static Fields 
        /// <summary>
        /// LanguageType that is being tested.
        /// </summary>
        protected override LanguageType CurrentLanguage {
            get {return LanguageType.vjs;}
        }
        #endregion

        #region Override implementation of SolutionTestBase

        /// <summary>
        /// Initialize example directory.
        /// </summary>
        public VjsSolutionTests () {
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
        /// Tests that the winforms solution builds using the nant solution task.  Ensures that
        /// the outputs are generated correctly.
        /// </summary>
        [Test]
        [Ignore("Solution type not currently supported.")]
        public void TestWinForm () {
            this.RunTestPlain();
        }

        #endregion Public Instance Methods

    }
}
