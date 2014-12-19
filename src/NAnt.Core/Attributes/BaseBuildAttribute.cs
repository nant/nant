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
// Embedev ( embedev@users.noreply.github.com )

using System;

namespace NAnt.Core.Attributes
{
    /// <summary>
    /// Base attribute for build items (attributes/elements).
    /// </summary>
    public abstract class BaseBuildAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseBuildAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the item.</param>
        protected BaseBuildAttribute(string name)
        {
            Name = name;
            ProcessXml = true;
            Required = false;
        }

        #region Public Instance Properties
        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>
        /// The name of the item.
        /// </value>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("name");
                }

                // XML element names cannot have whitespace at the beginning, 
                // or end.
                _name = value.Trim();

                if (_name.Length == 0)
                {
                    throw new ArgumentOutOfRangeException("name", value, "A zero-length string is not an allowed value.");
                } 
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is required.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the item is required; otherwise, 
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        public bool Required 
        {
            get { return _req; }
            set { _req = value; }
        }

        /// <summary>
        /// Used to specify how this element will be handled as the XML is parsed 
        /// and given to the element.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if XML should be processed; otherwise 
        /// <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool ProcessXml 
        { 
            get { return _procXml; } 
            set { _procXml = value; } 
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _name;
        private bool _procXml;
        private bool _req;

        #endregion Private Instance Fields
    }
}
