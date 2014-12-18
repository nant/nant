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
// Clayton Harbour (claytonharbour@sporadicism.com)

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace Tests.NAnt.Core.Attributes {
    [TestFixture]
    public class DateTimeValidatorAttributeTest : BuildTestBase {

        /// <summary>
        /// Test that valid dates do not throw an exception.
        /// </summary>
        [Test]
        public void Test_ValidDates() {
            Assert.IsTrue(IsValid("August 2, 1975"));
            Assert.IsTrue(IsValid("2004/01/01"));
            Assert.IsTrue(IsValid("1901/01/01"));
            Assert.IsTrue(IsValid("May 1, 2004"));
            Assert.IsTrue(IsValid("January 15, 2999"));
        }

        /// <summary>
        /// Test that invalid dates throw an exception.
        /// </summary>
        [Test]
        public void Test_InvalidDates() {
            Assert.IsFalse(IsValid("August is an awesome month."));
            Assert.IsFalse(IsValid("August 2nd is cool."));
            Assert.IsFalse(IsValid("1234567890"));
            Assert.IsFalse(IsValid("More invalid dates."));
            Assert.IsFalse(IsValid("@!#$%$^^"));
        }

        private bool IsValid (object date) {
            try {
                DateTimeValidatorAttribute validator = new DateTimeValidatorAttribute();
                validator.Validate(date);
                return true;
            } catch (ValidationException) {
                return false;
            }
        }
    }
}
