using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NAnt.Core.Util
{
    /// <summary> A file system enumerator. </summary>
    /// <summary> A file system extensions. </summary>
    public static class FileSystemExtensions
    {
        /// <summary> Enumerates the directories in this collection. </summary>
        /// <param name="target"> The target to act on. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the directories in this
        ///     collection.
        /// </returns>
        public static IEnumerable<DirectoryInfo> EnumerateDirectories( this DirectoryInfo target )
        {
            return EnumerateDirectories( target, "*" );
        }

        /// <summary> Enumerates the directories in this collection. </summary>
        /// <param name="target">        The target to act on. </param>
        /// <param name="searchPattern"> A pattern specifying the search. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the directories in this
        ///     collection.
        /// </returns>
        public static IEnumerable<DirectoryInfo> EnumerateDirectories( this DirectoryInfo target, string searchPattern )
        {
            string searchPath = Path.Combine( target.FullName, searchPattern );
            NativeWin32.WIN32_FIND_DATA findData;
            using (NativeWin32.SafeSearchHandle hFindFile = NativeWin32.FindFirstFile( searchPath, out findData ))
            {
                if (!hFindFile.IsInvalid)
                {
                    do
                    {
                        if ((findData.dwFileAttributes & FileAttributes.Directory) != 0 && findData.cFileName != "." && findData.cFileName != "..")
                        {
                            yield return new DirectoryInfo( Path.Combine( target.FullName, findData.cFileName ) );
                        }
                    } while (NativeWin32.FindNextFile( hFindFile, out findData ));
                }
            }

        }

        /// <summary> Enumerates the files in this collection. </summary>
        /// <param name="target"> The target to act on. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the files in this collection.
        /// </returns>
        public static IEnumerable<FileInfo> EnumerateFiles( this DirectoryInfo target )
        {
            return EnumerateFiles( target, "*" );
        }

        /// <summary> Enumerates the files in this collection. </summary>
        /// <param name="target">        The target to act on. </param>
        /// <param name="searchPattern"> A pattern specifying the search. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the files in this collection.
        /// </returns>
        public static IEnumerable<FileInfo> EnumerateFiles( this DirectoryInfo target, string searchPattern )
        {
            string searchPath = Path.Combine( target.FullName, searchPattern );
            NativeWin32.WIN32_FIND_DATA findData;
            using (NativeWin32.SafeSearchHandle hFindFile = NativeWin32.FindFirstFile( searchPath, out findData ))
            {
                if (!hFindFile.IsInvalid)
                {
                    do
                    {
                        if ((findData.dwFileAttributes & FileAttributes.Directory) == 0 && findData.cFileName != "." && findData.cFileName != "..")
                        {
                            yield return new FileInfo( Path.Combine( target.FullName, findData.cFileName ) );
                        }
                    } while (NativeWin32.FindNextFile( hFindFile, out findData ));
                }
            }

        }
    }
}
/// <summary> A native window 32. </summary>
internal static class NativeWin32
{
    /// <summary> Full pathname of the maximum file. </summary>
    public const int MAX_PATH = 260;

    /// <summary>
    ///     Win32 FILETIME structure.  The win32 documentation says this: "Contains a 64-bit value
    ///     representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).".
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct FILETIME
    {
        /// <summary> The low date time. </summary>
        public uint dwLowDateTime;
        /// <summary> The high date time. </summary>
        public uint dwHighDateTime;
    }

