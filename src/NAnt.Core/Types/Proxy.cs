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
    /// Contains HTTP proxy settings used to process requests to Internet 
    /// resources.
    /// </summary>
    [ElementName("proxy")]
    public class Proxy : DataTypeBase, IConditional {
        #region Private Instance Fields

        private string _host;
        private int _port;
        private bool _bypassOnLocal;
        private Credential _credentials;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Proxy" /> class.
        /// </summary>
        public Proxy() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the proxy host. 
        /// </summary>
        [TaskAttribute("host", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Host {
            get { return _host; }
            set { _host = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The port number on <see cref="Host" /> to use. 
        /// </summary>
        [TaskAttribute("port", Required=true)]
        [Int32Validator()]
        public int Port {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// Specifies whether to bypass the proxy server for local addresses.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("bypassonlocal", Required=false)]
        [BooleanValidator()]
        public bool BypassOnLocal {
            get { return _bypassOnLocal; }
            set { _bypassOnLocal = value; }
        }

        /// <summary>
        /// The credentials to submit to the proxy server for authentication.
        /// </summary>
        [BuildElement("credentials", Required=false)]
        public Credential Credentials {
            get { return _credentials; }
            set { _credentials = value; }
        }

        /// <summary>
        /// Indicates if the proxy should be used to connect to the external 
        /// resource. If <see langword="true" /> then the proxy will be used; 
        /// otherwise, not. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if", Required=false)]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the proxy should not be used to connect to the external
        /// resource. If <see langword="false" /> then the proxy will be used;
        /// otherwise, not. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless", Required=false)]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Gets a <see cref="WebProxy" /> instance representing the current
        /// <see cref="Proxy" />.
        /// </summary>
        /// <returns>
        /// A <see cref="WebProxy" /> instance representing the current 
        /// <see cref="Proxy" />, or <see langword="GlobalProxySelection.Select" /> 
        /// if this proxy should not be used to connect to the external resource.
        /// </returns>
        public IWebProxy GetWebProxy() {
            if (IfDefined && !UnlessDefined) {
                WebProxy proxy = new WebProxy(Host, Port);
                proxy.BypassProxyOnLocal = BypassOnLocal;

                // set authentication information
                if (Credentials != null) {
                    proxy.Credentials = Credentials.GetCredential();
                }

                return proxy;
            } else {
                // the global HTTP proxy
                return GlobalProxySelection.Select;
            }
        }

        #endregion Public Instance Methods
    }
}
