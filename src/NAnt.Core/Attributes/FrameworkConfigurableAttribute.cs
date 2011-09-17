// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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

using System;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Indicates that the value of the property to which the attribute is 
    /// assigned, can be configured on the framework-level in the NAnt application 
    /// configuration file.
    /// </summary>
    /// <example>
    /// <para>
    /// The following example shows a property of which the value can be 
    /// configured for a specific framework in the NAnt configuration file.
    /// </para>
    /// <code lang="C#">
    /// [FrameworkConfigurable("exename", Required=true)]
    /// public virtual string ExeName {
    ///     get { return _exeName; }
    ///     set { _exeName = value; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class FrameworkConfigurableAttribute : Attribute {
        #region Public Instance Constructors
       
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameworkConfigurableAttribute" />
        /// with the specified attribute name.
        /// </summary>
        /// <param name="name">The name of the framework configuration attribute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is a <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public FrameworkConfigurableAttribute(string name) {
            Name = name;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the framework configuration attribute.
        /// </summary>
        /// <value>The name of the framework configuration attribute.</value>
        public string Name {
            get { return _name; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException("name");
                }
                
                // attribute names cannot have whitespace at the beginning, 
                // or end.
                _name = value.Trim();

                if (_name.Length == 0) {
                    throw new ArgumentOutOfRangeException("name", value, "A zero-length string is not an allowed value.");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the configuration attribute 
        /// is required.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the configuration attribute is required; 
        /// otherwise, <see langword="true" />. The default is <see langword="false" />.
        /// </value>
        public bool Required {
            get { return _required; }
            set { _required = value; }
        }

        #endregion Public Instance Properties

        /// <summary>
        /// Gets or sets a value indicating whether property references should 
        /// be expanded.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if properties should be expanded; otherwise 
        /// <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool ExpandProperties {
            get { return _expandProperties; }
            set { _expandProperties = value; }
        }

        #region Private Instance Fields

        private string _name;
        private bool _required;
        private bool _expandProperties = true;

        #endregion Private Instance Fields
    }
}
