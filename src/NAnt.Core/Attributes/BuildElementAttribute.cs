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
// Ian MacLean ( ian@maclean.ms )

namespace SourceForge.NAnt.Attributes {

    using System;

    /// <summary>Indicates that field should be treated as a xml file set for the task.</summary>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public abstract class BuildElementAttribute : Attribute {
        #region Private Instance Fields

        string _name;
        bool _required;

        #endregion Private Instance Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildElementAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        protected BuildElementAttribute(string name) {
            Name = name;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <value>The name of the attribute.</value>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the attribute is required; otherwise, <c>false</c>. 
        /// The default is <c>false</c>.
        /// </value>
        public bool Required {
            get { return _required; }
            set { _required = value; }
        }

        #endregion Public Instance Properties
    }
}