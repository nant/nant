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
// Gert Driesen (gert.driesen@ardatis.com)

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Individual filter component of <see cref="FilterSet" />.
    /// </summary>
    public class Filter : Element {
        #region Private Instance Fields

        /// <summary>
        /// Holds the token which will be replaced in the filter operation.
        /// </summary>
        private string _token;
        
        /// <summary>
        /// Holsd the value which will replace the token in the filtering operation.
        /// </summary>
        private string _value;

        #endregion Private Instance Fields
        
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter" /> class.
        /// </summary>
        public Filter() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter" /> class with
        /// the given token and value.
        /// </summary>
        /// <param name="token">The token which will be replaced when filtering.</param>
        /// <param name="value">The value which will replace the token when filtering.</param>
        public Filter(string token, string value) {
            _token = token;
            _value = value;
        }

        #endregion Public Instance Constructors

        /// <summary>
        /// The token which will be replaced when filtering.
        /// </summary>
        [TaskAttribute("token", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Token {
            get { return _token; }
            set { _token = value; }
        }
        
        /// <summary>
        /// The value which will replace the token when filtering.
        /// </summary>
        [TaskAttribute("token", Required=true)]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }
    }
}
