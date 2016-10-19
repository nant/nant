// NAnt - A .NET build tool
// Copyright (C) 2001-2015 Gerry Shaw
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Diagnostics;

#if !NET_4_0
using System.Runtime.InteropServices;
#endif

namespace NAnt.Core
{
    /// <summary>
    /// This class provides information about the platform NAnt is currently executed on.
    /// </summary>
    [Serializable()]
    public static class PlatformHelper
    {
        #region Public Static Fields

        /// <summary>
        /// Gets a value indicating if Mono is the current execution plattform. 
        /// </summary>
        public static readonly bool IsMono;

        /// <summary>
        /// Gets a value indicating if Windows is the current execution plattform. 
        /// </summary>
        [Obsolete("Use IsWindows instead.")]
        public static readonly bool IsWin32;

        /// <summary>
        /// Gets a value indicating if Unix is the current execution plattform. 
        /// </summary>
        public static readonly bool IsUnix;

        /// <summary>
        /// Gets a value indicating if NAnt is running on a 64-bit process.
        /// </summary>
        public static readonly bool Is64BitProcess;

        /// <summary>
        /// Gets a value indicating if the underlying operating system NAnt
        /// is running on is considered 64-bit.
        /// </summary>
        public static readonly bool Is64BitOs;

        #endregion

        #region Static Constructor

        static PlatformHelper()
        {
            // check a class in mscorlib to determine if we're running on Mono
            if (Type.GetType("System.MonoType", false) != null)
            {
                // we're on Mono
                IsMono = true;
            }

            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
                IsUnix = true;

            IsWin32 = !IsUnix;

#if NET_4_0
            Is64BitProcess = Environment.Is64BitProcess;
            Is64BitOs = Environment.Is64BitOperatingSystem;
#else
            Is64BitProcess = (IntPtr.Size == 8);
            Is64BitOs = Is64BitProcess;

            if (!IsUnix)
            {
                Is64BitOs |= Is32Bit && OnWin64BitOperatingSystem();
            }
#endif
            if (IsUnix)
            {
                Is64BitOs |= Is32Bit && OnUnix64BitOperatingSystem();
            }
        }

        #endregion

        #region Public Static Properties

        /// <summary>
        /// Returns a value indicating whether NAnt is running on Windows.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if NAnt is running on Windows;
        /// otherwise, <see langword="false" />.
        /// </value>
        public static bool IsWindows
        {
            get { return !IsUnix; }
        }

        #endregion

        #region Internal Static Properties

        /// <summary>
        /// Returns a value indicating whether NAnt is running in 64-bit mode.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if NAnt is running in 64-bit mode; otherwise,
        /// <see langword="false" />.
        /// </value>
        [Obsolete("Use Is64BitProcess instead")]
        internal static bool Is64Bit
        {
            get { return Is64BitProcess; }
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
        internal static bool Is32Bit
        {
            get { return (IntPtr.Size == 4); }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Determines whether the volume of the the specified path is case sensitive.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the volume is case sensitive, else <c>false</c>.</returns>
        public static bool IsVolumeCaseSensitive(string path)
        {
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

        #endregion

        #region Private Static Methods

        private static bool OnUnix64BitOperatingSystem()
        {
            Process p = new Process();
            string output;

            p.StartInfo.FileName = "uname";
            p.StartInfo.Arguments = "-m";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            output = p.StandardOutput.ReadToEnd().TrimEnd('\n', '\r');
            p.WaitForExit();

            if (output.EndsWith("64"))
                return true;

            return false;
        }

#if !NET_4_0

        // This section should be removed as soon as the NAnt releases switch to
        // 4.0+ by default (i.e.:net-4.0/mono-4.0).

        delegate bool IsWow64ProcDel([In] IntPtr handle, [Out] out bool isWow64Proc);
        const string _lib = "kernel32";
        
        [DllImport(_lib, SetLastError=true, CharSet = CharSet.Ansi, 
            CallingConvention = CallingConvention.Winapi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport(_lib, CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true, 
            CallingConvention = CallingConvention.Winapi)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        static bool OnWin64BitOperatingSystem()
        {
            IsWow64ProcDel d;
            bool isWow64;
            bool stage;
            IntPtr lib;
            IntPtr procAddr;

            lib = LoadLibrary(_lib);
            if (lib.Equals(IntPtr.Zero)) return false;

            procAddr = GetProcAddress(lib, "IsWow64Process");
            if (procAddr.Equals(IntPtr.Zero)) return false;

            d = (IsWow64ProcDel)Marshal.GetDelegateForFunctionPointer
                (procAddr, typeof(IsWow64ProcDel));

            stage = d.Invoke(Process.GetCurrentProcess().Handle, out isWow64);
            return stage && isWow64;
        }

#endif

        #endregion
    }
}
