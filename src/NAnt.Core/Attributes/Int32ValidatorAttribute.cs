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

namespace SourceForge.NAnt.Attributes {

    using System;
    using System.Reflection;
    using System.Globalization;

    /// <summary>Indicates that field should be able to be converted into a Int32 within the given range.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited=true)]
    public sealed class Int32ValidatorAttribute : ValidatorAttribute {
        #region Private Instance Fields

        int _minValue = Int32.MinValue;
        int _maxValue = Int32.MaxValue;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Int32ValidatorAttribute" /> class.
        /// </summary>
        public Int32ValidatorAttribute() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Int32ValidatorAttribute" /> class
        /// with the specied minimum and maximum values.
        /// </summary>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        public Int32ValidatorAttribute(int minValue, int maxValue) {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        /// <remarks>
        /// The default value is <see cref="Int32.MinValue" />.
        /// </remarks>
        public int MinValue {
            get { return _minValue; }
            set { _minValue = value; }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        /// <remarks>
        /// The default value is <see cref="Int32.MaxValue" />.
        /// </remarks>
        public int MaxValue {
            get { return _maxValue; }
            set { _maxValue = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ValidatorAttribute

        /// <summary>
        /// Checks whether the specified value can be converted to an <see cref="Int32" /> and 
        /// whether the value lies within the range defined by the <see cref="MinValue" /> and
        /// <see cref="MaxValue" /> properties.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns>
        /// <c>true</c> if the value can be converted to an <see cref="Int32" /> and is in the 
        /// range defined by the <see cref="MinValue" /> and <see cref="MaxValue" /> properties;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Validate(object value) {
            try {
                Int32 intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (intValue < MinValue || intValue > MaxValue) {
                    throw new ValidationException(String.Format(CultureInfo.InvariantCulture, "Cannot resolve '{0}' to integer between '{1}' and '{2}'.", value.ToString(), MinValue, MaxValue));
                }
            } catch (Exception) {
                throw new ValidationException(String.Format(CultureInfo.InvariantCulture, "Cannot resolve '{0}' to integer value.", value.ToString()));
            }
            return true;
        }

        #endregion Override implementation of ValidatorAttribute
    }
}