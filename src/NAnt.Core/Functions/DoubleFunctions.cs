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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions to convert strings in double values and vice versa.
    /// </summary>
    [FunctionSet("double", "Conversion")]
    public class DoubleConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleConversionFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public DoubleConversionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the specified string representation of a number to its 
        /// double-precision floating point number equivalent.
        /// </summary>
        /// <param name="s">A string containing a number to convert.</param>
        /// <returns>
        /// A double-precision floating point number equivalent to the numeric 
        /// value or symbol specified in <paramref name="s" />.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="s" /> is not a number in a valid format.</exception>
        /// <exception cref="OverflowException"><paramref name="s" /> represents a number less than <see cref="double.MinValue" /> or greater than <see cref="double.MaxValue" />.</exception>
        /// <remarks>
        /// The <see cref="NumberFormatInfo" /> for the invariant culture is 
        /// used to supply formatting information about <paramref name="s" />.
        /// </remarks>
        [Function("parse")]
        public static double Parse(string s) {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the specified <see cref="double" /> to its equivalent 
        /// string representation.
        /// </summary>
        /// <param name="value">A <see cref="double" /> to convert.</param>
        /// <returns>
        /// The string representation of <paramref name="value" /> formatted
        /// using the general format specifier ("G").
        /// </returns>
        /// <remarks>
        /// <paramref name="value" /> is formatted with the 
        /// <see cref="NumberFormatInfo" /> for the invariant culture.
        /// </remarks>
        [Function("to-string")]
        public static string ToString(double value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion Public Static Methods
    }
}
