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

using System;
using System.Collections;
using System.CodeDom.Compiler;
using System.Xml;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
	/// <summary>
	/// Base class for all project classes
	/// </summary>
	public abstract class ProjectBase {

        #region Public Instance Properties

        public abstract string Name {
            get;
        }
        
        public abstract string GUID {
            get; set;
        }

        public abstract string[] Configurations {
            get;
        }

        public abstract Reference[] References {
            get;
        }

        #endregion

        public abstract string GetOutputFile(string configuration);

        public abstract bool Compile(string configuration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands);

        #region Protected Static Methods

        protected static bool IsURL(string fileName) 
        {
            if (fileName.StartsWith(Uri.UriSchemeFile) || fileName.StartsWith(Uri.UriSchemeHttp) || fileName.StartsWith(Uri.UriSchemeHttps)) {
                return true;
            }

            return false;
        }

        protected static XmlDocument LoadXmlDocument(string fileName) 
        {
            XmlDocument doc = new XmlDocument();
            if (!IsURL(fileName)) {
                doc.Load(fileName);
            } 
            else {
                Uri uri = new Uri(fileName);
                if (uri.Scheme == Uri.UriSchemeFile) {
                    doc.Load(uri.LocalPath);
                } 
                else {
                    doc.LoadXml(WebDavClient.GetFileContentsStatic(fileName));
                }
            }

            return doc;
        }

        #endregion
	}
}
