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
// Tomas Restrepo (tomasr@mvps.org)

using System;

using NUnit.Framework;

namespace NAnt.NUnit.Types {
    /// <summary>
    /// Carries data specified through the formatter element.
    /// </summary>
    [Serializable]
    public class FormatterData {
        #region Private Instance Fields

        private string _extension = null;
        private bool _usefile = false;
        private FormatterType _formatterType = FormatterType.Plain;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the type of the formatter.
        /// </summary>
        /// <value>The type of the formatter.</value>
        public FormatterType Type {
            get { return _formatterType; }
            set { _formatterType = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether output should be persisted 
        /// to a file. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output should be written to a file; otherwise, 
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        public bool UseFile {
            get { return _usefile; }
            set { _usefile = value; }
        }

        /// <summary>
        /// Gets or sets the extension to append to the output filename.
        /// </summary>
        /// <value>The extension to append to the output filename.</value>
        public string Extension {
            get { return _extension; }
            set { _extension = value; }
        }

        #endregion Public Instance Properties
    }
}
