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
using System.Reflection;
using NUnit.Framework;

namespace NAnt.DotNetTasks.Tests {

    // This class bundles all our tests into a single suite.  If you wanted
    // you could create other suites with a sub set of the tests.  All that is
    // required is a property called Suite that returns a ITest object.  The
    // ITest object most commonly returned is a TestSuite.  For single class
    // tests this member can be included within the TestCase.
    public class AllTests {

        public static ITest Suite {
            get  {
                // Force the loading of the correct NAnt.Core assembly to test against.
                // If the following lines are not used then the NAnt.Core that is
                // running the build will be tested against - not the just built NAnt.Core.dll
                // This was a major source of grief.
                Assembly assembly = Assembly.GetExecutingAssembly(); // build/NAnt.Tests.dll
                string path = Path.GetDirectoryName(assembly.Location);

                // Use reflection to automagically scan all the classes that 
                // inherit from TestCase and add them to the suite.
                TestSuite suite = new TestSuite("NAnt Tests");
                foreach(Type type in assembly.GetTypes()) {
                    if (type.IsSubclassOf(typeof(TestCase)) && !type.IsAbstract) {
                        //if (type.Name == "TargetTest") 
                        suite.AddTestSuite(type);
                    }
                }
                return suite;
            }
        }
    }
}
