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

namespace Tests.NAnt.Core.Tasks {
	[TestFixture]
    public class PropertyTest : BuildTestBase {
		[Test]
        public void Test_PropCreate() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value not set.\n" + result, result.IndexOf("I Love you") != -1);
        }

		[Test]
        public void Test_PropReset() {
            string _xml = @"
                    <project name='PropTests'>
                        <property name='foo' value='you'/>
                        <echo message='I Love ${foo}'/>
                        <property name='foo' value='me'/>
                        <echo message='I Love ${foo}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("Property value not re-set.\n" + result, result.IndexOf("I Love me") != -1);
        }
        
        [Test]
        public void Test_ROSet() {
/*
            XmlDocument doc = new XmlDocument();
            XmlElement project = doc.CreateElement("project");
            XmlElement echo = doc.CreateElement("echo");
            XmlAttribute message = doc.CreateAttribute("message");
            message.Value=""
            project.AppendChild(

            doc.AppendChild(project);
            return doc;
*/

            string _xml = @"
                    <project name='PropTests'>
                        <property name='nant.filename' value='you'/>
                        <echo message='nant.filename=${nant.filename}'/>
                    </project>";
            string result = RunBuild(_xml);
            Assertion.Assert("RO Property value was set.\n" + result, result.IndexOf("nant.filename=you") == -1);
        }
    }
}