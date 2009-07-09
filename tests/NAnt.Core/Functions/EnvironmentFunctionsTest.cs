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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Functions {
    [TestFixture]
    public class EnvironmentFunctionsTest : BuildTestBase {
        private Project _project;

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _project = CreateEmptyProject ();
        }

        [Test]
        public void NewLine() {
            AssertExpression("environment::newline()", Environment.NewLine);
        }

        private void AssertExpression(string expression, object expectedReturnValue) {
            string value = _project.ExpandProperties("${" + expression + "}", Location.UnknownLocation);
            string expectedStringValue = Convert.ToString(expectedReturnValue, CultureInfo.InvariantCulture);

            _project.Log(Level.Debug, "expression: " + expression);
            _project.Log(Level.Debug, "value: " + value + ", expected: " + expectedStringValue);
            Assert.AreEqual(expectedStringValue, value, expression);
        }
    }
}
