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
// Gert Driesen (drieseng@users.sourceforge.net)

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    [ElementName("formatter")]
    public class Formatter : Element, IConditional {
        #region Private Instance Fields

        private string _property;
        private string _pattern;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the NAnt property to set.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Property {
            get { return _property; }
            set { _property= value; }
        }

        /// <summary>
        /// The string pattern to use to format the property.
        /// </summary>       
        [TaskAttribute("pattern", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Pattern {
            get { return _pattern; }
            set { _pattern= value; }
        }

        /// <summary>
        /// Indicates if the formatter should be used to format the timestamp.
        /// If <see langword="true" /> then the formatter will be used; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the formatter should be not used to format the 
        /// timestamp. If <see langword="false" /> then the formatter will be 
        /// used; otherwise, skipped. The default is <see langword="false" />.
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
