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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Kevin Dente (kevindente@yahoo.com)

/*
Examples:
"**\*.class" matches all .class files/dirs in a directory tree.

"test\a??.java" matches all files/dirs which start with an 'a', then two
more characters and then ".java", in a directory called test.

"**" matches everything in a directory tree.

"**\test\**\XYZ*" matches all files/dirs that start with "XYZ" and where
there is a parent directory called test (e.g. "abc\test\def\ghi\XYZ123").

Example of usage:

DirectoryScanner scanner = DirectoryScanner();
scanner.Includes.Add("**\\*.class");
scanner.Exlucdes.Add("modules\\*\\**");
scanner.BaseDirectory = "test";
scanner.Scan();
foreach (string filename in GetIncludedFiles()) {
    Console.WriteLine(filename);
}
*/

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace SourceForge.NAnt {
    /// <summary>Used for searching file system based on given include/exclude rules.</summary>
    /// <example>
    ///     <para>Simple client code for testing the class.</para>
    ///     <code>
    ///         while(true) {
    ///             DirectoryScanner scanner = new DirectoryScanner();
    ///
    ///             Console.Write("Scan Basedirectory : ");
    ///             string s = Console.ReadLine();
    ///             if (s == "") break;
    ///             scanner.BaseDirectory = s;
    ///
    ///             while(true) {
    ///                 Console.Write("Include pattern : ");
    ///                 s = Console.ReadLine();
    ///                 if (s == "") break;
    ///                 scanner.Includes.Add(s);
    ///             }
    ///
    ///             while(true) {
    ///                 Console.Write("Exclude pattern : ");
    ///                 s = Console.ReadLine();
    ///                 if (s == "") break;
    ///                 scanner.Excludes.Add(s);
    ///             }
    ///
    ///             foreach (string name in scanner.FileNames)
    ///                 Console.WriteLine("file:" + name);
    ///             foreach (string name in scanner.DirectoryNames)
    ///                 Console.WriteLine("dir :" + name);
    ///
    ///             Console.WriteLine("");
    ///         }
    ///     </code>
    /// </example>
    /// <history>
    ///     <change date="20020220" author="Ari Hännikäinen">Added support for absolute paths and relative paths refering to parent directories ( ../ )</change>
    ///     <change date="20020221" author="Ari Hännikäinen">Changed implementation because of performance reasons - now scanning each directory only once</change>
    ///     <change date="20030224" author="Brian Deacon (bdeacon at vidya dot com)">
    ///         Fixed a bug that was causing absolute pathnames to turn into an invalid regex pattern, and thus never match.
    ///     </change>
    /// </history>
    public class DirectoryScanner {

        // Set to current directory in Scan if user doesn't specify something first.
        // Keeping it null, lets the user detect if it's been set or not.
        string _baseDirectory = null; 

        // holds the nant patterns (absolute or relative paths)
        StringCollection _includes = new StringCollection();
        StringCollection _excludes = new StringCollection();

        // holds the nant patterns converted to regular expression patterns (absolute canonized paths)
        StringCollection _includePatterns = null;
        StringCollection _excludePatterns = null;

        // holds the result from a scan
        StringCollection _fileNames = null;
        StringCollection _directoryNames = null;

        // directories that should be scanned and directories scanned so far
        StringCollection _searchDirectories = null;
        StringCollection _pathsAlreadySearched = null;
        ArrayList	 _searchDirIsRecursive = null;

        public StringCollection Includes {
            get { return _includes; }
        }

        public StringCollection Excludes {
            get { return _excludes; }
        }

        public string BaseDirectory {
            get { return _baseDirectory; }
            set { _baseDirectory = value; }
        }

        public StringCollection FileNames {
            get {
                if (_fileNames == null) {
                    Scan();
                }
                return _fileNames;
            }
        }

        public StringCollection DirectoryNames {
            get {
                if (_directoryNames == null) {
                    Scan();
                }
                return _directoryNames;
            }
        }



        /// <summary>
        ///     Use Includes and Excludes search criteria (relative to Basedirectory or absolute) to search for filesystem objects
        /// </summary>
        /// <history>
        ///     <change date="20020220" author="Ari Hännikäinen">Totally changed the scanning strategy</change>
        ///     <change date="20020221" author="Ari Hännikäinen">Changed it again because of performance reasons</change>
        /// </history>
        public void Scan() {
            if (BaseDirectory == null) {
                BaseDirectory = Environment.CurrentDirectory;
            }
    
            _includePatterns = new StringCollection();
            _excludePatterns = new StringCollection();
            _fileNames = new StringCollection();
            _directoryNames = new StringCollection();
            _searchDirectories = new StringCollection();
            _searchDirIsRecursive = new ArrayList();
            _pathsAlreadySearched = new StringCollection();

            // convert given NAnt patterns to regex patterns with absolute paths
            // side effect: searchDirectories will be populated
            convertPatterns(_includes, _includePatterns, true);
            convertPatterns(_excludes, _excludePatterns, true);
            
            for (int index = 0; index < _searchDirectories.Count; index++) {
                ScanDirectory(_searchDirectories[index], (bool)_searchDirIsRecursive[index]);
            }
        }


        /// <summary>
        ///     Parses given NAnt search patterns for search directories and corresponding regex patterns
        /// </summary>
        /// <param name="nantPatterns">In. NAnt patterns. Absolute or relative paths.</param>
        /// <param name="regexPatterns">Out. Regex patterns. Absolute canonical paths.</param>
        /// <param name="isInclude">In.</param>
        /// <history>
        ///     <change date="20020221" author="Ari Hännikäinen">Created</change>
        /// </history>
        public void convertPatterns(StringCollection nantPatterns, StringCollection regexPatterns, bool isInclude) {
            string searchDirectory;
            string regexPattern;
            bool isRecursive;
            foreach (string nantPattern in nantPatterns) {
                parseSearchDirectoryAndPattern(nantPattern, out searchDirectory, out isRecursive, out regexPattern);
                if (!isInclude)
                    continue;
                int index = _searchDirectories.IndexOf(searchDirectory);
                // If the directory was found before, but wasn't recursive and is now, mark it as so
                if (index > -1) {
                    if (!(bool)_searchDirIsRecursive[index] && isRecursive) {
                        _searchDirIsRecursive[index] = isRecursive;
                    }
                }
                // If the directory has not been added, add it
                if (index == -1) {
                    _searchDirectories.Add(searchDirectory);
                    _searchDirIsRecursive.Add(isRecursive);
                }
                if (!regexPatterns.Contains(regexPattern))
                    regexPatterns.Add(regexPattern);
            }
        }


        
        /// <summary>
        ///     Given a NAnt search pattern returns a search directory and an regex search pattern.
        /// </summary>
        /// <param name="originalNAntPattern">NAnt searh pattern (relative to the Basedirectory OR absolute, relative paths refering to parent directories ( ../ ) also supported)</param>
        /// <param name="searchDirectory">Out. Absolute canonical path to the directory to be searched</param>
        /// <param name="recursive">Out. Whether the pattern is potentially recursive or not</param>
        /// <param name="regexPattern">Out. Regex search pattern (absolute canonical path)</param>
        /// <history>
        ///     <change date="20020220" author="Ari Hännikäinen">Created</change>
        ///     <change date="20020221" author="Ari Hännikäinen">Returning absolute regex patterns instead of relative nant patterns</change>
        ///     <change date="20030224" author="Brian Deacon (bdeacon at vidya dot com)">
        ///     Added replacing of slashes with Path.DirectorySeparatorChar to make this OS-agnostic.  Also added the Path.IsPathRooted check
        ///     to support absolute pathnames to prevent basedir = "/foo/bar" and pattern="/fudge/nugget" from being incorrectly turned into 
        ///     "/foo/bar/fudge/nugget".  (pattern = "fudge/nugget" would still be treated as relative to basedir)
        ///     </change>
        /// </history>
        public void parseSearchDirectoryAndPattern(string originalNAntPattern, out string searchDirectory, out bool recursive, out string regexPattern) {
            string s = originalNAntPattern;
            s = s.Replace('\\', Path.DirectorySeparatorChar);
            s = s.Replace('/', Path.DirectorySeparatorChar);

            // Get indices of pieces used for recursive check only
            int indexOfFirstDirectoryWildcard = s.IndexOf("**");
            int indexOfLastOriginalDirectorySeparator = s.LastIndexOf(Path.DirectorySeparatorChar);

            // search for the first wildcard character (if any) and exclude the rest of the string beginnning from the character
            char[] wildcards = {'?', '*'};
            int indexOfFirstWildcard = s.IndexOfAny(wildcards);
            if (indexOfFirstWildcard != -1) { // if found any wildcard characters
                s = s.Substring(0, indexOfFirstWildcard);
            }

            // find the last DirectorySeparatorChar (if any) and exclude the rest of the string
            int indexOfLastDirectorySeparator = s.LastIndexOf(Path.DirectorySeparatorChar);

            // The pattern is potentially recursive if and only if more than one base directory could be matched.
            // ie: 
            //    **
            //    **/*.txt
            //    foo*/xxx
            //    x/y/z?/www
            // This condition is true if and only if:
            //  - The first wildcard is before the last directory separator, or
            //  - The pattern contains a directory wildcard ("**")
            recursive = ( indexOfFirstWildcard < indexOfLastOriginalDirectorySeparator ) || indexOfFirstDirectoryWildcard != -1;

            // substring preceding the separator represents our search directory and the part following it represents nant search pattern relative to it            
            if (indexOfLastDirectorySeparator != -1) {
                s = originalNAntPattern.Substring(0, indexOfLastDirectorySeparator);
            } else {
                s = "";
            }
            
            //We only prepend BaseDirectory when s represents a relative path.
            if (Path.IsPathRooted(s)) {
                searchDirectory = new DirectoryInfo(s).FullName;
            }
            else {
                //We also (correctly) get to this branch of code when s == ""
                searchDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, s)).FullName;
            }
            
            string modifiedNAntPattern = originalNAntPattern.Substring(indexOfLastDirectorySeparator + 1);
            regexPattern = ToRegexPattern(searchDirectory, modifiedNAntPattern);

            //Specify pattern as case-insensitive if appropriate to this file system.
            if (!IsCaseSensitiveFileSystem()) {
                regexPattern = "(?i)" + regexPattern;
            }
            
            
        }

        bool IsCaseSensitiveFileSystem() {
            //Windows (not case-sensitive) is backslash, others (e.g. Unix) are not
            return (Path.DirectorySeparatorChar != '\\'); 
        }

        /// <summary>
        ///     Searches a directory recursively for files and directories matching the search criteria
        /// </summary>
        /// <param name="path">Directory in which to search (absolute canonical path)</param>
        /// <param name="recursive">Whether to scan recursively or not</param>
        /// <history>
        ///     <change date="20020221" author="Ari Hännikäinen">Checking if the directory has already been scanned</change>
        /// </history>
        void ScanDirectory(string path, bool recursive) {
            // scan each directory only once
            if (_pathsAlreadySearched.Contains(path)) {
                return;
            }
            _pathsAlreadySearched.Add(path);

            //if the path doesn't exist, return.
            if(!Directory.Exists(path)) {
                return;
            }

            // get info for the current directory
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(path);
            
            bool caseSensitive = VolumeInfo.IsVolumeCaseSensitive(new Uri(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar));

            foreach (DirectoryInfo directoryInfo in currentDirectoryInfo.GetDirectories()) {
                if (recursive) {
                    // scan subfolders if we are running recursively
                    ScanDirectory(directoryInfo.FullName, true);
                } else {
                    // otherwise just test to see if the subdirectories are included
                    if (IsPathIncluded(directoryInfo.FullName, caseSensitive)) {
                        _directoryNames.Add(directoryInfo.FullName);
                    }
                }
            }

            // scan files
            foreach (FileInfo fileInfo in currentDirectoryInfo.GetFiles()) {
                string filename = Path.Combine(path, fileInfo.Name);
                if (IsPathIncluded(filename, caseSensitive)) {
                    _fileNames.Add(filename);
                }
            }

            // Check current path last so that delete task will correctly
            // delete empty directories.  This may *seem* like a special case
            // but it is more like formalizing something in a way that makes
            // writing the delete task easier :)
            if (IsPathIncluded(path, caseSensitive)) {
                _directoryNames.Add(path);
            }
        }


        bool IsPathIncluded(string path) {
            return IsPathIncluded(path, false);
        }

        bool IsPathIncluded(string path, bool caseSensitive) {
            bool included = false;
            
            RegexOptions regexOptions = RegexOptions.None;
            if (!caseSensitive) {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            // check path against includes
            foreach (string pattern in _includePatterns) {
                Match m = Regex.Match(path, pattern, regexOptions);
                if (m.Success) {
                    included = true;
                    break;
                }
            }

            // check path against excludes
            if (included) {
                foreach (string pattern in _excludePatterns) {
                    Match m = Regex.Match(path, pattern, regexOptions);
                    if (m.Success) {
                        included = false;
                        break;
                    }
                }
            }

            return included;
        }

        /// <summary>
        ///     Converts NAnt search pattern to a regular expression pattern
        /// </summary>
        /// <param name="baseDir">Base directory for the search</param>
        /// <param name="nantPattern">Search pattern relative to the search directory</param>
        /// <returns>Regular expresssion (absolute path) for searching matching file/directory names</returns>
        /// <history>
        ///     <change date="20020220" author="Ari Hännikäinen">Added parameter baseDir, using  it instead of class member variable</change>
        /// </history>
        static string ToRegexPattern(string baseDir, string nantPattern) {

            StringBuilder pattern = new StringBuilder(nantPattern);

            // NAnt patterns can use either / \ as a directory seperator.
            // We must replace both of these characters with Path.DirectorySeperatorChar
            pattern.Replace('/',  Path.DirectorySeparatorChar);
            pattern.Replace('\\', Path.DirectorySeparatorChar);

            // Patterns MUST be full paths.
            if (!Path.IsPathRooted(pattern.ToString())) {
                pattern = new StringBuilder(Path.Combine(baseDir, pattern.ToString()));
            }

            // The '\' character is a special character in regular expressions
            // and must be escaped before doing anything else.
            pattern.Replace(@"\", @"\\");

            // Escape the rest of the regular expression special characters.
            // NOTE: Characters other than . $ ^ { [ ( | ) * + ? \ match themselves.
            // TODO: Decide if ] and } are missing from this list, the above
            // list of characters was taking from the .NET SDK docs.
            pattern.Replace(".", @"\.");
            pattern.Replace("$", @"\$");
            pattern.Replace("^", @"\^");
            pattern.Replace("{", @"\{");
            pattern.Replace("[", @"\[");
            pattern.Replace("(", @"\(");
            pattern.Replace(")", @"\)");
            pattern.Replace("+", @"\+");

            // Special case directory seperator string under Windows.
            string seperator = Path.DirectorySeparatorChar.ToString();
            if (seperator == @"\") {
                seperator = @"\\";
            }

            // Convert NAnt pattern characters to regular expression patterns.

            // SPECIAL CASE: to match subdirectory OR current directory.  If
            // we don't do this then we can write something like 'src/**/*.cs'
            // to match all the files ending in .cs in the src directory OR
            // subdirectories of src.
            pattern.Replace(seperator + "**", "(" + seperator + ".|)|");

            // | is a place holder for * to prevent it from being replaced in next line
            pattern.Replace("**", ".|");
            pattern.Replace("*", "[^" + seperator + "]*");
            pattern.Replace("?", "[^" + seperator + "]?");
            pattern.Replace('|', '*'); // replace place holder string

            // Help speed up the search
            pattern.Insert(0, '^'); // start of line
            pattern.Append('$'); // end of line

            return pattern.ToString();
        }
    }
}
