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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Used to indicate that a property should be able to be converted into a <see cref="bool" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class BooleanValidatorAttribute : ValidatorAttribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanValidatorAttribute" /> class.
        /// </summary>
        public BooleanValidatorAttribute() {
        }

        #endregion Public Instance Constructors

        #region Override implementation of ValidatorAttribute

        /// <summary>
        /// Checks if the specified value can be converted to a <see cref="bool" />.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns>
        /// <c>true</c> if the value can be converted to a <see cref="bool" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Validate(object value) {
            try {
                Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            } catch (Exception) {
                throw new ValidationException(String.Format(CultureInfo.InvariantCulture, "Cannot resolve to '{0}' to Boolean value.", value.ToString()));
            }
            return true;
        }

        #endregion Override implementation of ValidatorAttribute
    }
}