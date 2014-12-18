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
// Ian MacLean (imaclean@gmail.com)

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that class should be treated as a NAnt element.
    /// </summary>
    /// <remarks>
    /// Attach this attribute to a subclass of Element to have NAnt be able
    /// to recognize it.  The name should be short but must not confict
    /// with any other element already in use.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public class ElementNameAttribute : Attribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cre="ElementNameAttribute" /> 
        /// with the specified name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public ElementNameAttribute(string name) {
            Name = name;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        /// <value>
        /// The name of the element.
        /// </value>
        public string Name {
            get { return _name; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException("name");
                }

                // Element names cannot have whitespace at the beginning, 
                // or end.
                _name = value.Trim(); 

                if (_name.Length == 0) {
                    throw new ArgumentOutOfRangeException("name", value, "A zero-length string is not an allowed value.");
                }
            }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _name;

        #endregion Private Instance Fields
    }
}
