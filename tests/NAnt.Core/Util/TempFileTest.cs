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
using System.Globalization;
using System.IO;

using NUnit.Framework;

namespace Tests.NAnt.Core.Util {
    [TestFixture]
    public class TempFileTest {
        [Test]
        public void Test_Create() {
            string fileName = TempFile.Create();
            Assert.IsTrue(File.Exists(fileName), fileName + " does not exists.");

            try {
                TimeSpan diff = DateTime.Now - File.GetCreationTime(fileName);
                Assert.IsTrue(diff.TotalSeconds < 10.0, "Creation time should be less than 10 seconds ago.");
            } finally {
                File.Delete(fileName);
                Assert.IsFalse(File.Exists(fileName), fileName + " exists.");
            }
        }

        [Test]
        public void Test_Create_NullArgument() {
            try {
                TempFile.Create(null);
                Assert.Fail("Exception not thrown.");
            } catch {
            }
        }

        [Test]
        public void Test_Create_WithContents() {
            string expected = string.Format(CultureInfo.InvariantCulture, "Line 1{0}Line Two{0}{0}Line Three", Environment.NewLine);
            string fileName = TempFile.CreateWithContents(expected);

            try {
                string actual = TempFile.Read(fileName);
                Assert.AreEqual(expected, actual);
            } finally {
                // delete the temp file
                File.Delete(fileName);
                Assert.IsFalse(File.Exists(fileName), fileName + " exists.");
            }
        }
    }
}
