// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Martin Aliger
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
// Martin Aliger (martin_aliger@gordic.cz)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Xml;
using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an XML namespace.
    /// </summary>
    [ElementName("namespace")]
    public class XmlNamespace : Element, IConditional {
        #region Private Instance Fields

        private string _prefix;
        private string _uri;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The prefix to associate with the namespace.
        /// </summary>
        [TaskAttribute("prefix", Required=true)]
        [StringValidator(AllowEmpty=true)]
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }

        /// <summary>
        /// The associated XML namespace URI.
        /// </summary>
        [TaskAttribute("uri", Required=true)]
        [StringValidator(AllowEmpty=true)]
        public string Uri {
            get { return _uri; }
            set { _uri = value; }
        }

        /// <summary>
        /// Indicates if the namespace should be added to the <see cref="XmlNamespaceManager" />.
        /// If <see langword="true" /> then the namespace will be added; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the namespace should not be added to the <see cref="XmlNamespaceManager" />.
        /// list. If <see langword="false" /> then the parameter will be 
        /// added; otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
