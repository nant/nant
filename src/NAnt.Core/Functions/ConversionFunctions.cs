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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("convert", "Conversion")]
    public class ConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public ConversionFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {}

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the argument to an integer.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns><paramref name="value" /> converted to integer. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-int")]
        public static int ToInt(object value) {
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Converts the argument to double
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns><paramref name="value" /> converted to double. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-double")]
        public static double ToDouble(object value) {
            return Convert.ToDouble(value);
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
        public static string ConvertToString(object value) {
            return Convert.ToString(value);
        }

        /// <summary>
        /// Converts the argument to a datetime.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns><paramref name="value" /> converted to datetime. The function fails with an exception when the conversion is not possible.</returns>
        [Function("to-datetime")]
        public static DateTime ToDateTime(object value) {
            return Convert.ToDateTime(value);
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
        public static bool ToBoolean(string value) {
            return Convert.ToBoolean(value);
        }

        #endregion Public Static Methods
    }
}
