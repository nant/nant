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

using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class DirectoryScannerTest : BuildTestBase {
        #region Private Instance Fields

        private string _folder1;
        private string _folder2;
        private string _folder3;
        private DirectoryScanner _scanner;

        #endregion Private Instance Fields

        #region Public Instance Methods

        [Test]
        public void CaseSensitive () {
            Assert.AreEqual (PlatformHelper.IsUnix, _scanner.CaseSensitive, "#1");
            _scanner.CaseSensitive = true;
            Assert.IsTrue (_scanner.CaseSensitive, "#2");
            _scanner.CaseSensitive = false;
            Assert.IsFalse (_scanner.CaseSensitive, "#3");
        }

        [Test]
        public void Matching_CaseInsensitive () {
            string folder4 = Path.Combine(TempDirName, "FoldeR4");
            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(folder4, "FileB.tlB"));
            TempFile.Create(Path.Combine(folder4, "FileC.tlb"));

            _scanner.CaseSensitive = false;
            _scanner.Includes.Add ("Folder*/fIlEb.t*");
            _scanner.Includes.Add ("Folde*2/fOldeR3/fIleA.tx*");
            Assert.AreEqual (2, _scanner.FileNames.Count, "#1");

            File.Delete (Path.Combine(folder4, "FileB.tlB"));

            _scanner.CaseSensitive = false;
            Assert.AreEqual (2, _scanner.FileNames.Count, "#2");

            _scanner.CaseSensitive = true;
            _scanner.CaseSensitive = false;
            Assert.AreEqual (1, _scanner.FileNames.Count, "#3");
        }

        [Test]
        public void Matching_CaseSensitive () {
            string folder4 = Path.Combine(TempDirName, "FoldeR4");
            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(folder4, "FileB.tlB"));
            TempFile.Create(Path.Combine(folder4, "FileC.tlb"));

            _scanner.CaseSensitive = true;
            _scanner.Includes.Add ("Folder4/fIlEb.t*");
            _scanner.Includes.Add ("fOldeR3/FiLeA.txt");
            Assert.AreEqual (0, _scanner.FileNames.Count, "#1");

            _scanner.Includes.Clear ();
            _scanner.Includes.Add ("FoldeR4/FileB.t*");
            _scanner.Includes.Add ("folder2/folder3/filea.txt");
            _scanner.Scan ();
            Assert.AreEqual (2, _scanner.FileNames.Count, "#1");

            File.Delete (Path.Combine(folder4, "FileB.tlB"));

            _scanner.CaseSensitive = true;
            Assert.AreEqual (2, _scanner.FileNames.Count, "#2");

            _scanner.CaseSensitive = false;
            _scanner.CaseSensitive = true;
            Assert.AreEqual (1, _scanner.FileNames.Count, "#3");
        }

        [Test]
        public void IncludeNames_Unix () {
            if (!PlatformHelper.IsUnix) {
                return;
            }

            string folder4 = Path.Combine(TempDirName, "FoldeR4");
            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(folder4, "FileB.tlB"));
            TempFile.Create(Path.Combine(folder4, "FileC.tlb"));

            _scanner.Includes.Add ("Folder4/fIlEb.tlb");
            _scanner.Includes.Add ("FoldeR2/fOldeR3/fIleA.txt");
            Assert.AreEqual (0, _scanner.FileNames.Count, "#1");

            _scanner.CaseSensitive = false;
            Assert.AreEqual (0, _scanner.FileNames.Count, "#2");

            _scanner.CaseSensitive = true;
            _scanner.Includes.Clear ();
            _scanner.Includes.Add ("FoldeR4/fIlEb.tlb");
            _scanner.Includes.Add ("folder2/folder3/fIleA.txt");
            Assert.AreEqual (0, _scanner.FileNames.Count, "#3");

            _scanner.CaseSensitive = false;
            Assert.AreEqual (2, _scanner.FileNames.Count, "#4");

            _scanner.CaseSensitive = true;
            _scanner.Includes.Clear ();
            _scanner.Includes.Add ("Folder4/FileB.tlB");
            _scanner.Includes.Add ("FoldeR2/fOldeR3/filea.txt");
            Assert.AreEqual (0, _scanner.FileNames.Count, "#5");

            _scanner.CaseSensitive = false;
            Assert.AreEqual (0, _scanner.FileNames.Count, "#6");
        }

        [Test]
        public void IncludeNames_Windows () {
            if (PlatformHelper.IsUnix) {
                return;
            }

            string folder4 = Path.Combine(TempDirName, "FoldeR4");
            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(folder4, "FileB.tlB"));
            TempFile.Create(Path.Combine(folder4, "FileC.tlb"));

            _scanner.Includes.Add ("Folder4/fIlEb.tlb");
            _scanner.Includes.Add ("FoldeR2/fOldeR3/fIleA.txt");
            Assert.AreEqual (2, _scanner.FileNames.Count, "#1");

            _scanner.CaseSensitive = true;
            Assert.AreEqual (0, _scanner.FileNames.Count, "#2");

            _scanner.CaseSensitive = false;
            _scanner.Includes.Clear ();
            _scanner.Includes.Add ("FoldeR4/fIlEb.tlb");
            _scanner.Includes.Add ("folder2/folder3/fIleA.txt");
            Assert.AreEqual (2, _scanner.FileNames.Count, "#3");

            _scanner.CaseSensitive = true;
            Assert.AreEqual (0, _scanner.FileNames.Count, "#4");

            _scanner.CaseSensitive = false;
            _scanner.Includes.Clear ();
            _scanner.Includes.Add ("Folder4/FileB.tlB");
            _scanner.Includes.Add ("FoldeR2/fOldeR3/filea.txt");
            Assert.AreEqual (2, _scanner.FileNames.Count, "#5");

            _scanner.CaseSensitive = true;
            Assert.AreEqual (2, _scanner.FileNames.Count, "#6");
        }

        /// <summary>Test ? wildcard and / seperator.</summary>
        /// <remarks>
        ///   Matches all the files in the folder2 directory that being with Foo 
        ///   and one extra character and end with .txt.
        /// </remarks>
        [Test]
        public void Test_WildcardMatching1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo.txt"),
                                                          Path.Combine(_folder2, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo3"),
                                                          Path.Combine(_folder2, "Foo4.bar"),
                                                          Path.Combine(_folder1, "Foo5.txt"),
                                                          Path.Combine(_folder3, "Foo6.txt")
                                                      };
            _scanner.Includes.Add("folder2/Foo?.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test * wildcard.</summary>
        /// <remarks>
        ///   Matches all the files in base directory that end with .txt.
        /// </remarks>
        [Test]
        public void Test_WildcardMatching2() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            _scanner.Includes.Add(@"*.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>
        /// Test * wildcard with basedirectory ending with directory separator 
        /// character.
        /// </summary>
        [Test]
        public void Test_WildcardMatching3() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };

            // ensure base directory ends with directory separator character
            if (!_scanner.BaseDirectory.FullName.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                _scanner.BaseDirectory = new DirectoryInfo(_scanner.BaseDirectory.FullName 
                    + Path.DirectorySeparatorChar);
            }
            _scanner.Includes.Add(@"Foo*.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>
        /// Test * wildcard with basedirectory ending with slash
        /// character.
        /// </summary>
        /// <remarks>
        ///   Matches all the files in base directory that end with .txt.
        /// </remarks>
        [Test]
        public void Test_WildcardMatching4() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };

            // ensure base directory ends with slash
            if (!_scanner.BaseDirectory.FullName.EndsWith("/") &&  !_scanner.BaseDirectory.FullName.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                _scanner.BaseDirectory = new DirectoryInfo(_scanner.BaseDirectory.FullName 
                    + "/");
            }
            _scanner.Includes.Add(@"Foo*.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>
        /// Test wildcard matching base directory.
        /// </summary>
        [Test]
        public void Test_WildcardMatching_BaseDirectory1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo2.txt")
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo1.txt")
                                                      };
            string[] includedDirs = new string[] {
                                                     _folder3
                                                 };
            string[] excludedDirs = new string[] {
                                                     TempDirName,
                                                     _folder2
                                                 };

            // the folder2 directory should NOT be matched
            _scanner.Includes.Add("folder2/**/*");
            _scanner.Excludes.Add("folder2/**/Foo1.txt");

            // the folder2 directory should not be included in the DirectoryNames 
            // collection
            CheckScan(includedFileNames, excludedFileNames, includedDirs, excludedDirs);
        }

        /// <summary>
        /// Test wildcard matching base directory.
        /// </summary>
        [Test]
        public void Test_WildcardMatching_BaseDirectory2() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo2.txt")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo1.txt")
                                                      };
            string[] includedDirs = new string[] {
                                                     _folder2,
                                                     _folder3
                                                 };
            string[] excludedDirs = new string[] {
                                                     TempDirName,
                                                 };

            // the folder2 directory should now be matched
            _scanner.Includes.Add("folder2/**");
            _scanner.Excludes.Add("folder2/**/Foo1.txt");

            // the folder2 directory should be included in the DirectoryNames 
            // collection
            CheckScan(includedFileNames, excludedFileNames, includedDirs, excludedDirs);
        }

        /// <summary>
        /// Test wildcard matching base directory.
        /// </summary>
        [Test]
        public void Test_WildcardMatching_BaseDirectory3() {
            string tempDirBin = Path.Combine(TempDirName, "bin");
            string folder1Bin = Path.Combine(_folder1, "bin");
            string folder2Bin = Path.Combine(_folder2, "bin");
            string folder3Bin = Path.Combine(_folder3, "bin");
            string folder2BinTest = Path.Combine(folder2Bin, "test");

            string[] includedFileNames = new string[] {
                                                          Path.Combine(tempDirBin, "Foo2.txt"),
                                                          Path.Combine(folder1Bin, "Foo.whatever"),
                                                          Path.Combine(folder2BinTest, "whatever.txt"),
                                                          // exclude only deals with Foo1.txt in bin directories itself
                                                          Path.Combine(folder2BinTest, "Foo1.txt")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(tempDirBin, "Foo1.txt"),
                                                          Path.Combine(folder1Bin, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "whatever.txt"),
                                                          Path.Combine(_folder1, "Foo.whatever.txt")
                                                      };
            string[] includedDirs = new string[] {
                                                     tempDirBin,
                                                     folder2Bin,
                                                     folder3Bin,
                                                     folder2BinTest
                                                 };
            string[] excludedDirs = new string[] {
                                                     TempDirName,
                                                     _folder1,
                                                     _folder2,
                                                     _folder3,
            };

            // all bin directories (and their files and subdirectories) should 
            // now be matched
            _scanner.Includes.Add("**/bin/**");
            // exclude all Foo1.txt files that are in bin directories
            _scanner.Excludes.Add("**/bin/Foo1.txt");

            CheckScan(includedFileNames, excludedFileNames, includedDirs, excludedDirs);
        }

        /// <summary>Tests without wildcards.</summary>
        /// <remarks>
        ///   Try to match the files without wildcards.
        /// </remarks>
        [Test]
        public void Test_NoWildcardMatching1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            _scanner.Includes.Add(@"Foo1.txt");
            _scanner.Includes.Add(@"folder2/Foo2.txt");
            _scanner.Includes.Add(@"folder2\Foo3.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }
        
        /// <summary>Tests without wildcards.</summary>
        /// <remarks>
        ///   Try to match the files without wildcards.  Include a file with one slash and exclude it with the other.
        /// </remarks>
        [Test]
        public void Test_NoWildcardMatching2() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            _scanner.Includes.Add(@"Foo1.txt");
            _scanner.Includes.Add(@"folder2/Foo2.txt");
            _scanner.Excludes.Add(@"folder2\Foo2.txt");
            _scanner.Includes.Add(@"folder2\Foo4.txt");
            _scanner.Excludes.Add(@"folder2/Foo4.txt");
            _scanner.Includes.Add(@"folder2\Foo3.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }
        
        /// <summary>Tests parent directory patterns.</summary>
        /// <remarks>
        ///   Tests for inclusion of files in the parent directory.
        /// </remarks>
        [Test]
        public void Test_ParentDirectory1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            _scanner.BaseDirectory = new DirectoryInfo(_folder2);
            _scanner.Includes.Add(@"../Foo1.txt");
            _scanner.Includes.Add(@"Foo2.txt");
            _scanner.Includes.Add(@"../folder2\Foo3.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Tests parent directory patterns.</summary>
        /// <remarks>
        ///   Tests for inclusion of files in the parent directory.
        /// </remarks>
        [Test]
        public void Test_ParentDirectory2() 
        {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo.bar"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            _scanner.BaseDirectory = new DirectoryInfo(_folder2);
            _scanner.Includes.Add(@"../**.txt");
            _scanner.Excludes.Add(@"**/Foo4.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }


        /// <summary>Test ** wildcard.</summary>
        /// <remarks>
        ///   Matches everything in the base directory and sub directories.
        /// </remarks>
        [Test]
        public void Test_RecursiveWildcardMatching1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.Foo"),
                                                          Path.Combine(_folder3, "Foo4.me")
                                                      };
            string[] excludedFileNames = new string[] {
                                                      };
            _scanner.Includes.Add(@"**");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test ** wildcard.</summary>
        /// <remarks>
        ///   Matches everything in the base directory and sub directories that
        ///   ends with .txt.
        /// </remarks>
        [Test]
        public void Test_RecursiveWildcardMatching2() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo1.txt"),
                                                          Path.Combine(TempDirName, "Foo2.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder3, "Foo4.txt")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar")
                                                      };

            _scanner.Includes.Add(@"**\*.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test ** wildcard.</summary>
        /// <remarks>
        ///   Matches all files/dirs that start with "XYZ" and where there is a 
        ///   parent directory called 'folder3' (e.g. "abc\folder3\def\ghi\XYZ123").
        /// </remarks>
        [Test]
        public void Test_RecursiveWildcardMatching3() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak"),
                                                          Path.Combine(_folder3, "XYZzzz.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar")
                                                      };

            _scanner.Includes.Add(@"**\folder3\**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test specifying a folder both recursively and non-recursively.</summary>
        /// <remarks>
        ///   Matches all files/dirs that start with "XYZ" and where there is a 
        ///   parent directory called 'folder3' (e.g. "abc\folder3\def\ghi\XYZ123").
        /// </remarks>
        [Test]
        public void Test_RecursiveWildcardMatching4() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak"),
                                                          Path.Combine(_folder3, "XYZzzz.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar")
                                                      };

            _scanner.Includes.Add(@"folder3/XYZ*");
            _scanner.Includes.Add(@"**\folder3\**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test shorthand for including all files recursively.</summary>
        [Test]
        public void Test_ShorthandIncludes() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder2, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak")
            };
            string[] excludedFileNames = new string[0];

            _scanner.Includes.Add(@"folder2/");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test shorthand for exclusing all files recursively.</summary>
        [Test]
        public void Test_ShorthandExcludes() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder1, "filea.txt")
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder2, "fileb.txt"),
                                                          Path.Combine(_folder3, "filec.txt")
            };

            _scanner.Includes.Add(@"**");
            _scanner.Excludes.Add(@"folder2/");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test excluding files.</summary>
        /// <remarks>
        ///   Matches all XYZ* files, but then excludes them.
        /// </remarks>
        [Test]
        public void Test_Excludes1() {
            string[] includedFileNames = new string[] {
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar"),
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak"),
                                                          Path.Combine(_folder3, "XYZzzz.txt"),
            };

            _scanner.Includes.Add(@"folder3/XYZ*");
            _scanner.Excludes.Add(@"**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test excluding files.</summary>
        /// <remarks>
        ///   Matches all files, then excludes XYZ*.
        /// </remarks>
        [Test]
        public void Test_Excludes2() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak"),
                                                          Path.Combine(_folder3, "XYZzzz.txt"),
            };

            _scanner.Includes.Add(@"**");
            _scanner.Excludes.Add(@"**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test excluding files.</summary>
        /// <remarks>
        ///   Matches all files from the temp directory, then excludes XYZ*.  See if adding a recursive exclude to a 
        ///   non-recursive include break things.
        /// </remarks>
        [Test]
        public void Test_Excludes3() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar"),
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak"),
                                                          Path.Combine(_folder3, "XYZzzz.txt")
                                                      };

            _scanner.Includes.Add(@"*");
            _scanner.Excludes.Add(@"**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test excluding files.</summary>
        /// <remarks>
        ///   Matches all files from the temp directory, then excludes XYZzzz.txt.  See if adding an exact exclude to a 
        ///   recursive include break things.
        /// </remarks>
        [Test]
        public void Test_Excludes4() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar"),
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder3, "XYZzzz.txt")
                                                      };

            _scanner.Includes.Add(@"**");
            _scanner.Excludes.Add(@"folder2\folder3\XYZzzz.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test excluding files.</summary>
        /// <remarks>
        ///   Matches all files from the temp directory, then excludes XYZzzz.txt.  See if adding an exact exclude to a 
        ///   recursive include break things.
        /// </remarks>
        [Test]
        public void Test_Dont_Match_Basedir() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar"),
                                                          Path.Combine(_folder3, "XYZ.txt"),
                                                          Path.Combine(_folder3, "XYZ.bak")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(TempDirName, "Foo2.bar"),
                                                          Path.Combine(_folder3, "XYZzzz.txt")
                                                      };

            _scanner.Includes.Add(@"folder2/**/*");
            _scanner.Excludes.Add(@"folder2/**/XYZzzz.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>
        /// Test for bug #1195736.
        /// </summary>
        [Test]
        public void Test_Dont_Match_BaseDir_2() {
            string baseDir =  CreateTempDir("NAnt.Tests.DirectoryScannerBaseDirTest");
            TempFile.Create(Path.Combine(baseDir, "filea.txt"));
            TempFile.Create(Path.Combine(baseDir, "fileb.txt"));

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = new DirectoryInfo(baseDir + Path.DirectorySeparatorChar);
            _scanner.Includes.Add(@"filea.txt");
            _scanner.Includes.Add(Path.Combine(baseDir, "fileb.txt"));
            _scanner.Scan();

            Assert.AreEqual(2, _scanner.FileNames.Count);
        }

        /// <summary>
        /// Test for bug #1776101.
        /// </summary>
        [Test]
        public void Test_BaseDir_CaseInSensitive() {
            if (PlatformHelper.IsUnix) {
                return;
            }

            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(_folder3, "fileb.tlb"));

            _scanner.Includes.Add("folder2/folder3/*.txt");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#1");

            _scanner.Includes.Add("Folder2/**/folder3/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(2, _scanner.FileNames.Count, "#2");

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = TempDirectory;
            _scanner.Includes.Add("**/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#3");

            _scanner.Excludes.Add("folder2/folder3/*.txt");
            _scanner.Excludes.Add("Folder2/**/folder3/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(0, _scanner.FileNames.Count, "#4");
        }

        /// <summary>
        /// Test for bug #1776101.
        /// </summary>
        [Test]
        public void Test_BaseDir_CaseSensitive() {
            if (!PlatformHelper.IsUnix) {
                return;
            }

            TempFile.Create(Path.Combine(_folder3, "filea.txt"));
            TempFile.Create(Path.Combine(_folder3, "fileb.tlb"));

            _scanner.Includes.Add("folder2/folder3/*.txt");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#1");

            _scanner.Includes.Add("Folder2/**/folder3/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#2");

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = TempDirectory;
            _scanner.Includes.Add("**/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#3");

            _scanner.Excludes.Add("folder2/folder3/*.txt");
            _scanner.Excludes.Add("Folder2/**/folder3/*.tlb");
            _scanner.Scan();
            Assert.AreEqual(1, _scanner.FileNames.Count, "#4");
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            _folder1 = Path.Combine(TempDirName, "folder1");
            _folder2 = Path.Combine(TempDirName, "folder2");
            _folder3 = Path.Combine(_folder2, "folder3");
            Directory.CreateDirectory(_folder1);
            Directory.CreateDirectory(_folder2);
            Directory.CreateDirectory(_folder3);

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = TempDirectory;
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Helper function for running scan tests.
        /// </summary>
        private void CheckScan(string[] includedFileNames, string[] excludedFileNames) {
            // create all the files
            foreach (string fileName in includedFileNames) {
                TempFile.Create(fileName);
            }
            foreach (string fileName in excludedFileNames) {
                TempFile.Create(fileName);
            }
            // Run the scan, _scanner.Includes and _scanner.Excludes need to have
            // been set up before calling this method.
            _scanner.Scan();

            // Make sure only the included file names were picked up in the scan
            // and none of the excluded.
            foreach (string fileName in includedFileNames) {
                Assert.IsTrue(_scanner.FileNames.IndexOf(fileName) != -1, fileName + " not included.");
            }
            foreach (string fileName in excludedFileNames) {
                Assert.IsTrue(_scanner.FileNames.IndexOf(fileName) == -1, fileName + " included.");
            }
        }

        /// <summary>
        /// Helper function for running scan tests.
        /// </summary>
        private void CheckScan(string[] includedFiles, string[] excludedFiles, string[] includedDirs, string[] excludedDirs) {
            // create all the directories
            foreach (string dir in includedDirs) {
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
            }
            foreach (string dir in excludedDirs) {
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
            }

            CheckScan(includedFiles, excludedFiles);

            foreach (string dir in includedDirs) {
                Assert.IsTrue(_scanner.DirectoryNames.IndexOf(dir) != -1, dir + " not included.");
            }
            foreach (string dir in excludedDirs) {
                Assert.IsTrue(_scanner.DirectoryNames.IndexOf(dir) == -1, dir + " included.");
            }
        }

        #endregion Private Instance Methods
    }
}

