// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez (ScottHernandez@hotmail.com)
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

// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tests {

	[TestFixture]
    public class IfTest : BuildTestBase {

		[Test]
        public void Test_IF_PropExists_Positive() {
            string _xml = @"
                    <project>
                        <property name='line' value='hi'/>
                        <if propertyexists='line'>
                            <echo message='line=${line}'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("line=hi") != -1);
        }

		[Test]
        public void Test_IF_PropExists_Negative() {
            string _xml = @"
                    <project>
                        <if propertyexists='line'>
                            <echo message='line=${line}'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("line=") == -1);
        }

		[Test]
        public void Test_IF_PropTrue_Positive() {
            string _xml = @"
                    <project>
                        <property name='myProp' value='true'/>
                        <if propertytrue='myProp'>
                            <echo message='Hello'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("Hello") != -1);
        }

		[Test]
        public void Test_IF_PropTrue_Negative() {
            string _xml = @"
                    <project>
                        <property name='myProp' value='false'/>
                        <if propertytrue='myProp'>
                            <echo message='Hello'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("Hello") == -1);
        }
        
        [Test]
        public void Test_IF_Target_Positive() {
            string _xml = @"
                    <project>
                        <target name='callme'>
                            <echo message='called'/>
                        </target>
                        <if targetexists='callme'>
                            <call target='callme'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("called") != -1);
        }

		[Test]
        public void Test_IF_Target_Negative() {
            string _xml = @"
                    <project>
                        <if targetexists='myProp'>
                            <call target='callme'/>
                        </if>
                    </project>";
            string result = RunBuild(_xml);
            //Log.WriteLine(result);
            Assertion.Assert(result.IndexOf("failed") == -1);
        }
    }
}