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
using System.Runtime.InteropServices;
using System.Text;

namespace NAnt.Core {
    [Serializable()]
    public class PlatformHelper {
        public static readonly bool IsMono;
        public static readonly bool IsWin32;
        public static readonly bool IsUnix;
        
        static PlatformHelper() {
            // check a class in mscorlib to determine if we're running on Mono
            if (Type.GetType("System.MonoType", false) != null) {
                // we're on Mono
                IsMono = true;
            }
            
            PlatformID platformID = Environment.OSVersion.Platform;

            if (platformID == PlatformID.Win32NT || platformID == PlatformID.Win32Windows) {
                IsWin32 = true;
            }

            // check for (non-)Unix platforms - see FAQ for more details
            // http://www.mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
            int platform = (int) Environment.OSVersion.Platform;
            if (platform == 4 || platform == 128) {
                IsUnix = true;
            }
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
    }
}
