// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Simon Keary (simonkeary@post.com)

namespace NAnt.VSNet.Types {
    /// <summary>
    /// Indicates the possible ways in which precompiled header file use is 
    /// specified in a Visual C++ project.
    /// </summary>
    /// <remarks>
    /// The integer values assigned match those specified in the Visual C++ 
    /// project file for each setting.
    /// </remarks>>
    public enum UsePrecompiledHeader {
        /// <summary>
        /// Precompiled header file use not specified.
        /// </summary>
        Unspecified = -1,

        /// <summary>
        /// Don't use a precompiled header file.
        /// </summary>
        /// <remarks>
        /// For further information on the use of this option
        /// see the Microsoft documentation on the C++ compiler flag /Yc.
        /// </remarks>
        No = 0,

        /// <summary>
        /// Create precompiled header file.
        /// </summary>
        /// <remarks>
        /// For further information on the use of this option
        /// see the Microsoft documentation on the C++ compiler flag /Yc.
        /// </remarks>
        Create = 1,

        /// <summary>
        /// Automatically create precompiled header file if necessary.
        /// </summary>
        /// <remarks>
        /// For further information on the use of this option
        /// see the Microsoft documentation on the C++ compiler flag /Yc.
        /// </remarks>
        AutoCreate = 2,

        /// <summary>
        /// Use a precompiled header file.
        /// </summary>
        /// <remarks>
        /// For further information on the use of this option
        /// see the Microsoft documentation on the C++ compiler flag /Yu.
        /// </remarks>
        Use = 3
    }
}
