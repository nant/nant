// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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

namespace NAnt.VisualCpp.Types {
    /// <summary>
    /// Represents a library.
    /// </summary>
    public class Library : Element, IConditional {
        #region Private Instance Fields

        private string _libraryName;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Library" /> class.
        /// </summary>
        public Library() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Library" /> class with
        /// the specified name.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        public Library(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            _libraryName = name;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the library.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string LibraryName {
            get { return _libraryName; }
            set { _libraryName = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the element will be processed;
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the element will be skipped;
        /// otherwise, processed. The default is <see langword="false" />.
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
