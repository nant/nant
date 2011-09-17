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
    /// Marks a command-line option as being the default option.  When the name of 
    /// a command-line argument is not specified, this option will be assumed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DefaultCommandLineArgumentAttribute : CommandLineArgumentAttribute {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentAttribute" /> class
        /// with the specified argument type.
        /// </summary>
        /// <param name="argumentType">Specifies the checking to be done on the argument.</param>
        public DefaultCommandLineArgumentAttribute(CommandLineArgumentTypes argumentType) : base(argumentType) {
        }
    }
}
