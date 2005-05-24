// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class BuildFilesInResourcesTest {
        #region Public Instance Methods

        [Test]
        public void Test_FilesInResources() {
            foreach (string resName in Assembly.GetExecutingAssembly().GetManifestResourceNames()){
                if (resName.StartsWith("XML_.Build.Files")) {
                    TextReader file = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resName));
                    bool success = false;                    
                    string stuff = null;

                    try {
                        System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                        doc.LoadXml(file.ReadToEnd());
                        Project p = new Project(doc, Level.Info, 0);
                        stuff = BuildTestBase.ExecuteProject(p);
                        success = true;
                    } catch(Exception){
                    } finally {
                        if (resName.IndexOf(".Invalid.") > 0){
                            if(!success) stuff = "expected a failure:" + stuff;
                            success = !success;
                        }

                        Assert.IsTrue(success, resName + " " + stuff);
                    }
                }
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        [SetUp]
        protected void SetUp() {
        }

        [TearDown]
        protected void TearDown() {
        }

        #endregion Protected Instance Methods
    }
}
