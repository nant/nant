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
    /// Indicates that class should be treated as a set of functions.
    /// </summary>
    /// <remarks>
    /// Attach this attribute to a class that derives from <see cref="FunctionSetBase" /> 
    /// to have NAnt be able to recognize it as containing custom functions.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public sealed class FunctionSetAttribute : Attribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSetAttribute" /> 
        /// class with the specified name.
        /// </summary>
        /// <param name="prefix">The prefix used to distinguish the functions.</param>
        /// <param name="category">The category of the functions.</param>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="prefix" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="category" /> is <see langword="null" />.</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para><paramref name="prefix" /> is a zero-length <see cref="string" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="category" /> is a zero-length <see cref="string" />.</para>
        /// </exception>
        public FunctionSetAttribute(string prefix, string category) {
            if (prefix == null) {
                throw new ArgumentNullException("prefix");
            }
            if (category == null) {
                throw new ArgumentNullException("category");
            }

            if (prefix.Trim().Length == 0) {
                throw new ArgumentOutOfRangeException("prefix", prefix, "A zero-length string is not an allowed value.");
            }
            if (category.Trim().Length == 0) {
                throw new ArgumentOutOfRangeException("category", category, "A zero-length string is not an allowed value.");
            }

            _prefix = prefix;
            _category = category;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the category of the function set.
        /// </summary>
        /// <value>
        /// The name of the category of the function set.
        /// </value>
        /// <remarks>
        /// This will be displayed in the user docs.
        /// </remarks>
        public string Category {
            get { return _category; }
            set { _category = value; }
        }
        
        /// <summary>
        /// Gets or sets the prefix of all functions in this function set.
        /// </summary>
        /// <value>
        /// The prefix of the functions in this function set.
        /// </value>
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }
        
        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _prefix;
        private string _category;

        #endregion Private Instance Fields
    }
}
