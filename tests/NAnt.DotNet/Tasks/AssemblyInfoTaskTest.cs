// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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

using NUnit.Framework;

using Tests.NAnt.Core;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class AssemblyInfoTaskTest : BuildTestBase {
        private const string _buildXml = @"<?xml version='1.0'?>
            <project>
                <asminfo output='AssemblyInfo.cs' language='CSharp'>
                    <imports>
                        <import namespace='System' />
                        <import namespace='System.Reflection' />
                        <import namespace='System.Runtime.InteropServices' />
                    </imports>
                    <attributes>
                        <attribute type='ComVisibleAttribute' value='false' />
                        <attribute type='CLSCompliantAttribute' value='true' />
                        <attribute type='AssemblyVersionAttribute' value='1.0.0.0' />
                        <attribute type='AssemblyTitleAttribute' value='My fun assembly' />
                        <attribute type='AssemblyDescriptionAttribute' value='More fun than a barrel of monkeys' />
                        <attribute type='AssemblyCopyrightAttribute' value='Copyright (c) 2002, Monkeyboy, Inc.' />
                    </attributes>
                </asminfo>
            </project>";

        private const string _buildXmlTwo = @"<?xml version='1.0'?>
            <project>
                <asminfo output='AssemblyInfo.cs' language='CSharp'>
                    <imports>
                        <import namespace='System' />
                        <import namespace='System.Security' />
                    </imports>
                    <attributes>
                        <attribute type='AllowPartiallyTrustedCallersAttribute' />
                    </attributes>
                </asminfo>
            </project>";

        private const string _buildXmlError = @"<?xml version='1.0'?>
            <project>
                <asminfo output='AssemblyInfo.cs' language='CSharp'>
                    <imports>
                        <import namespace='System' />
                        <import namespace='System.Security' />
                    </imports>
                    <attributes>
                        <attribute type='AllowPartiallyTrustedCallersAttribute' value='' />
                    </attributes>
                </asminfo>
            </project>";

        private const string _buildXmlErrorTwo = @"<?xml version='1.0'?>
            <project>
                <asminfo output='AssemblyInfo.cs' language='CSharp'>
                    <imports>
                        <import namespace='System' />
                        <import namespace='System.Security' />
                    </imports>
                    <attributes>
                        <attribute type='AssemblyTitleAttribute' />
                    </attributes>
                </asminfo>
            </project>";

        /// <summary>
        /// Standard test for the AsmInfo task.
        /// </summary>
        [Test]
        public void Test_Normal() {
            RunBuild(_buildXml);
        }

        /// <summary>
        /// Tests the AsmInfo task when attribute type does not
        /// have the "value" attribute.
        /// </summary>
        [Test]
        public void attributeWithNoValueShouldWorkWhenPublicDefaultConstructorExists()
        {
            RunBuild(_buildXmlTwo);
        }

        /// <summary>
        /// Runs a AsmInfo task that is expected to fail because
        /// one of the attribute constructors does not except a
        /// string parameter.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void specifyingValueForAttrbuteWithNoConstructorTakingASingleParameterShouldCauseBuildException()
        {
            RunBuild(_buildXmlError);
        }

        /// <summary>
        /// Runs a AsmInfo task that is expected to fail because
        /// the attribute constructors does not have a default
        /// constructer with no parameters.
        /// </summary>
        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void attributeWithNoValueShouldCauseBuildExceptionWhenNoPublicDefaultConstructorExists()
        {
            RunBuild(_buildXmlErrorTwo);
        }
    }
}