// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("directory", "Directory")]
    public class DirectoryFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public DirectoryFunctions(Project project, PropertyDictionary propDict) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Determines whether the given path refers to an existing directory 
        /// on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="path" /> refers to an
        /// existing directory; otherwise, <see langword="false" />.
        /// </returns>
        [Function("exists")]
        public static bool Exists(string path) {
            return Directory.Exists(path);
        }

        #endregion Public Static Methods
    }
}
