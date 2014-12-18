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
using System.Reflection;

namespace NAnt.Core.Util {
    /// <summary>
    /// Provides a set of helper methods related to reflection.
    /// </summary>
    public sealed class ReflectionUtils {
        #region Private Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionUtils" /> class.
        /// </summary>
        /// <remarks>
        /// Uses a private access modifier to prevent instantiation of this class.
        /// </remarks>
        private ReflectionUtils() {
        }

        #endregion Private Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Loads the type specified in the type string with assembly qualified name.
        /// </summary>
        /// <param name="typeName">The assembly qualified name of the type to load.</param>
        /// <param name="throwOnError">Flag set to <see langword="true" /> to throw an exception if the type cannot be loaded.</param>
        /// <exception cref="TypeLoadException">
        ///     <paramref name="throwOnError" /> is <see langword="true" /> and 
        ///     an error is encountered while loading the <see cref="Type" />, or 
        ///     <paramref name="typeName" /> is not an assembly qualified name.
        /// </exception>
        /// <remarks>
        /// If the <see cref="Type" /> cannot be instantiated from the assembly
        /// qualified type name, then we'll try to instantiate the type using its
        /// simple type name from an already loaded assembly with an assembly 
        /// name mathing the assembly in the assembly qualified type name.
        /// </remarks>
        /// <returns>
        /// The type loaded or <see langword="null" /> if it could not be loaded.
        /// </returns>
        public static Type GetTypeFromString(string typeName, bool throwOnError) {
            Type type = Type.GetType(typeName, throwOnError);
            if (type == null) {
                // if the type name specifies the assembly name, try to instantiate
                // type from currently loaded assemblies
                if (typeName.IndexOf(',') != -1) {
                    int startAssembly = typeName.IndexOf(',');
                    int startVersion = typeName.IndexOf(',', startAssembly + 1);

                    // get type from assembly qualified type name
                    string simpleTypeName = typeName.Substring(
                        0, startAssembly);

                    string assemblyName = null;
                    if (startVersion != -1) {
                        assemblyName = typeName.Substring(startAssembly + 1, 
                            startVersion - startAssembly - 1).Trim();
                    } else {
                        assemblyName = typeName.Substring(startAssembly + 1).Trim();
                    }

                    // try to instantiate type from currently loaded assemblies
                    type = Type.GetType(simpleTypeName + ", " + assemblyName, false);
                } else {
                    // iterate over currently loaded assemblies and try to 
                    // instantiate type from one of them
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies) {
                        type = assembly.GetType(typeName, false);
                        if (type != null) {
                            break;
                        }
                    }
                }
            }

            return type;
        }

        #endregion Public Static Methods
    }
}
