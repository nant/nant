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
using System.Xml;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class BuildFilesInResourcesTest {
        #region Public Instance Methods

        [SetUp]
        public void SetUp() {
            _tempFolder = Path.Combine (Path.GetTempPath (),
                "Tests.NAnt.Core.BuildFilesInResourcesTest");
            if (!Directory.Exists (_tempFolder))
                Directory.CreateDirectory (_tempFolder);
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists (_tempFolder))
                Directory.Delete (_tempFolder, true);
        }

        [Test]
        public void Test_FilesInResources() {
            string buildFile = Path.Combine (_tempFolder, "default.build");

            foreach (string resName in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                if (!resName.StartsWith("XML_.Build.Files")) {
                    continue;
                }

                using (FileStream fs = File.Open (buildFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {
                    byte[] buffer = new byte[8192];

                    Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
                    while (true) {
                        int bytesRead = rs.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            break;
                        }
                        fs.Write(buffer, 0, bytesRead);
                    }
                }

                bool expectSuccess = (resName.IndexOf(".Valid.") > 0);

                try {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(buildFile);
                    Project p = new Project(doc, Level.Info, 0);
                    string output = BuildTestBase.ExecuteProject(p);
                    Assert.IsTrue (expectSuccess, "#1: " + resName + " " + output);
                } catch (Exception ex) {
                    Assert.IsFalse (expectSuccess, "#2: " +resName + " " + ex.ToString());
                }
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private string _tempFolder;

        #endregion Private Instance Fields
    }
}
