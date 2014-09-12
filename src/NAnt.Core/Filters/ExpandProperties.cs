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
    /// Parses NAnt properties and expressions
    /// </summary>
    /// <remarks>
    /// <para>
    /// This filter parses any NAnt properties or expressions found in its input, 
    /// inlining their values in its output.
    /// </para>
    /// <para>
    /// Note: Due to limitations on buffering, expressions longer than 2048 
    /// characters are not guaranteed to be expanded.
    /// </para>
    /// Filters are intended to be used as a element of a <see cref="FilterChain"/>.
    /// </remarks>
    /// <example>
    ///   <para>Replace all properties with their corresponding values.</para>
    ///   <code>
    ///     <![CDATA[
    /// <expandproperties />
    ///     ]]>
    ///   </code>
    /// </example>
    [ElementName("expandproperties")]
    public class ExpandProperties : Filter {
        #region Private Instance Fields

        /// <summary>
        /// Holds data for expression expansion between input and output.
        /// </summary>
        private StringBuilder _buffer;

        #endregion Private Instance Fields

        #region Private Static Fields

        // Due to limitations on buffering, expressions longer than this number of characters are not guaranteed to be expanded.
        const ushort MAX_RELIABLE_EXPRESSION_LENGTH = 2048;

        // A buffer this size ensures that any expression up to MAX_RELIABLE_EXPRESSION_LENGTH will be sent in one piece to ExpandExpression.
        const int BUFFER_LENGTH = MAX_RELIABLE_EXPRESSION_LENGTH * 2 - 1;

        #endregion Private Static Fields

        #region Private Instance Properties

        /// <summary>
        /// Determines whether we've passed the end of our data.
        /// </summary>
        private bool AtEnd {
            get { return _buffer.Length == 0; }
        }

        #endregion Private Instance Properties

        #region Override implementation of Filter

        /// <summary>
        /// Called after construction and after properties are set. Allows
        /// for filter initialization.
        /// </summary>
        public override void InitializeFilter() {
            _buffer = new StringBuilder(BUFFER_LENGTH);
            ReplenishBuffer();
        }

        #endregion Override implementation of Filter

        #region Override implementation of ChainableReader

        /// <summary>
        /// Reads the next character applying the filter logic.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Read() {
            int temp = Peek();
            if (! AtEnd) {
                Advance();
            }
            return temp;
        }

        /// <summary>
        /// Reads the next character applying the filter logic without advancing the current position in the stream.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Peek() {
            if (AtEnd) {
                return -1;
            } else {
                return _buffer[0];
            }
        }

        #endregion Override implementation of ChainableReader

        #region Private Instance Methods

        /// <summary>
        /// Moves to the next character.
        /// </summary>
        private void Advance() {
            if (AtEnd) {
                throw new IndexOutOfRangeException(ResourceUtils.GetString("String_EndOfOutput"));
            }

            _buffer.Remove(0, 1);

            if (_buffer.Length == (MAX_RELIABLE_EXPRESSION_LENGTH - 1)) {
                ReplenishBuffer();
            }
        }

        /// <summary>
        /// Refills the buffer, running our input through 
        /// <see cref="M:PropertyDictionary.ExpandProperties(string, Location)" />.)
        /// </summary>
        private void ReplenishBuffer () {
            // Fill buffer from input.
            bool isMoreInput = true;
            int curCharInt;
            while ((_buffer.Length < BUFFER_LENGTH) && isMoreInput) {
                curCharInt = base.Read();
                if (curCharInt != -1) {
                    _buffer.Append((char) curCharInt);
                } else {
                    isMoreInput = false;
                }
            }

            string bufferBeforeExpand = _buffer.ToString();
            int lastExprStartIndex = bufferBeforeExpand.LastIndexOf("${");
            int lastExprEndIndex = bufferBeforeExpand.LastIndexOf('}');
            string bufferAfterExpand;

            // Expand properties from input into buffer for output.
            if (lastExprEndIndex < lastExprStartIndex) {
                // There's an unfinished expression - don't attempt to expand it yet. Perhaps it will all fit in the buffer next time around.
                bufferAfterExpand = Project.Properties.ExpandProperties(bufferBeforeExpand.Substring(0, lastExprStartIndex), Location);
                bufferBeforeExpand = bufferBeforeExpand.Substring(lastExprStartIndex);
                _buffer = new StringBuilder(bufferAfterExpand, Math.Max(BUFFER_LENGTH, bufferAfterExpand.Length + bufferBeforeExpand.Length));
                _buffer.Append(bufferBeforeExpand);
            } else {
                // No partial expressions - keep it simple.
                bufferAfterExpand = Project.Properties.ExpandProperties(bufferBeforeExpand, Location);
                _buffer = new StringBuilder(bufferAfterExpand, Math.Max(BUFFER_LENGTH, bufferAfterExpand.Length));
            }
        }

        #endregion Private Instance Methods
    }
}
