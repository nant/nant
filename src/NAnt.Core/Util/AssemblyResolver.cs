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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace NAnt.Core.Util {
    /// <summary> 
    /// Resolves assemblies by caching assembly that were loaded.
    /// </summary>
    public class AssemblyResolver {
        #region Public Instance Constructors

        /// <summary> 
        /// Initializes an instanse of the <see cref="AssemblyResolver" /> 
        /// class.
        /// </summary>
        public AssemblyResolver() {
            this._assemblyCache = new Hashtable();
        }

        #endregion

        #region Public Methods and Properties

        /// <summary> 
        /// Installs the assembly resolver by hooking up to the 
        /// <see cref="AppDomain.AssemblyResolve" /> event.
        /// </summary>
        public void Attach() {
            AppDomain.CurrentDomain.AssemblyResolve +=
                new ResolveEventHandler(AssemblyResolve);

            AppDomain.CurrentDomain.AssemblyLoad += 
                new AssemblyLoadEventHandler(AssemblyLoad);
        }

        /// <summary> 
        /// Uninstalls the assembly resolver.
        /// </summary>
        public void Detach() {
            AppDomain.CurrentDomain.AssemblyResolve -=
                new ResolveEventHandler(this.AssemblyResolve);

            AppDomain.CurrentDomain.AssemblyLoad -= 
                new AssemblyLoadEventHandler(AssemblyLoad);

            this._assemblyCache.Clear();
        }

        #endregion

        #region Private Instance Methods

        /// <summary> 
        /// Resolves an assembly not found by the system using the assembly 
        /// cache.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">A <see cref="ResolveEventArgs" /> that contains the event data.</param>
        /// <returns>
        /// The loaded assembly, or <see langword="null" /> if not found.
        /// </returns>
        private Assembly AssemblyResolve(object sender, ResolveEventArgs args) {
            // first try to find an already loaded assembly
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies) {
                if (assembly.FullName == args.Name) {
                    return assembly;
                }
            }

            // find assembly in cache
            if (_assemblyCache.Contains(args.Name)) {
                return (Assembly) _assemblyCache[args.Name];
            }

            return null;
        }

        /// <summary>
        /// Occurs when an assembly is loaded. The loaded assembly is added 
        /// to the assembly cache.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">An <see cref="AssemblyLoadEventArgs" /> that contains the event data.</param>
        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args) {
            _assemblyCache[args.LoadedAssembly.FullName] = args.LoadedAssembly;
        }

        #endregion Protected Instance Methods

        #region Private Instance Fields

        /// <summary>
        /// Holds the loaded assemblies.
        /// </summary>
        private Hashtable _assemblyCache;

        #endregion Private Instance Fields
    }
}
