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


using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Parameter for a <see cref="FilterElement"/>
    /// </summary>
    /// <remarks>
    /// Used to pass filters into generic filters.  These are filters
    /// specified with &lt;filter&gt; tag.
    /// </remarks>
    [ElementName("param")]
    public class FilterElementParameter : Element {
        #region Private Instance Members

        private string _name;
        private string _value;

        #endregion Private Instance Members

        #region Public Instance Counstrucors

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterElementParameter() : base() {}

        #endregion Public Instance Counstrucors

        #region Public Instance Properties

        /// <summary>
        /// Name of parameter
        /// </summary>
        [TaskAttribute("name", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string ParamName {
            get { return _name;  }
            set { _name = value; }
        }

        /// <summary>
        /// Value assigned to parameter
        /// </summary>
        [TaskAttribute("value", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string ParamValue {
            get { return _value;  }
            set { _value = value; }
        }
        #endregion Public Instance Properties
    }
}
