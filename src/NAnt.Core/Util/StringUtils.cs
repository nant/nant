using System;

namespace NAnt.Core.Util {
    /// <summary>
    /// Groups a set of useful <see cref="string" /> manipulation and validation 
    /// methods.
    /// </summary>
    public sealed class StringUtils {
        #region Private Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringUtils" /> class.
        /// </summary>
        /// <remarks>
        /// Prevents instantiation of the <see cref="StringUtils" /> class.
        /// </remarks>
        private StringUtils() {
        }

        #endregion Private Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Indicates whether or not the specified <see cref="string" /> is 
        /// <see langword="null" /> or an <see cref="string.Empty" /> string.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is <see langword="null" />
        /// or an empty string (""); otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsNullOrEmpty(string value) {
            return (value == null || value.Trim().Length == 0);
        }

        /// <summary>
        /// Converts an empty string ("") to <b>null</b>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        /// <b>null</b> if <paramref name="value" /> is an empty string ("")
        /// or <b>null</b>; otherwise, <paramref name="value" />.
        /// </returns>
        public static string ConvertEmptyToNull(string value) {
            if (!IsNullOrEmpty(value)) {
                return value;
            }

            return null;
        }

        #endregion Public Static Methods
    }
}
