// NAnt - A .NET build tool
// Copyright (C) 2001-2008 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net.be)

using System;
using System.ComponentModel;

namespace NAnt.Core {
    /// <summary>
    /// Defines the types of frameworks.
    /// </summary>
    [Flags]
    public enum FrameworkTypes {
        /// <summary>
        /// Frameworks that are supported on the current platform, but are not
        /// installed.
        /// </summary>
        NotInstalled = 1,

        /// <summary>
        /// Frameworks that are installed on the current system.
        /// </summary>
        Installed = 2,

        /// <summary>
        /// Retrieves installation state attributes.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        InstallStateMask = NotInstalled | Installed,

        /// <summary>
        /// Frameworks that typically target full desktop devices.
        /// </summary>
        Desktop = 4,

        /// <summary>
        /// Frameworks that target compact devices.
        /// </summary>
        Compact = 8,

        /// <summary>
        /// Frameworks that run in a browser.
        /// </summary>
        Browser = 16,

        /// <summary>
        /// Retrieves device attributes.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        DeviceMask = Desktop | Compact | Browser,

        /// <summary>
        /// Frameworks released as part of the open-source <see href="http://www.mono-project.com">Mono</see>
        /// project.
        /// </summary>
        Mono = 32,

        /// <summary>
        /// Frameworks released by Microsoft.
        /// </summary>
        MS = 64,

        /// <summary>
        /// Retrieves vendor attributes.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        VendorMask = Mono | MS,

        /// <summary>
        /// All frameworks supported on the current platform, regarless of their
        /// installation state, target device or vendor.
        /// </summary>
        All = Installed | NotInstalled
	}
}
