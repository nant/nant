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
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;

using System.Text;

namespace NAnt.Core {
    [Serializable()]
    internal class PlatformHelper {
        public static readonly bool IsMono;
        public static readonly bool IsWindows;
        public static readonly bool IsUnix;
        public static readonly bool PInvokeOK;
        
        static PlatformHelper() {
            //
            // check a class in mscorlib to determine if we're running on Mono
            // 
            if (Type.GetType("Mono.Runtime", false) != null) {
                // we're on Mono
                IsMono = true;
            } else {
                IsMono = false;
            }
            
            PlatformID platformID = System.Environment.OSVersion.Platform;

            if ((platformID == PlatformID.Win32NT) || (platformID == PlatformID.Win32Windows)) {
                IsWindows = true;
            } else {
                IsWindows = false;
            }

            if (IsMono && (int)platformID == 128) {
                IsUnix = true;
            }

            if (IsWindows && !IsMono) {
                PInvokeOK = true;
            } else {
                PInvokeOK = false;
            }
        }

        public static bool IsVolumeCaseSensitive(string path) {
            PlatformID platformID = System.Environment.OSVersion.Platform;
            
            if (PInvokeOK) {
                StringBuilder VolLabel = new StringBuilder(256);    // Label
                UInt32 VolFlags = new UInt32();
                StringBuilder FSName = new StringBuilder(256);  // File System Name
                UInt32 SerNum = 0;
                UInt32 MaxCompLen = 0;

                long Ret = PInvokeHelper.GetVolumeInformationWrapper(path, 
                        VolLabel, 
                        (UInt32)VolLabel.Capacity, 
                        ref SerNum, 
                        ref MaxCompLen, 
                        ref VolFlags, 
                        FSName, 
                        (UInt32)FSName.Capacity);

                return (((VolumeFlags) VolFlags) & VolumeFlags.CaseSensitive) == VolumeFlags.CaseSensitive;
            }

            if (IsUnix) {
                return true;
            } else {
                return false;
            }
        }
        
        class PInvokeHelper
        {
            [DllImport("kernel32.dll")]
            private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);

            public static long GetVolumeInformationWrapper(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize)
            {
                return GetVolumeInformation(PathName, VolumeNameBuffer, VolumeNameSize, ref VolumeSerialNumber, ref MaximumComponentLength, ref FileSystemFlags, FileSystemNameBuffer, FileSystemNameSize);
            }
        }
    }
}
