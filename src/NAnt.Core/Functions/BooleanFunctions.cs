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
    /// Class which provides NAnt functions to convert strings in boolean values and vice versa.
    /// </summary>
    [FunctionSet("bool", "Conversion")]
    public class BooleanConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanConversionFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public BooleanConversionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the specified string representation of a logical value to 
        /// its <see cref="bool" /> equivalent.
        /// </summary>
        /// <param name="s">A string containing the value to convert.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="s" /> is equivalent to 
        /// "True"; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="s" /> is not equivalent to <see cref="bool.TrueString" /> or <see cref="bool.FalseString" />.</exception>
        [Function("parse")]
        public static bool Parse(string s) {
            return bool.Parse(s);
        }

        /// <summary>
        /// Converts the specified <see cref="bool" /> to its equivalent string
        /// representation.
        /// </summary>
        /// <param name="value">A <see cref="bool" /> to convert.</param>
        /// <returns>
        /// "True" if <paramref name="value" /> is <see langword="true" />, or 
        /// "False" if <paramref name="value" /> is <see langword="false" />. 
        /// </returns>
        [Function("to-string")]
        public static string ToString(bool value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion Public Static Methods
    }
}
