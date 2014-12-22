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

using System.IO;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Represents a physical <see cref="TextReader" />.  That is a reader based 
    /// on a stream.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="ChainableReader"/> to represent a <see cref="Filter" />
    /// based on a <see cref="TextReader" /> in the chain.
    /// </remarks>
    internal class PhysicalTextReader : Filter {
        public PhysicalTextReader(TextReader textReader) {
            base.Chain(textReader);
        }
    }
}
