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
    /// Indicates that property should be treated as a xml attribute for the task.
    /// </summary>
    /// <example>
    /// Examples of how to specify task attributes
    /// <code>
    /// // task XmlType default is string
    /// [BuildAttribute("out", Required=true)]
    /// string _out = null; // assign default value here
    ///
    /// [BuildAttribute("optimize")]
    /// [BooleanValidator()]
    /// // during ExecuteTask you can safely use Convert.ToBoolean(_optimize)
    /// string _optimize = Boolean.FalseString;
    ///
    /// [BuildAttribute("warnlevel")]
    /// [Int32Validator(0,4)] // limit values to 0-4
    /// // during ExecuteTask you can safely use Convert.ToInt32(_optimize)
    /// string _warnlevel = "0";
    ///
    /// [FileSet("sources")]
    /// FileSet _sources = new FileSet();
    /// </code>
    /// NOTE: Attribute values must be of type of string if you want
    /// to be able to have macros.  The field stores the exact value during
    /// InitializeTask.  Just before ExecuteTask is called NAnt will expand
    /// all the macros with the current values.
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public abstract class BuildAttributeAttribute : Attribute {
        #region Private Instance Fields

        string _name;
        bool _required = false;
        bool _expandProperties = true;

        #endregion Private Instance Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAttributeAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        protected BuildAttributeAttribute(string name) {
            _name = name;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the name of the xml attribute.
        /// </summary>
        /// <value>The name of the xml attribute.</value>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attribute is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the attribute is required; otherwise, <c>false</c>. 
        /// Default is <c>false</c>.
        /// </value>
        public bool Required {
            get { return _required; }
            set { _required = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether property references should 
        /// be expanded.
        /// </summary>
        /// <value>
        /// <c>true</c> if properties should be expanded; otherwise <c>false</c>.
        /// Default is <c>true</c>.
        /// </value>
        public bool ExpandProperties {
            get { return _expandProperties; }
            set { _expandProperties = value; }
        }

        #endregion Public Instance Properties
    }
}
