// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class FrameworkInfoTest : BuildTestBase {
        [Test]
        public void Serialization_Valid() {
            Project p = CreateEmptyProject();
            
            FrameworkInfoDictionary frameworks = p.Frameworks;
            BinaryFormatter bf = new BinaryFormatter();

            foreach (FrameworkInfo framework in frameworks) {
                if (!framework.IsValid)
                    continue;

                MemoryStream ms = new MemoryStream();

                bf.Serialize(ms, framework);
                ms.Position = 0;
                FrameworkInfo df = (FrameworkInfo) bf.Deserialize(ms);

                Assert.IsNotNull(framework.ClrVersion, "#1");
                Assert.AreEqual(framework.ClrVersion, df.ClrVersion, "#2");
                Assert.IsNotNull(framework.Description, "#3");
                Assert.AreEqual(framework.Description, df.Description, "#4");
                Assert.IsNotNull(framework.Family, "#5");
                Assert.AreEqual(framework.Family, df.Family, "#6");
                Assert.IsNotNull(framework.FrameworkAssemblyDirectory, "#7");
                Assert.IsNotNull(df.FrameworkAssemblyDirectory, "#8");
                Assert.AreEqual(framework.FrameworkAssemblyDirectory.FullName,
                    df.FrameworkAssemblyDirectory.FullName, "#9");
                Assert.IsNotNull(framework.FrameworkDirectory, "#10");
                Assert.IsNotNull(df.FrameworkDirectory, "#11");
                Assert.AreEqual(framework.FrameworkDirectory.FullName,
                    df.FrameworkDirectory.FullName, "#12");
                Assert.IsNotNull(framework.Name, "#13");
                Assert.AreEqual(framework.Name, df.Name, "#14");
                Assert.IsNotNull(framework.Project, "#15");
                Assert.IsNotNull(df.Project, "#16");

                if (framework.SdkDirectory != null) {
                    Assert.IsNotNull(df.SdkDirectory, "#18");
                    Assert.AreEqual(framework.SdkDirectory.FullName,
                        df.SdkDirectory.FullName, "#19");
                } else {
                    Assert.IsNull(df.SdkDirectory, "#18");
                }

                Assert.IsNotNull(framework.TaskAssemblies, "#20");
                Assert.IsNotNull(df.TaskAssemblies, "#21");
                Assert.IsNotNull(framework.TaskAssemblies.BaseDirectory, "#22");
                Assert.IsNotNull(df.TaskAssemblies.BaseDirectory, "#23");
                Assert.AreEqual(framework.TaskAssemblies.BaseDirectory.FullName,
                    df.TaskAssemblies.BaseDirectory.FullName, "#24");
                Assert.AreEqual(framework.TaskAssemblies.FileNames.Count,
                    df.TaskAssemblies.FileNames.Count, "#25");

                Assert.IsNotNull(framework.Version, "#26");
                Assert.AreEqual(framework.Version, df.Version, "#27");
            }
        }

        [Test]
        public void Serialization_Invalid() {
            Project p = CreateEmptyProject();
            
            FrameworkInfoDictionary frameworks = p.Frameworks;
            BinaryFormatter bf = new BinaryFormatter();

            foreach (FrameworkInfo framework in frameworks) {
                if (framework.IsValid)
                    continue;

                MemoryStream ms = new MemoryStream();

                bf.Serialize(ms, framework);
                ms.Position = 0;
                FrameworkInfo df = (FrameworkInfo) bf.Deserialize(ms);

                Assert.IsNotNull(framework.Description, "#A1");
                Assert.AreEqual(framework.Description, df.Description, "#A2");
                Assert.IsNotNull(framework.Family, "#A3");
                Assert.AreEqual(framework.Family, df.Family, "#A4");
                Assert.IsNotNull(framework.Name, "#A5");
                Assert.AreEqual(framework.Name, df.Name, "#A6");
                Assert.IsNotNull(framework.ClrVersion, "#A7");
                Assert.AreEqual(framework.ClrVersion, df.ClrVersion, "#A8");
                Assert.IsNotNull(framework.Version, "#A7");
                Assert.AreEqual(framework.Version, df.Version, "#A8");
                Assert.IsTrue (Enum.IsDefined(typeof(ClrType), framework.ClrType), "#A9");
                Assert.AreEqual(framework.ClrType, df.ClrType, "#A10");
                Assert.IsNotNull(framework.VisualStudioVersion, "#A11");

                try {
                    object x = df.FrameworkAssemblyDirectory;
                    Assert.Fail ("#C1:" + x);
                } catch (ArgumentException ex) {
                    // The current framework is not valid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#C2");
                    Assert.IsNull(ex.InnerException, "#C3");
                    Assert.IsNotNull(ex.Message, "#C4");
                    Assert.IsNull(ex.ParamName, "#C5");
                }

                try {
                    object x = df.FrameworkDirectory;
                    Assert.Fail ("#D1:" + x);
                } catch (ArgumentException ex) {
                    // The current framework is not valid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#D2");
                    Assert.IsNull(ex.InnerException, "#D3");
                    Assert.IsNotNull(ex.Message, "#D4");
                    Assert.IsNull(ex.ParamName, "#D5");
                }

                try {
                    object x = df.Project;
                    Assert.Fail ("#E1:" + x);
                } catch (ArgumentException ex) {
                    // The current framework is not valid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#E2");
                    Assert.IsNull(ex.InnerException, "#E3");
                    Assert.IsNotNull(ex.Message, "#E4");
                    Assert.IsNull(ex.ParamName, "#E5");
                }

                try {
                    object x = df.SdkDirectory;
                    Assert.Fail ("#F1" + x);
                } catch (ArgumentException ex) {
                    // The current framework is not valid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#F2");
                    Assert.IsNull(ex.InnerException, "#F3");
                    Assert.IsNotNull(ex.Message, "#F4");
                    Assert.IsNull(ex.ParamName, "#F5");
                }

                try {
                    object x = df.TaskAssemblies;
                    Assert.Fail ("#G1" + x);
                } catch (ArgumentException ex) {
                    // The current framework is not valid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#G2");
                    Assert.IsNull(ex.InnerException, "#G3");
                    Assert.IsNotNull(ex.Message, "#G4");
                    Assert.IsNull(ex.ParamName, "#G5");
                }
            }
        }

        [Test]
        public void Invalid_SDK() {
            const string xml = @"<?xml version=""1.0"" ?>
                <project>
                    <property name=""nant.settings.currentframework"" value=""testnet-1.0"" />
                    <property name=""gacutil.tool"" value=""${framework::get-tool-path('gacutil.exe')}"" />
                    <fail unless=""${file::exists(gacutil.tool)}"" />
                    <echo>${gacutil.tool}</echo>
                </project>";

            XmlDocument configDoc = new XmlDocument ();
            using (Stream cs = Assembly.GetExecutingAssembly().GetManifestResourceStream("NAnt.Core.Tests.Framework.config")) {
                configDoc.Load (cs);
            }

            Project project = CreateFilebasedProject(xml, Level.Info,
                configDoc.DocumentElement);
            FrameworkInfo tf = project.Frameworks ["testnet-1.0"];
            if (!tf.IsValid) {
                Assert.Ignore(tf.Description + " is not available.");
            }

            Assert.IsNull(tf.SdkDirectory, "#1");
            ExecuteProject(project);
        }
    }
}
