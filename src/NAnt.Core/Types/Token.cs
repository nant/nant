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

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// ReplaceTokens filter token.
    /// </summary>
    [ElementName("token")]
    public class Token : Element, IConditional {
        #region Private Instance Fields

        private string _key;
        private string _value;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Token to be replaced.
        /// </summary>
        [TaskAttribute("key", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Key {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// New value of token.
        /// </summary>
        [TaskAttribute("value", Required=true)]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Indicates if the token should be used to replace values. 
        /// If <see langword="true" /> then the token will be used; 
        /// otherwise, not. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if", Required=false)]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the token should not be used to replace values.
        /// If <see langword="false" /> then the token will be used;
        /// otherwise, not. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless", Required=false)]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
