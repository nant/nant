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
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Replaces all occurrences of a given string in the original input with 
    /// user-supplied replacement string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter replaces all occurrences of a given string in the original 
    /// input stream with a user-supplied replacement string. By default string 
    /// comparisons are case sensitive but this can be changed by setting the 
    /// optional <see cref="IgnoreCase"/> attribute to <see langword="true" />.
    /// </para>
    /// <para>
    /// To use this filter specify the string to be replaced with the 
    /// <see cref="From"/> attribute and the string to replace it with using the 
    /// <see cref="To" /> attribute. 
    /// </para>
    /// <para>
    /// Filters are intended to be used as a element of a <see cref="FilterChain" />.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Replace all occurrences of "3.14" with "PI".
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <replacestring from="3.14" to="PI" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Replace all occurrences of "string", "String", etc. with "System.String".
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <replacestring from="String" to="System.String" ignorecase="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    [ElementName("replacestring")] 
    public class ReplaceString : Filter {
        /// <summary>
        /// Delegate for Read and Peek. Allows the same implementation
        /// to be used for both methods.
        /// </summary>
        delegate int AcquireCharDelegate();

        #region Private Instance Fields

        private string _from;
        private string _to = string.Empty;
        private string _outputBuffer;
        private bool _endStreamAfterBuffer;
        private int _bufferPosition = 0;
        private bool _stringNotFound = true;
        private bool _ignoreCase;

        //Method used for Read
        private AcquireCharDelegate ReadChar;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The string to be replaced.
        /// </summary>
        [TaskAttribute("from", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string From {
            get { return _from; }
            set { _from = value; }
        }

        /// <summary>
        /// The new value for the replaced string.
        /// Am empty string is permissible.
        /// </summary>
        [TaskAttribute("to", Required=false)]
        [StringValidator(AllowEmpty=true)]
        public string To {
            get { return _to; }
            set { _to = value; }
        }

        /// <summary>
        /// Determines if case will be ignored.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("ignorecase", Required=false)]
        [BooleanValidator()]
        public bool IgnoreCase {
            get { return _ignoreCase; }
            set { _ignoreCase = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ChainableReader

        /// <summary>
        /// Construct that allows this filter to be chained to the one
        /// in the parameter chainedReader.
        /// </summary>
        /// <param name="chainedReader">Filter that the filter will be chained to</param>
        public override void Chain(ChainableReader chainedReader) {
            base.Chain(chainedReader);
            ReadChar = new AcquireCharDelegate(base.Read);
        }

        /// <summary>
        /// Reads the next character applying the filter logic.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Read() {
            return GetNextCharacter(ReadChar);
        }

        /// <summary>
        /// Reads the next character applying the filter logic without
        /// advancing the current position in the stream.
        ///
        /// Peek currently is not supported.
        /// </summary>
        /// <returns>
        /// Char as an int or -1 if at the end of the stream.
        /// </returns>
        public override int Peek() {
            //Need to maintain seperate state for Read and Peek for this to work
            throw new ApplicationException(ResourceUtils.GetString("String_PeekNotSupported"));
        }

        #endregion Override implementation of ChainableReader

        #region Private Instance Methods

        /// <summary>
        /// <para>
        /// Helper function used to search for the filter's traget string. If the string
        /// is found the result is true. If the string was not found false is returned and
        /// nonMatchingChars contains the characters that were read to determine if the 
        /// string is present.
        /// </para>
        /// 
        /// <para>
        /// It is assumed the stream is positioned at the character after the first character 
        /// in the target string.
        /// </para>
        /// </summary>
        /// <param name="startChar">First character in target string</param>
        /// <param name="streamEnded">Ture if the stream ended while search for the string.</param>
        /// <param name="nonMatchingChars">Characters that were read while searching for the string.</param>
        /// <returns></returns>
        private bool FindString(int startChar, out bool streamEnded, out string nonMatchingChars) {

            //Init output parameters
            streamEnded = false;
            nonMatchingChars = "";

            //create a new buffer
            StringBuilder buffer = new StringBuilder(_from.Length, _from.Length);

            //Add first char that initiate the FindString
            buffer.Append((char)startChar);

            //Try to read each character of the string to replace.
            //Store the characters in the output buffer until we know 
            //we have found the string.
            int streamChar;
            for (int pos = 1 ; pos < _from.Length ; pos++) {
                //Read a character
                streamChar = base.Read();

                //Store the character if it is not the end of the buffer character
                if (streamChar != -1) {
                    buffer.Append((char)streamChar);
                }

                //Is it the correct character?
                if (CompareCharacters(streamChar, _from[pos]) == false) {
                    //Check for end of stream
                    if (streamChar == -1) {
                        streamEnded = true;
                    }

                    //Put any characters that were read into the output buffer since
                    //the string was not found.
                    nonMatchingChars = buffer.ToString();

                    return false;
                }
            }

            //The string was found
            return true;
        }

        /// <summary>
        /// Returns the next character in the stream replacing the specified character. Using the
        /// <see cref="AcquireCharDelegate"/> allows for the same implementation for Read and Peek
        /// </summary>
        /// <param name="AcquireChar">Delegate to acquire the next character. (Read/Peek)</param>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        private int GetNextCharacter(AcquireCharDelegate AcquireChar) {
            int ch;

            //Either read the next character or if there is a buffer output the next character
            if (_outputBuffer == null) {
                ch = base.Read();
            } else {
                // characters left in the buffer?
                if (_bufferPosition < _outputBuffer.Length) {

                    //If this is the last character of a buffer that was not the replacemant string
                    //process the last charactor again since it might be the beginning of another token.
                    if ((_stringNotFound == true) && (_bufferPosition == _outputBuffer.Length - 1)) {
                        //Process token end char again. It could be the same as token begin.
                        ch = _outputBuffer[_outputBuffer.Length - 1];
                        _bufferPosition++;
                    } else {
                        //Pass along buffer character
                        return _outputBuffer[_bufferPosition++];
                    }
                } else  {//End of buffer

                    //Reset buffer and get next char
                    _outputBuffer = null;
                    _bufferPosition = 0;

                    //Read the next character or end the stream the end of the stream
                    //was encountered while reading the buffer.
                    if (!_endStreamAfterBuffer) {
                        ch = ReadChar();
                    } else {
                        return -1;
                    }
                }
            }

            // if the character matches the first character of the target string then search
            // for the string.
            if (CompareCharacters(ch, _from[0])) {
                // search for the target string
                if (FindString(ch, out _endStreamAfterBuffer, out _outputBuffer) == true) {
                    //Target was found

                    _stringNotFound = false;

                    //Do nothing if _to is null; otherwise output _to
                    if (_to==string.Empty) {
                        _outputBuffer = null;
                        return GetNextCharacter(AcquireChar);
                    }
                    else {
                        _outputBuffer = _to;
                        _bufferPosition = 1;
                    }

                    return _to[0];
                } else {
                    //Target not found

                    _stringNotFound = true;
                    _bufferPosition = 1;
                    return ch;
                }

            } else {
                //This was not a beginning token so just pass it through
                return ch;
            }

        }

        /// <summary>
        /// Compares to characters taking into account the _ignoreCase flag.
        /// </summary>
        /// <param name="char1"></param>
        /// <param name="char2"></param>
        /// <returns></returns>
        private bool CompareCharacters(int char1, int char2) {
            //Compare chars with or without case
            if (_ignoreCase == true) {
                    
                return (char.ToUpper((char)char1) == char.ToUpper((char)char2));
            }
            else {
                return char1 == char2;
            }
        }

        #endregion Private Instance Methods
    }
}

