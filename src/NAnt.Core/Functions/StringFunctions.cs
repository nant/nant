// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("string", "String")]
    public class StringFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public StringFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Get the length of a string.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The strings length.
        /// </returns>
        [Function("get-length")]
        public static int GetLength(string s) {
            return s.Length;
        }

        /// <summary>
        /// Returns the substring of the specified string
        /// </summary>
        /// <param name="str">an input string</param>
        /// <param name="start">position of the start of the substring</param>
        /// <param name="length">the length of the substring</param>
        /// <returns>If the <paramref name="length" /> is less than zero,
        /// the function returns a trailing portion of the string
        /// <paramref name="str" /> starting at <paramref name="start" /> position.
        /// If the <paramref name="length" /> is greater than zero, the
        /// function returns the substring starting at character position
        /// <paramref name="start" /> with the length of <paramref name="length" />
        /// characters.
        /// </returns>
        [Function("substring")]
        public static string Substring(string str, int start, int length) {
            if (length < 0) {
                return str.Substring(start);
            } else {
                return str.Substring(start, length);
            }
        }

        /// <summary>
        /// Determines whether characters at the beginning of the first string 
        /// are identical to the second string.
        /// </summary>
        /// <param name="s1">the string to be checked.</param>
        /// <param name="s2">the string to seek at the beginning of s1.</param>
        /// <returns>
        /// <see langword="true" /> when <paramref name="s1" /> starts with 
        /// <paramref name="s2" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// The result of this functions is equal to the result of s1.StartsWith(s2).
        /// </remarks>
        [Function("starts-with")]
        public static bool Startswith(string s1, string s2) {
            return s1.StartsWith(s2);
        }

        /// <summary>
        /// Determines whether characters at the end of the first string are 
        /// identical to the second string.
        /// </summary>
        /// <param name="s1">the string to be checked.</param>
        /// <param name="s2">the string to seek at the end of s1.</param>
        /// <returns>
        /// <see langword="true" /> when <paramref name="s1" /> ends with 
        /// <paramref name="s2" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// The result of this function is equal to the result of s1.EndsWith(s2).
        /// </remarks>
        [Function("ends-with")]
        public static bool EndsWith(string s1, string s2) {
            return s1.EndsWith(s2);
        }

        /// <summary>
        /// Converts a string to lowercase.
        /// </summary>
        /// <param name="s">the string to be converted.</param>
        /// <returns>
        /// The string <paramref name="s" /> in lowercase.
        /// </returns>
        [Function("to-lower")]
        public static string ToLower(string s) {
            return s.ToLower();
        }
        /// <summary>
        /// Converts a string to uppercase.
        /// </summary>
        /// <param name="s">the string to be converted.</param>
        /// <returns>
        /// The string <paramref name="s" /> in uppercase.
        /// </returns>
        [Function("to-upper")]
        public static string ToUpper(string s) {
            return s.ToUpper();
        }
        /// <summary>
        /// Determines whether a string is contained in another string.
        /// </summary>
        /// <param name="str">the string to be checked.</param>
        /// <param name="substr">the string to seek.</param>
        /// <returns>
        /// <see langword="true" /> when <paramref name="substr" /> is contained 
        /// in <paramref name="str" />; otherwise, <see langword="false" />.
        /// </returns>
        [Function("contains")]
        public static bool Contains(string str, string substr) {
            return str.IndexOf(substr) >= 0;
        }

        /// <summary>
        /// Returns the position of the first occurence of the specified string in another string
        /// </summary>
        /// <param name="str">the string to be checked.</param>
        /// <param name="substr">the string to seek.</param>
        /// <returns>
        /// The position of <paramref name="substr" /> if that string is found, 
        /// or <c>-1</c> if it is not. If <paramref name="substr" /> is an empty 
        /// string, the return value is <c>0</c>.
        /// </returns>
        [Function("index-of")]
        public static int IndexOf(string str, string substr) {
            return str.IndexOf(substr);
        }

        /// <summary>
        /// Returns the position of the last occurence of the specified string 
        /// in another string.
        /// </summary>
        /// <param name="str">the string to be checked.</param>
        /// <param name="substr">the string to seek.</param>
        /// <returns>
        /// The position of <paramref name="substr" /> if that string is found, 
        /// or <c>-1</c> if it is not. If <paramref name="substr" /> is an empty 
        /// string, the return value is <c>0</c>.
        /// </returns>
        [Function("last-index-of")]
        public static int LastIndexOf(string str, string substr) {
            return str.LastIndexOf(substr);
        }

        /// <summary>
        /// Pads the string on the left with a specified character for a specified total length.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="width"></param>
        /// <param name="paddingChar"></param>
        /// <returns></returns>
        [Function("pad-left")]
        public static string PadLeft(string s, int width, string paddingChar) {
            return s.PadLeft(width, paddingChar[0]);
        }

        /// <summary>
        /// Pads the string on the right with a specified character for a specified total length.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="width"></param>
        /// <param name="paddingChar"></param>
        /// <returns></returns>
        [Function("pad-right")]
        public static string PadRight(string s, int width, string paddingChar) {
            return s.PadRight(width, paddingChar[0]);
        }

        /// <summary>
        /// Trims the string by removing whitespace characters from the beginning 
        /// and the end of the string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [Function("trim")]
        public static string Trim(string s) {
            return s.Trim();
        }
        
        /// <summary>
        /// Trims the string by removing whitespace characters from the beginning 
        /// of the string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [Function("trim-start")]
        public static string TrimStart(string s) {
            return s.TrimStart();
        }
        
        /// <summary>
        /// Trims the string by removing whitespace characters from the end of 
        /// the string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [Function("trim-end")]
        public static string TrimEnd(string s) {
            return s.TrimEnd();
        }

        #endregion Public Static Methods
    }
}
