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
// Jay Turpin (JayTurpin@Hotmail.Com)

using System;
using System.IO;
using System.Net;

using SourceForge.NAnt.Tasks;
using NUnit.Framework;

namespace SourceForge.NAnt.Tests {

    public class GetTaskTest : TestCase {

        string _proxy = null;

        public GetTaskTest(String name) : base(name) {
        }

        protected override void SetUp() {
        }

        protected override void TearDown() {
        }

        /// <summary>
        /// Fetch a page from a FTP site.
        /// </summary>
        /// <remarks>
        /// <c><get
        ///    src="http://www.yahoo.com/"
        ///    dest="c:\temp\yahoo.htm"
        ///    proxy="myproxy.mycompany.com:8080"
        ///    ignoreerrors="true"
        ///    verbose="true"
        ///    usetimestamp="false"/></c>
        /// </remarks>
        public void Test_GetFtpFile() {
            GetTask getTask = new GetTask();
            getTask.Project = new Project();
            getTask.Proxy = _proxy;

            string source = "ftp://ftp.info-zip.org/pub/infozip/zlib/zlib.html";
            string destination = Path.GetTempFileName() + ".html";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assert(destination + " exists, but shouldn't.", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.useTimeStamp = false;
            getTask.ignoreErrors = true;
            getTask.Verbose = true;;
            try {
                getTask.Execute();
            } catch {
                // error is expected until FTP support is added
            }

            // after FTP support is added, do the assert
            //Assert(destination + " should exist, but doesn't.", File.Exists(destination));
        }

        /// <summary>
        /// Fetch a small binary file from a web site.
        /// File will have same timestamp as on remote server.
        /// </summary>
        /// <remarks>
        /// <c><get
        ///    src="http://www.intel.com/images/intelogo.gif"
        ///    dest="c:\temp\intel_logo.gif"
        ///    proxy="myproxy.mycompany.com:8080"
        ///    ignoreerrors="true"
        ///    verbose="true"
        ///    usetimestamp="true"/></c>
        /// </remarks>
        public void Test_GetLittleFile() {
            string source = "http://nant.sourceforge.net/arrow.gif";
            string destination = Path.GetTempFileName() + ".gif";

            {
                GetTask getTask = new GetTask();
                getTask.Project = new Project();
                getTask.Proxy = _proxy;

                if (File.Exists(destination)) {
                    File.Delete(destination);
                }
                Assert(destination + " exists, but shouldn't", !File.Exists(destination));

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.useTimeStamp = true;
                getTask.ignoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert(destination + " doesn't exist, but should", File.Exists(destination));
            }

            // check for file exists using TimeStampEqual
            {
                GetTask getTask = new GetTask();
                getTask.Project = new Project();
                getTask.Proxy = _proxy;

                Assert(destination + " does not exist, but should", File.Exists(destination));

                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.useTimeStamp = true;
                getTask.ignoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert(destination + " lastModified times are different", fileDateTime.Equals(File.GetLastWriteTime(destination)));
            }

            // Test_FileExists_UseTimeStamp
            {
                GetTask getTask = new GetTask();
                getTask.Project = new Project();
                getTask.Proxy = _proxy;

                Assert(destination + " doesn't exist", File.Exists(destination));
                File.SetLastWriteTime(destination, DateTime.Parse("01/01/2000 00:00"));
                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.useTimeStamp = true;
                getTask.ignoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert(destination + " was not fetched", !fileDateTime.Equals(File.GetLastWriteTime(destination)));
            }

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
        }

        /// <summary>
        /// Fetch a large binary file from a web site.
        /// </summary>
        /// <remarks>
        /// <c><get
        ///    src="http://www.intel.com/images/intelogo.gif"
        ///    dest="c:\temp\intel_logo.gif"
        ///    proxy="myproxy.mycompany.com:8080"
        ///    ignoreerrors="true"
        ///    verbose="true"
        ///    usetimestamp="true"/></c>
        /// </remarks>
        #if false
        // This is a really slow test.
        public void Test_GetBigFile() {
            GetTask getTask = new GetTask();
            getTask.Project = new Project();
            getTask.Proxy = _proxy;

            string source = "http://www.tolvanen.com/eraser/eraser52.zip";
            string destination = Path.GetTempFileName() + ".zip";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assert(destination + " exists, but shouldn't", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.useTimeStamp = true;
            getTask.ignoreErrors = true;
            getTask.Verbose = true;;
            getTask.Execute();

            Assert(destination + " doesn't exist.", File.Exists(destination));

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assert(destination + " exists, but shouldn't.", !File.Exists(destination));
        }
        #endif

        /// <summary>
        /// Fetch a HTML page from a web site.
        /// </summary>
        /// <remarks>
        /// <c><get
        ///    src="http://www.yahoo.com/"
        ///    dest="c:\temp\yahoo.htm"
        ///    proxy="myproxy.mycompany.com:8080"
        ///    ignoreerrors="true"
        ///    verbose="true"
        ///    usetimestamp="false"/></c>
        /// </remarks>
        public void Test_GetHtmlFile() {
            GetTask getTask = new GetTask();
            getTask.Project = new Project();
            getTask.Proxy = _proxy;

            string source = "http://nant.sourceforge.net/index.html";
            string destination = Path.GetTempFileName() + ".gif";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assert(destination + " exists, but shouldn't.", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.useTimeStamp = false;
            getTask.ignoreErrors = true;
            getTask.Verbose = true;;
            getTask.Execute();

            Assert(destination + " should exist, but doesn't.", File.Exists(destination));

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assert(destination + " exists, but shouldn't.", !File.Exists(destination));
        }

        /// <summary>
        /// Test Object Accessors
        /// </summary>
        public void Test_Accessors() {

            GetTask getTask = new GetTask();
            getTask.Project = new Project();

            string proxy = _proxy;
            getTask.Proxy = proxy;
            Assert("Proxy accessor bug", getTask.Proxy == proxy);

            string source = "http://nant.sourceforge.net/arrow.gif";
            getTask.Source = source;
            Assert("Source accessor bug", getTask.Source == source);

            string destination = Path.GetTempFileName();
            getTask.Destination = destination;
            Assert("Destination accessor bug", getTask.Destination == destination);

            bool ignoreErrors = true;
            getTask.ignoreErrors = ignoreErrors;
            Assert("ignoreErrors=true accessor bug", getTask.ignoreErrors == ignoreErrors);

            ignoreErrors = false;
            getTask.ignoreErrors = ignoreErrors;
            Assert("ignoreErrors=false accessor bug", getTask.ignoreErrors == ignoreErrors);

            bool useTimeStamp = true;
            getTask.useTimeStamp = useTimeStamp;
            Assert("useTimeStamp=true accessor bug", getTask.useTimeStamp == useTimeStamp);

            useTimeStamp = false;
            getTask.useTimeStamp = useTimeStamp;
            Assert("useTimeStamp=false accessor bug", getTask.useTimeStamp == useTimeStamp);

            bool verbose = true;
            getTask.Verbose = verbose;
            Assert("Verbose=true accessor bug", getTask.Verbose == verbose);

            verbose = false;
            getTask.Verbose = verbose;
            Assert("Verbose=false accessor bug", getTask.Verbose == verbose);
        }
    }
}
