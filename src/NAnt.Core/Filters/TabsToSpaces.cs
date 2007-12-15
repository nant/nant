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
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Converts tabs to spaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="TabsToSpaces" /> filter replaces tabs in a text file 
    /// with spaces.
    /// </para>
    /// <para>
    /// Filters are intended to be used as a element of a <see cref="FilterChain"/>.
    /// </para>
    /// </remarks>
    /// <example>
    ///  <para>Replace all tabs with four spaces.</para>
    ///  <code>
    ///    <![CDATA[
    /// <tabtospaces tablength="4" />
    ///    ]]>
    ///  </code>
    /// </example>
    [ElementName("tabstospaces")]
    public class TabsToSpaces : Filter {
        /// <summary>
        /// Delegate for Read and Peek. Allows the same implementation
        /// to be used for both methods.
        /// </summary>
        delegate int AcquireCharDelegate();

        #region Private Instance Fields

        private int _tabLength = 8;
        private int _spacesRemaining;

        //Method used for Read
        private AcquireCharDelegate ReadChar;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The number of spaces used when converting a tab. The default is 
        /// "8".
        /// </summary>
        [TaskAttribute("tablength")]
        [Int32Validator(MinValue=1, MaxValue=100)]
        public int TabLength {
            get { return _tabLength; }
            set { _tabLength = value; }
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
        /// <para>Retrieves the next character with moving the position in the stream.</para>
        /// <note>This method is not implemented</note>
        /// </summary>
        /// <returns>-1 if end of stream otherwise a character</returns>
        public override int Peek() {
            //Need to maintain seperate state for Read and Peek for this to work
            throw new ApplicationException(ResourceUtils.GetString("String_PeekNotSupported"));
        }

        /// <summary>
        /// <para>Retrieves the next character in the stream.</para>
        /// </summary>
        /// <returns>-1 if end of stream otherwise a character</returns>
        public override int Read() {
            return GetNextCharacter(ReadChar);
        }

        #endregion Override implementation of ChainableReader

        #region Private Instance Methods

        /// <summary>
        /// Returns the next character in the stream replacing the specified character. Using the
        /// <see cref="AcquireCharDelegate"/> allows for the same implementation for Read and Peek
        /// </summary>
        /// <param name="AcquireChar">Delegate to acquire the next character. (Read/Peek)</param>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        private int GetNextCharacter(AcquireCharDelegate AcquireChar) {
            if (_spacesRemaining == 0) {
                int nextChar = AcquireChar();
                if (nextChar == '\t') {
                    _spacesRemaining = TabLength - 1;
                    return ' ';
                } else {
                    return nextChar;
                }
            } else {
                _spacesRemaining--;
                return ' ';
            }
        }

        #endregion Private Instance Methods
    }
}
