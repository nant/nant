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

    public class TStampTaskTest : BuildTestBase {

        const string _format = @"<?xml version='1.0' ?>
            <project>
                <tstamp {0}>{1}</tstamp>
            </project>";

		public TStampTaskTest(String name) : base(name) {
        }

        public void Test_Normal() {
            string result = RunBuild(String.Format(_format, "", ""));
            Assert("Task should have executed.\n" + result, result.IndexOf("[tstamp]") != -1);
        }

        public void Test_Custom() {
            string result = RunBuild(String.Format(_format, " verbose='true' property='build.date' pattern='yyyy-MM-DDTHH:mm:ss zzz'", ""));
            Assert("Task should have executed.\n" + result, result.IndexOf("[tstamp]") != -1);
            Assert("build.date property should have been set.\n" + result, result.IndexOf("build.date") != -1);
        }

        public void Test_NoVerbose() {
            string result = RunBuild(String.Format(_format, "property='build.date' pattern='yyyy-MM-DDTHH:mm:ss zzz'", ""));
            Assert("Task should have executed.\n" + result, result.IndexOf("[tstamp]") != -1);
            Assert("build.date property should not have been printed to log.\n" + result, result.IndexOf("build.date") == -1);
        }

        public void Test_Formatter() {
            string result = RunBuild(String.Format(_format, "verbose='true'", "<formatter property='TODAY' pattern='dd MMM yyyy'/><formatter property='DSTAMP' pattern='yyyyMMdd'/><formatter property='TSTAMP' pattern='HHmm'/>"));
            Assert("Task should have executed.\n" + result, result.IndexOf("[tstamp]") != -1);
        }
    }
}


/* TODO:

<tstamp property="build.date" pattern="yyyyMMdd"/>

  [tstamp] Monday, March 4, 2002 11:31pm

<!--
    by default the long locale version of the date and time are displayed
    verbose="true" causes the properties being set to be displayed
-->

<tstamp property="build.date" pattern="yyyyMMdd" verbose="true"/>

  [tstamp] Monday, March 4, 2002 11:31pm
  [tstamp] build.date = 20020305

<!-- for ant like compatiability -->
<tstamp verbose="true">
    <format property="TODAY" pattern="dd mmm yyyy"/>
    <format property="DSTAMP" pattern="yyyyMMdd"/>
    <format property="TSTAMP" pattern="HHmm"/>
</tstamp>

  [tstamp] Monday, March 4, 2002 11:31pm
  [tstamp] TODAY = 5 Mar 2002
  [tstamp] DSTAMP = 20020305
  [tstamp] TSTAMP = 2331

*/