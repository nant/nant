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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    public class FileSetTest : BuildTestBase {

        FileSet _fileSet;

        public FileSetTest(String name) : base(name) {
        }

        protected override void SetUp() {
            base.SetUp();

            // create the file set
            _fileSet = new FileSet();
            _fileSet.BaseDirectory = TempDirName;

            // create some test files to match against
            TempFile.Create(Path.Combine(TempDirName, "world.peace"));
            TempFile.Create(Path.Combine(TempDirName, "world.war"));
            TempFile.Create(Path.Combine(TempDirName, "reefer.maddness"));
            TempFile.Create(Path.Combine(TempDirName, "reefer.saddness"));

            string sub1Path = Path.Combine(TempDirName, "sub1");
            Directory.CreateDirectory(sub1Path);
            TempFile.Create(Path.Combine(sub1Path, "sub.one"));
        }

        public void Test_AsIs() {
            _fileSet.AsIs.Add("foo");
            _fileSet.AsIs.Add("bar");
            AssertMatch("foo", false);
            AssertMatch("bar", false);
            AssertEquals(2, _fileSet.FileNames.Count);
        }

        public void Test_IncludesAndAsIs() {
            _fileSet.Includes.Add("foo");
            _fileSet.AsIs.Add("foo");
            _fileSet.AsIs.Add("bar");
            AssertMatch("foo", false);
            AssertMatch("bar", false);
            AssertEquals(2, _fileSet.FileNames.Count);
        }

        public void Test_Includes_All() {
            _fileSet.Includes.Add("**/*");
            AssertMatch("sub1" + Path.DirectorySeparatorChar + "sub.one");
            AssertMatch("world.peace");
            AssertMatch("world.war");
            AssertMatch("reefer.maddness");
            AssertMatch("reefer.saddness");
            AssertEquals(5, _fileSet.FileNames.Count);
        }

        public void Test_Includes_Wildcards1() {
            _fileSet.Includes.Add("world.*");
            AssertMatch("world.peace");
            AssertMatch("world.war");
            AssertEquals(2, _fileSet.FileNames.Count);
        }

        public void Test_Includes_Wildcards2() {
            _fileSet.Includes.Add("*.?addness");
            AssertMatch("reefer.maddness");
            AssertMatch("reefer.saddness");
            AssertEquals(2, _fileSet.FileNames.Count);
        }

        public void Test_Includes_Sub1() {
            _fileSet.Includes.Add("sub?/sub*");
            AssertMatch("sub1" + Path.DirectorySeparatorChar + "sub.one");
            AssertEquals(1, _fileSet.FileNames.Count);
        }

        void AssertMatch(string fileName) {
            AssertMatch(fileName, true);
        }

        void AssertMatch(string fileName, bool prefixBaseDir) {
            if (prefixBaseDir && !Path.IsPathRooted(fileName)) {
                fileName = Path.Combine(_fileSet.BaseDirectory, fileName);
            }
            Assert(fileName + " should have been in file set.", _fileSet.FileNames.IndexOf(fileName) != -1);
        }
    }
}
