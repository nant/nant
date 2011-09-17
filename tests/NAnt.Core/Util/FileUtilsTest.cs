// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Util;

namespace Tests.NAnt.Core.Util {
    [TestFixture]
    public class FileUtilsTest {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_GetFullPath_Null() {
            FileUtils.GetFullPath(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_GetFullPath_Empty() {
            FileUtils.GetFullPath(string.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_GetFullPath_Whitespace() {
            FileUtils.GetFullPath(" ");
        }

        [Test]
        public void Test_GetFullPath() {
            if (!PlatformHelper.IsWin32) {
                return;
            }

            Assert.IsTrue(FileUtils.GetFullPath("Z:").StartsWith(@"Z:\"), "#1");
            Assert.AreEqual(@"c:\abc\def", FileUtils.GetFullPath (@"c:\abc\def"), "#2");
            Assert.IsTrue(FileUtils.GetFullPath(@"\").EndsWith(@"\"), "#3");
            Assert.IsTrue(FileUtils.GetFullPath("/").EndsWith (@"\"), "#4");
            Assert.IsTrue(FileUtils.GetFullPath("readme.txt").EndsWith(@"\readme.txt"), "#5");
            Assert.IsTrue(FileUtils.GetFullPath("c").EndsWith(@"\c"), "#5");
            Assert.IsTrue(FileUtils.GetFullPath(@"abc\def").EndsWith(@"abc\def"), "#6");
            Assert.IsTrue(FileUtils.GetFullPath(@"\abc\def").EndsWith(@"\abc\def"), "#7");
            Assert.AreEqual(@"\\abc\def", FileUtils.GetFullPath (@"\\abc\def"), "#8");
            Assert.AreEqual(Directory.GetCurrentDirectory() + @"\abc\def", FileUtils.GetFullPath(@"abc//def"), "#9");
            Assert.AreEqual(Directory.GetCurrentDirectory().Substring(0,2) + @"\abc\def", FileUtils.GetFullPath("/abc/def"), "#10");
            Assert.AreEqual(@"\\abc\def", FileUtils.GetFullPath("//abc/def"), "#11");

            StringBuilder sb = new StringBuilder();
            while (sb.Length < 260) {
                sb.Append(@"test\..\");
            }
            sb.Append("what.txt");

            Assert.AreEqual(Path.Combine(Directory.GetCurrentDirectory(), "what.txt"), 
                FileUtils.GetFullPath(sb.ToString()), "#12");

            // clear buffer
            sb.Length = 0;

            string[] currentDirParts = Directory.GetCurrentDirectory().Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < (currentDirParts.Length - 1); i++) {
                sb.Append(@"..\");
            }
            sb.Append(@"test\what.txt");

            Assert.AreEqual(Path.Combine(currentDirParts[0] + Path.DirectorySeparatorChar, @"test\what.txt"), 
                FileUtils.GetFullPath(sb.ToString()), "#13");

            // filter out single dot parts
            Assert.AreEqual(Directory.GetCurrentDirectory() + @"\abc\def", FileUtils.GetFullPath(@"abc/./def"), "#14");
            Assert.AreEqual(Directory.GetCurrentDirectory() + @"\abc\def", FileUtils.GetFullPath(@"abc\.\def"), "#15");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_CombinePaths_Path1_Null() {
            FileUtils.CombinePaths((string) null, "whatever");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_CombinePaths_Path2_Null() {
            FileUtils.CombinePaths("whatever", (string) null);
        }

        [Test]
        public void Test_CombinePaths() {
            if (!PlatformHelper.IsWin32) {
                return;
            }

            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test", @"whatever\test.txt"), "#1");
            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test", "whatever/test.txt"), "#2");

            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test\whatever", @"..\whatever\test.txt"), "#3");
            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test\whatever", "../whatever/test.txt"), "#4");

            Assert.AreEqual(@"\\test\c$\whatever.txt", FileUtils.CombinePaths(@"\\test\c$", @"test\..\whatever.txt"), "#5");
            Assert.AreEqual(@"\\test\c$\whatever.txt", FileUtils.CombinePaths(@"\\test\c$", "test/../whatever.txt"), "#6");

            Assert.AreEqual(@"c:\test\whatever\obj", FileUtils.CombinePaths(@"c:\test", @"whatever\obj"), "#7");
            Assert.AreEqual(@"c:\test\whatever\obj", FileUtils.CombinePaths(@"c:\test", @"whatever/obj"), "#8");

            // retain trailing separator character
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever\obj\"), "#7");
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever\obj/"), "#8");

            // ignore duplicate directory separators
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever\obj\/"), "#8");
            Assert.AreEqual(@"c:\test\whatever\obj", FileUtils.CombinePaths(@"c:\test", @"whatever\/obj"), "#8");
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever\\obj\\"), "#9");
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever\\\obj\\\"), "#9");
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever//obj//"), "#10");
            Assert.AreEqual(@"c:\test\whatever\obj\", FileUtils.CombinePaths(@"c:\test", @"whatever///obj///"), "#10");

            // if second path is rooted, it should be returned as is
            Assert.AreEqual(@"c:\whatever\..\test", FileUtils.CombinePaths(@"c:\test", @"c:\whatever\..\test"), "#11");
            Assert.AreEqual(@"c:\whatever\..\test\\", FileUtils.CombinePaths(@"c:\test", @"c:\whatever\..\test\\"), "#12");
            Assert.AreEqual(@"\\server\c$\test.txt", FileUtils.CombinePaths(@"c:\test", @"\\server\c$\test.txt"), "#13");

            // skip single dot parts
            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test", @"whatever\.\test.txt"), "#14");
            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test", @"whatever/./test.txt"), "#15");

            // trailing path seperators
            Assert.AreEqual(@"c:\test\whatever\test.txt", FileUtils.CombinePaths(@"c:\test\whatever\", @"..\whatever\test.txt"), "#16");
            Assert.AreEqual(@"c:\whatever\test.txt", FileUtils.CombinePaths(@"c:\", @"whatever\test.txt"), "#17");
            Assert.AreEqual(@"c:\..\whatever\test.txt", FileUtils.CombinePaths(@"c:\", @"..\whatever\test.txt"), "#18");

            // handle zero-length paths and paths containing only directoy separator char
            Assert.AreEqual(@"..\whatever\test.txt", FileUtils.CombinePaths(@"", @"..\whatever\test.txt"), "#19");
            Assert.AreEqual(@"\..\whatever\test.txt", FileUtils.CombinePaths(@"", @"\..\whatever\test.txt"), "#20");
            Assert.AreEqual(@"c:\test\whatever", FileUtils.CombinePaths(@"c:\test\whatever", @""), "#21");
            Assert.AreEqual(@"\", FileUtils.CombinePaths(@"c:\test\whatever", @"\"), "#22");
            Assert.AreEqual(@"\..\whatever\test.txt", FileUtils.CombinePaths(@"\", @"..\whatever\test.txt"), "#23");
            Assert.AreEqual(@"\..\whatever\test.txt", FileUtils.CombinePaths(@"\", @"\..\whatever\test.txt"), "#24");

            Assert.AreEqual(@"c:\whatever\test.txt", FileUtils.CombinePaths(@"c:\test", @"..\whatever\test.txt"), "#25");
            Assert.AreEqual(@"c:\whatever\test.txt", FileUtils.CombinePaths(@"c:\test\", "../whatever/test.txt"), "#26");
        }
    }
}
