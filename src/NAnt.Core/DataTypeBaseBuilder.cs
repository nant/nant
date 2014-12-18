// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Ian MacLean (imaclean@gmail.com)

using System;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

using NAnt.Core.Attributes;
using NAnt.Core.Extensibility;

namespace NAnt.Core {
    /// <summary>
    /// Factory to create <see cref="DataTypeBase"/> instances.
    /// </summary>
    public class DataTypeBaseBuilder : ExtensionBuilder {
        #region Public Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="DataTypeBaseBuilder" /> class
        /// for the specified <see cref="DataTypeBase" /> class in the specified
        /// <see cref="Assembly" />.
        /// </summary>
        /// <remarks>
        /// An <see cref="ExtensionAssembly" /> for the specified <see cref="Assembly" />
        /// is cached for future use.
        /// </remarks>
        /// <param name="assembly">The <see cref="Assembly" /> containing the <see cref="DataTypeBase" />.</param>
        /// <param name="className">The class representing the <see cref="DataTypeBase" />.</param>
        public DataTypeBaseBuilder (Assembly assembly, string className)
            : this (ExtensionAssembly.Create (assembly), className) {
        }

        #endregion Public Instance Constructors

        #region Internal Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="DataTypeBaseBuilder" />
        /// class for the specified <see cref="DataTypeBase" /> class in the
        /// <see cref="ExtensionAssembly" /> specified.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="DataTypeBase" />.</param>
        /// <param name="className">The class representing the <see cref="DataTypeBase" />.</param>
        internal DataTypeBaseBuilder(ExtensionAssembly extensionAssembly, string className) : base (extensionAssembly) {
            _className = className;
        }

        #endregion Internal Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the <see cref="DataTypeBase" /> class that can be
        /// created using this <see cref="DataTypeBaseBuilder" />.
        /// </summary>
        /// <value>
        /// The name of the <see cref="DataTypeBase" /> class that can be created
        /// using this <see cref="DataTypeBaseBuilder" />.
        /// </value>
        public string ClassName {
            get { return _className; }
        }

        /// <summary>
        /// Gets the name of the data type which the <see cref="DataTypeBaseBuilder" />
        /// can create.
        /// </summary>
        /// <value>
        /// The name of the data type which the <see cref="DataTypeBaseBuilder" />
        /// can create.
        /// </value>
        public string DataTypeName {
            get {
                if (_dataTypeName == null) {
                    ElementNameAttribute ElementNameAttribute = (ElementNameAttribute) 
                        Attribute.GetCustomAttribute(Assembly.GetType(ClassName), 
                        typeof(ElementNameAttribute));
                    _dataTypeName = ElementNameAttribute.Name;
                }
                return _dataTypeName;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Creates the <see cref="DataTypeBase"/> instance.
        /// </summary>
        /// <returns>The created instance.</returns>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public DataTypeBase CreateDataTypeBase() {
            return (DataTypeBase) Assembly.CreateInstance(
                ClassName, 
                true, 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                null,
                CultureInfo.InvariantCulture,
                null);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private readonly string _className;
        private string _dataTypeName;

        #endregion Private Instance Fields
    }
}
