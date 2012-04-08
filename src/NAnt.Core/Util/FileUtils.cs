// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core.Filters;
using NAnt.Core.Types;

namespace NAnt.Core.Util
{
    /// <summary>
    /// Provides modified version for Copy and Move from the File class that 
    /// allow for filter chain processing.
    /// </summary>
    public static class FileUtils {

        #region Private Static Fields

        /// <summary>
        /// Constant buffer size for copy/move functions.
        /// </summary>
        /// <remarks>Default value is 8k</remarks>
        private const int _bufferSize = 8192;

        #endregion Private Static Fields

        #region Public Static Methods

        /// <summary>
        /// Copies a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="sourceFileName">
        /// The file to copy
        /// </param>
        /// <param name="destFileName">
        /// The file to copy to
        /// </param>
        /// <param name="filterChain">
        /// Chain of filters to apply when copying, or <see langword="null" /> is no
        /// filters should be applied.
        /// </param>
        /// <param name="inputEncoding">
        /// The encoding used to read the soure file.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding used to write the destination file.
        /// </param>
        public static void CopyFile(
            string sourceFileName,
            string destFileName,
            FilterChain filterChain,
            Encoding inputEncoding,
            Encoding outputEncoding)
        {
            // If the source file does not exist, throw an exception.
            if (!File.Exists(sourceFileName))
            {
                throw new BuildException(
                    String.Format("Cannot copy file: Source File {0} does not exist",
                        sourceFileName));
            }

            // determine if filters are available
            bool filtersAvailable = !FilterChain.IsNullOrEmpty(filterChain);

            // if no filters have been defined, and no input or output encoding
            // is set, we can just use the File.Copy method
            if (!filtersAvailable && inputEncoding == null && outputEncoding == null)
            {
                File.Copy(sourceFileName, destFileName, true);
            }
            else
            {
                // determine actual input encoding to use. if no explicit input
                // encoding is specified, we'll use the system's current ANSI
                // code page
                Encoding actualInputEncoding = (inputEncoding != null) ?
                    inputEncoding : Encoding.Default;

                // get base filter built on the file's reader.
                // Use the value of _bufferSize as the buffer size.
                using (StreamReader sourceFileReader =
                    new StreamReader(sourceFileName, actualInputEncoding, true, _bufferSize))
                {
                    Encoding actualOutputEncoding = outputEncoding;
                    if (actualOutputEncoding == null)
                    {
                        // if no explicit output encoding is specified, we'll
                        // just use the encoding of the input file as determined
                        // by the runtime
                        // 
                        // Note : the input encoding as specified on the filterchain
                        // might not match the current encoding of the streamreader
                        //
                        // eg. when specifing an ANSI encoding, the runtime might
                        // still detect the file is using UTF-8 encoding, because 
                        // we use BOM detection
                        actualOutputEncoding = sourceFileReader.CurrentEncoding;
                    }

                    // writer for destination file
                    using (StreamWriter destFileWriter =
                        new StreamWriter(destFileName, false, actualOutputEncoding, _bufferSize))
                    {
                        if (filtersAvailable)
                        {
                            Filter baseFilter =
                                filterChain.GetBaseFilter(new PhysicalTextReader(sourceFileReader));

                            bool atEnd = false;
                            int character;
                            while (!atEnd)
                            {
                                character = baseFilter.Read();
                                if (character > -1)
                                {
                                    destFileWriter.Write((char)character);
                                }
                                else
                                {
                                    atEnd = true;
                                }
                            }
                        }
                        else
                        {
                            char[] buffer = new char[_bufferSize];

                            while (true)
                            {
                                int charsRead = sourceFileReader.Read(buffer, 0, buffer.Length);
                                if (charsRead == 0)
                                {
                                    break;
                                }
                                destFileWriter.Write(buffer, 0, charsRead);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moves a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="sourceFileName">
        /// The file to move.
        /// </param>
        /// <param name="destFileName">
        /// The file to move move to.
        /// </param>
        /// <param name="filterChain">
        /// Chain of filters to apply when moving, or <see langword="null" /> is no
        /// filters should be applied.
        /// </param>
        /// <param name="inputEncoding">
        /// The encoding used to read the soure file.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding used to write the destination file.
        /// </param>
        public static void MoveFile(
            string sourceFileName,
            string destFileName,
            FilterChain filterChain,
            Encoding inputEncoding,
            Encoding outputEncoding)
        {
            // If the source file does not exist, throw an exception.
            if (!File.Exists(sourceFileName))
            {
                throw new BuildException(
                    String.Format("Cannot move file: Source File {0} does not exist",
                        sourceFileName));
            }

            // if no filters have been defined, and no input or output encoding
            // is set, we can just use the File.Move method
            if (FilterChain.IsNullOrEmpty(filterChain) &&
                inputEncoding == null && outputEncoding == null)
            {
                File.Move(sourceFileName, destFileName);
            }
            else
            {
                CopyFile(sourceFileName, destFileName, filterChain, inputEncoding, outputEncoding);
                File.Delete(sourceFileName);
            }
        }

        /// <summary>
        /// Copies a directory while filtering its file content through the filter chain.
        /// </summary>
        /// <param name="sourceDirectory">
        /// Source directory to copy from.
        /// </param>
        /// <param name="destDirectory">
        /// Destination directory to copy to.
        /// </param>
        /// <param name="filterChain">
        /// Chain of filters to apply when copying, or <see langword="null" /> is no
        /// filters should be applied.
        /// </param>
        /// <param name="inputEncoding">
        /// The encoding used to read the soure file.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding used to write the destination file.
        /// </param>
        internal static void CopyDirectory(
            string sourceDirectory,
            string destDirectory,
            FilterChain filterChain,
            Encoding inputEncoding,
            Encoding outputEncoding)
        {
            // If the source directory does not exist, throw an exception.
            if (!Directory.Exists(sourceDirectory))
            {
                throw new BuildException(
                    String.Format("Cannot copy directory: Source Directory {0} does not exist",
                        sourceDirectory)
                        );
            }

            // Create the destination directory if it does not exist.
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            // Copy all of the files to the destination directory using
            // the CopyFile static method.
            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                string targetFile = CombinePaths(destDirectory, Path.GetFileName(file));
                CopyFile(file, targetFile, filterChain, inputEncoding, outputEncoding);
            }

            // Copy all of the subdirectory information by calling this method again.
            foreach (string dir in Directory.GetDirectories(sourceDirectory))
            {
                string targetDir = CombinePaths(destDirectory, Path.GetFileName(dir));
                CopyDirectory(dir, targetDir, filterChain, inputEncoding, outputEncoding);
            }
        }

        /// <summary>
        /// Moves a directory while filtering its file content through the filter chain.
        /// </summary>
        /// <param name="sourceDirectory">
        /// Source directory to move from.
        /// </param>
        /// <param name="destDirectory">
        /// Destination directory to move to.
        /// </param>
        /// <param name="filterChain">
        /// Chain of filters to apply when copying, or <see langword="null" /> is no
        /// filters should be applied.
        /// </param>
        /// <param name="inputEncoding">
        /// The encoding used to read the soure file.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding used to write the destination file.
        /// </param>
        internal static void MoveDirectory(
            string sourceDirectory,
            string destDirectory,
            FilterChain filterChain,
            Encoding inputEncoding,
            Encoding outputEncoding)
        {

            // If the source directory does not exist, throw an exception.
            if (!Directory.Exists(sourceDirectory))
            {
                throw new BuildException(
                    String.Format("Cannot move directory: Source Directory {0} does not exist",
                        sourceDirectory));
            }

            // if no filters have been defined, and no input or output encoding
            // is set, proceed with a straight directory move.
            if (FilterChain.IsNullOrEmpty(filterChain) &&
                inputEncoding == null &&
                outputEncoding == null)
            {

                // If the source & target paths are completely the same, including
                // case, throw an exception.
                if (sourceDirectory.Equals(destDirectory, StringComparison.InvariantCulture))
                {
                    throw new BuildException("Source and Target paths are identical");
                }

                try
                {
                    // wants to rename a directory with the same name but different casing
                    // (ie: C:\nant to C:\NAnt), then the move needs to be staged.
                    if (PlatformHelper.IsWindows)
                    {
                        // If the directory names are the same but different casing, stage
                        // the move by moving the source directory to a temp location
                        // before moving it to the destination.
                        if (sourceDirectory.Equals(destDirectory,
                            StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Since the directory is being renamed with different
                            // casing, the temp directory should be in the same
                            // location as the destination directory to avoid
                            // possible different volume errors.
                            string rootPath = Directory.GetParent(destDirectory).FullName;
                            string stagePath =
                                Path.Combine(rootPath, Path.GetRandomFileName());

                            try
                            {
                                // Move the source dir to the stage path
                                // before moving everything to the destination
                                // path.
                                Directory.Move(sourceDirectory, stagePath);
                                Directory.Move(stagePath, destDirectory);
                            }
                            catch
                            {
                                // If an error occurred during the staged directory
                                // move, check to see if the stage path exists.  If
                                // the source directory successfully moved to the
                                // stage path, move the stage path back to the
                                // source path and rethrow the exception.
                                if (Directory.Exists(stagePath))
                                {
                                    if (!Directory.Exists(sourceDirectory))
                                    {
                                        Directory.Move(stagePath, sourceDirectory);
                                    }
                                }
                                throw;
                            }
                        }
                        // If the directory source and destination names are
                        // different, use Directory.Move.
                        else
                        {
                            Directory.Move(sourceDirectory, destDirectory);
                        }
                    }

                    // Non-Windows systems, such as Linux/Unix, filenames and directories
                    // are case-sensitive. So as long as the directory names are not
                    // identical, with the check above, the Directory.Move method
                    // can be used.
                    else
                    {
                        Directory.Move(sourceDirectory, destDirectory);
                    }
                }
                // Catch and rethrow any IO exceptions that may arise during
                // the directory move.
                catch (IOException ioEx)
                {
                    // If the error occurred because the destination directory
                    // exists, throw a build exception to tell the user that the
                    // destination directory already exists.
                    if (Directory.Exists(destDirectory))
                    {
                        throw new BuildException(
                            string.Format(CultureInfo.InvariantCulture,
                            "Failed to move directory {0}." +
                            "Directory '{1}' already exists.",
                            sourceDirectory, destDirectory));
                    }
                    // Any other IOExceptions should be displayed to the user
                    // via build exception.
                    else
                    {
                        throw new BuildException(
                            String.Format("Unhandled IOException when trying to move directory '{0}' to '{1}'",
                            sourceDirectory, destDirectory), ioEx);
                    }
                }
            }
            else
            {
                // Otherwise, use the copy directory method and directory.delete
                // method to move the directory over.
                CopyDirectory(sourceDirectory, destDirectory, filterChain,
                    inputEncoding, outputEncoding);
                Directory.Delete(sourceDirectory, true);
            }
        }

        /// <summary>
        /// Generates a new temporary directory name based on the system's
        /// temporary path.
        /// </summary>
        /// <returns>
        /// The temp directory name.
        /// </returns>
        internal static string GetTempDirectoryName()
        {
            return CombinePaths(Path.GetTempPath(), Path.GetRandomFileName());
        }

        /// <summary>
        /// Reads a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="fileName">The file to read.</param>
        /// <param name="filterChain">Chain of filters to apply when reading, or <see langword="null" /> is no filters should be applied.</param>
        /// <param name="inputEncoding">The encoding used to read the file.</param>
        /// <remarks>
        /// If <paramref name="inputEncoding" /> is <see langword="null" />,
        /// then the system's ANSI code page will be used to read the file.
        /// </remarks>
        public static string ReadFile(string fileName, FilterChain filterChain, Encoding inputEncoding) {
            string content = null;

            // determine character encoding to use
            Encoding encoding = (inputEncoding != null) ? inputEncoding : Encoding.Default;

            // read file
            using (StreamReader sr = new StreamReader(fileName, encoding, true)) {
                if (filterChain == null || filterChain.Filters.Count == 0) {
                    content = sr.ReadToEnd();
                } else {
                    Filter baseFilter = filterChain.GetBaseFilter(
                        new PhysicalTextReader(sr));

                    StringWriter sw = new StringWriter();
                    while (true) {
                        int character = baseFilter.Read();
                        if (character == -1)
                            break;
                        sw.Write((char) character);
                    }
                    content = sw.ToString();
                }
            }

            return content;
        }

        /// <summary>
        /// Returns a uniquely named empty temporary directory on disk.
        /// </summary>
        /// <value>
        /// A <see cref="DirectoryInfo" /> representing the temporary directory.
        /// </value>
        public static DirectoryInfo GetTempDirectory() {
            // create a uniquely named zero-byte file
            string tempFile = Path.GetTempFileName();
            // remove the temporary file
            File.Delete(tempFile);
            // create a directory named after the unique temporary file
            Directory.CreateDirectory(tempFile);
            // return the 
            return new DirectoryInfo(tempFile);
        }

        /// <summary>
        /// Combines two path strings.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>
        /// A string containing the combined paths. If one of the specified 
        /// paths is a zero-length string, this method returns the other path. 
        /// If <paramref name="path2" /> contains an absolute path, this method 
        /// returns <paramref name="path2" />.
        /// </returns>
        /// <remarks>
        ///   <para>On *nix, processing is delegated to <see cref="System.IO.Path.Combine(string, string)" />.</para>
        ///   <para>
        ///   On Windows, this method normalized the paths to avoid running into
        ///   the 260 character limit of a path and converts forward slashes in 
        ///   both <paramref name="path1" /> and <paramref name="path2" /> to 
        ///   the platform's directory separator character.
        ///   </para>
        /// </remarks>
        public static string CombinePaths(string path1, string path2) {
            if (PlatformHelper.IsUnix) {
                return Path.Combine(path1, path2);
            }

            if (path1 == null) {
                throw new ArgumentNullException("path1");
            }
            if (path2 == null) {
                throw new ArgumentNullException("path2");
            }

            if (Path.IsPathRooted(path2)) {
                return path2;
            }

            char separatorChar = Path.DirectorySeparatorChar;
            char[] splitChars = new char[] {'/', separatorChar};

            // Now we split the Path by the Path Separator
            String[] path2Parts = path2.Split(splitChars);

            ArrayList arList = new ArrayList();
            
            // for each Item in the path that differs from ".." we just add it 
            // to the ArrayList, but skip empty parts
            for (int iCount = 0; iCount < path2Parts.Length; iCount++) {
                string currentPart = path2Parts[iCount];

                // skip empty parts or single dot parts
                if (currentPart.Length == 0 || currentPart == ".") {
                    continue;
                }

                // if we get a ".." Try to remove the last item added (as if 
                // going up in the Directory Structure)
                if (currentPart == "..") {
                    if (arList.Count > 0 && ((string) arList[arList.Count - 1] != "..")) {
                        arList.RemoveAt(arList.Count -1);
                    } else {
                        arList.Add(currentPart);
                    }
                } else {
                    arList.Add(currentPart);
                }
            }

            bool trailingSeparator = (path1.Length > 0 && path1.IndexOfAny(splitChars, path1.Length - 1) != -1);
            
            // if the first path ends in directory seperator character, then 
            // we need to omit that trailing seperator when we split the path
            string[] path1Parts;
            if (trailingSeparator) {
                path1Parts = path1.Substring(0, path1.Length - 1).Split(splitChars);
            } else {
                path1Parts = path1.Split(splitChars);
            }
            
            int counter = path1Parts.Length;

            // if the second path starts with parts to move up the directory tree, 
            // then remove corresponding parts in the first path
            //
            // eg. path1 = d:\whatever\you\want\to\do 
            //     path2 = ../../test
            //     
            //     ->
            //
            //     path1 = d:\whatever\you\want
            //     path2 = test
            ArrayList arList2 = (ArrayList) arList.Clone();
            for (int i = 0; i < arList2.Count; i++) {
                // never discard first part of path1
                if ((string) arList2[i] != ".." || counter < 2) {
                    break;
                }

                // skip part of current directory
                counter--;

                arList.RemoveAt(0);
            }

            string separatorString = separatorChar.ToString(CultureInfo.InvariantCulture);

            // if path1 only has one remaining part, and the original path had
            // a trailing separator character or the remaining path had multiple
            // parts (which were discarded by a relative path in path2), then
            // add separator to remaining part
            if (counter == 1 && (trailingSeparator || path1Parts.Length > 1)) {
                path1Parts[0] += separatorString;
            }

            string combinedPath = Path.Combine(string.Join(separatorString, path1Parts,
                0, counter), string.Join(separatorString, (String[]) arList.ToArray(typeof(String))));

            // if path2 ends in directory separator character, then make sure
            // combined path has trailing directory separator character
            if (path2.EndsWith("/") || path2.EndsWith(separatorString)) {
                combinedPath += Path.DirectorySeparatorChar;
            }

            return combinedPath;
        }

        /// <summary>
        /// Returns Absolute Path (Fix for 260 Char Limit of Path.GetFullPath(...))
        /// </summary>
        /// <param name="path">The file or directory for which to obtain absolute path information.</param>
        /// <returns>Path Resolved</returns>
        /// <exception cref="ArgumentException">path is a zero-length string, contains only white space or contains one or more invalid characters as defined by <see cref="Path.InvalidPathChars" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
        public static string GetFullPath(string path) {
            if (path == null) {
                throw new ArgumentNullException("path");
            }

            if (PlatformHelper.IsUnix || Path.IsPathRooted(path)) {
                return Path.GetFullPath(path);
            }

            if (path.Length == 0 || path.Trim().Length == 0 || path.IndexOfAny(Path.InvalidPathChars) != -1) {
                throw new ArgumentException("The path is not of a legal form.");
            }

            string combinedPath = FileUtils.CombinePaths(
                Directory.GetCurrentDirectory(), path);

            return Path.GetFullPath(combinedPath);
        }

        /// <summary>
        /// Returns the home directory of the current user.
        /// </summary>
        /// <returns>
        /// The home directory of the current user.
        /// </returns>
        public static string GetHomeDirectory() {
            if (PlatformHelper.IsUnix) {
                return Environment.GetEnvironmentVariable("HOME");
            } else {
                return Environment.GetEnvironmentVariable("USERPROFILE");
            }
        }

        /// <summary>
        /// Scans a list of directories for the specified filename.
        /// </summary>
        /// <param name="directories">The list of directories to search.</param>
        /// <param name="fileName">The name of the file to look for.</param>
        /// <param name="recursive">Specifies whether the directory should be searched recursively.</param>
        /// <remarks>
        /// The directories are scanned in the order in which they are defined.
        /// </remarks>
        /// <returns>
        /// The absolute path to the specified file, or null if the file was
        /// not found.
        /// </returns>
        public static string ResolveFile(string[] directories, string fileName, bool recursive) {
            if (directories == null)
                throw new ArgumentNullException("directories");
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            string resolvedFile = null;

            foreach (string directory in directories) {
                if (!Directory.Exists(directory))
                    continue;

                resolvedFile = ScanDirectory (directory, fileName, recursive);
                if (resolvedFile != null)
                    break;
            }

            return resolvedFile;
        }

        #endregion Public Static Methods

        #region Private Static Methods

        private static string ScanDirectory(string directory, string fileName, bool recursive) {
            string absolutePath = Path.Combine(directory, fileName);
            if (File.Exists(absolutePath))
                return absolutePath;

            if (!recursive)
                return null;

            string[] subDirs = Directory.GetDirectories(directory);
            foreach (string subDir in subDirs) {
                absolutePath = ScanDirectory (Path.Combine (directory, subDir),
                    fileName, recursive);
                if (absolutePath != null)
                    return absolutePath;
            }

            return null;
        }

        #endregion Private Static Methods

    }
}
