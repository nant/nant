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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;

using NUnit.Framework;

namespace Tests.NAnt.Core.Util {

    public sealed class TempDir {
        /// <summary>Creates a temporary directory for a unit test.</summary>
        /// <remarks>
        ///   <para>If the directory already exists it will first be deleted and a new empty directory will be created.</para>
        /// <returns>The complete path to the created directory.</remarks>
        public static string Create(string name) {
            string path = Path.Combine(Path.GetTempPath(), name);
            if(Path.IsPathRooted(name))
                path = name;

            // delete any existing directory from previously failed test
            Delete(path);

            // create the new empty directory
            Directory.CreateDirectory(path);
            if (!Directory.Exists(path)) {
                throw new AssertionException("TempDir: " + path + " does not exists.");
            }
            return path;
        }

        /// <summary>Delete the directory at the given path and everything in it.</summary>
        public static void Delete(string path) {
            bool bError = false;
            try {
                if (Directory.Exists(path)) {
                    // ensure directorty is writable
                    File.SetAttributes(path, FileAttributes.Normal);
                    // ensure all files and subdirectories are writable
                    SetAllFileAttributesToNormal(path);
                    string[] directoryNames = Directory.GetDirectories(path);
                    foreach (string directoryName in directoryNames) {
                        Delete(directoryName);
                    }
                    string[] fileNames = Directory.GetFiles(path);
                    foreach (string fileName in fileNames) {
                        File.Delete(fileName);
                    }
                    Directory.Delete(path, true);
                }
            } catch(Exception ex) {
                bError = true;
                throw new AssertionException("Unable to cleanup '" + path + "'.  " + ex.Message, ex);
            } finally {
                if (!bError && Directory.Exists(path)) {
                    throw new AssertionException("TempDir: "+ path + " still exists.");
                }
            }
        }

        /// <summary>
        /// Recurse over all files in the directory setting each file's attributes 
        /// to <see cref="FileAttributes.Normal" />.
        /// </summary>
        private static void SetAllFileAttributesToNormal(string path) {
            string[] fileNames = Directory.GetFiles(path);
            foreach (string fileName in fileNames) {
                File.SetAttributes(fileName, FileAttributes.Normal);
            }

            string[] directoryNames = Directory.GetDirectories(path);
            foreach (string directoryName in directoryNames) {
                File.SetAttributes(directoryName, FileAttributes.Normal);
                SetAllFileAttributesToNormal(directoryName);
            }
        }
    }
}
