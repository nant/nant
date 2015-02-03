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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Represents an assembly-level attribute.
    /// </summary>
    [ElementName("attribute")]
    [Serializable()]
    public class AssemblyAttribute : Element, IConditional {
        #region Private Instance Fields

        private string _typeName;
        private string _value;
        private bool _asIs;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyAttribute" /> 
        /// class.
        /// </summary>
        public AssemblyAttribute() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Typename of the assembly-level attribute.
        /// </summary>
        [TaskAttribute("type", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string TypeName {
            get { return _typeName; }
            set { _typeName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Value of the attribute.
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the value of the attribute will be 
        /// set as is, without actually looking for a matching constructor or 
        /// named properties. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the value of the attribute should be set 
        /// as is; otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("asis")]
        [BooleanValidator()]
        public bool AsIs {
            get { return _asIs; }
            set { _asIs = value; }
        }

        /// <summary>
        /// Indicates if the attribute should be generated. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the attribute should be generated; 
        /// otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the attribute should be not generated. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the attribute should be not generated; 
        /// otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
