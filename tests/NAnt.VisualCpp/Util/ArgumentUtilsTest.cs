// NAnt - A .NET build tool
// Copyright (C) 2005 Gert Driesen (gert.driesen@ardatis.com)
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml; 

using NUnit.Framework;

using NAnt.VisualCpp.Util;

namespace Tests.NAnt.VisualCpp.Util {
    [TestFixture]
    public class ArgumentUtilsTest {
        #region Public Instance Methods

        [Test]
        public void Test_DuplicateTrailingBackSlash() {
            Assert.AreEqual(string.Empty, ArgumentUtils.DuplicateTrailingBackSlash(string.Empty), "#1");
            Assert.AreEqual("a", ArgumentUtils.DuplicateTrailingBackSlash("a"), "#2");
            Assert.AreEqual("a\\\\", ArgumentUtils.DuplicateTrailingBackSlash("a\\"), "#3");
            Assert.AreEqual("\"a\\\\\"", ArgumentUtils.DuplicateTrailingBackSlash("\"a\\\""), "#4");
            Assert.AreEqual("a\\\"", ArgumentUtils.DuplicateTrailingBackSlash("a\\\""), "#5");
        }

        [Test]
        public void Test_FixTrailingBackSlash() {
            Assert.AreEqual(string.Empty, ArgumentUtils.FixTrailingBackSlash(string.Empty), "#1");
            Assert.AreEqual("a", ArgumentUtils.FixTrailingBackSlash("a"), "#2");
            Assert.AreEqual("a", ArgumentUtils.FixTrailingBackSlash("a\\"), "#3");
            Assert.AreEqual("a\\", ArgumentUtils.FixTrailingBackSlash("a\\\\"), "#4");
            Assert.AreEqual("\"a\\\"", ArgumentUtils.FixTrailingBackSlash("\"a\\\""), "#5");
            Assert.AreEqual("a\\\"", ArgumentUtils.FixTrailingBackSlash("a\\\""), "#6");
        }

        [Test]
        public void Test_CleanTrailingBackSlash() {
            Assert.AreEqual(string.Empty, ArgumentUtils.CleanTrailingBackSlash(string.Empty), "#1");
            Assert.AreEqual("a", ArgumentUtils.CleanTrailingBackSlash("a"), "#2");
            Assert.AreEqual("a", ArgumentUtils.CleanTrailingBackSlash("a\\"), "#3");
            Assert.AreEqual("a", ArgumentUtils.CleanTrailingBackSlash("a\\\\"), "#4");
            Assert.AreEqual("\"a\\\"", ArgumentUtils.CleanTrailingBackSlash("\"a\\\""), "#5");
            Assert.AreEqual("a\\\"", ArgumentUtils.CleanTrailingBackSlash("a\\\""), "#6");
        }

        #endregion Public Instance Methods
    }
}

