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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tests {

    public class SysInfoTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0' ?>
            <project>
                <sysinfo {0}/>
            </project>";

		public SysInfoTaskTest(String name) : base(name) {
        }

        public void Test_Normal() {
            string result = RunBuild(String.Format(_format, ""));
            Assert("Task should have executed.\n" + result, result.IndexOf("[sysinfo]") != -1);
        }

        public void Test_Verbose() {
            string result = RunBuild(String.Format(_format, "verbose='true'"));
            Assert("Task should have executed.\n" + result, result.IndexOf("[sysinfo]") != -1);
        }
    }
}
