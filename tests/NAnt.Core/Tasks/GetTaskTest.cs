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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Jay Turpin (JayTurpin@Hotmail.Com)

using System;
using System.IO;
using NUnit.Framework;

using NAnt.Core.Tasks;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    [Category("InetAccess")]
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
            Assert.IsFalse(File.Exists(destination), destination + " exists, but shouldn't.");

            getTask.Source = source;
            getTask.DestinationFile = new FileInfo(destination);
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
                Assert.IsFalse(File.Exists(destination), destination + " exists, but shouldn't");

                getTask.Source = source;
                getTask.DestinationFile = new FileInfo(destination);
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert.IsTrue(File.Exists(destination), destination + " doesn't exist, but should");
            }

            // check for file exists using TimeStampEqual
            {
                GetTask getTask = new GetTask();
                getTask.Project = CreateEmptyProject();
                getTask.HttpProxy = _proxy;

                Assert.IsTrue(File.Exists(destination), destination + " does not exist, but should");

                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.DestinationFile = new FileInfo(destination);
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert.IsTrue(fileDateTime.Equals(File.GetLastWriteTime(destination)),
                    destination + " lastModified times are different");
            }

            // Test_FileExists_UseTimeStamp
            {
                GetTask getTask = new GetTask();
                getTask.Project = CreateEmptyProject();
                getTask.HttpProxy = _proxy;

                Assert.IsTrue(File.Exists(destination), destination + " doesn't exist");
                File.SetLastWriteTime(destination, DateTime.Parse("01/01/2000 00:00"));
                DateTime fileDateTime = File.GetLastWriteTime(destination);

                getTask.Source = source;
                getTask.DestinationFile = new FileInfo(destination);
                getTask.UseTimeStamp = true;
                getTask.IgnoreErrors = true;
                getTask.Verbose = true;;
                getTask.Execute();

                Assert.IsFalse(fileDateTime.Equals(File.GetLastWriteTime(destination)),
                    destination + " was not fetched");
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
        [Test]
        public void Test_GetBigFile() {
            GetTask getTask = new GetTask();
            getTask.Project = CreateEmptyProject();

            string source = "http://www.tolvanen.com/eraser/eraser52.zip";
            string destination = Path.GetTempFileName() + ".zip";

            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assert.IsTrue(!File.Exists(destination), destination + " exists, but shouldn't");

            getTask.Source = source;
            getTask.DestinationFile = new FileInfo(destination);
            getTask.UseTimeStamp = true;
            getTask.Verbose = true;
            getTask.Execute();

            Assert.IsTrue(File.Exists(destination), destination + " doesn't exist.");

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }

            Assert.IsTrue(!File.Exists(destination), destination + " exists, but shouldn't.");
        }

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

            Assert.IsFalse(File.Exists(destination), destination + " exists, but shouldn't.");

            getTask.Source = source;
            getTask.DestinationFile = new FileInfo(destination);
            getTask.UseTimeStamp = false;
            getTask.IgnoreErrors = true;
            getTask.Verbose = true;;
            getTask.Execute();

            Assert.IsTrue(File.Exists(destination), destination + " should exist, but doesn't.");

            // cleanup 
            if (File.Exists(destination)) {
                File.Delete(destination);
            }
            Assert.IsFalse(File.Exists(destination), destination + " exists, but shouldn't.");
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
            Assert.IsTrue(getTask.HttpProxy == proxy, "Proxy accessor bug");

            string source = "http://nant.sourceforge.net/arrow.gif";
            getTask.Source = source;
            Assert.IsTrue(getTask.Source == source, "Source accessor bug");

            string destination = Path.GetTempFileName();
            getTask.DestinationFile = new FileInfo(destination);

            bool ignoreErrors = true;
            getTask.IgnoreErrors = ignoreErrors;
            Assert.IsTrue(getTask.IgnoreErrors == ignoreErrors, "ignoreErrors=true accessor bug");

            ignoreErrors = false;
            getTask.IgnoreErrors = ignoreErrors;
            Assert.IsTrue(getTask.IgnoreErrors == ignoreErrors, "ignoreErrors=false accessor bug");

            bool useTimeStamp = true;
            getTask.UseTimeStamp = useTimeStamp;
            Assert.IsTrue(getTask.UseTimeStamp == useTimeStamp, "useTimeStamp=true accessor bug");

            useTimeStamp = false;
            getTask.UseTimeStamp = useTimeStamp;
            Assert.IsTrue(getTask.UseTimeStamp == useTimeStamp, "useTimeStamp=false accessor bug");

            bool verbose = true;
            getTask.Verbose = verbose;
            Assert.IsTrue(getTask.Verbose == verbose, "Verbose=true accessor bug");

            verbose = false;
            getTask.Verbose = verbose;
            Assert.IsTrue(getTask.Verbose == verbose, "Verbose=false accessor bug");
        }
    }
}
