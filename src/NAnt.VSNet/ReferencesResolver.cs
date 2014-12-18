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
// Matthew Mastracci (matt@aclaro.com)
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public class ReferencesResolver : MarshalByRefObject {
        #region Override implementation of MarshalByRefObject

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for 
        /// this instance.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="ILease" /> used to control the lifetime 
        /// policy for this instance. This is the current lifetime service object 
        /// for this instance if one exists; otherwise, a new lifetime service 
        /// object initialized with a lease that will never time out.
        /// </returns>
        public override Object InitializeLifetimeService() {
            ILease lease = (ILease) base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial) {
                lease.InitialLeaseTime = TimeSpan.Zero;
            }
            return lease;
        }

        #endregion Override implementation of MarshalByRefObject

        #region Public Instance Methods

        public void AppendReferencedModulesLocatedInGivenDirectory(string moduleDirectory, string moduleName, ref Hashtable allReferences, ref Hashtable unresolvedReferences) {
            Assembly module = null;

            try {
                module = Assembly.LoadFrom(moduleName);
            } catch (FileLoadException) {
                // for now ignore assemblies that cannot be loaded. A better
                // solution might be to disable signature verification and try
                // again, that way we can load assemblies that are delay-signed
                return;
            }

            AssemblyName[] referencedAssemblies = module.GetReferencedAssemblies();

            foreach (AssemblyName referencedAssemblyName in referencedAssemblies) {
                string fullPathToReferencedAssembly = FileUtils.CombinePaths(moduleDirectory, referencedAssemblyName.Name + ".dll");

                // we only add referenced assemblies which are located in given directory
                if (File.Exists(fullPathToReferencedAssembly) && !allReferences.ContainsKey(fullPathToReferencedAssembly)) {
                    allReferences.Add(fullPathToReferencedAssembly, null);
                    unresolvedReferences.Add(fullPathToReferencedAssembly, null);
                }
            }
        }

        /// <summary>
        /// Gets the file name of the assembly with the given assembly name.
        /// </summary>
        /// <param name="assemblyName">The assembly name of the assembly of which the file name should be returned.</param>
        /// <returns>
        /// The file name of the assembly with the given assembly name.
        /// </returns>
        public string GetAssemblyFileName(string assemblyName) {
            Assembly assembly = Assembly.Load(assemblyName);
            return (new Uri(assembly.CodeBase)).LocalPath;
        }

        #endregion Public Instance Methods
    }
}