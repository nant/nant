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

// Jay Turpin (jayturpin@hotmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Net;
using SourceForge.NAnt.Attributes;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Get a particular file from a URL source.</summary>
    /// <remarks>
    ///   <para>Options include verbose reporting, timestamp based fetches and controlling actions on failures.</para>
    ///   <para>Currently, only HTTP and UNC protocols are supported. FTP support may be added when more pluggable protocols are added to the System.Net assembly.</para>
    ///   <para>The <c>useTimeStamp</c> option enables you to control downloads so that the remote file is only fetched if newer than the local copy. If there is no local copy, the download always takes place. When a file is downloaded, the timestamp of the downloaded file is set to the remote timestamp.</para>
    ///   <note>This timestamp facility only works on downloads using the HTTP protocol.</note>
    /// </remarks>
    /// <example>
    ///   <para>Gets the index page of the NAnt home page, and stores it in the file help/index.html.</para>
    ///   <code>&lt;get src="http://nant.sourceforge.org/" dest="help/index.html"/&gt;</code>
    /// </example>
    [TaskName("get")]
    public class GetTask : Task {

        string _src = null;
        string _dest = null;
        string _proxy = null;
        bool _ignoreErrors = false;
        bool _useTimeStamp = false;

        FileSet _fileset = new FileSet();

        /// <summary>The URL from which to retrieve a file.</summary>
        [TaskAttribute("src", Required=true )]
        public string Source { get { return _src; } set { _src = value; } }

        /// <summary>The file where to store the retrieved file.</summary>
        [TaskAttribute("dest", Required=true )]
        public string Destination { get { return _dest; } set { _dest = value; }}

        /// <summary>If inside a firewall, proxy server/port information
        /// Format: {proxy server name}:{port number}
        /// Example: proxy.mycompany.com:8080 </summary>
        [TaskAttribute("httpproxy")]
        public string Proxy { get { return _proxy; } set { _proxy = value; } }

        /// <summary>Log errors but don't treat as fatal. ("true"/"false"). Default is "false".</summary>
        [TaskAttribute("ignoreerrors")]
        [BooleanValidator()]
        public bool ignoreErrors {  get { return _ignoreErrors; } set { _ignoreErrors = value; } }

        /// <summary>Conditionally download a file based on the timestamp of the local copy. HTTP only. ("true"/"false"). Default is "false".</summary>
        [TaskAttribute("usetimestamp")]
        [BooleanValidator()]
        public bool useTimeStamp { get { return _useTimeStamp; }  set { _useTimeStamp = value; } }


        /// <summary>FileSets are used to select files to get.</summary>
        [FileSet("fileset")]
        public FileSet FileSet { get { return _fileset; }  }

        ///<summary>Initializes task and ensures the supplied attributes are valid.</summary>
        ///<param name="taskNode">Xml node used to define this task instance.</param>
        protected override void InitializeTask(System.Xml.XmlNode taskNode) {

            if (Source == null) {
                throw new BuildException("src attribute is required.", Location);
            }

            if (Destination == null) {
                throw new BuildException("dest attribute is required.", Location);
            }

            if (Directory.Exists(Destination)) {
                throw new BuildException("Specified destination is a directory.", Location);
            }

            if (File.Exists(Destination) && (FileAttributes.ReadOnly == (File.GetAttributes(Destination) & FileAttributes.ReadOnly))) {
                throw new BuildException("Cannot write to " + Destination, Location);
            }
        }

        /// <summary>This is where the work is done </summary>
        protected override void ExecuteTask() {

            try {
                //set the timestamp to the file date.
                DateTime fileTimeStamp = new DateTime();

                if (useTimeStamp && File.Exists(Destination)) {
                    fileTimeStamp = File.GetLastWriteTime(Destination);
                    Log.WriteLineIf(Verbose, LogPrefix + "Local file time stamp: " + fileTimeStamp.ToString());
                }

                //set up the URL connection
                WebRequest webRequest = getWebRequest(Source, fileTimeStamp);
                WebResponse webResponse = webRequest.GetResponse();

                // Get stream
                // try three times, then error out
                Stream responseStream = null;
                for (int i = 0; i < 3; i++) {
                    try {
                        responseStream = webResponse.GetResponseStream();
                        break;
                    } catch (IOException ex) {
                        Log.WriteLine(LogPrefix + "Error opening connection: " + ex.Message);
                    }
                }

                if (responseStream == null) {
                    Log.WriteLine(LogPrefix + "Cannot get " + Source + " to " + Destination);
                    if (ignoreErrors) {
                        return;
                    }
                    throw new BuildException( "Cannot get " + Source + " to " + Destination, Location);
                }

                // Open file for writing
                Log.WriteLine(LogPrefix + Source);
                BinaryWriter destWriter = new BinaryWriter(new FileStream(Destination, FileMode.Create));

                // Read in stream from URL and write data in chunks
                // to the dest file.
                int bufferSize = 100 * 1024;
                byte[] buffer = new byte[bufferSize];
                int totalReadCount = 0;
                int totalBytesReadFromStream = 0;
                int totalBytesReadSinceLastDot = 0;

                do {
                    totalReadCount = responseStream.Read(buffer, 0, bufferSize);
                    if ( totalReadCount != 0 ) { // zero means EOF
                        // write buffer into file
                        destWriter.Write(buffer, 0, totalReadCount);
                        // increment byte counters
                        totalBytesReadFromStream += totalReadCount;
                        totalBytesReadSinceLastDot += totalReadCount;
                        // display progress
                        if (Verbose && totalBytesReadSinceLastDot > bufferSize) {
                            if (totalBytesReadSinceLastDot == totalBytesReadFromStream) {
                                Log.Write(LogPrefix);
                            }
                            Log.Write(".");
                            totalBytesReadSinceLastDot = 0;
                        }
                    }
                } while (totalReadCount != 0);

                if (Verbose) {
                    if (totalBytesReadFromStream > bufferSize) {
                        Log.WriteLine();
                    }
                    Log.WriteLine(LogPrefix + "Number of bytes read: " + totalBytesReadFromStream.ToString());
                }

                // clean up response streams
                destWriter.Close();
                responseStream.Close();

                //if (and only if) the use file time option is set, then the
                //saved file now has its timestamp set to that of the downloaded file
                if (useTimeStamp)  {
                    // HTTP only
                    if (webRequest is HttpWebRequest) {

                        HttpWebResponse httpResponse = (HttpWebResponse) webResponse;

                        // get timestamp of remote file
                        DateTime remoteTimestamp = httpResponse.LastModified;

                        Log.WriteLineIf(Verbose, LogPrefix + Destination + " last modified on " + remoteTimestamp.ToString() );
                        touchFile(Destination,remoteTimestamp);
                    }
                }
            } catch (WebException webException) {
                // If status is WebExceptionStatus.ProtocolError,
                //   there has been a protocol error and a WebResponse
                //   should exist. Display the protocol error.
                if (webException.Status == WebExceptionStatus.ProtocolError) {

                    // test for a 304 result (HTTP only)
                    // Get HttpWebResponse so we can check the HTTP status code
                    HttpWebResponse httpResponse = (HttpWebResponse)webException.Response;
                    if (httpResponse.StatusCode == HttpStatusCode.NotModified) {
                        //not modified so no file download. just return instead
                        //and trace out something so the user doesn't think that the
                        //download happened when it didn't

                        Log.WriteLineIf(Verbose, LogPrefix + Destination + " not downloaded.  Not modified since " + httpResponse.LastModified.ToString());
                        return;
                    } else {
                        Log.WriteLine(LogPrefix + (int)httpResponse.StatusCode + ": " + httpResponse.StatusDescription);
                        return;
                    }
                } else {
                    Log.WriteLine(LogPrefix + webException.Status + ": " + webException.Message);
                    return;
                }
            } catch (IOException e) {
                string msg = "Error getting " + Source + " to " + Destination;
                Log.WriteLine(LogPrefix + msg);
                throw new BuildException(msg, Location, e);
            }
        }

        private WebRequest getWebRequest(string url, DateTime fileLastModified) {

            Uri uri = new Uri(url);

            // conditionally determine type of connection
            // if HTTP, cast to an HttpWebRequest so that IfModifiedSince can be set
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) {

                HttpWebRequest httpRequest = (HttpWebRequest) WebRequest.Create(uri);

                if (Proxy != null) {
                    httpRequest.Proxy = new WebProxy(Proxy);
                }

                //modify the headers
                //things like user authentication could go in here too.
                if (!fileLastModified.Equals(new DateTime())) {

                    // When IfModifiedSince is set, it internally converts the local time
                    // to UTC (or, for us old farts, GMT). For all locations behind UTC
                    // (US and Canada), this causes the IfModifiedSince time to always be
                    // set to a time earlier than the file timestamp and force the file
                    // to be fetched, even if it hasn't changed. The UtcOffset is used to
                    // counter this behavior and a second is added for good measure.

                    TimeSpan timeSpan = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                    DateTime gmtTime = fileLastModified.AddSeconds(1).Subtract(timeSpan);
                    httpRequest.IfModifiedSince = gmtTime;

                    //REVISIT: at this point even non HTTP connections may support the if-modified-since
                    //behaviour -we just check the date of the content and skip the write if it is not
                    //newer. Some protocols (FTP) dont include dates, of course.

                }
                return httpRequest;
            } else {
                try {
                    WebRequest webRequest = WebRequest.Create(uri);

                    if (Proxy != null) {
                        webRequest.Proxy = new WebProxy(Proxy);
                    }

                    return webRequest;

                } catch (Exception e) {
                    string msg = uri.Scheme + " protocol is not supported.";
                    Log.WriteLine(LogPrefix + msg);
                    throw new BuildException(msg, Location, e);
                }
            }
        }

        /// <summary>Set the timestamp of a named file to a specified time.</summary>
        protected void touchFile(string fileName, DateTime touchDateTime) {

            try {
                if (File.Exists(fileName)) {
                    Log.WriteLineIf(Verbose, LogPrefix + "Touching file {0} with {1}", fileName, touchDateTime.ToString());
                    File.SetLastWriteTime(fileName, touchDateTime);
                } else {
                    throw new FileNotFoundException();
                }
            } catch (Exception e) {
                // swallow any errors and move on
                Log.WriteLineIf(Verbose, LogPrefix + "Error: {0}", e.Message);
            }
        }
    }
}

