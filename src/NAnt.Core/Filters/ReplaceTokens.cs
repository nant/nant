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
using System.Collections;
using System.Collections.Specialized;

namespace NAnt.Core.Filters {

    /// <summary>
    /// Replaces tokens in the original input with user-supplied values.
    /// </summary>
    ///
    /// <remarks>
    ///  Replaces all tokens between the beginning and ending
    ///  token; begintoken and endtoken.
    ///  <para>
    ///  Tokens are specified using the &lt;token&gt; element implemented in the <see cref="ReplaceTokensToken"/> class.
    ///  </para>
    ///  <para>
    ///  The beginning and ending token defualts to @.
    ///  </para>
    ///
    /// <para>Parameters:</para>
    /// <list type="table">
    ///    <listheader>
    ///   <term>Parameter</term>
    ///   <description>Description</description>
    ///  </listheader>
    ///  <item>
    ///   <term><code>&lt;order&gt;</code></term>
    ///   <description>The order this filter will be in the <see cref="FilterChain"></see></description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;begintoken&gt;</code></term>
    ///   <description>(Optional) Marks the beginning of a token.  Default = @</description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;endtoken&gt;</code></term>
    ///   <description>(Optional) Marks the end of a token.  Default = @</description>
    ///  </item>
    /// </list>
    /// </remarks>
    ///
    /// <example>
    ///  <para>Standard Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <replacetokens begintoken="@" endtoken="@" order="1">
    ///   <token key="DATE" value="${TODAY}"/>
    ///  </replacetokens>
    ///  ]]>
    ///  </code>
    ///  <para>Generic Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <filter assembly="NAnt.Core" class="NAnt.Core.Filters.ReplaceTokens" order="1">
    ///    <parameter name="begintoken" value="@"/>
    ///    <parameter name="endtoken" value="@"/>
    ///  </filter>
    ///  ]]>
    ///  </code>
    /// </example>
    ///
    public class ReplaceTokens : Filter {
        //Delegate for Read and Peek. Allows the same implementation
        //to be used for both methods.
        delegate int AcquireCharDelegate();

        #region Private Instance Members

        private char _beginToken = '@';
        private char _endToken = '@';
        private StringDictionary _tokenValues = new StringDictionary();

        private StringBuilder _tokenString = null;
        private int _maxTokenLength;
        private string _outputBuffer = null;
        private bool _endStreamAfterBuffer = false;
        private int _bufferPosition = 0;
        private bool _unknownToken = true;
        private bool _tokenNotFound = true;

        //Methods used for Read and Peek
        private AcquireCharDelegate ReadChar = null;
        private AcquireCharDelegate PeekChar = null;

        #endregion Private Instance Members

        #region Public Instance Methods

        /// <summary>
        /// Construct that allows this filter to be chained to the one
        /// in the parameter chainedReader.
        /// </summary>
        /// <param name="chainedReader">Filter that the filter will be chained to</param>
        public ReplaceTokens(ChainableReader chainedReader) : base(chainedReader) {
            ReadChar = new AcquireCharDelegate(base.Read);
            PeekChar = new AcquireCharDelegate(base.Peek);
        }

        /// <summary>
        /// Initialize the filter by setting its parameters.
        /// </summary>
        public override void Initialize() {

            //Process each parameter
            for (int index = 0 ; index < base.Parameters.Count ; index++) {
                if (Parameters.GetValues(index).Length > 1) {
                    base.Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "More than one value was specified for {0}. Only the first one will be used.", Parameters.Keys[index]));
                }

                switch (Parameters.Keys[index]) {
                case "begintoken" : {
                        if (null == Parameters.GetValues(index)[0]) {
                            throw new BuildException("The parameter 'begintoken' can not be empty.");
                        } else if (Parameters.GetValues(index)[0].Length != 1) {
                            Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "The parameter 'begintoken' is greater than 1 character. Only the first character will be used. begintoken = {0}", Parameters.GetValues(index)[0][0]));
                        }
                        _beginToken = Parameters.GetValues(index)[0][0];

                        break;
                    }

