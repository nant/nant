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


using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Filters {

    /// <summary>
    /// <para>Allows users to specifies properties elements as attributes.</para>
    /// </summary>
    ///
    /// <remarks>See <see cref="ReplaceCharacter"/> for usage</remarks>
    ///
    [ElementName("replacecharacter")]
    public class ReplaceCharacterConvenience : FilterElement {
        /// <summary>
        /// Character to be replaced.
        /// </summary>
        [TaskAttribute("chartoreplace", Required = true)]
        public string CharToReplace {
            set { base.Parameters.Add("chartoreplace", value); }
        }

        /// <summary>
        /// Character replacecharacter will be replaced with.
        /// </summary>
        [TaskAttribute("replacementchar", Required = true)]
        public string ReplacementChar {
            set { base.Parameters.Add("replacementchar", value); }
        }

        /// <summary>
        /// Default constructor. Sets assembly and class name of the base.
        /// </summary>
        public ReplaceCharacterConvenience() : base() {
            base.AssemblyName = "NAnt.Core";
            base.ClassName = "NAnt.Core.Filters.ReplaceCharacter";
        }

    }
}
