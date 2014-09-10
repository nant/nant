// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
    ///   Examples of how to specify task attributes
    ///   <code>
    /// #region Public Instance Properties
    /// 
    /// [BuildAttribute("out", Required=true)]
    /// public string Output {
    ///     get { return _out; }
    ///     set { _out = value; }
    /// }
    ///
    /// [BuildAttribute("optimize")]
    /// [BooleanValidator()]
    /// public bool Optimize {
    ///     get { return _optimize; }
    ///     set { _optimize = value; }
    /// }
    ///
    /// [BuildAttribute("warnlevel")]
    /// [Int32Validator(0,4)] // limit values to 0-4
    /// public int WarnLevel {
    ///     get { return _warnLevel; }
    ///     set { _warnLevel = value; }
    /// }
    ///
    /// [BuildElement("sources")]
    /// public FileSet Sources {
    ///     get { return _sources; }
    ///     set { _sources = value; }
    /// }
    /// 
    /// #endregion Public Instance Properties
    /// 
    /// #region Private Instance Fields
    /// 
    /// private string _out = null;
    /// private bool _optimize = false;
    /// private int _warnLevel = 4;
    /// private FileSet _sources = new FileSet();
    /// 
    /// #endregion Private Instance Fields
    ///   </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public abstract class BuildAttributeAttribute : BaseBuildAttribute {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAttributeAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is a zero-length <see cref="string" />.</exception>
        protected BuildAttributeAttribute(string name) : base(name) {
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

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

        #endregion Public Instance Properties

        #region Private Instance Fields

        private bool _expandProperties = true;

        #endregion Private Instance Fields
    }
}
