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
    /// Indicates that property should be treated as a XML attribute for the 
    /// task.
    /// </summary>
    /// <example>
    /// Examples of how to specify task attributes
    /// <code>
    /// // task XmlType default is string
    /// [TaskAttribute("out", Required=true)]
    /// string _out = null; // assign default value here
    ///
    /// [TaskAttribute("optimize")]
    /// [BooleanValidator()]
    /// // during ExecuteTask you can safely use Convert.ToBoolean(_optimize)
    /// string _optimize = Boolean.FalseString;
    ///
    /// [TaskAttribute("warnlevel")]
    /// [Int32Validator(0,4)] // limit values to 0-4
    /// // during ExecuteTask you can safely use Convert.ToInt32(_optimize)
    /// string _warnlevel = "0";
    ///
    /// [BuildElement("sources")]
    /// FileSet _sources = new FileSet();
    /// </code>
    /// NOTE: Attribute values must be of type of string if you want
    /// to be able to have macros.  The field stores the exact value during
    /// Initialize.  Just before ExecuteTask is called NAnt will expand
    /// all the macros with the current values.
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class TaskAttributeAttribute : BuildAttributeAttribute {
        #region Public Instance Constructors
       
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAttributeAttribute" />
        /// with the specified attribute name.
        /// </summary>
        /// <param name="name">The name of the task attribute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is a <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        public TaskAttributeAttribute(string name) : base(name){
        }

        #endregion Public Instance Constructors
    }
}
