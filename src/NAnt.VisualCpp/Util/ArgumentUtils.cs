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
using System.ComponentModel;

namespace NAnt.VisualCpp.Util {
    /// <summary>
    /// Defines how to deal with backslashes in values of command line 
    /// arguments.
    /// </summary>
    public enum BackslashProcessingMethod {
        /// <summary>
        /// Does not perform any processing on backslashes.
        /// </summary>
        None = 0,

        /// <summary>
        /// Duplicates the trailing backslash.
        /// </summary>
        Duplicate = 1,

        /// <summary>
        /// Fixes the trailing backslash by replaces trailing double backslashes
        /// with only one backslash and removing single trailing backslashes.
        /// </summary>
        Fix = 2,

        /// <summary>
        /// Removes all the trailing backslashes.
        /// </summary>
        Clean = 3
    }

    /// <summary>
    /// Groups a set of useful <see cref="string" /> manipulation methods for
    /// command-line arguments.
    /// </summary>
    public class ArgumentUtils {
        /// <summary>
        /// Performs backslash processing on the specified value using a given
        /// method.
        /// </summary>
        /// <param name="value">The <see cref="string" /> to process.</param>
        /// <param name="processingMethod">The <see cref="BackslashProcessingMethod" /> to use.</param>
        /// <returns>
        /// <paramref name="value" /> with backslashes processed using the given
        /// <see cref="BackslashProcessingMethod" />.
        /// </returns>
        public static string ProcessTrailingBackslash(string value, BackslashProcessingMethod processingMethod) {
            string processedValue = null;

            switch (processingMethod) {
                case BackslashProcessingMethod.None:
                    processedValue = value;
                    break;
                case BackslashProcessingMethod.Duplicate:
                    processedValue = DuplicateTrailingBackslash(value);
                    break;
                case BackslashProcessingMethod.Fix:
                    processedValue = FixTrailingBackslash(value);
                    break;
                case BackslashProcessingMethod.Clean:
                    processedValue = CleanTrailingBackslash(value);
                    break;
                default:
                    throw new InvalidEnumArgumentException("processingMethod",
                        (int) processingMethod, typeof(BackslashProcessingMethod));
            }

            return processedValue;
        }

        /// <summary>
        /// Duplicates the trailing backslash.
        /// </summary>
        /// <param name="value">The input string to check and duplicate the trailing backslash if necessary.</param>
        /// <returns>The result string after being processed.</returns>
        /// <remarks>
        /// Also duplicates trailing backslash in quoted value.
        /// </remarks>
        public static string DuplicateTrailingBackslash(string value) {
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
        public static string FixTrailingBackslash(string value) {
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
        public static string CleanTrailingBackslash(string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            return value.TrimEnd('\\');
        }

        /// <summary>
        /// Quotes an argument value and processes backslashes using a given
        /// <see cref="BackslashProcessingMethod" />.
        /// </summary>
        /// <param name="value">The argument value to quote.</param>
        /// <param name="processingMethod">The <see cref="BackslashProcessingMethod" /> to use.</param>
        /// <returns>
        /// The quoted argument value.
        /// </returns>
        public static string QuoteArgumentValue(string value, BackslashProcessingMethod processingMethod) {
            // duplicate trailing backslashes (even if value is quoted)
            string quotedValue = ArgumentUtils.ProcessTrailingBackslash(value, processingMethod);
            
            // determine if value is already quoted
            bool isQuoted = value.StartsWith("\"") && value.EndsWith("\"");

            if (!isQuoted) {
                quotedValue = "\"" + quotedValue + "\"";
            }

            return quotedValue;
        }
    }
}
