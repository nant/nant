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
// Ian Maclean (imaclean@gmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Globalization;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions to work with strings.
    /// </summary>
    [FunctionSet("string", "String")]
    public class StringFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="propDict">The projects property dictionary.</param>
        public StringFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Returns the length of the specified string.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string's length.
        /// </returns>
        /// <example>
        /// <code>string::get-length('foo') ==> 3</code>
        /// </example>
        /// <example>
        /// <code>string::get-length('') ==> 0</code>
        /// </example>
        [Function("get-length")]
        public static int GetLength(string s) {
            return s.Length;
        }

        /// <summary>
        /// Returns a substring of the specified string.
        /// </summary>
        /// <param name="str">input string</param>
        /// <param name="startIndex">position of the start of the substring</param>
        /// <param name="length">the length of the substring</param>
        /// <returns>
        /// <para>
        /// If the <paramref name="length" /> is greater than zero, the
        /// function returns a substring starting at character position
        /// <paramref name="startIndex" /> with a length of <paramref name="length" />
        /// characters.
        /// </para>
        /// <para>
        /// If the <paramref name="length" /> is equal to zero, the function
        /// returns an empty string.
        /// </para>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> or <paramref name="length" /> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is greater than the length of <paramref name="str" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> plus <paramref name="length" /> indicates a position not within <paramref name="str" />.</exception>
        /// <example>
        /// <code>string::substring('testing string', 0, 4) ==> 'test'</code>
        /// </example>
        /// <example>
        /// <code>string::substring('testing string', 8, 3) ==> 'str'</code>
        /// </example>
        /// <example>
        /// <code>string::substring('testing string', 8, 0) ==> ''</code>
        /// </example>
        /// <example>
        /// <code>string::substring('testing string', -1, 5) ==> ERROR</code>
        /// </example>
        /// <example>
        /// <code>string::substring('testing string', 8, -1) ==> ERROR</code>
        /// </example>
        /// <example>
        /// <code>string::substring('testing string', 5, 17) ==> ERROR</code>
        /// </example>
        [Function("substring")]
        public static string Substring(string str, int startIndex, int length) {
            return str.Substring(startIndex, length);
        }

        /// <summary>
        /// Tests whether the specified string starts with the specified prefix
        /// string.
        /// </summary>
        /// <param name="s1">test string</param>
        /// <param name="s2">prefix string</param>
        /// <returns>
        /// <see langword="true" /> when <paramref name="s2" /> is a prefix for
        /// the string <paramref name="s1" />. Meaning, the characters at the 
        /// beginning of <paramref name="s1" /> are identical to
        /// <paramref name="s2" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// This function performs a case-sensitive word search using the 
        /// invariant culture.
        /// </remarks>
        /// <example>
        /// <code>string::starts-with('testing string', 'test') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::starts-with('testing string', 'testing') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::starts-with('testing string', 'string') ==> false</code>
        /// </example>
        /// <example>
        /// <code>string::starts-with('test', 'testing string') ==> false</code>
        /// </example>
        [Function("starts-with")]
        public static bool StartsWith(string s1, string s2) {
            return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(s1, s2);
        }

        /// <summary>
        /// Tests whether the specified string ends with the specified suffix
        /// string.
        /// </summary>
        /// <param name="s1">test string</param>
        /// <param name="s2">suffix string</param>
        /// <returns>
        /// <see langword="true" /> when <paramref name="s2" /> is a suffix for
        /// the string <paramref name="s1" />. Meaning, the characters at the 
        /// end of <paramref name="s1" /> are identical to 
        /// <paramref name="s2" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// This function performs a case-sensitive word search using the 
        /// invariant culture.
        /// </remarks>
        /// <example>
        /// <code>string::ends-with('testing string', 'string') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::ends-with('testing string', '') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::ends-with('testing string', 'bring') ==> false</code>
        /// </example>
        /// <example>
        /// <code>string::ends-with('string', 'testing string') ==> false</code>
        /// </example>
        [Function("ends-with")]
        public static bool EndsWith(string s1, string s2) {
            return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(s1, s2);
        }

        /// <summary>
        /// Returns the specified string converted to lowercase.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string <paramref name="s" /> in lowercase.
        /// </returns>
        /// <remarks>
        /// The casing rules of the invariant culture are used to convert the
        /// <paramref name="s" /> to lowercase.
        /// </remarks>
        /// <example>
        /// <code>string::to-lower('testing string') ==> 'testing string'</code>
        /// </example>
        /// <example>
        /// <code>string::to-lower('Testing String') ==> 'testing string'</code>
        /// </example>
        /// <example>
        /// <code>string::to-lower('Test 123') ==> 'test 123'</code>
        /// </example>
        [Function("to-lower")]
        public static string ToLower(string s) {
            return s.ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the specified string converted to uppercase.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string <paramref name="s" /> in uppercase.
        /// </returns>
        /// <remarks>
        /// The casing rules of the invariant culture are used to convert the
        /// <paramref name="s" /> to uppercase.
        /// </remarks>
        /// <example>
        /// <code>string::to-upper('testing string') ==> 'TESTING STRING'</code>
        /// </example>
        /// <example>
        /// <code>string::to-upper('Testing String') ==> 'TESTING STRING'</code>
        /// </example>
        /// <example>
        /// <code>string::to-upper('Test 123') ==> 'TEST 123'</code>
        /// </example>
        [Function("to-upper")]
        public static string ToUpper(string s) {
            return s.ToUpper(CultureInfo.InvariantCulture);
        }
        
        /// <summary>
        /// Returns a string corresponding to the replacement of a given string
        /// with another in the specified string.
        /// </summary>
        /// <param name="str">input string</param>
        /// <param name="oldValue">A <see cref="string" /> to be replaced.</param>
        /// <param name="newValue">A <see cref="string" /> to replace all occurrences of <paramref name="oldValue" />.</param>
        /// <returns>
        /// A <see cref="String" /> equivalent to <paramref name="str" /> but 
        /// with all instances of <paramref name="oldValue" /> replaced with 
        /// <paramref name="newValue" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="oldValue" /> is an empty string.</exception>
        /// <remarks>
        /// This function performs a word (case-sensitive and culture-sensitive) 
        /// search to find <paramref name="oldValue" />.
        /// </remarks>
        /// <example>
        /// <code>string::replace('testing string', 'test', 'winn') ==> 'winning string'</code>
        /// </example>
        /// <example>
        /// <code>string::replace('testing string', 'foo', 'winn') ==> 'testing string'</code>
        /// </example>
        /// <example>
        /// <code>string::replace('testing string', 'ing', '') ==> 'test str'</code>
        /// </example>
        /// <example>
        /// <code>string::replace('banana', 'ana', 'ana') ==> 'banana'</code>
        /// </example>
        [Function("replace")]
        public static string Replace(string str, string oldValue, string newValue) {
            return str.Replace(oldValue, newValue);
        }
        /// <summary>
        /// Tests whether the specified string contains the given search string.
        /// </summary>
        /// <param name="source">The string to search.</param>
        /// <param name="value">The string to locate within <paramref name="source" />.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in 
        /// <paramref name="source" />; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// This function performs a case-sensitive word search using the 
        /// invariant culture.
        /// </remarks>
        /// <example>
        /// <code>string::contains('testing string', 'test') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::contains('testing string', '') ==> true</code>
        /// </example>
        /// <example>
        /// <code>string::contains('testing string', 'Test') ==> false</code>
        /// </example>
        /// <example>
        /// <code>string::contains('testing string', 'foo') ==> false</code>
        /// </example>
        [Function("contains")]
        public static bool Contains(string source, string value) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, 
                value, CompareOptions.None) >= 0;
        }

        /// <summary>
        /// Returns the position of the first occurrence in the specified string
        /// of the given search string.
        /// </summary>
        /// <param name="source">The string to search.</param>
        /// <param name="value">The string to locate within <paramref name="source" />.</param>
        /// <returns>
        /// <para>
        /// The lowest-index position of <paramref name="value" /> in
        /// <paramref name="source" /> if it is found, or -1 if <paramref name="source" /> 
        /// does not contain <paramref name="value" />.
        /// </para>
        /// <para>
        /// If <paramref name="value" /> is an empty string, the return value
        /// will always be <c>0</c>.
        /// </para>
        /// </returns>
        /// <remarks>
        /// This function performs a case-sensitive word search using the 
        /// invariant culture.
        /// </remarks>
        /// <example>
        /// <code>string::index-of('testing string', 'test') ==> 0</code>
        /// </example>
        /// <example>
        /// <code>string::index-of('testing string', '') ==> 0</code>
        /// </example>
        /// <example>
        /// <code>string::index-of('testing string', 'Test') ==> -1</code>
        /// </example>
        /// <example>
        /// <code>string::index-of('testing string', 'ing') ==> 4</code>
        /// </example>
        [Function("index-of")]
        public static int IndexOf(string source, string value) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, 
                value, CompareOptions.None);
        }

        /// <summary>
        /// Returns the position of the last occurrence in the specified string
        /// of the given search string.
        /// </summary>
        /// <param name="source">The string to search.</param>
        /// <param name="value">The string to locate within <paramref name="source" />.</param>
        /// <returns>
        /// <para>
        /// The highest-index position of <paramref name="value" /> in
        /// <paramref name="source" /> if it is found, or -1 if <paramref name="source" /> 
        /// does not contain <paramref name="value" />.
        /// </para>
        /// <para>
        /// If <paramref name="value" /> is an empty string, the return value
        /// is the last index position in <paramref name="source" />.
        /// </para>
        /// </returns>
        /// <remarks>
        /// This function performs a case-sensitive word search using the 
        /// invariant culture.
        /// </remarks>
        /// <example>
        /// <code>string::last-index-of('testing string', 'test') ==> 0</code>
        /// </example>
        /// <example>
        /// <code>string::last-index-of('testing string', '') ==> 13</code>
        /// </example>
        /// <example>
        /// <code>string::last-index-of('testing string', 'Test') ==> -1</code>
        /// </example>
        /// <example>
        /// <code>string::last-index-of('testing string', 'ing') ==> 11</code>
        /// </example>
        [Function("last-index-of")]
        public static int LastIndexOf(string source, string value) {
            return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(source, 
                value, CompareOptions.None);
        }

        /// <summary>
        /// Returns the given string left-padded to the given length.
        /// </summary>
        /// <param name="s">The <see cref="string" /> that needs to be left-padded.</param>
        /// <param name="totalWidth">The number of characters in the resulting string, equal to the number of original characters plus any additional padding characters.</param>
        /// <param name="paddingChar">A Unicode padding character.</param>
        /// <returns>
        /// If the length of <paramref name="s" /> is at least 
        /// <paramref name="totalWidth" />, then a new <see cref="string" /> identical
        /// to <paramref name="s" /> is returned. Otherwise, <paramref name="s" /> 
        /// will be padded on the left with as many <paramref name="paddingChar" />
        /// characters as needed to create a length of <paramref name="totalWidth" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="totalWidth" /> is less than zero.</exception>
        /// <remarks>
        /// Note that only the first character of <paramref name="paddingChar" />
        /// will be used when padding the result.
        /// </remarks>
        /// <example>
        /// <code>string::pad-left('test', 10, ' ') ==> '      test'</code>
        /// </example>
        /// <example>
        /// <code>string::pad-left('test', 10, 'test') ==> 'tttttttest'</code>
        /// </example>
        /// <example>
        /// <code>string::pad-left('test', 3, ' ') ==> 'test'</code>
        /// </example>
        /// <example>
        /// <code>string::pad-left('test', -4, ' ') ==> ERROR</code>
        /// </example>
        [Function("pad-left")]
        public static string PadLeft(string s, int totalWidth, string paddingChar) {
            return s.PadLeft(totalWidth, paddingChar[0]);
        }

        /// <summary>
        /// Returns the given string right-padded to the given length.
        /// </summary>
        /// <param name="s">The <see cref="string" /> that needs to be right-padded.</param>
        /// <param name="totalWidth">The number of characters in the resulting string, equal to the number of original characters plus any additional padding characters.</param>
        /// <param name="paddingChar">A Unicode padding character.</param>
        /// <returns>
        /// If the length of <paramref name="s" /> is at least 
        /// <paramref name="totalWidth" />, then a new <see cref="string" /> identical
        /// to <paramref name="s" /> is returned. Otherwise, <paramref name="s" /> 
        /// will be padded on the right with as many <paramref name="paddingChar" />
        /// characters as needed to create a length of <paramref name="totalWidth" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="totalWidth" /> is less than zero.</exception>
        /// <remarks>
        /// Note that only the first character of <paramref name="paddingChar" />
        /// will be used when padding the result.
        /// </remarks>
        /// <example>
        /// <code>string::pad-right('test', 10, ' ') ==> 'test      '</code>
        /// </example>
        /// <example>
        /// <code>string::pad-right('test', 10, 'abcd') ==> 'testaaaaaa'</code>
        /// </example>
        /// <example>
        /// <code>string::pad-right('test', 3, ' ') ==> 'test'</code>
        /// </example>
        /// <example>
        /// <code>string::pad-right('test', -3, ' ') ==> ERROR</code>
        /// </example>
        [Function("pad-right")]
        public static string PadRight(string s, int totalWidth, string paddingChar) {
            return s.PadRight(totalWidth, paddingChar[0]);
        }

        /// <summary>
        /// Returns the given string trimmed of whitespace.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string <paramref name="s" /> with any leading or trailing
        /// white space characters removed.
        /// </returns>
        /// <example>
        /// <code>string::trim('  test  ') ==> 'test'</code>
        /// </example>
        /// <example>
        /// <code>string::trim('\t\tfoo  \r\n') ==> 'foo'</code>
        /// </example>
        [Function("trim")]
        public static string Trim(string s) {
            return s.Trim();
        }
        
        /// <summary>
        /// Returns the given string trimmed of leading whitespace.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string <paramref name="s" /> with any leading
        /// whites pace characters removed.
        /// </returns>
        /// <example>
        /// <code>string::trim-start('  test  ') ==> 'test  '</code>
        /// </example>
        /// <example>
        /// <code>string::trim-start('\t\tfoo  \r\n') ==> 'foo  \r\n'</code>
        /// </example>
        [Function("trim-start")]
        public static string TrimStart(string s) {
            return s.TrimStart();
        }
        
        /// <summary>
        /// Returns the given string trimmed of trailing whitespace.
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>
        /// The string <paramref name="s" /> with any trailing
        /// white space characters removed.
        /// </returns>
        /// <example>
        /// <code>string::trim-end('  test  ') ==> '  test'</code>
        /// </example>
        /// <example>
        /// <code>string::trim-end('\t\tfoo  \r\n') ==> '\t\tfoo'</code>
        /// </example>
        [Function("trim-end")]
        public static string TrimEnd(string s) {
            return s.TrimEnd();
        }

        #endregion Public Static Methods
    }
}
