// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Kevin Dente (kevindente@yahoo.com)

// This class is an extremely stripped down version of Jared Bienz's code from CodeProject.com, 
// Even stripped down, it still includes more than NAnt needs right now, but
// the extra functionality was left in there in case it's needed in the future.

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Runtime.Serialization;

namespace SourceForge.NAnt {

    // these exceptions now have all appropriate constructors to pass the Exception_test in the unit test suite. Not sure if this is overkill or not.
    [Serializable]
    public class InvalidVolumeException : ApplicationException {
        /// <summary>
        /// Constructs a build exception with no descriptive information.
        /// </summary>
        public InvalidVolumeException() : base() {
        }
        public InvalidVolumeException(String message) : base(message) {
        }
        public InvalidVolumeException(String message, Exception e) : base(message, e) {
        }

        public InvalidVolumeException(Uri VolUri) : base("Volume information could not be retreived for the path '" + VolUri.LocalPath + "'. Verify that the path is valid and ends in a trailing backslash, and try again."){}
        public InvalidVolumeException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
        public override string Message {
            get {
                return base.Message;
            }
        }
    }

    [Serializable]
    public class InvalidVolumeTypeException : ApplicationException {
        /// <summary>
        /// Constructs a build exception with no descriptive information.
        /// </summary>      
        public InvalidVolumeTypeException() : base("This action cannot be performed because of the volume is of the wrong type."){}

        public InvalidVolumeTypeException(String message) : base(message) {
        }
        public InvalidVolumeTypeException(String message, Exception e) : base(message, e) {
        } 
        public InvalidVolumeTypeException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }

    [Serializable]
    public class VolumeAccessException : ApplicationException {
        /// <summary>
        /// Constructs a build exception with no descriptive information.
        /// </summary> 
        public VolumeAccessException() : base("The volume could not be accessed and may be offline."){}

         public VolumeAccessException(String message) : base(message) {
        }
        public VolumeAccessException(String message, Exception e) : base(message, e) {
        }
        public VolumeAccessException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
      
    }

    /// <summary>
    /// Represents the different types of drives that may exist in a system.
    /// </summary>
    public enum VolumeTypes {
        Unknown,	// The drive type cannot be determined. 
        Invalid,	// The root path is invalid. For example, no volume is mounted at the path. 
        Removable,	// The disk can be removed from the drive. 
        Fixed,		// The disk cannot be removed from the drive. 
        Remote,		// The drive is a remote (network) drive. 
        CDROM,		// The drive is a CD-ROM drive. 
        RAMDisk		// The drive is a RAM disk. 
    };

    /// <summary>
    /// Represents the different supporting flags that may be set on a file system.
    /// </summary>
    [Flags]
    public enum VolumeFlags {
        Unknown					= 0x0,
        CaseSensitive			= 0x00000001,
        Compressed				= 0x00008000,
        PersistentACLS			= 0x00000008,
        PreservesCase			= 0x00000002,
        ReadOnly				= 0x00080000,
        SupportsEncryption		= 0x00020000,
        SupportsFileCompression	= 0x00000010,
        SupportsNamedStreams	= 0x00040000,
        SupportsObjectIDs		= 0x00010000,
        SupportsQuotas			= 0x00000020,
        SupportsReparsePoints	= 0x00000080,
        SupportsSparseFiles		= 0x00000040,
        SupportsUnicodeOnVolume	= 0x00000004
};

    /// <summary>
    /// Presents information about a volume.
    /// </summary>
    public class VolumeInfo  {
        /**********************************************************
        * Private Constants
        *********************************************************/
        private const int NAMESIZE = 80;
        private const int MAX_PATH = 256;
        private const int FILE_ATTRIBUTE_NORMAL = 128;
        private const int SHGFI_USEFILEATTRIBUTES = 16;
        private const int SHGFI_ICON = 256;
        private const int SHGFI_LARGEICON = 0;
        private const int SHGFI_SMALLICON = 1;

        /**********************************************************
        * Private Structures
        *********************************************************/
        [StructLayout(LayoutKind.Sequential)]
        private class UniversalNameInfo { 
            public string NetworkPath=null;
        }

        [StructLayout ( LayoutKind.Sequential, CharSet=CharSet.Ansi )]
        public struct SHFILEINFOA { 
            public IntPtr   hIcon; 
            public int      iIcon; 
            public uint   dwAttributes; 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string szDisplayName; 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=NAMESIZE)]
            public string szTypeName; 
        };

        /**********************************************************
        * Private Enums
        *********************************************************/
        private enum UniInfoLevels {
            Universal=1,
            Remote=2
        };

        /**********************************************************
         * Method Imports
         *********************************************************/
        [DllImport("mpr.dll")]
        private static extern UInt32 WNetGetUniversalName( string driveLetter, UniInfoLevels InfoLevel, IntPtr Ptr, ref UInt32 UniSize );
        [DllImport("kernel32.dll")]
        private static extern long GetDriveType(string driveLetter);
        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);
        
        private static void ValidateURI(Uri uri) {
            // Make sure we were passed something
            if (uri == null) throw new ArgumentNullException();

            // Make sure we can handle this type of uri
            if (!uri.IsFile) throw new InvalidVolumeException(uri);

            // Make sure Uri is trailed properly
            string dirsep =  string.Format("{0}",Path.DirectorySeparatorChar);
            if (!uri.LocalPath.EndsWith(dirsep) ) throw new InvalidVolumeException(uri);
        }
        
        /// <summary>
        /// Determines whether the file system is case sensitive. Performs a 
        /// P/Invoke to the Win32 API GetVolumeInformation.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        static public bool IsVolumeCaseSensitive(Uri uri) {
            ValidateURI(uri);

            bool isCaseSensitive = true;
            PlatformID platformID = System.Environment.OSVersion.Platform;

            if ((platformID == PlatformID.Win32NT) ||
                (platformID == PlatformID.Win32Windows)) {
            
                //We're on some version of Windows, so the PInvoke is OK.

                // Declare Receiving Variables
                StringBuilder VolLabel = new StringBuilder(256);	// Label
                UInt32 VolFlags = new UInt32();
                StringBuilder FSName = new StringBuilder(256);	// File System Name
                UInt32 SerNum = 0;
                UInt32 MaxCompLen = 0;
            
                // Attempt to retreive the information
                long Ret = GetVolumeInformation(uri.LocalPath, VolLabel, (UInt32)VolLabel.Capacity, ref SerNum, ref MaxCompLen, ref VolFlags, FSName, (UInt32)FSName.Capacity);

                isCaseSensitive = (((VolumeFlags)VolFlags) & VolumeFlags.CaseSensitive) == VolumeFlags.CaseSensitive;
            }
            else if ((int)platformID == 128) {
                // Mono uses Platform id = 128 for Unix
                isCaseSensitive = true;

                //TODO - figure out what Rotor uses for non-Windows platforms
            }
            return isCaseSensitive;
        }
    }
}
