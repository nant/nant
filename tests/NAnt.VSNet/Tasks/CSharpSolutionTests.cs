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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;

using NUnit.Framework;
using NAnt.Core;
using NAnt.Core.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.VSNet.Tasks {
    /// <summary>
    /// Test that c# projects are built successfully.
    /// </summary>
    [TestFixture]
    public class CSharpSolutionTests : SolutionTestBase {


        private string _solutionProject =
            @"<?xml version='1.0'?>
<project default='solution'>
    <target name='solution' description='Build the project using the solution file.'>
        <solution solutionfile='{0}' configuration='{1}'>
        </solution>
    </target>
</project>";

        private const string SharpSchedule = "sharpschedule";


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
            //base.TearDown();
        }

        private void GetProject (DirectoryInfo destination) {
            string cvsroot = ":pserver:anonymous@cvs.sourceforge.net:/cvsroot/sharpschedule";
            string module = SharpSchedule;
            string password = "";

            // use if the build is breaking often
            DateTime date = DateTime.Now;
            this.CheckoutFiles (cvsroot, module, destination.FullName, password, date);
        }

        [Test]
        public void TestSharpscheduleBuild () {
            DirectoryInfo destination = new DirectoryInfo(this.TempDirName);
            GetProject(destination);

            string gentlePath = Path.Combine(destination.FullName, SharpSchedule);
            object[] args = {Path.Combine(gentlePath, "schedule.sln"), "release"};

            string build = FormatBuildFile(_solutionProject, args);
            System.Console.WriteLine(build);
            string result = 
                RunBuild(build, Level.Debug);

        }
    }

}
