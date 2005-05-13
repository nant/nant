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
        public static readonly bool PInvokeOK;
        
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

            if (IsWin32 && !IsMono) {
                PInvokeOK = true;
            }
        }

        public static bool IsVolumeCaseSensitive(string path) {  
            if (PInvokeOK) {
                StringBuilder VolLabel = new StringBuilder(256);    // Label
                UInt32 VolFlags = new UInt32();
                StringBuilder FSName = new StringBuilder(256);  // File System Name
                UInt32 SerNum = 0;
                UInt32 MaxCompLen = 0;

                PInvokeHelper.GetVolumeInformationWrapper(path, 
                    VolLabel, 
                    (UInt32) VolLabel.Capacity, 
                    ref SerNum, 
                    ref MaxCompLen, 
                    ref VolFlags, 
                    FSName, 
                    (UInt32) FSName.Capacity);

                return (((VolumeFlags) VolFlags) & VolumeFlags.CaseSensitive) == VolumeFlags.CaseSensitive;
            }

            return IsUnix;
        }
        
        private class PInvokeHelper {
            [DllImport("kernel32.dll")]
            private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);

            public static long GetVolumeInformationWrapper(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize) {
                return GetVolumeInformation(PathName, VolumeNameBuffer, VolumeNameSize, ref VolumeSerialNumber, ref MaximumComponentLength, ref FileSystemFlags, FileSystemNameBuffer, FileSystemNameSize);
            }
        }
    }
}
