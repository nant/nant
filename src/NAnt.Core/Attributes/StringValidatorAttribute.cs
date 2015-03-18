// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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
using System.Text.RegularExpressions;

using NAnt.Core.Util;

namespace NAnt.Core.Attributes {
    /// <summary>
    /// Used to indicate whether a <see cref="string" /> property should allow 
    /// an empty string value or not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class StringValidatorAttribute : ValidatorAttribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringValidatorAttribute" /> 
        /// class.
        /// </summary>
        public StringValidatorAttribute() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets a value indicating whether an empty string or
        /// <see langword="null" /> should be a considered a valid value.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if an empty string or <see langword="null" />
        /// should be considered a valid value; otherwise, <see langword="false" />.
        /// The default is <see langword="true" />.
        /// </value>
        public bool AllowEmpty {
            get { return _allowEmpty; }
            set { _allowEmpty = value; }
        }

        /// <summary>
        /// Gets or sets a regular expression.  The string will be validated to
        ///     determine if it matches the expression.
        /// </summary>
        /// <value>
        /// <see cref="System.Text.RegularExpressions"/>
        /// </value>
        public string Expression {
            get { return _expression; }
            set { _expression = value; }
        }

        /// <summary>
        /// An optional error message that can be used to better describe the
        /// regular expression error.
        /// </summary>
        public string ExpressionErrorMessage {
            get { return this._expressionErrorMessage; }
            set { this._expressionErrorMessage = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ValidatorAttribute

        /// <summary>
        /// Checks if the specified value adheres to the rules defined by the 
        /// properties of the <see cref="StringValidatorAttribute" />.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <exception cref="ValidationException"><paramref name="value" /> is an empty string value and <see cref="AllowEmpty" /> is set to <see langword="false" />.</exception>
        public override void Validate(object value) {
            string valueString;

            try {
                valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            } catch (Exception ex) {
                throw new ValidationException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1092"), value.ToString()), ex);
            }

            if (String.IsNullOrEmpty(valueString)) {
                if (!AllowEmpty) {
                    throw new ValidationException("An empty value is not allowed.");
                }

                // if we allow empty value, then there's no need to validate
                // value against expression
                return;
            }

            if (!String.IsNullOrEmpty(Expression)) {
                if (!Regex.IsMatch(Convert.ToString(value), Expression)) {
                    string msg = string.Format("String {0} does not match expression {1}.",
                            value, Expression);
                    if (null != this.ExpressionErrorMessage && 
                        this.ExpressionErrorMessage.Length > 0) {
                        msg = this.ExpressionErrorMessage;
                    }
                    throw new ValidationException(msg);
                }
            }
        }

        #endregion Override implementation of ValidatorAttribute

        #region Private Instance Fields

        private bool _allowEmpty = true;
        private string _expression;
        private string _expressionErrorMessage;

        #endregion Private Instance Fields
    }
}