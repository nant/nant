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

using System;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Test that c# projects are built successfully.
    /// </summary>
    [TestFixture]
    public class SolutionTestBase : BuildTestBase {
        private readonly string _checkoutXML = @"<?xml version='1.0'?>
            <project default='checkout'>
                <target name='checkout'>
                    <cvs-checkout   module='{0}' 
                                    cvsroot='{1}'
                                    destination='{2}'
                                    password='{3}'
                                    date='{4}' 
                                    usesharpcvslib='false'/>
                </target>
            </project>";

        /// <summary>
        /// Constructor.
        /// </summary>
        public SolutionTestBase () {
        }

        /// <summary>
        /// Execute any default setup.
        /// </summary>
        [SetUp]
        protected override void SetUp () {
            base.SetUp ();
        }

        /// <summary>
        /// Execute any default tear down.
        /// </summary>
        [TearDown]
        protected override void TearDown () {
            base.TearDown();
        }

        #region Protected Instance Methods
        /// <summary>
        /// Checkout the project to a temporary path.
        /// </summary>
        /// <param name="cvsroot">Cvsroot used to checkout the project.</param>
        /// <param name="module">Module to checkout.</param>
        /// <param name="destination">Place to put the files checkout out.</param>
        /// <param name="password">Password, or <code>String.Empty</code> if no passord.</param>
        /// <param name="date">The date tag to use when checking out the project (used to seperate
        ///     a failing test from a failing build, and most times this is the current date 
        ///     unless a project has been failing consistently.</param>
        protected void CheckoutFiles (string cvsroot, string module, string destination, 
            string password, DateTime date) {
            object[] args = { 
                 module, cvsroot, destination, string.Empty, DateTime.Now};

            string build = FormatBuildFile(_checkoutXML, args);
            System.Console.WriteLine(build);
            string result = 
                RunBuild(build, Level.Debug);

            System.Console.WriteLine(result);
        }

        protected string FormatBuildFile(string baseFile, object[] args) {
            return string.Format(CultureInfo.InvariantCulture, baseFile, args);
        }

        #endregion Protected Instance Methods

    }
}
