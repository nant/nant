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

using System;
using System.Net;
using System.IO;

namespace SourceForge.NAnt.Tasks
{
    /// <summary>
    /// Summary description for WebDavClient.
    /// </summary>
    public class WebDavClient
    {
        public WebDavClient( Uri uriBase )
        {
            _strWebProjectBaseUrl = uriBase.ToString();
        }

        public void UploadFile( string strLocalFilename, string strRemoteFilename )
        {
            WebRequest request = WebRequest.Create( _strWebProjectBaseUrl + "/" + strRemoteFilename );
            Console.WriteLine( request.RequestUri.ToString() );
            request.Method = "PUT";
            request.Headers.Add( "Translate: f" );
            request.Credentials = CredentialCache.DefaultCredentials;

            FileInfo fi = new FileInfo( strLocalFilename );
            request.ContentLength = fi.Length;

            int bufferSize = 100 * 1024;
            byte[] buffer = new byte[bufferSize];
            using ( FileStream fsInput = new FileStream( fi.FullName, FileMode.Open ) )
            {
                using ( Stream s = request.GetRequestStream() )
                {
                    int nRead;
                    do
                    {
                        nRead = fsInput.Read( buffer, 0, bufferSize );
                        s.Write( buffer, 0, nRead );
                    }
                    while ( nRead > 0 );
                }
            }

            buffer = null;
            try
            {
                using ( request.GetResponse() ) { }
            }
            catch ( WebException we )
            {
                HttpWebResponse hwr = ( HttpWebResponse )we.Response;
                
                if ( ( int )hwr.StatusCode != 423 )
                    throw;
            }
        }

        public void DeleteFile( string strLocalFilename, string strRemoteFilename )
        {
            WebRequest request = WebRequest.Create( _strWebProjectBaseUrl + "/" + strRemoteFilename );
            Console.WriteLine( request.RequestUri.ToString() );
            request.Method = "DELETE";
            request.Headers.Add( "Translate: f" );
            request.Credentials = CredentialCache.DefaultCredentials;

            using ( request.GetResponse() ) { }
        }

        public void DownloadFile( string strLocalFilename, string strRemoteFilename )
        {
            WebRequest request = WebRequest.Create( _strWebProjectBaseUrl + "/" + strRemoteFilename );
            Console.WriteLine( request.RequestUri.ToString() );
            request.Method = "GET";
            request.Headers.Add( "Translate: f" );
            request.Credentials = CredentialCache.DefaultCredentials;
            FileInfo fi = new FileInfo( strLocalFilename );
            if ( !Directory.Exists( fi.DirectoryName ) )
                Directory.CreateDirectory( fi.DirectoryName );

            int bufferSize = 100 * 1024;
            byte[] buffer = new byte[bufferSize];
            using ( FileStream fsOutput = new FileStream( fi.FullName, FileMode.OpenOrCreate ) )
            {
                using ( Stream s = request.GetResponse().GetResponseStream() )
                {
                    int nRead;
                    do
                    {
                        nRead = s.Read( buffer, 0, bufferSize );
                        fsOutput.Write( buffer, 0, nRead );
                    }
                    while ( nRead > 0 );
                }
            }

            buffer = null;
        }

        public string GetFileContents( string strRemoteFilename )
        {
            WebRequest request = WebRequest.Create( _strWebProjectBaseUrl + "/" + strRemoteFilename );
            Console.WriteLine( request.RequestUri.ToString() );
            request.Method = "GET";
            request.Headers.Add( "Translate: f" );
            request.Credentials = CredentialCache.DefaultCredentials;

            using ( StreamReader sr = new StreamReader( request.GetResponse().GetResponseStream() ) )
            {
                return sr.ReadToEnd();
            }
        }

        public static string GetFileContentsStatic( string strRemoteFilename )
        {
            WebRequest request = WebRequest.Create( strRemoteFilename );
            Console.WriteLine( request.RequestUri.ToString() );
            request.Method = "GET";
            request.Headers.Add( "Translate: f" );
            request.Credentials = CredentialCache.DefaultCredentials;

            using ( StreamReader sr = new StreamReader( request.GetResponse().GetResponseStream() ) )
            {
                return sr.ReadToEnd();
            }
        }

        private string _strWebProjectBaseUrl;
    }
}
