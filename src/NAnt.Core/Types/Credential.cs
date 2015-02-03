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

using System.Net;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Types {
    /// <summary>
    /// Provides credentials for password-based authentication schemes.
    /// </summary>
    [ElementName("credential")]
    public class Credential : DataTypeBase, IConditional {
        #region Private Instance Fields

        private string _domain;
        private string _password;
        private string _userName;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        public Credential() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The domain or computer name that verifies the credentials.
        /// </summary>
        [TaskAttribute("domain", Required=false)]
        public string Domain {
            get { return _domain; }
            set { _domain = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The password for the user name associated with the credentials.
        /// </summary>
        [TaskAttribute("password", Required=false)]
        public string Password {
            get { return _password; }
            set { _password = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The user name associated with the credentials. 
        /// </summary>
        [TaskAttribute("username", Required=false)]
        public string UserName {
            get { return _userName; }
            set { _userName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Indicates if the credentials should be used to provide authentication
        /// information to the external resource. If <see langword="true" /> then 
        /// the credentials will be passed; otherwise, not. The default is 
        /// <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the credentials should not be used to provide authentication
        /// information to the external resource. If <see langword="false" /> then the 
        /// credentials will be passed; otherwise, not. The default is 
        /// <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Returns a <see cref="NetworkCredential" /> instance representing
        /// the current <see cref="Credential" />.
        /// </summary>
        /// <returns>
        /// A <see cref="NetworkCredential" /> instance representing the current 
        /// <see cref="Credential" />, or <see langword="null" /> if the 
        /// credentials should not be used to provide authentication information
        /// to the external resource.
        /// </returns>
        public ICredentials GetCredential() {
            ICredentials credentials = null;

            if (IfDefined && !UnlessDefined) {
                credentials = new NetworkCredential(UserName, Password, Domain);
            }

            return credentials;
        }

        #endregion Public Instance Methods
    }
}
