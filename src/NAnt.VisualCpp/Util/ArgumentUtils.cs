// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Hani Atassi (haniatassi@users.sourceforge.net)

using System;

namespace NAnt.VisualCpp.Util {
    /// <summary>
    /// Groups a set of useful <see cref="string" /> manipulation methods for
    /// command-line arguments.
    /// </summary>
    public class ArgumentUtils {
        /// <summary>
        /// Duplicates the trailing backslash.
        /// </summary>
        /// <param name="value">The input string to check and duplicate the trailing backslash if necessary.</param>
        /// <returns>The result string after being processed.</returns>
        /// <remarks>
        /// Also duplicates trailing backslash in quoted value.
        /// </remarks>
        public static string DuplicateTrailingBackSlash(string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (value.Length == 0) {
                return value;
            }

            bool isQuoted = value.Length > 2 && value.StartsWith("\"") && value.EndsWith("\"");

            int lastIndex = (isQuoted ? value.Length - 2 : value.Length - 1);
            if (value[lastIndex] == '\\') {
                return value.Insert(lastIndex, @"\");
            }
            return value;
        }

        /// <summary>
        /// Fixes the trailing backslash. This function replaces the trailing double backslashes with
        /// only one backslash. It also, removes the single trailing backslash.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <returns>The result string after being processed.</returns>
        public static string FixTrailingBackSlash(string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (value.Length == 0) {
                return value;
            }

            if (value.EndsWith(@"\\")) {
                return value.Remove(value.Length - 2, 2) + @"\";
            } else if (value.EndsWith(@"\")) {
                return value.Remove(value.Length - 1, 1);
            } else {
                return value;
            }
        }

        /// <summary>
        /// Removes all the trailing backslashes from the input.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <returns>The result string without trailing backslashes.</returns>
        public static string CleanTrailingBackSlash(string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            return value.TrimEnd('\\');
        }
    }
}