    /// <summary>
    ///     The Win32 find data structure.  The documentation says: "Contains information about the
    ///     file that is found by the FindFirstFile, FindFirstFileEx, or FindNextFile function.".
    /// </summary>
    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
    public struct WIN32_FIND_DATA
    {
        /// <summary> The file attributes. </summary>
        public FileAttributes dwFileAttributes;
        /// <summary> The ft creation time. </summary>
        public FILETIME ftCreationTime;
        /// <summary> The ft last access time. </summary>
        public FILETIME ftLastAccessTime;
        /// <summary> The ft last write time. </summary>
        public FILETIME ftLastWriteTime;
        /// <summary> The file size high. </summary>
        public uint nFileSizeHigh;
        /// <summary> The file size low. </summary>
        public uint nFileSizeLow;
        /// <summary> The reserved 0. </summary>
        public uint dwReserved0;
        /// <summary> The first reserved. </summary>
        public uint dwReserved1;

        /// <summary> Filename of the file. </summary>
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = MAX_PATH )]
        public string cFileName;

        /// <summary> Filename of the alternate file. </summary>
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 14 )]
        public string cAlternateFileName;
    }

    /// <summary>
    ///     Searches a directory for a file or subdirectory with a name that matches a specific name
    ///     (or partial name if wildcards are used).
    /// </summary>
    /// <param name="lpFileName"> The directory or path, and the file name, which can include wildcard
    ///                           characters, for example, an asterisk (*) or a question mark (?). </param>
    /// <param name="lpFindData"> [out] A pointer to the WIN32_FIND_DATA structure that receives
    ///                           information about a found file or directory. </param>
    /// <returns>
    ///     If the function succeeds, the return value is a search handle used in a subsequent call
    ///     to FindNextFile or FindClose, and the lpFindFileData parameter contains information about
    ///     the first file or directory found. If the function fails or fails to locate files from
    ///     the search string in the lpFileName parameter, the return value is INVALID_HANDLE_VALUE
    ///     and the contents of lpFindFileData are indeterminate.
    /// </returns>
    [DllImport( "kernel32", CharSet = CharSet.Auto, SetLastError = true )]
    public static extern SafeSearchHandle FindFirstFile( string lpFileName, out WIN32_FIND_DATA lpFindData );

    /// <summary>
    ///     Continues a file search from a previous call to the FindFirstFile or FindFirstFileEx
    ///     function.
    /// </summary>
    /// <param name="hFindFile">  The search handle returned by a previous call to the FindFirstFile or
    ///                           FindFirstFileEx function. </param>
    /// <param name="lpFindData"> [out] A pointer to the WIN32_FIND_DATA structure that receives
    ///                           information about the found file or subdirectory. The structure
    ///                           can be used in subsequent calls to FindNextFile to indicate from
    ///                           which file to continue the search. </param>
    /// <returns>
    ///     If the function succeeds, the return value is nonzero and the lpFindFileData parameter
    ///     contains information about the next file or directory found. If the function fails, the
    ///     return value is zero and the contents of lpFindFileData are indeterminate.
    /// </returns>
    [DllImport( "kernel32", CharSet = CharSet.Auto, SetLastError = true )]
    public static extern bool FindNextFile( SafeSearchHandle hFindFile, out WIN32_FIND_DATA lpFindData );

    /// <summary>
    ///     Closes a file search handle opened by the FindFirstFile, FindFirstFileEx, or
    ///     FindFirstStreamW function.
    /// </summary>
    /// <param name="hFindFile"> The file search handle. </param>
    /// <returns>
    ///     If the function succeeds, the return value is nonzero. If the function fails, the return
    ///     value is zero.
    /// </returns>
    [DllImport( "kernel32", SetLastError = true )]
    public static extern bool FindClose( IntPtr hFindFile );

    /// <summary>
    ///     Class to encapsulate a seach handle returned from FindFirstFile.  Using a wrapper like
    ///     this ensures that the handle is properly cleaned up with FindClose.
    /// </summary>
    public class SafeSearchHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        ///     Initializes a new instance of the
        ///     NAnt.Core.Util.FileSystemEnumerator.NativeWin32.SafeSearchHandle class.
        /// </summary>
        public SafeSearchHandle() : base( true ) { }

        protected override bool ReleaseHandle()
        {
            return FindClose( handle );
        }
    }
}
