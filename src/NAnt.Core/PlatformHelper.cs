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

// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace NAnt.Core {
    [Serializable()]
    public class PlatformHelper {
        public static readonly bool IsMono;
        [Obsolete ("Use IsWindows instead.")]
        public static readonly bool IsWin32;
        public static readonly bool IsUnix;

        static PlatformHelper() {
            // check a class in mscorlib to determine if we're running on Mono
            if (Type.GetType("System.MonoType", false) != null) {
                // we're on Mono
                IsMono = true;
            }

            int p = (int) Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
                IsUnix = true;

            IsWin32 = !IsUnix;
        }

        public static bool IsVolumeCaseSensitive(string path) {
            // GetVolumeInformation is useless, since it marks NTFS drives as
            // case-sensitive and provides no information for non-root
            // directories.
            // 
            // This gave us the impression that it worked since a zero VolFlags
            // would be considered as case-insensitive.
            // 
            // For now, we just return false on Unix and true in all other
            // cases.

            return IsUnix;
        }

        /// <summary>
        /// Returns a value indicating whether NAnt is running in 64-bit mode.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if NAnt is running in 64-bit mode; otherwise,
        /// <see langword="false" />.
        /// </value>
        internal static bool Is64Bit {
            get { return (IntPtr.Size == 8); }
        }

        /// <summary>
        /// Returns a value indicating whether NAnt is running in 32-bit mode.
        /// </summary>
        /// <remarks>
        /// Note that even if the platform is 64-bit, NAnt may be running in
        /// 32-bit mode.
        /// </remarks>
        /// <value>
        /// <see langword="true" /> if NAnt is running in 32-bit mode; otherwise,
        /// <see langword="false" />.
        /// </value>
        internal static bool Is32Bit {
            get { return (IntPtr.Size == 4); }
        }

        /// <summary>
        /// Returns a value indicating whether NAnt is running on Windows.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if NAnt is running on Windows;
        /// otherwise, <see langword="false" />.
        /// </value>
        public static bool IsWindows {
            get { return !IsUnix; }
        }
    }
}
