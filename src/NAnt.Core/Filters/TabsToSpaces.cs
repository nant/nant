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
    /// Converts tabs to spaces
    /// </summary>
    ///
    /// <remarks>
    /// The TabsToSpaces filter replaces tabs in a text file with spaces.
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
    ///   <term><code>&lt;replacementchar&gt;</code></term>
    ///   <description>(Optional) Character to replace tabs with.  Default = " "</description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;replacementspaces&gt;</code></term>
    ///   <description>(Optional) Number of characters each tab should be replaced with.  Default = 4</description>
    ///  </item>
    /// </list>
    /// </remarks>
    ///
    /// <example>
    ///  <para>Standard Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <tabtospaces replacementchar=" " tablength="4" order="1"/>
    ///  ]]>
    ///  </code>
    ///  <para>Generic Syntax</para>
    ///  <code>
    ///  <![CDATA[
    ///  <filter>
    ///   <assembly="NAnt.Core" class="NAnt.Core.Filters.TabsToSpaces"/>;
    ///    <param name="replacementchar" value=" "/>
    ///    <param name="tablength"  value="4"/>
    ///    <param name="order" value="1"/>
    ///  </filter>
    ///  ]]>
    ///  </code>
    /// </example>
    ///
    ///
    ///
    public class TabsToSpaces : Filter {
        //Delegate for Read and Peek. Allows the same implementation
        //to be used for both methods.
        delegate int AcquireCharDelegate();

        private int _replacementSpaces = 4;
        private char _replacementChar = ' ';
        int _spacesRemaining = 0;

        //Methods used for Read and Peek
        private AcquireCharDelegate ReadChar = null;
        private AcquireCharDelegate PeekChar = null;


        /// <summary>
        /// Construct that allows this filter to be chained to the one
        /// in the parameter chainedReader.
        /// </summary>
        /// <param name="chainedReader">Filter that the filter will be chained to</param>
        public TabsToSpaces(ChainableReader chainedReader) : base(chainedReader) {
            ReadChar = new AcquireCharDelegate(base.Read);
            PeekChar = new AcquireCharDelegate(base.Peek);
        }

        /// <summary>
        /// <para>Retrieves the next character with moving the position in the stream.</para>
        /// <note>This method is not implemented</note>
        /// </summary>
        /// <returns>-1 if end of stream otherwise a character</returns>
        public override int Peek() {
            //Need to maintain seperate state for Read and Peek for this to work
            throw new ApplicationException("Peek currently is not supported.");
            //return GetNextCharacter(PeekChar)
        }

        /// <summary>
        /// <para>Retrieves the next character in the stream.</para>
        /// </summary>
        /// <returns>-1 if end of stream otherwise a character</returns>
        public override int Read() {
            return GetNextCharacter(ReadChar);
        }

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
                    _spacesRemaining = _replacementSpaces - 1;
                    return _replacementChar;
                } else {
                    return nextChar;
                }
            } else {
                _spacesRemaining--;
                return _replacementChar;
            }
        }

        /// <summary>
        /// Initialize the filter by setting its parameters.
        /// </summary>
        public override void Initialize() {
            string temp = Parameters["replacementchar"];
            if (temp != null) {
                if (temp.Length != 1) {
                    if (temp.Length == 0) {
                        throw new BuildException("The parameter 'replacementchar' is empty.");
                    } else {
                        Log(Level.Warning, string.Format(CultureInfo.InvariantCulture, "The parameter 'replacementchar' is greater than 1 character. Only the first character will be used . replacementchar = {0}", temp));
                    }
                }

                _replacementChar = temp[0];
            }

            temp = Parameters["replacementspaces"];
            if (temp != null) {
                try {
                    _replacementSpaces = Int32.Parse(temp);

                    if ((_replacementSpaces < 1) || (_replacementSpaces > 100)) {
                        throw new BuildException("The parameter 'replacementspaces' must be between 1 and 100.");
                    }
                } catch (FormatException e) {
                    throw new BuildException("The parameter 'replacementspaces' must be a numeric value.", e);
                }
            }
        }
    }
}


