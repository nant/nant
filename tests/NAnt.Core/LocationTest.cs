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
using System.Text.RegularExpressions;

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
            Assertion.AssertNotNull(l);
            Assertion.AssertEquals(0, l.LineNumber);
            Assertion.AssertEquals(0, l.ColumnNumber);
            Assertion.AssertEquals(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_Constructor_FileNameLineColumn() {
            Location l = new Location(_tempFileName, 2, 5);
            Assertion.AssertNotNull(l);
            Assertion.AssertEquals(2, l.LineNumber);
            Assertion.AssertEquals(5, l.ColumnNumber);
            Assertion.AssertEquals(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_Constructor_UriFileName() {
            Uri uri = new Uri("file://" + _tempFileName);
            Location l = new Location(uri.ToString(), 3, 6);
            Assertion.AssertNotNull(l);
            Assertion.AssertEquals(3, l.LineNumber);
            Assertion.AssertEquals(6, l.ColumnNumber);
            Assertion.AssertEquals(_tempFileName, l.FileName);
        }

        [Test]
        public void Test_ToString() {
            // NOTE: This regular expression will fail on file systems that do not use '\' as the directory seperator.
            Assertion.AssertEquals('\\', Path.DirectorySeparatorChar);

            // This expression will extract the name, line and column from the location ToString result.
            // Created using RegEx http://www.organicbit.com/regex/
            string expression = @"(?<fileName>^.*\\[.\w]+)\((?<line>[0-9]+),(?<column>[0-9]+)\)";

            Location location = new Location(_tempFileName, 2, 5);
            Match match = Regex.Match(location.ToString(), expression);
            Assertion.Assert("match should have been successful", match.Success);
            string expected = _tempFileName + " 2 5";
            string actual = match.Result("${fileName} ${line} ${column}");
            Assertion.AssertEquals(expected, actual);
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
            Assertion.Assert(_tempFileName + " exists.", !File.Exists(_tempFileName));
        }

        #endregion Protected Instance Methods
    }
}
