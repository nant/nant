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
// Ian MacLean ( ian@maclean.ms )
// Scott Hernandez (ScottHernandez_at_HOtMail_dot_dot_dot_com?)

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that the property should be treated as an XML element and 
    /// further processing should be done.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Should only be applied to properties exposing strongly typed arrays or 
    /// strongly typed collections.
    /// </para>
    /// <para>
    /// The XML format is like this:
    /// <code>
    ///     <![CDATA[
    /// <task>
    ///     <elementName ...>
    ///         <morestuff />
    ///     </elementName>
    /// </task>
    ///     ]]>
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited=true)]
    public class BuildElementAttribute : Attribute {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildElementAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public BuildElementAttribute(string name) {
            Name = name;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <value>
        /// The name of the attribute.
        /// </value>
        public string Name {
            get { return _name; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException("name");
                }
                
                // XML element names cannot have whitespace at the beginning, 
                // or end.
                _name = value.Trim(); 

                if (_name.Length == 0) {
                    throw new ArgumentOutOfRangeException("name", value, "A zero-length string is not an allowed value.");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute is required.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the attribute is required; otherwise, 
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        public bool Required {
            get { return _required; }
            set { _required = value; }
        }

        /// <summary>
        /// Used to specify how this element will be handled as the XML is parsed 
        /// and given to the element.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if XML should be processed; otherwise 
        /// <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool ProcessXml {
            get { return _processXml; }
            set { _processXml = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _name;
        private bool _required = false;
        private bool _processXml = true;

        #endregion Private Instance Fields
    }
}