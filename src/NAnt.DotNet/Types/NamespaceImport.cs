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

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Represents a namespace to import.
    /// </summary>
    [Serializable()]
    [ElementName("import")]
    public class NamespaceImport : Element, IConditional {
        #region Private Instance Fields

        private string _namespace;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceImport" /> 
        /// class.
        /// </summary>
        public NamespaceImport() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceImport" /> 
        /// class for the specified namespace.
        /// </summary>
        /// <param name="nameSpace">The namespace.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nameSpace" /> is <see langword="null" />.</exception>
        public NamespaceImport(string nameSpace) {
            if (nameSpace == null) {
                throw new ArgumentNullException("nameSpace");
            }

            this._namespace = nameSpace;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the namespace to import.
        /// </summary>
        /// <value>
        /// The name of the namespace to import.
        /// </value>
        [TaskAttribute("name", Required=false)]
        [StringValidator(AllowEmpty=false)]
        [Obsolete("Use \"namespace\" attribute instead.", false)]
        public string TempName {
            get { return Namespace; }
            set { Namespace = value; }
        }

        // TODO: we need to mark "namespace" as required after the release of
        // NAnt 0.85
        //
        // We can't do this right now, as it would be a major breaking change
        // (build authors still use "name" right now).

        /// <summary>
        /// The name of the namespace to import.
        /// </summary>
        /// <value>
        /// The name of the namespace to import.
        /// </value>
        [TaskAttribute("namespace", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string Namespace {
            get { return _namespace; }
            set { _namespace = value.Trim(); }
        }

        /// <summary>
        /// Indicates if the import should be generated. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the import should be generated; otherwise,
        /// <see langword="false" />.
        /// </value>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the import should be not generated. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the import should be not generated; 
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
