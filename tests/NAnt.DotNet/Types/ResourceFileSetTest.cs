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

using System;
using System.IO;
using System.Text;
using System.Xml;

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
        }
    }
}
