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

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Attributes;

using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Attributes {
    [TestFixture]
    public class StringValidatorAttributeTest : BuildTestBase {

        /// <summary>
        /// Test that valid dates do not throw an exception.
        /// </summary>
        [Test]
        public void Test_ValidStrings() {
            Assertion.Assert(IsValid("August 2, 1975"));
            Assertion.Assert(IsValid("blah"));
            Assertion.Assert(IsValid("n", false));
            Assertion.Assert(IsValid("http://nant.sourceforge.net", 
                @"(?<Protocol>\w+):\/\/(?<Domain>[\w.]+\/?)\S*"));

            // validate name of file
            Assertion.Assert(IsValid("name_of_file",
                @"^[A-Za-z0-9][A-Za-z0-9._\-]*$"));
        }

        /// <summary>
        /// Test that invalid dates throw an exception.
        /// </summary>
        [Test]
        public void Test_InvalidStrings() {
            Assertion.Assert(!IsValid("the/path/to/a/file",
                @"^[A-Za-z0-9][A-Za-z0-9._\-]*$"));
            Assertion.Assert(!IsValid("", false));
            Assertion.Assert(!IsValid("blah blah",
                @"(?<Protocol>\w+):\/\/(?<Domain>[\w.]+\/?)\S*"));
        }

        private bool IsValid (object value) {
            return this.IsValid(true, null);
        }

        private bool IsValid (object value, bool allowEmpty) {
            return this.IsValid(value, allowEmpty, null);
        }

        private bool IsValid (object value, string expression) {
            return this.IsValid(value, true, expression);
        }

        private bool IsValid (object value, bool allowEmpty, string expression) {
            try {
                StringValidatorAttribute validator = 
                    new StringValidatorAttribute();
                validator.AllowEmpty = allowEmpty;
                validator.Expression = expression;
                validator.Validate(value);
                return true;
            } catch (ValidationException) {
                return false;
                //throw;
            }
        }

    }
}
