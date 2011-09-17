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
//
// Gert Driesen (drieseng@users.sourceforge.net)
// Scott Hernandez (ScottHernandez_at_HOtMail_dot_dot_dot_com?)

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that the property should be treated as a container for a 
    /// collection of build elements.
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
    ///     <collectionName>
    ///         <elementName ... />
    ///         <elementName ... />
    ///         <elementName ... />
    ///         <elementName ... />
    ///     </collectionName>
    /// </task>
    ///     ]]>
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class BuildElementCollectionAttribute : BuildElementArrayAttribute{
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildElementCollectionAttribute" /> with the 
        /// specified name and child element name.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="childName">The name of the child elements in the collection</param>
        /// <exception cref="ArgumentNullException"><paramref name="childName" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="childName" /> is a zero-length <see cref="string" />.</exception>
        public BuildElementCollectionAttribute(string collectionName, string childName) : base(collectionName) {
            if (childName == null) { 
                throw new ArgumentNullException("childName"); 
            }

            _elementName = childName.Trim();

            if (_elementName.Length == 0) {
                throw new ArgumentOutOfRangeException("childName", childName, "A zero-length string is not an allowed value.");
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Constructors

        /// <summary>
        /// The name of the child element within the collection.
        /// </summary>
        /// <value>
        /// The name to check for in the XML of the elements in the collection.
        /// </value>
        /// <remarks>
        /// This can be used for validation and schema generation.
        /// </remarks>
        public string ChildElementName {
            get { return _elementName; }
        }

        #endregion Public Instance Constructors

        #region Private Instance Fields

        private string _elementName;

        #endregion Private Instance Fields
    }
}