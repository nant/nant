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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    public class DirectoryScannerTest : TestCase {

        public DirectoryScannerTest(String name) : base(name) {
        }

        string _tempDir;
        string _folder1;
        string _folder2;
        string _folder3;
        DirectoryScanner _scanner;

        protected override void SetUp() {
            _tempDir = TempDir.Create("NAnt.Tests.DirectoryScannerTest");

            _folder1 = Path.Combine(_tempDir, "folder1");
            _folder2 = Path.Combine(_tempDir, "folder2");
            _folder3 = Path.Combine(_folder2, "folder3");
            Directory.CreateDirectory(_folder1);
            Directory.CreateDirectory(_folder2);
            Directory.CreateDirectory(_folder3);

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = _tempDir;
        }

        protected override void TearDown() {
            TempDir.Delete(_tempDir);
        }

        /// <summary>Helper function for running scan tests.</summary>
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
                Assert(fileName + " not included.", _scanner.FileNames.IndexOf(fileName) != -1);
            }
            foreach (string fileName in excludedFileNames) {
                Assert(fileName + " included.", _scanner.FileNames.IndexOf(fileName) == -1);
            }
        }

        /// <summary>Test ? wildcard and / seperator.</summary>
        /// <remarks>
        ///   Matches all the files in the folder2 directory that being with Foo 
        ///   and one extra character and end with .txt.
        /// </remarks>
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
        public void Test_WildcardMatching2() {
            string[] includedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo1.txt"),
                Path.Combine(_tempDir, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo.bar"),
                Path.Combine(_folder2, "Foo3.txt"),
                Path.Combine(_folder3, "Foo4.txt")
            };
            _scanner.Includes.Add(@"*.txt");
            CheckScan(includedFileNames, excludedFileNames);
        }

        /// <summary>Test ** wildcard.</summary>
        /// <remarks>
        ///   Matches everything in the base directory and sub directories.
        /// </remarks>
        public void Test_RecursiveWildcardMatching1() {
            string[] includedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo1.txt"),
                Path.Combine(_tempDir, "Foo2.bar"),
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
        public void Test_RecursiveWildcardMatching2() {
            string[] includedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo1.txt"),
                Path.Combine(_tempDir, "Foo2.txt"),
                Path.Combine(_folder2, "Foo3.txt"),
                Path.Combine(_folder3, "Foo4.txt")
            };
            string[] excludedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo2.bar"),
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
        public void Test_RecursiveWildcardMatching3() {
            string[] includedFileNames = new string[] {
                Path.Combine(_folder3, "XYZ.txt"),
                Path.Combine(_folder3, "XYZ.bak"),
                Path.Combine(_folder3, "XYZzzz.txt"),
            };
            string[] excludedFileNames = new string[] {
                Path.Combine(_tempDir, "Foo2.bar"),
                Path.Combine(_folder2, "Foo3.bar"),
                Path.Combine(_folder3, "Foo4.bar")
            };

            _scanner.Includes.Add(@"**\folder3\**\XYZ*");
            CheckScan(includedFileNames, excludedFileNames);
        }
    }
}
