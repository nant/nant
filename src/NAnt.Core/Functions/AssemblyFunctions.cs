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
    /// <summary>
    /// Functions to return information for a given assembly.
    /// </summary>
    [FunctionSet("assembly", "Assembly")]
    public class AssemblyFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public AssemblyFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {}

        #endregion Public Instance Constructors

        #region Public Static Methods
        
        /// <summary>
        /// Gets the version of the given assembly file.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to get version info for.</param>
        /// <returns>
        /// The full version of the given assembly file.
        /// </returns>
        [Function("get-version")]
        public static string GetVersion(string fileName) {
            return AssemblyName.GetAssemblyName(fileName).Version.ToString(); 
        }
        
        /// <summary>
        /// Gets the full name of the assembly, also known as the display name.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to get the full name for.</param>
        /// <returns>
        /// The full name of the assembly, also known as the display name.
        /// </returns>
        [Function("get-full-name")]
        public static string GetFullName(string fileName) {
            return AssemblyName.GetAssemblyName(fileName).FullName;
        }
        
        /// <summary>
        /// Gets the simple, unencrypted name of the assembly.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to get the name for.</param>
        /// <returns>
        /// The simple, unencrypted name of the assembly.
        /// </returns>
        [Function("get-name")]
        public static string GetName(string fileName) {
            return AssemblyName.GetAssemblyName(fileName).Name;
        }
        
        /// <summary>
        /// Gets the culture supported by the assembly.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to get the culture for.</param>
        /// <returns>
        /// Display name of the assembly's culture.
        /// </returns>
        [Function("get-culture")]
        public static string GetCulture(string fileName) {
            return AssemblyName.GetAssemblyName(fileName).CultureInfo.DisplayName;
        }

        #endregion Public Static Methods
    }
}
