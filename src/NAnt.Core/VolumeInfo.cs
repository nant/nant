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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace NAnt.Core {

    [Serializable]
    public class InvalidVolumeException : ApplicationException {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVolumeException" /> class.
        /// </summary>
        public InvalidVolumeException() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVolumeException" /> class 
        /// with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public InvalidVolumeException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVolumeException" /> class
        /// with the specified descriptive message and inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public InvalidVolumeException(string message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVolumeException" /> class
        /// with the specified <see cref="Uri" />.
        /// </summary>
        /// <param name="volUri"><see cref="Uri" /> of the invalid volume.</param>
        public InvalidVolumeException(Uri volUri) : base("Volume information could not be retrieved for the path '" + volUri.LocalPath + "'. Verify that the path is valid and ends in a trailing backslash, and try again."){}

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidVolumeException" /> class 
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected InvalidVolumeException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        #endregion Protected Instance Constructors
    }

    /// <summary>
    /// Represents the different types of drives that may exist in a system.
    /// </summary>
    public enum VolumeType {
        Unknown,    // The drive type cannot be determined.
        Invalid,    // The root path is invalid. For example, no volume is mounted at the path.
        Removable,  // The disk can be removed from the drive.
        Fixed,      // The disk cannot be removed from the drive.
        Remote,     // The drive is a remote (network) drive.
        CDRom,      // The drive is a CD-ROM drive.
        RamDisk     // The drive is a RAM disk.
    };

    /// <summary>
    /// Represents the different supporting flags that may be set on a file system.
    /// </summary>
    [Flags]
    public enum VolumeFlags {
        Unknown                 = 0x0,
        CaseSensitive           = 0x00000001,
        Compressed              = 0x00008000,
        PersistentAcls          = 0x00000008,
        PreservesCase           = 0x00000002,
        ReadOnly                = 0x00080000,
        SupportsEncryption      = 0x00020000,
        SupportsFileCompression = 0x00000010,
        SupportsNamedStreams    = 0x00040000,
        SupportsObjectIds       = 0x00010000,
        SupportsQuotas          = 0x00000020,
        SupportsReparsePoints   = 0x00000080,
        SupportsSparseFiles     = 0x00000040,
        SupportsUnicodeOnVolume = 0x00000004
    };

    /// <summary>
    /// Presents information about a volume.
    /// </summary>
    public sealed class VolumeInfo  {
        #region Private Static Fields

        private const int NAMESIZE = 80;
        private const int MAX_PATH = 256;
        private const int FILE_ATTRIBUTE_NORMAL = 128;
        private const int SHGFI_USEFILEATTRIBUTES = 16;
        private const int SHGFI_ICON = 256;
        private const int SHGFI_LARGEICON = 0;
        private const int SHGFI_SMALLICON = 1;

        #endregion Private Static Fields

        #region Private classes, structs and enums

        [StructLayout(LayoutKind.Sequential)]
        private class UniversalNameInfo {
            public string NetworkPath = null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        private struct SHFILEINFOA {
            public IntPtr   hIcon;
            public int      iIcon;
            public uint   dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=NAMESIZE)]
            public string szTypeName;
        };

        private enum UniInfoLevel {
            Universal=1,
            Remote=2
        };

        #endregion Private classes, structs and enums

        #region Private Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeInfo" /> class.
        /// </summary>
        /// <remarks>
        /// Uses a private access modifier to prevent instantiation of this class.
        /// </remarks>
        private VolumeInfo() {
        }

        #endregion Private Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Determines whether the file system is case sensitive. Performs a
        /// P/Invoke to the Win32 API GetVolumeInformation.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>
        /// <c>true</c> if the specified volume is case-sensitive; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVolumeCaseSensitive(Uri uri) {
            ValidateURI(uri);

            bool isCaseSensitive = true;
            PlatformID platformID = System.Environment.OSVersion.Platform;
            
            if ((platformID == PlatformID.Win32NT) ||
                (platformID == PlatformID.Win32Windows) ) {

                //We're on some version of Windows, so the PInvoke is OK. Unless we're on mono in which case assume false
#if (!mono)
                // Declare Receiving Variables
                StringBuilder VolLabel = new StringBuilder(256);    // Label
                UInt32 VolFlags = new UInt32();
                StringBuilder FSName = new StringBuilder(256);  // File System Name
                UInt32 SerNum = 0;
                UInt32 MaxCompLen = 0;

                // Attempt to retreive the information
                long Ret = GetVolumeInformation(uri.LocalPath, VolLabel, (UInt32)VolLabel.Capacity, ref SerNum, ref MaxCompLen, ref VolFlags, FSName, (UInt32) FSName.Capacity);

                isCaseSensitive = (((VolumeFlags) VolFlags) & VolumeFlags.CaseSensitive) == VolumeFlags.CaseSensitive;
#endif
            }
            else if ((int) platformID == 128) {
                // Mono uses Platform id = 128 for Unix
                isCaseSensitive = true;

                //TODO - figure out what Rotor uses for non-Windows platforms
            }
            return isCaseSensitive;
        }

        #endregion Public Static Methods

        #region Private Static Methods

#if (!mono)
        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);
#endif

        private static void ValidateURI(Uri uri) {
            // Make sure we were passed something
            if (uri == null) throw new ArgumentNullException();

            // Make sure we can handle this type of uri
            if (!uri.IsFile) throw new InvalidVolumeException(uri);

            // Make sure Uri is trailed properly
            string dirsep =  String.Format(CultureInfo.InvariantCulture, "{0}", Path.DirectorySeparatorChar);
            if (!uri.LocalPath.EndsWith(dirsep) ) throw new InvalidVolumeException(uri);
        }

        #endregion Private Static Methods
    }
}
