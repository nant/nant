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

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an XSLT parameter.
    /// </summary>
    [ElementName("xsltparameter")]
    public class XsltParameter : Element, IConditional {
        #region Private Instance Fields

        private string _name = string.Empty;
        private string _namespaceUri = string.Empty;
        private string _value = string.Empty;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XsltParameter" /> 
        /// class.
        /// </summary>
        public XsltParameter() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the XSLT parameter.
        /// </summary>
        /// <value>
        /// The name of the XSLT parameter, or <see cref="string.Empty" /> if 
        /// not set.
        /// </value>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string ParameterName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The namespace URI to associate with the parameter.
        /// </summary>
        /// <value>
        /// The namespace URI to associate with the parameter, or 
        /// <see cref="string.Empty" /> if not set.
        /// </value>
        [TaskAttribute("namespaceuri")]
        public string NamespaceUri {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }

        /// <summary>
        /// The value of the XSLT parameter.
        /// </summary>
        /// <value>
        /// The value of the XSLT parameter, or <see cref="string.Empty" /> if 
        /// not set.
        /// </value>
        [TaskAttribute("value", Required=true)]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Indicates if the parameter should be added to the XSLT argument list.
        /// If <see langword="true" /> then the parameter will be added; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the parameter should not be added to the XSLT argument
        /// list. If <see langword="false" /> then the parameter will be 
        /// added; otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
