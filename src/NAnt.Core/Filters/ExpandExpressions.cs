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

namespace NAnt.Core.Filters {
    /// <summary>
    /// Parses NAnt properties and expressions
    /// </summary>
    /// <remarks>
    /// <para>This filter parses any NAnt properties or expressions found in its input, inlining their values in its output.</para>
    /// <para>Note: Due to limitations on buffering, expressions longer than 2048 characters are not guaranteed to be expanded.</para>
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
    /// </list>
    /// </remarks>
    /// <example>
    ///  <para>Standard Syntax</para>
    ///  <code><![CDATA[
    ///    <expandexpressions order="0" />
    ///  ]]></code>
    ///  <para>Generic Syntax</para>
    ///   <code><![CDATA[
    ///    <filter assembly="NAnt.Core" class="NAnt.Core.Filters.ExpandExpressions" order="0" />
    ///  ]]></code>
    /// </example>
    public class ExpandExpressions : Filter {
        // Due to limitations on buffering, expressions longer than this number of characters are not guaranteed to be expanded.
        const ushort MAX_RELIABLE_EXPRESSION_LENGTH = 2048;

        // A buffer this size ensures that any expression up to MAX_RELIABLE_EXPRESSION_LENGTH will be sent in one piece to ExpandExpression.
        const int BUFFER_LENGTH = MAX_RELIABLE_EXPRESSION_LENGTH * 2 - 1;

        StringBuilder _buffer;  // Holds data for expression expansion between input and output.

        /// <summary>
        /// Construct that allows this filter to be chained to the one
        /// in the parameter chainedReader.
        /// </summary>
        /// <param name="chainedReader">Filter that the filter will be chained to</param>
        public ExpandExpressions(ChainableReader chainedReader) : base(chainedReader) { }

        /// <summary>
        /// .) Called after construction and after properties are set.
        /// </summary>
        /// <remarks>
        /// <para>See <see cref="Filter.Initialize()">Filter.Initialize</see> for contract information.</para>
        /// <para>Initializes the internal buffer.</para>
        /// </remarks>
        public override void Initialize() {
            _buffer = new StringBuilder(BUFFER_LENGTH);
            ReplenishBuffer();
        }

        /// <summary>
        /// Reads the next character applying the filter logic.
        /// </summary>
        /// <returns>Char as an int or -1 if at the end of the stream</returns>
        public override int Read() {
            int temp = Peek();
            if ( ! AtEnd) {
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

        /// <summary>
        /// Determines whether we've passed the end of our data.
        /// </summary>
        bool AtEnd {
            get {
                return _buffer.Length == 0;
            }
        }

        /// <summary>
        /// Moves to the next character.
        /// </summary>
        void Advance () {
            if (AtEnd) {
                throw new IndexOutOfRangeException("End of output has been reached.");
            }

            _buffer.Remove(0, 1);

            if (_buffer.Length == (MAX_RELIABLE_EXPRESSION_LENGTH - 1)) {
                ReplenishBuffer();
            }
        }

        /// <summary>
        /// Refills the buffer, running our input through our Project.Properties.ExpandProperties. (See <see cref="PropertyDictionary.ExpandProperties"/>.)
        /// </summary>
        void ReplenishBuffer () {
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
    }
}
