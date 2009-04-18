using System;
using System.ComponentModel;
using System.Globalization;

using NAnt.Compression.Tasks;

namespace NAnt.Compression.Types {
    /// <summary>
    /// Specifies the compression methods supported by <see cref="TarTask" />
    /// and <see cref="UnTarTask" />.
    /// </summary>
    [TypeConverter(typeof(TarCompressionMethodConverter))]
    public enum TarCompressionMethod {
        /// <summary>
        /// No compression.
        /// </summary>
        None = 0,

        /// <summary>
        /// GZIP compression.
        /// </summary>
        GZip = 1,

        /// <summary>
        /// BZIP2 compression.
        /// </summary>
        BZip2 = 2
    }

    /// <summary>
    /// Specialized <see cref="EnumConverter" /> for <see cref="TarCompressionMethod" />
    /// that ignores case when converting from string.
    /// </summary>
    internal class TarCompressionMethodConverter : EnumConverter {
        /// <summary>
        /// Initializes a new instance of the <see cref="TarCompressionMethodConverter" />
        /// class.
        /// </summary>
        public TarCompressionMethodConverter() : base(typeof(TarCompressionMethod)) {
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
            string stringValue = value as string;
            if (stringValue != null)
                return Enum.Parse(EnumType, stringValue, true);

            // default to EnumConverter behavior
            return base.ConvertFrom(context, culture, value);
        }
    }
}
