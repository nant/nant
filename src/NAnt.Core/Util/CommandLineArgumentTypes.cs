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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;

namespace NAnt.Core.Util {
    /// <summary>
    /// Used to control parsing of command-line arguments.
    /// </summary>
    [Flags]    
    public enum CommandLineArgumentTypes {
        /// <summary>
        /// Indicates that this field is required. An error will be displayed
        /// if it is not present when parsing arguments.
        /// </summary>
        Required    = 0x01,

        /// <summary>
        /// Only valid in conjunction with Multiple.
        /// Duplicate values will result in an error.
        /// </summary>
        Unique      = 0x02,

        /// <summary>
        /// Inidicates that the argument may be specified more than once.
        /// Only valid if the argument is a collection
        /// </summary>
        Multiple    = 0x04,

        /// <summary>
        /// Inidicates that if this argument is specified, no other arguments may be specified.
        /// </summary>
        Exclusive    = 0x08,

        /// <summary>
        /// The default type for non-collection arguments.
        /// The argument is not required, but an error will be reported if it is specified more than once.
        /// </summary>
        AtMostOnce  = 0x00,
        
        /// <summary>
        /// The default type for collection arguments.
        /// The argument is permitted to occur multiple times, but duplicate 
        /// values will cause an error to be reported.
        /// </summary>
        MultipleUnique  = Multiple | Unique,
    }
}
