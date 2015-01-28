// NAnt - A .NET build tool
// Copyright (C) 2001-2015 Gerry Shaw
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using NAnt.Core;
using NUnit.Framework;

namespace Tests.NAnt.Core
{
    [TestFixture]
    public class PropertyDictionaryTest : BuildTestBase
    {
        /// <summary>
        /// Tests to ensure that null values cannot be added to the
        /// PropertyDictionary.
        /// </summary>
        [Test]
        public void Test_NullPropertyTest()
        {
            const string xml = "<project><property name='temp.var' value='some.value'/></project>";
            Project p = CreateFilebasedProject(xml);
            PropertyDictionary d = new PropertyDictionary(p);
            TestDelegate assn = delegate() { d["temp.var"] = null; };

            Assert.Throws<BuildException>(assn,
                "Null values should not be allowed in PropertyDictionary");
        }
    }
}
