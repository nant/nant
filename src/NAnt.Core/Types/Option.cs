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
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (gert.driesen@ardatis.com)

using System;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Types {
    /// <summary>
    /// Represents an option.
    /// </summary>
    [ElementName("option")]
    public class Option : Element {
        #region Private Instance Fields

        private string _name = null;
        private string _value = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of this property
        /// </summary>
        [TaskAttribute("name", Required=true)]
        public string OptionName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Value of this property. Default is null;
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        #endregion Public Instance Properties
    }
}
