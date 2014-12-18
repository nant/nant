// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class LocationTest {
        #region Private Instance Fields

        private string _tempFileName = null;

        #endregion Private Instance Fields

        #region Public Instance Methods

        [Test]
        public void Test_Constructor_FileName() {
            Location l = new Location(_tempFileName);
            Assert.IsNotNull(l);
            Assert.AreEqual(0, l.LineNumber);
            Assert.AreEqual(0, l.ColumnNumber);
            Assert.AreEqual(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_Constructor_FileNameLineColumn() {
            Location l = new Location(_tempFileName, 2, 5);
            Assert.IsNotNull(l);
            Assert.AreEqual(2, l.LineNumber);
            Assert.AreEqual(5, l.ColumnNumber);
            Assert.AreEqual(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_Constructor_UriFileName() {
            Uri uri = new Uri("file://" + _tempFileName);
            Location l = new Location(uri.ToString(), 3, 6);
            Assert.IsNotNull(l);
            Assert.AreEqual(3, l.LineNumber);
            Assert.AreEqual(6, l.ColumnNumber);
            Assert.AreEqual(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_ToString() {
            Location location = new Location(_tempFileName, 2, 5);
            string expected = _tempFileName + "(2,5):";
            string actual = location.ToString();
            Assert.AreEqual(expected, actual);
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        [SetUp]
        protected void SetUp() {
            _tempFileName = TempFile.Create();
        }

        [TearDown]
        protected void TearDown() {
            File.Delete(_tempFileName);
            Assert.IsFalse(File.Exists(_tempFileName), _tempFileName + " exists.");
        }

        #endregion Protected Instance Methods
    }
}
