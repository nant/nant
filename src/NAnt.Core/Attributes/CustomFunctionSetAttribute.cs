// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that class should be treated as a task.
    /// </summary>
    /// <remarks>
    /// Attach this attribute to a subclass of CustomFunctionSetBase to have NAnt be able
    /// to recognize it as containing custom functions
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public sealed class CustomFunctionSetAttribute : Attribute {
        #region Public Instance Constructors

        // TODO remove this
        public CustomFunctionSetAttribute( )  {            
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomFunctionSetAttribute" /> class
        /// with the specified name.
        /// </summary>
        /// <param name="prefix">The name of the task.</param>
        /// <param name="namespaceuri">The name of the task.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public CustomFunctionSetAttribute(string prefix, string namespaceuri ) {
            if (prefix == null) {
                throw new ArgumentNullException("prefix");
            }

            if (prefix.Trim().Length == 0) {
                throw new ArgumentOutOfRangeException("prefix", prefix, "A zero-length string is not an allowed value.");
            }
            _prefix = prefix;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the category of the function set. This will be displayed in userdoc.
        /// </summary>
        /// <value>
        /// The name of the category
        /// </value>
        public string Category {
            get { return _category; }
            set { _category = value; }
        }
        
        /// <summary>
        /// Gets or sets the prefix of all functions in this function set.
        /// </summary>
        /// <value>
        /// The prefix.
        /// </value>
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }
        
        /// <summary>
        /// Gets or sets the namespace URI of the functon set.
        /// </summary>
        /// <value>
        /// The namespace URI.
        /// </value>
        public string NamespaceUri 
        {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _prefix;
        private string _category;
        private string _namespaceUri;        

        #endregion Private Instance Fields
    }
}