                case "endtoken" : {
                        if (null == Parameters.GetValues(index)[0]) {
                            throw new BuildException("The parameter 'begintoken' can not be empty.");
                        } else if (Parameters.GetValues(index)[0].Length != 1) {
                            Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "The parameter 'endtoken' is greater than 1 character. Only the first character will be used. begintoken = {0}", Parameters.GetValues(index)[0][0]));
                        }

                        _endToken = Parameters.GetValues(index)[0][0];

                        break;
                    }

                default : {
                        //Add new token pair
                        _tokenValues.Add(Parameters.Keys[index], Parameters.GetValues(index)[0]);

                        //Track max character length
                        if (Parameters.Keys[index].Length > _maxTokenLength) {
                            _maxTokenLength = Parameters.Keys[index].Length;
                        }
                        break;
                    }
                }
            }

            if (_tokenValues.Count < 1) {
                throw new ApplicationException("One or more tokens and replacement values should be specified.");
            }

            //Create a string builder to use for a buffer while searching for tokens.
            _tokenString = new StringBuilder(_maxTokenLength + 1, _maxTokenLength + 1);
        }

        /// <summary>
        /// Finds a token give that we are positioned at a beginning token character.  Either a
        /// token replacement is returned or the characters that were read looking for the token.
        /// </summary>
        /// <param name="tokenNotFound">A token was not found</param>
        /// <param name="unknownToken">A token was found by there is no replacement</param>
        /// <param name="streamEnded">The stream ended while looking for the token</param>
        /// <returns>Either the replacement token or the characters that were read looking for the token</returns>
        private string FindTokenContents( out bool tokenNotFound, out bool unknownToken, out bool streamEnded) {
            int charactersScanned = 0;
            char currentChar = _beginToken;
            bool tokenFound = false;
            tokenNotFound = false;
            streamEnded = false;
            unknownToken = true;

            //Reset token string
            _tokenString.Length = 0;

            //Only peak within the limits of the largest token
            while ((charactersScanned <= _maxTokenLength)) {
                charactersScanned++;

                //Read a character
                int streamChar = base.Read();
                currentChar = (char) streamChar;

                //Check for end of stream
                if (streamChar == -1) {
                    //Two adjacent tokens were found
                    tokenNotFound = true;
                    unknownToken = true;
                    streamEnded = true;
                    return _tokenString.ToString();
                }

                if ((currentChar == _endToken)) {
                    tokenFound = true;
                    break;
                } else if ((currentChar == _beginToken) && (_endToken != _beginToken)) {
                    //Only happens if the beginning and ending tokens are not the same
                    //Add end char and break
                    tokenNotFound = true;
                    unknownToken = true;
                    _tokenString.Append((char)currentChar);
                    return _tokenString.ToString();
                } else {
                    //Add possiable token contents to the buffer
                    _tokenString.Append((char)currentChar);
                }
            }

            //Token found and length greater than 0
            if ((tokenFound)) {
                string replacementValue = null;

                //Look up token if not empty
                if (_tokenString.Length != 0) {
                    //Token found so look it up
                    string contentsRead = _tokenString.ToString();
                    replacementValue = _tokenValues[contentsRead];
                } else {
                    //Two adjacent tokens were found
                    tokenNotFound = true;
                    unknownToken = true;

                    return new string(currentChar, 1);
                }


                //Did we find a replacement value for the token?
                if (replacementValue != null) {
                    //This was a token we can handle
                    tokenNotFound = false;
                    unknownToken = false;

                    //Return the replacment value to output
                    return replacementValue;
                } else //We don't know about the token
                {
                    //The token was not in the list so just output it but add then ending
                    //token character back.
                    tokenNotFound = true;
                    unknownToken = true;
                    return _tokenString.Append(currentChar).ToString();
                }
            } else {  //Read max number of characters
                
                //return string to output in future reads
                tokenNotFound = true;
                unknownToken = false;

                return _tokenString.ToString();
            }

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
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Peek() {
            //Need to maintain seperate state for Read and Peek for this to work
            throw new ApplicationException("Peek currently is not supported.");
            //return GetNextCharacter(PeekChar);
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

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
                //Characters left in the buffer?
                if (_bufferPosition < _outputBuffer.Length) {

                    //If this is the last character of a token string that is unknown
                    //process the charactor again since it might be the beginning of another token.
                    if ((_tokenNotFound == true) && (_unknownToken == true) && (_bufferPosition == _outputBuffer.Length - 1)) {
                        //Process token end char again. It could be the same as token begin.
                        ch = _outputBuffer[_outputBuffer.Length - 1];
                        _bufferPosition++;
                    } else {
                        //Pass along buffer character
                        return _outputBuffer[_bufferPosition++];
                    }
                } else  {//End of buffer

                    //Reset buff and get next char
                    _outputBuffer = null;
                    _bufferPosition = 0;

                    //Reaad the next character or end the stream the end of the stream
                    //was encountered while reading the buffer.
                    if (!_endStreamAfterBuffer) {
                        ch = ReadChar();
                    } else {
                        return -1;
                    }
                }
            }

            //Process beginning token
            if (ch == _beginToken) {
                //Look for a token after _beginToken and return either the replacement token
                //or the charactors that were read.
                _outputBuffer = FindTokenContents(out _tokenNotFound, out _unknownToken, out _endStreamAfterBuffer);

                //A token was not found so _beginToken needs to be accounted for.
                if (_tokenNotFound) {
                    _bufferPosition = 0;
                    return _beginToken;
                } else {
                    //Output first character of buffer
                    _bufferPosition = 1;
                    return _outputBuffer[0];
                }
            } else {
                //This was not a beginning token so just pass it through
                return ch;
            }
        }
        #endregion Private Instance Methods
    }
}

