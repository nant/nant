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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// A specialized <see cref="FileSet" /> used for specifying a set of 
    /// directories.
    /// </summary>
    /// <remarks>
    /// Hint for supporting tasks that the included directories instead of 
    /// files should be used.
    /// </remarks>
    [ElementName("dirset")]
    public class DirSet : FileSet, ICloneable {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DirSet" /> class.
        /// </summary>
        public DirSet() : base() {
        }

        /// <summary>
        /// Copy constructor for <see cref="FileSet" />. Required in order to 
        /// assign references of <see cref="FileSet" /> type where 
        /// <see cref="DirSet" /> is used.
        /// </summary>
        /// <param name="fs">A <see cref="FileSet" /> instance to create a <see cref="DirSet" /> from.</param>
        public DirSet(FileSet fs) : base(fs) {
        }

        #endregion Public Instance Constructors
   }
}