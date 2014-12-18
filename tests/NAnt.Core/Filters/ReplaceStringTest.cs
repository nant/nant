// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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

using NUnit.Framework;

using NAnt.Core.Filters;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the <see cref="ReplaceString" /> filter.
    /// </summary>
    [TestFixture]
    public class ReplaceStringTest : FilterTestBase {
        const string _tagName = "replacestring";

        [Test]
        public void InstantiationTest() {
            base.FilterTest(@"<" + _tagName + @" from=""cat""" + @" to=""fat cat""/>", " ", " ");
        }

        [Test]
        public void EmptyFileTest() {
            base.FilterTest(@"<" + _tagName + @" from=""cat""" + @" to=""fat cat""/>", " ", " ");
        }

        /// <summary>
        ///General Test
        /// </summary>
        [Test]
        public void ComplexTest() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" from=""cat"" to=""fat cat""/>";

            string input = @"catpublic class ProjectName {
    static void Main() 
    {

        /*
           ccccccccaaaacattcatccatcacatcattcat

        */
                        
                        
        System.catConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..cat";


            string expectedOutput = @"fat catpublic class ProjectName {
    static void Main() 
    {

        /*
           ccccccccaaaafat cattfat catcfat catcafat catfat cattfat cat

        */
                        
                        
        System.fat catConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..fat cat";

            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }


        /// <summary>
        /// Test for ignorecase
        /// </summary>
        [Test]
        public void ComplexTestCase() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" from=""CAT"" to=""UPPER CAT"" ignorecase=""true""/>";

            string input = @"catpublic class ProjectName {
    static void Main() 
    {

        /*
           ccccccccaaaacattcatccatcacatcattcat

        */
                        
                        
        System.catConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..cat";
            string expectedOutput = @"UPPER CATpublic class ProjectName {
    static void Main() 
    {

        /*
           ccccccccaaaaUPPER CATtUPPER CATcUPPER CATcaUPPER CATUPPER CATtUPPER CAT

        */
                        
                        
        System.UPPER CATConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..UPPER CAT";

            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Single character replacement
        /// </summary>
        [Test]
        public void ComplexTestSingleChar() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" from=""c"" to=""b""/>";

            string input = @"catpublic class ProjectName {
    static void Main() 
    {

        /*
           ccccccccaaaacattcatccatcacatcattcat

        */
                        
                        
        System.catConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..cat";
            string expectedOutput = @"batpublib blass ProjebtName {
    statib void Main() 
    {

        /*
           bbbbbbbbaaaabattbatbbatbabatbattbat

        */
                        
                        
        System.batConsole.WriteLine(""Hello World using C# ~13 May 2004~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
    }
}//End of file..bat";

            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test repeating replacement chars
        /// </summary>
        [Test]
        public void ComplexTestRepeating() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" from=""cat"" to=""frog""/>";

            string input = @"catcatcatcatcat";
            string expectedOutput = @"frogfrogfrogfrogfrog";

            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test replacing with null
        /// </summary>
        [Test]
        public void ComplexTestNull() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" from=""cat"" to=""""/>";

            string input = @"catcatsuccesscatcatcat";
            string expectedOutput = @"success";

            base.FilterTest(filterXml, input, expectedOutput, prologueXml);
        }
    }
}
