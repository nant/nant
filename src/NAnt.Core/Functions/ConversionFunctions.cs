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
    /// Obsolete class which provides NAnt functions to convert data types.
    /// </summary>
    [FunctionSet("convert", "Conversion")]
    public class ConversionFunctions : FunctionSetBase {

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversionFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="propDict">The projects property dictionary.</param>
        public ConversionFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {}

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the argument to an integer.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns><paramref name="value" /> converted to integer. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-int")]
        [Obsolete("Use type-specific conversion functions instead.", false)]
        public static int ToInt(int value) {
            return value; // conversion is done at the invocation level
        }

        /// <summary>
        /// Converts the argument to double
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns><paramref name="value" /> converted to double. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-double")]
        [Obsolete("Use type-specific conversion functions instead.", false)]
        public static double ToDouble(double value) {
            return value; // conversion is done at the invocation level
        }

        /// <summary>
        /// Converts the argument to a string.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns>
        /// <paramref name="value" /> converted to string. The function fails 
        /// with an exception when the conversion is not possible.
        /// </returns>
        /// <remarks>
        /// Named method ConvertToString as a static ToString method would break
        /// CLS compliance.
        /// </remarks>
        [Function("to-string")]
        [Obsolete("Use type-specific conversion functions instead.", false)]
        public static string ConvertToString(string value) {
            return value; // conversion is done at the invocation level
        }

        /// <summary>
        /// Converts the argument to a datetime.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns><paramref name="value" /> converted to datetime. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-datetime")]
        [Obsolete("Use type-specific conversion functions instead.", false)]
        public static DateTime ToDateTime(DateTime value) {
            return value; // conversion is done at the invocation level
        }

        /// <summary>
        /// Converts the argument to a boolean 
        /// </summary>
        /// <param name="value">The string value to be converted to boolean. Must be 'true' or 'false'.</param>
        /// <returns>
        /// <paramref name="value" /> converted to boolean. The function fails 
        /// with an exception when the conversion is not possible.
        /// </returns>
        [Function("to-boolean")]
        [Obsolete("Use type-specific conversion functions instead.", false)]
        public static bool ToBoolean(bool value) {
            return value; // conversion is done at the invocation level
        }

        #endregion Public Static Methods
    }
}
