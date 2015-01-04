// NAnt - A .NET build tool
// Copyright (C) 2001-2015 Gerry Shaw
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

namespace NAnt.Core
{
    /// <summary>
    /// Provides indicators to <see cref="NAnt.Core.Element"/> based classes that
    /// tells <see cref="NAnt.Core.Element"/> that conditional checks should be
    /// evaluated before processing.
    /// </summary>
    public interface IConditional
    {
        #region Properties

        /// <summary>
        /// Indicates whether or not the implementing class should execute.
        /// </summary>
        bool IfDefined { get; set; }

        /// <summary>
        /// Indicates whether or not the implementing class should NOT execute.
        /// </summary>
        bool UnlessDefined { get; set; }

        #endregion
    }
}
