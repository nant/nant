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

using System;
using System.IO;

using NUnit.Framework;

namespace Tests.NAnt.Core.Util {

    public sealed class TempFile {
        /// <summary>Creates a small temp file returns the file name.</summary>
        public static string Create() {
            return Create(Path.GetTempFileName());
        }

        public static string Create(string fileName) {
            string contents = "You can delete this file." + Environment.NewLine;
            return CreateWithContents(contents, fileName);
        }

        public static string CreateWithContents(string contents) {
            return CreateWithContents(contents, Path.GetTempFileName());
        }

        public static string CreateWithContents(string contents, string fileName) {
            // ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            // write the text into the temp file.
            using (FileStream f = new FileStream(fileName, FileMode.Create)) {
                StreamWriter s = new StreamWriter(f);
                s.Write(contents);
                s.Close();
                f.Close();
            }

            if (!File.Exists(fileName)) {
                throw new AssertionException("TempFile: " + fileName + " wasn't created.");
            }

            return fileName;
        }

        public static string Read(string fileName) {
            string contents;
            using (StreamReader s = File.OpenText(fileName)) {
                contents = s.ReadToEnd();
                s.Close();
            }
            return contents;
        }
    }
}
