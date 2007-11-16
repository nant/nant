using System;
using System.ComponentModel;
using System.Globalization;

namespace NAnt.Core.Types
{
    [TypeConverter(typeof(ManagedExecutionConverter))]
	public enum ManagedExecution
	{
        Default,
        Auto,
        Strict
	}

    /// <summary>
    /// Specialized <see cref="EnumConverter" /> that also supports 
    /// case-insensitive conversion of &quot;true&quot; to 
    /// <see cref="ManagedExecution.Auto" /> and &quot;false&quot; to
    /// <see cref="ManagedExecution.Default" />.
    /// </summary>
    public class ManagedExecutionConverter : EnumConverter {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedExecutionConverter" />
        /// class.
        /// </summary>
        public ManagedExecutionConverter() : base(typeof(ManagedExecution)) {
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the 
        /// specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object. If a <see langword="null"/> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="Object"/> to convert.</param>
        /// <returns>
        /// An <see cref="Object"/> that represents the converted value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                string stringValue = (string) value;
                if (string.Compare(stringValue, Boolean.TrueString, true, culture) == 0) {
                    return ManagedExecution.Auto;
                }
                if (string.Compare(stringValue, Boolean.FalseString, true, culture) == 0) {
                    return ManagedExecution.Default;
                }

                return Enum.Parse(typeof(ManagedExecution), stringValue, true);
            }

            // default to EnumConverter behavior
            return base.ConvertFrom(context, culture, value);
        }
    }
}
