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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Xml;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an element of which the XML is processed by its parent task 
    /// or type.
    /// </summary>
    public class RawXml : Element {
        #region Public Instance Properties

        /// <summary>
        /// Gets the XML that this element represents.
        /// </summary>
        public XmlNode Xml {
            get { return base.XmlNode; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element

        /// <summary>
        /// Gets a value indicating whether the element is performing additional
        /// processing using the <see cref="XmlNode" /> that was use to 
        /// initialize the element.
        /// </summary>
        /// <value>
        /// <see langword="true" />, as the XML that represents this build 
        /// element is processed by the containing task or type.
        /// </value>
        protected override bool CustomXmlProcessing {
            get { return true; }
        }

        #endregion Override implementation of Element
    }
}
