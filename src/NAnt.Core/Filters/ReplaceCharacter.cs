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
using System.Globalization;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Replaces a specific character in a file.
    /// </summary>
    ///
    /// <remarks>
    /// Replaces the character specified by the chartoreplace parameter with the
    /// character specified with the replacementchar parameter.
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
    ///   <term><code>&lt;chartoreplace&gt;</code></term>
    ///   <description>Character to replace</description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;replacementchar&gt;</code></term>
    ///   <description>Character the will replace &lt;chartoreplace&gt;</description>
    ///  </item>>
    /// </list>
    /// </remarks>
    ///
    /// <example>
    ///  <para>Standard Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <replacecharacter chartoreplace="@" replacementchar="~" order="1"/>
    ///  ]]>
    ///  </code>
    ///  <para>Generic Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <filter assembly="NAnt.Core" class="NAnt.Core.Filters.ReplaceCharacter" order="1">
    ///    <parameter name="chartoreplace" value="@"/>
    ///    <parameter name="replacementchar" value="~"/>
    ///  </filter>
    ///  ]]>
    ///  </code>
    /// </example>
    ///
    public class ReplaceCharacter : Filter {

        //Delegate for Read and Peek. Allows the same implementation
        //to be used for both methods.
        delegate int AcquireCharDelegate();

        #region Private Instance Members

        //Replacement characters
        private char _charToReplace = ' ';
        private char _replacementChar = ' ';

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
        public ReplaceCharacter(ChainableReader chainedReader) : base(chainedReader) {
            ReadChar = new AcquireCharDelegate(base.Read);
            PeekChar = new AcquireCharDelegate(base.Peek);
        }

        /// <summary>
        /// Initialize the filter by setting its parameters.
        /// </summary>
        public override void Initialize() {
            string temp = Parameters["replacementchar"];
            if (temp != null) {
                if (temp.Length != 1) {
                    Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "The parameter 'replacementchar' is greater than 1 character. Only the first character will be used. replacementchar = {0}", temp));
                }
                _replacementChar = temp[0];
            }


            temp = Parameters["chartoreplace"];
            if (temp != null) {
                if (temp.Length != 1) {
                    Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "The parameter 'chartoreplace' is greater than 1 character. Only the first character will be used. chartoreplace = {0}", temp));
                }

                _charToReplace = temp[0];
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
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Peek() {
            return GetNextCharacter(PeekChar);
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
            int nextChar = AcquireChar();
            if (nextChar == _charToReplace) {
                return _replacementChar;
            }
            return nextChar;
        }
        #endregion Private Instance Methods
    }
}



