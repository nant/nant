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
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions for working with floating point numbers.
    /// </summary>
    [FunctionSet("math", "Math")]
    public class MathFunctions : FunctionSetBase {
        #region Public Instance Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="MathFunctions"/> class.
      /// </summary>
      /// <param name="project">The current project.</param>
      /// <param name="properties">The projects properties.</param>
        public MathFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Rounds the value to the nearest whole number
        /// </summary>
        /// <param name="value">Number to be rounded, can be anything convertible to a double.</param>
        /// <returns>
        /// Rounded value.
        /// </returns>
        [Function("round")]
        public static double Round(double value) {
            return Math.Round(value);
        }

        /// <summary>
        /// Returns the largest whole number less than or equal to the specified 
        /// number.
        /// </summary>
        /// <param name="value">value to be , can be anything convertible to a double</param>
        /// <returns>
        /// The largest whole number less than or equal to the specified number.
        /// </returns>
        [Function("floor")]
        public static double Floor(double value) {
            return Math.Floor(value);
        }

        /// <summary>
        /// Returns the smallest whole number greater than or equal to the specified number
        /// </summary>
        /// <param name="value">value</param>
        /// <returns>
        /// The smallest whole number greater than or equal to the specified number.
        /// </returns>
        [Function("ceiling")]
        public static double Ceiling(double value) {
            return Math.Ceiling(value);
        }

        /// <summary>
        /// Returns the absolute value of the specified number
        /// </summary>
        /// <param name="value">value to take the absolute value from</param>
        /// <returns>
        /// <paramref name="value" /> when <paramref name="value" /> is greater 
        /// than or equal to zero; otherwise, -<paramref name="value" />.
        /// </returns>
        [Function("abs")]
        public static double Abs(double value) {
            return Math.Abs(value);
        }

        #endregion Public Static Methods
    }
}
