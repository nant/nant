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
using System.IO;
using System.Text;
using System.Globalization;
using NAnt.Core.Filters;
using NAnt.Core.Types;

namespace NAnt.Core.Util {
    /// <summary>
    /// Provides modified version for Copy and Move from the File class that allow for filter chain processing.
    /// </summary>
    public class FileUtils {
        private FileUtils() {
        }

        /// <summary>
        /// Copies a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="sourceFileName">Pathname of file to copy</param>
        /// <param name="destFileName">Pathname of file to copy to</param>
        /// <param name="filterChain">Chain of filter to apply when copying. Null is allowed</param>
        public static void CopyWithFilters(string sourceFileName, string destFileName, FilterChain filterChain) {
            CopyWithFilters(sourceFileName, destFileName, filterChain, true);
        }

        /// <summary>
        /// Moves a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="sourceFileName">Pathname of file to move</param>
        /// <param name="destFileName">Pathname of file to move to</param>
        /// <param name="filterChain">Chain of filter to apply when moving. Null is allowed</param>
        public static void MoveWithFilters(string sourceFileName, string destFileName, FilterChain filterChain) {
            CopyWithFilters(sourceFileName, destFileName, filterChain, false);

            File.Delete(sourceFileName);
        }

        /// <summary>
        /// Copies a file filtering its content through the filter chain.
        /// </summary>
        /// <param name="sourceFileName">Pathname of file to copy</param>
        /// <param name="destFileName">Pathname of file to copy to</param>
        /// <param name="filterChain">Chain of filter to apply when copying. Null is allowed</param>
        /// <param name="overwrite">True if the destination file can be overwritten; otherwise, false.</param>
        static void CopyWithFilters(string sourceFileName, string destFileName, FilterChain filterChain, bool overwrite) {
            //If a filter chain is specified
            if (filterChain != null) {

                //Get base filter built on the file's reader. Use a 4k buffer.
                StreamReader sourceFileReader = new StreamReader(sourceFileName, Encoding.Default, true, 4096);//TODO: Buffer as parameter?
                try {
                    Filter baseFilter= filterChain.GetBaseFilter(new PhysicalTextReader(sourceFileReader));

                    //Create reader for the source file
                    StreamWriter destFileWriter = new StreamWriter(destFileName, false, sourceFileReader.CurrentEncoding, 4096);
                    try {
                        bool atEnd = false;
                        int character;
                        while ( ! atEnd) {
                            character = baseFilter.Read();
                            if (character > -1) {
                                destFileWriter.Write((char)character);
                            } else {
                                atEnd = true;
                            }
                        }
                    }
                    finally {
                        destFileWriter.Close();
                    }
                }
                finally {
                    sourceFileReader.Close();
                }
            }
            else {
                File.Copy(sourceFileName, destFileName, overwrite);
            }
        }
    }
}
