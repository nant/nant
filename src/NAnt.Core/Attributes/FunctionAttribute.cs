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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that the method should be exposed as a function in NAnt build 
    /// files.
    /// </summary>
    /// <remarks>
    /// Attach this attribute to a method of a class that derives from 
    /// <see cref="FunctionSetBase" /> to have NAnt be able to recognize it.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed class FunctionAttribute : Attribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionAttribute" />
        /// class with the specified name.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public FunctionAttribute(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (name.Trim().Length == 0) {
                throw new ArgumentOutOfRangeException("name", name, "A zero-length string is not an allowed value.");
            }

            _name = name;
        }

        #endregion Public Instance Constructors
         
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        /// <value>
        /// The name of the function.
        /// </value>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _name;
        
        #endregion Private Instance Fields
    }
}
