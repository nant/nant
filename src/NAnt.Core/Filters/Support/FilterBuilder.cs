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

namespace NAnt.Core.Filters {
    public class FilterBuilder : ExtensionBuilder {
        #region Public Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="FilterBuilder" /> class
        /// for the specified <see cref="Filter" /> class in the specified
        /// <see cref="Assembly" />.
        /// </summary>
        /// <remarks>
        /// An <see cref="ExtensionAssembly" /> for the specified <see cref="Assembly" />
        /// is cached for future use.
        /// </remarks>
        /// <param name="assembly">The <see cref="Assembly" /> containing the <see cref="Filter" />.</param>
        /// <param name="className">The class representing the <see cref="Filter" />.</param>
        public FilterBuilder (Assembly assembly, string className)
            : this (ExtensionAssembly.Create (assembly), className) {
        }

        #endregion Public Instance Constructors

        #region Internal Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="FilterBuilder" /> class
        /// for the specified <see cref="Filter" /> class in the specified
        /// <see cref="ExtensionAssembly" />.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="Filter" />.</param>
        /// <param name="className">The class representing the <see cref="Filter" />.</param>
        internal FilterBuilder(ExtensionAssembly extensionAssembly, string className) : base (extensionAssembly) {
            _className = className;

            // get Element name from attribute
            ElementNameAttribute ElementNameAttribute = (ElementNameAttribute)
                Attribute.GetCustomAttribute(Assembly.GetType(ClassName),
                typeof(ElementNameAttribute));

            _filterName = ElementNameAttribute.Name;
        }

        #endregion Internal Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the <see cref="Filter" /> class that can be created
        /// using this <see cref="FilterBuilder" />.
        /// </summary>
        /// <value>
        /// The name of the <see cref="Filter" /> class that can be created using
        /// this <see cref="FilterBuilder" />.
        /// </value>
        public string ClassName {
            get { return _className; }
        }

        /// <summary>
        /// Gets the name of the filter which the <see cref="FilterBuilder" />
        /// can create.
        /// </summary>
        /// <value>
        /// The name of the task which the <see cref="TaskBuilder" /> can 
        /// create.
        /// </value>
        public string FilterName {
            get { return _filterName; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public Filter CreateFilter() {
            return (Filter) Assembly.CreateInstance(
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
        private readonly string _filterName;

        #endregion Private Instance Fields
    }
}
