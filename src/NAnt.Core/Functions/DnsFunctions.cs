// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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
using System.Net;
using System.Net.Sockets;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Functions for requesting information from DNS.
    /// </summary>
    [FunctionSet("dns", "DNS")]
    public class DnsFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public DnsFunctions(Project project, PropertyDictionary properties) : base(project, properties) {}

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Gets the host name of the local computer.
        /// </summary>
        /// <returns>
        /// A string that contains the DNS host name of the local computer. 
        /// </returns>
        /// <exception cref="SocketException">An error is encountered when resolving the local host name.</exception>
        [Function("get-host-name")]
        public static string GetHostName() {
            return Dns.GetHostName();
        }

        #endregion Public Static Methods
    }
}
