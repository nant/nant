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

using NUnit.Framework;

using NAnt.Core.Tasks;

namespace Tests.NAnt.Core.Tasks {

	[TestFixture]
    public class GetTaskTest : BuildTestBase {

        string _proxy = null;

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
        [Test]
        public void Test_GetFtpFile() {
            GetTask getTask = new GetTask();
            getTask.Project = CreateEmptyProject();
            getTask.HttpProxy = _proxy;

            string source = "ftp://ftp.info-zip.org/pub/infozip/zlib/zlib.html";
            string destination = Path.GetTempFileName() + ".html";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assertion.Assert(destination + " exists, but shouldn't.", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.UseTimeStamp = false;
            getTask.IgnoreErrors = true;
            getTask.Verbose = true;;
            try {
                getTask.Execute();
            } catch {
                // error is expected until FTP support is added
            }

            // after FTP support is added, do the assert
            //Assertion.Assert(destination + " should exist, but doesn't.", File.Exists(destination));
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
        [Test]
        public void Test_GetLittleFile() {
            string source = "http://nant.sourceforge.net/arrow.gif";
            string destination = Path.GetTempFileName() + ".gif";

            {
                GetTask getTask = new GetTask();
                getTask.Project = CreateEmptyProject();
                getTask.HttpProxy = _proxy;

                if (File.Exists(destination)) {
                    File.Delete(destination);
                }
                Assertion.Assert(destination + " exists, but shouldn't", !File.Exists(destination));

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assertion.Assert(destination + " doesn't exist, but should", File.Exists(destination));
            }

            // check for file exists using TimeStampEqual
            {
                GetTask getTask = new GetTask();
                getTask.Project = CreateEmptyProject();
                getTask.HttpProxy = _proxy;

                Assertion.Assert(destination + " does not exist, but should", File.Exists(destination));

                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assertion.Assert(destination + " lastModified times are different", fileDateTime.Equals(File.GetLastWriteTime(destination)));
            }

            // Test_FileExists_UseTimeStamp
            {
                GetTask getTask = new GetTask();
                getTask.Project = CreateEmptyProject();
                getTask.HttpProxy = _proxy;

                Assertion.Assert(destination + " doesn't exist", File.Exists(destination));
                File.SetLastWriteTime(destination, DateTime.Parse("01/01/2000 00:00"));
                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.Destination = destination;
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assertion.Assert(destination + " was not fetched", !fileDateTime.Equals(File.GetLastWriteTime(destination)));
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
        [Test]
        public void Test_GetBigFile() {
            GetTask getTask = new GetTask();
            getTask.Project = CreateEmptyProject();
            getTask.Proxy = _proxy;

            string source = "http://www.tolvanen.com/eraser/eraser52.zip";
            string destination = Path.GetTempFileName() + ".zip";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assertion.Assert(destination + " exists, but shouldn't", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.useTimeStamp = true;
            getTask.ignoreErrors = true;
            getTask.Verbose = true;;
            getTask.Execute();

            Assertion.Assert(destination + " doesn't exist.", File.Exists(destination));

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assertion.Assert(destination + " exists, but shouldn't.", !File.Exists(destination));
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
        [Test]
        public void Test_GetHtmlFile() {
            GetTask getTask = new GetTask();
            getTask.Project = CreateEmptyProject();
            getTask.HttpProxy = _proxy;

            string source = "http://nant.sourceforge.net/index.html";
            string destination = Path.GetTempFileName() + ".gif";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assertion.Assert(destination + " exists, but shouldn't.", !File.Exists(destination));

            getTask.Source = source;
            getTask.Destination = destination;
            getTask.UseTimeStamp = false;
            getTask.IgnoreErrors = true;
            getTask.Verbose = true;;
            getTask.Execute();

            Assertion.Assert(destination + " should exist, but doesn't.", File.Exists(destination));

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assertion.Assert(destination + " exists, but shouldn't.", !File.Exists(destination));
        }

        /// <summary>
        /// Test Object Accessors
        /// </summary>
        [Test]
        public void Test_Accessors() {

            GetTask getTask = new GetTask();
            getTask.Project = CreateEmptyProject();

            string proxy = _proxy;
            getTask.HttpProxy = proxy;
            Assertion.Assert("Proxy accessor bug", getTask.HttpProxy == proxy);

            string source = "http://nant.sourceforge.net/arrow.gif";
            getTask.Source = source;
            Assertion.Assert("Source accessor bug", getTask.Source == source);

            string destination = Path.GetTempFileName();
            getTask.Destination = destination;
            Assertion.Assert("Destination accessor bug", getTask.Destination == destination);

            bool ignoreErrors = true;
            getTask.IgnoreErrors = ignoreErrors;
            Assertion.Assert("ignoreErrors=true accessor bug", getTask.IgnoreErrors == ignoreErrors);

            ignoreErrors = false;
            getTask.IgnoreErrors = ignoreErrors;
            Assertion.Assert("ignoreErrors=false accessor bug", getTask.IgnoreErrors == ignoreErrors);

            bool useTimeStamp = true;
            getTask.UseTimeStamp = useTimeStamp;
            Assertion.Assert("useTimeStamp=true accessor bug", getTask.UseTimeStamp == useTimeStamp);

            useTimeStamp = false;
            getTask.UseTimeStamp = useTimeStamp;
            Assertion.Assert("useTimeStamp=false accessor bug", getTask.UseTimeStamp == useTimeStamp);

            bool verbose = true;
            getTask.Verbose = verbose;
            Assertion.Assert("Verbose=true accessor bug", getTask.Verbose == verbose);

            verbose = false;
            getTask.Verbose = verbose;
            Assertion.Assert("Verbose=false accessor bug", getTask.Verbose == verbose);
        }
    }
}
