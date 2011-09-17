// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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
using System.ComponentModel;
using System.Globalization;

namespace NAnt.Compression.Types {
    /// <summary>
    /// Specifies how entries with the same name should be processed.
    /// </summary>
    [TypeConverter(typeof(DuplicateHandlingConverter))]
    public enum DuplicateHandling {
        /// <summary>
        /// Overwrite existing entry with same name.
        /// </summary>
        Add = 0,

        /// <summary>
        /// Preserve existing entry with the same name.
        /// </summary>
        Preserve = 1,

        /// <summary>
        /// Report failure when two entries have the same name.
        /// </summary>
        Fail = 2
    }

    /// <summary>
    /// Specialized <see cref="EnumConverter" /> for <see cref="TarCompressionMethod" />
    /// that ignores case when converting from string.
    /// </summary>
    internal class DuplicateHandlingConverter : EnumConverter {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateHandlingConverter" />
        /// class.
        /// </summary>
        public DuplicateHandlingConverter() : base(typeof(DuplicateHandling)) {
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
