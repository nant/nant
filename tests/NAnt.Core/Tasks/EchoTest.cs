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
using System.Xml;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// Tests the Echo test.
    /// </summary>
    [TestFixture]
    public class EchoTest : BuildTestBase {
    
    	[SetUp]
    	protected override void SetUp() {
    		base.SetUp();
    	}
        
        [Test]
        public void Test_Echo() {
            //TODO: Use this once we have fixed the LocationMap issue
            /*
            XmlDocument doc = new XmlDocument();
            XmlElement project = doc.CreateElement("project");
            XmlElement echo = doc.CreateElement("echo");
            echo.SetAttribute("message","Go Away!");
            project.AppendChild(echo);
            doc.AppendChild(project);

            Project p = new Project(doc);
            
            string result = ExecuteProject(p);
            */
            string _xml = @"
                    <project>
                        <echo message='Go Away!'/>
                    </project>";
            string result = RunBuild(_xml);            
            Assertion.Assert("Echo message missing:" + result, result.IndexOf("Go Away!") != -1);
        }
    }
}
