// NAnt - A .NET build tool
// Copyright (C) 2002 Ian MacLean (Ian_maclean@another.com)
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

// Ian MacLean (Ian_maclean@another.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// <para>Load Tasks Test.</para>
    /// </summary>
    [TestFixture]
    public class LoadTasksTest : BuildTestBase {
    
        [SetUp]
        protected override void SetUp() {
            base.SetUp();
        }
        
        [Test]
        public void Test_FoundTask() {
        
        }
        
        [Test]
        public void Test_AssemblyNotExists() {
            string _xml = @"
                    <project name='foo'>
                        <loadtasks assembly='foo.dll'  />
                    </project>";
            try {                
                string result = RunBuild(_xml);
                Assertion.Fail("Invalid assembly path did not generate an exception");
            }
            catch(TestBuildException be) { 
                if( be.InnerException.Message.IndexOf("'does not exist") != -1) {
                    Assertion.Fail("Wrong type of exception; does not contain words 'does not exist'!\n " + be.ToString()); 
                }
            }
            catch {
                Assertion.Fail("Incorrect exception type !");
            }
        }
        public void Test_IncorrectArgs() {
            string _xml = @"
            <project name='foo'>
                <loadtasks  path ='c:\cvs\NAntContrib\build' assembly='foo.dll'  />
            </project>";
            try {                
                string result = RunBuild(_xml);
                Assertion.Fail("Invalid attribute combination did not generate an exception");
            }
            catch(TestBuildException e) {
                if(!(e.InnerException is BuildException))
                    Assertion.Fail("Incorrect exception type !");
            }
            catch {
                Assertion.Fail("Incorrect exception type !");
            }
        }
   }
}