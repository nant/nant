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

using System;
using System.IO;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core {
    [TestFixture]
    public class DirectoryScannerTest {
        #region Private Instance Fields

        private string _tempDir;
        private string _folder1;
        private string _folder2;
        private string _folder3;
		private string _folder4;
		private string _folder5;
		private string _folder6;
		private string _folder7;
		private DirectoryScanner _scanner;

        #endregion Private Instance Fields

        #region Public Instance Methods

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

        /// <summary>Tests without wildcards.</summary>
        /// <remarks>
        ///   Try to match the files without wildcards.
        /// </remarks>
        [Test]
        public void Test_NoWildcardMatching1() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_tempDir, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
                                                          Path.Combine(_folder2, "Foo2.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_tempDir, "Foo.bar"),
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
                                                          Path.Combine(_tempDir, "Foo1.txt"),
                                                          Path.Combine(_folder2, "Foo3.txt"),
            };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_tempDir, "Foo.bar"),
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
        
        /// <summary>Test ** wildcard.</summary>
        /// <remarks>
        ///   Matches everything in the base directory and sub directories.
        /// </remarks>
        [Test]
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
        [Test]
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
        [Test]
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
                                                          Path.Combine(_tempDir, "Foo2.bar"),
                                                          Path.Combine(_folder2, "Foo3.bar"),
                                                          Path.Combine(_folder3, "Foo4.bar")
                                                      };

            _scanner.Includes.Add(@"folder3/XYZ*");
            _scanner.Includes.Add(@"**\folder3\**\XYZ*");
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
                                                          Path.Combine(_tempDir, "Foo2.bar"),
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
                                                          Path.Combine(_tempDir, "Foo2.bar"),
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
                                                          Path.Combine(_tempDir, "Foo2.bar")
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
                                                          Path.Combine(_tempDir, "Foo2.bar"),
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
		///   This is a test for setting the base directory in a directory that would normally be excluded
		///   (set to ".../CVS/" for ".../CVS/some_other_files").
		///   
		///   Even though one of the default include patterns is **/CVS/**, we won't see it since it is part
		///   of the base directory.
		/// </remarks>
        [Test]
        public void Test_Rooted_Patterns() {
            string[] includedFileNames = new string[] {
                                                          Path.Combine(_folder4, "Foo2.bar"),
                                                          Path.Combine(_folder4, "Foo3.bar"),
                                                          Path.Combine(_folder4, "Foo4.bar"),
                                                          Path.Combine(_folder4, "XYZ.txt"),
                                                          Path.Combine(_folder4, "XYZ.bak")
                                                      };
            string[] excludedFileNames = new string[] {
                                                          Path.Combine(_folder4, "XYZzzz.txt")
                                                      };

            // Change the base directory for this test
            _scanner.BaseDirectory = new DirectoryInfo(_folder4);
            _scanner.Includes.Add(@"**");
            _scanner.Excludes.Add(Path.Combine(_folder4, "XYZzzz.txt"));
            CheckScan(includedFileNames, excludedFileNames);
        }

		/// <summary>Test excluding files.</summary>
		/// <remarks>
		///   This is a test for setting the base directory in a directory that would normally be excluded
		///   (set to ".../CVS/" for ".../CVS/some_other_files").
		///   
		///   Even though one of the default include patterns is **/CVS/**, we won't see it since it is part
		///   of the base directory.
		/// </remarks>
		[Test]
		public void Test_Rooted_Patterns_2() 
		{
			string[] includedFileNames = new string[] {
														  Path.Combine(_folder5, "Foo2.bar"),
														  Path.Combine(_folder5, "Foo3.bar"),
														  Path.Combine(_folder5, "Foo4.bar"),
														  Path.Combine(_folder5, "XYZ.txt"),
														  Path.Combine(_folder5, "XYZ.bak")
													  };
			string[] excludedFileNames = new string[] {
														  Path.Combine(_folder5, "XYZzzz.txt")
													  };

			// Change the base directory for this test
			_scanner.BaseDirectory = new DirectoryInfo(_folder5);
			_scanner.Includes.Add(@"**");
			_scanner.Excludes.Add(@"**\XYZzzz.txt");
			CheckScan(includedFileNames, excludedFileNames);
		}

		/// <summary>Test excluding files.</summary>
		/// <remarks>
		///   We include the default-excluded directory now, but it will be ignored since it matches the 
		///   default **/CVS/** pattern.
		/// </remarks>
		[Test]
		public void Test_Rooted_Patterns_3() 
		{
			string[] includedFileNames = new string[] {
													  };
			string[] excludedFileNames = new string[] {
														  Path.Combine(_folder5, "Foo2.bar"),
														  Path.Combine(_folder5, "Foo3.bar"),
														  Path.Combine(_folder5, "Foo4.bar"),
														  Path.Combine(_folder5, "XYZ.txt"),
														  Path.Combine(_folder5, "XYZ.bak"),
														  Path.Combine(_folder5, "XYZzzz.txt")
													  };

			// Change the base directory for this test
			_scanner.BaseDirectory = new DirectoryInfo(_folder1);
			_scanner.Includes.Add(_folder4 + @"\**");
			_scanner.Excludes.Add(@"**/CVS/**");
			CheckScan(includedFileNames, excludedFileNames);
		}
		
		#endregion Public Instance Methods

        #region Protected Instance Methods

        [SetUp]
        protected void SetUp() {
            _tempDir = TempDir.Create("NAnt.Tests.DirectoryScannerTest");

            _folder1 = Path.Combine(_tempDir, "folder1");
            _folder2 = Path.Combine(_tempDir, "folder2");
            _folder3 = Path.Combine(_folder2, "folder3");
            _folder4 = Path.Combine(_tempDir, "cvs");
            _folder5 = Path.Combine(_folder4, "nested1");
            _folder6 = Path.Combine(_folder5, "nested2");
            _folder7 = Path.Combine(_folder5, "nested3");
            Directory.CreateDirectory(_folder1);
            Directory.CreateDirectory(_folder2);
            Directory.CreateDirectory(_folder3);
            Directory.CreateDirectory(_folder4);
            Directory.CreateDirectory(_folder5);
            Directory.CreateDirectory(_folder6);
            Directory.CreateDirectory(_folder7);

            _scanner = new DirectoryScanner();
            _scanner.BaseDirectory = new DirectoryInfo(_tempDir);
        }

        [TearDown]
        protected void TearDown() {
            TempDir.Delete(_tempDir);
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

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
                Assert.IsTrue(_scanner.FileNames.IndexOf(fileName) != -1, fileName + " not included.");
            }
            foreach (string fileName in excludedFileNames) {
                Assert.IsTrue(_scanner.FileNames.IndexOf(fileName) == -1, fileName + " included.");
            }
        }

        #endregion Private Instance Methods
    }
}
