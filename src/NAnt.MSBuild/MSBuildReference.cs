// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System.Xml;

namespace NAnt.MSBuild {
    internal class DummyXmlElement : XmlElement {
        internal DummyXmlElement(XmlDocument doc)
            : base("", "dummy", "", doc) {
        }
    }

    internal class MSBuildReferenceHelper {
        private readonly bool _isPrivate;
        private readonly bool _isPrivateSpecified;

        public MSBuildReferenceHelper(bool isPrivateSpecified, bool isPrivate) {
            _isPrivateSpecified = isPrivateSpecified;
            _isPrivate = isPrivate;
        }

        public MSBuildReferenceHelper(string priv, bool privatedefault) {
            _isPrivateSpecified = !string.IsNullOrEmpty(priv);
            if (_isPrivateSpecified) {
                _isPrivate = (priv.ToLower() == "true");
            }
            else {
                _isPrivate = privatedefault;
            }
        }

        public bool IsPrivate {
            get { return _isPrivate; }
        }

        public bool IsPrivateSpecified {
            get { return _isPrivateSpecified; }
        }
    }
}
