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

// Jason Pettys (jason.pettys@workforceroi.com)

using System.IO;
using NUnit.Framework;

using NAnt.DotNet.Types;

using Tests.NAnt.Core;

namespace Tests.NAnt.DotNet.Types {
    [TestFixture]
    public class ResourceFileSetTest : BuildTestBase {
        [Test]
        public void Test_ManifestResourceName_NoPrefix() {
            ResourceFileSet fileset = new ResourceFileSet();

            fileset.BaseDirectory = TempDirectory;
            string actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("file.txt", actualName, "Incorrect manifest resource name.");

            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("file.txt", actualName, "Incorrect manifest resource name.");

            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"));
            Assert.AreEqual("file.txt", actualName, "Incorrect manifest resource name.");

            // resource with a different logical path
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"), Path.Combine(
                fileset.BaseDirectory.FullName, "test" + Path.DirectorySeparatorChar +
                "new.txt"));
            Assert.AreEqual("file.txt", actualName, "Incorrect manifest resource name.");
        }

        [Test]
        public void Test_ManifestResourceName_StaticPrefix() {
            ResourceFileSet fileset = new ResourceFileSet();

            fileset.BaseDirectory = TempDirectory;
            fileset.Prefix = @"Root.MyProj.Howdy";
            string actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".file.txt", actualName, 
                "Incorrect manifest resource name.");
        
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".file.txt", actualName, 
                "Incorrect manifest resource name.");

            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".file.txt", actualName,
                "Incorrect manifest resource name.");

            // resource with a different logical path
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"), Path.Combine(
                fileset.BaseDirectory.FullName, "test" + Path.DirectorySeparatorChar + 
                "new.txt"));
            Assert.AreEqual(fileset.Prefix + ".file.txt", actualName,
                "Incorrect manifest resource name.");
        }

        [Test]
        public void Test_ManifestResourceName_DynamicWithPrefix() {
            ResourceFileSet fileset = new ResourceFileSet();

            fileset.BaseDirectory = TempDirectory;
            fileset.Prefix = @"Root.MyProj.Howdy";
            fileset.DynamicPrefix = true;
            string actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".mydir.file.txt", actualName,
                "Incorrect manifest resource name.");
        
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".mydir.file.txt", actualName,
                "Incorrect manifest resource name.");

            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"));
            Assert.AreEqual(fileset.Prefix + ".file.txt", actualName,
                "Incorrect manifest resource name.");

            // resource with a different logical path
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"), Path.Combine(
                fileset.BaseDirectory.FullName, "test" + Path.DirectorySeparatorChar + 
                "new.txt"));
            Assert.AreEqual(fileset.Prefix + ".test.file.txt", actualName,
                "Incorrect manifest resource name.");
        }

        [Test]
        public void Test_ManifestResourceName_DynamicWithEmptyPrefix() {
            ResourceFileSet fileset = new ResourceFileSet();

            fileset.BaseDirectory = TempDirectory;
            fileset.Prefix = "";
            fileset.DynamicPrefix = true;
            string actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("mydir.file.txt", actualName, "Incorrect manifest resource name.");
        
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("mydir.file.txt", actualName, "Incorrect manifest resource name.");

            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"));
            Assert.AreEqual("file.txt", actualName, "Incorrect manifest resource name.");

            // resource with a different logical path
            fileset.BaseDirectory = new DirectoryInfo(TempDirName + Path.DirectorySeparatorChar);
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "file.txt"), Path.Combine(
                fileset.BaseDirectory.FullName, "test" + Path.DirectorySeparatorChar + 
                "new.txt"));
            Assert.AreEqual("test.file.txt", actualName,
                "Incorrect manifest resource name.");
        }

        [Test]
        public void Test_ManifestResourceName_PrefixWithSpecialCharacters() {
            ResourceFileSet fileset = new ResourceFileSet();
            fileset.BaseDirectory = TempDirectory;
            fileset.DynamicPrefix = true;

            string actualName;

            // if part of the prefix starts with a number, it should be
            // prefixed with an underscore
            fileset.Prefix = @"Root.1MyProj.Howdy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("Root._1MyProj.Howdy" + ".mydir.file.txt", actualName,
                "Incorrect manifest resource name.");

            // any character in the prefix that is neither letter nor digit should
            // be replaced with an underscore
            fileset.Prefix = @"Root.-MyProj.H0w!dy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.txt"));
            Assert.AreEqual("Root._MyProj.H0w_dy" + ".mydir.file.txt", actualName,
                "Incorrect manifest resource name.");

            // the file name part of a manifest resource name can start with a 
            // digit
            fileset.Prefix = @"Root.MyProj.Howdy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "1file.txt"));
            Assert.AreEqual("Root.MyProj.Howdy" + ".mydir.1file.txt", actualName,
                "Incorrect manifest resource name.");

            // the extension part of a manifest resource name can start with a 
            // digit
            fileset.Prefix = @"Root.MyProj.Howdy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.1txt"));
            Assert.AreEqual("Root.MyProj.Howdy" + ".mydir.file.1txt", actualName,
                "Incorrect manifest resource name.");

            // the file name part of a manifest resource name can contain 
            // characters that are neither letter nor digit
            fileset.Prefix = @"Root.MyProj.Howdy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "f-ile.txt"));
            Assert.AreEqual("Root.MyProj.Howdy" + ".mydir.f-ile.txt", actualName,
                "Incorrect manifest resource name.");

            // the extension part of a manifest resource name can contain 
            // characters that are neither letter nor digit
            fileset.Prefix = @"Root.MyProj.Howdy";
            actualName = fileset.GetManifestResourceName(Path.Combine(
                fileset.BaseDirectory.FullName, "mydir" + Path.DirectorySeparatorChar 
                + "file.t!xt"));
            Assert.AreEqual("Root.MyProj.Howdy" + ".mydir.file.t!xt", actualName,
                "Incorrect manifest resource name.");
        }
    }
}
