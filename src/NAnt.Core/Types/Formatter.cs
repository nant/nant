// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Gert Driesen (gert.driesen@ardatis.com)

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Types {
    [ElementName("formatter")]
    public class Formatter : Element {
        #region Private Instance Fields

        string _property;
        string _pattern;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The property to set.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        public string Property {
            get { return _property; }
            set { _property= value; }
        }

        /// <summary>
        /// The string pattern to use to format the property.
        /// </summary>       
        [TaskAttribute("pattern", Required=true)]
        public string Pattern {
            get { return _pattern; }
            set { _pattern= value; }
        }

        #endregion Public Instance Properties
    }
}
