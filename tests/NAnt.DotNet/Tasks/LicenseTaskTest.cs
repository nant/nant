// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using NUnit.Framework;

using NAnt.Core;

using NAnt.DotNet.Tasks;
using Tests.NAnt.Core;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class LicenseTaskTest : BuildTestBase {
        [Test]
        public void Test_Serializable() {
            Project project = CreateEmptyProject();
            LicenseTask lt = new LicenseTask();
            lt.Parent = project;
            lt.Project = project;
            lt.Assemblies.Parent = project;
            lt.NamespaceManager = project.NamespaceManager;

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, lt);
        }
    }
}
