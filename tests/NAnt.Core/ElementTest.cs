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

// Tomas Restrepo (tomasr@mvps.org)


using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;

using SourceForge.NAnt.Attributes;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Types;

namespace SourceForge.NAnt.Tests {

    /// <summary>A simple task with a null element to test failures.</summary>
    [TaskName("elementTest1")]
    class ElementTest1Task : Task {

        [FileSet("fileset")]
        public FileSet FileSet {
            get { return null; } // we'll test for null later!
        }
        protected override void ExecuteTask() { 
        }


    }

	[TestFixture]
    public class ElementTest : BuildTestBase {

        
        /// <summary>
        /// Test that a read-only property with an element doesn't 
        /// return null when the getter is invoked
        /// </summary>
        [Test]
        public void Test_Null_Element_Prop_Value() {
            const string build = @"<?xml version='1.0' ?>
               <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1>
                           <fileset><includes name='*.cs'/></fileset>
                        </elementTest1>
                     </target>
                  </project>";


            try {
               string result = RunBuild(build);
               Assertion.Fail("Null property value allowed.\n" + result);
            } catch ( TestBuildException e ) {
                if (!(e.InnerException is BuildException)) {
                    Assertion.Fail("Unexpected exception thrown.\n" + e.ToString());
                }
            } catch ( Exception e ) {
               Assertion.Fail("Unexpected exception thrown.\n" + e.ToString());
            }
        }

    }
}
