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
// Matt Mastracci <mmastrac@canada.com>
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;

using System.Security;
using System.Security.Permissions;

namespace NAnt.Core.Util {
    /// <summary>
    /// Helper class for determining whether assemblies are located in the 
    /// Global Assembly Cache.
    /// </summary>
    public sealed class GacCache : IDisposable {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GacCache"/> class in 
        /// the context of the given <see cref="Project" />.
        /// </summary>
        public GacCache(Project project) {
            _project = project;
            _gacQueryCache = CollectionsUtil.CreateCaseInsensitiveHashtable();
            RecreateDomain();
        }

        #endregion Public Instance Constructors

        #region Public Instance Destructors

        ~GacCache() {
            Dispose(false);
        }

        #endregion Public Instance Destructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the <see cref="Project" /> context of the <see cref="GacCache" />.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> context of the <see cref="GacCache" />.
        /// </value>
        public Project Project {
            get { return _project; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private AppDomain Domain {
            get { return _domain; }
        }

        private GacResolver Resolver {
            get {
                if (_resolver == null) {
                    _resolver = ((GacResolver) Domain.CreateInstanceFrom(
                        Assembly.GetExecutingAssembly().Location,
                        typeof(GacResolver).FullName).Unwrap());
                }
                return _resolver;
            }
        }

        #endregion Private Instance Properties

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing) {
            if (!_disposed) {
                AppDomain.Unload(_domain);
                _disposed = true;
            }
        }

        #endregion Implementation of IDisposable

        #region Public Instance Methods

        public void RecreateDomain() {
            // don't recreate this domain unless it has actually loaded an assembly
            if (!_hasLoadedAssembly && _domain != null)
                return;

            if (_domain != null)
                AppDomain.Unload(_domain);

            _resolver = null;

            PermissionSet domainPermSet = new PermissionSet(PermissionState.Unrestricted);
            _domain = AppDomain.CreateDomain("GacCacheDomain", AppDomain.CurrentDomain.Evidence, 
                AppDomain.CurrentDomain.SetupInformation, domainPermSet);

            _hasLoadedAssembly = false;
        }

        /// <summary>
        /// Determines whether an assembly is installed in the Global
        /// Assembly Cache given its file name or path.
        /// </summary>
        /// <param name="assemblyFile">The name or path of the file that contains the manifest of the assembly.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="assemblyFile" /> is 
        /// installed in the Global Assembly Cache; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// To determine whether the specified assembly is installed in the 
        /// Global Assembly Cache, the assembly is loaded into a separate
        /// <see cref="AppDomain" />.
        /// </para>
        /// <para>
        /// If the family of the current runtime framework does not match the
        /// family of the current target framework, this method will return
        /// <see langword="false" /> for all assemblies as there's no way to
        /// determine whether a given assembly is in the Global Assembly Cache
        /// for another framework family than the family of the current runtime
        /// framework.
        /// </para>
        /// </remarks>
        public bool IsAssemblyInGac(string assemblyFile) {
            if (Project.RuntimeFramework.Family != Project.TargetFramework.Family) {
                return false;
            }

            string assemblyFilePath = Path.GetFullPath(assemblyFile);
            if (_gacQueryCache.Contains(assemblyFilePath)) {
                return (bool) _gacQueryCache[assemblyFilePath];
            }

            _hasLoadedAssembly = true;
            _gacQueryCache[assemblyFilePath] = Resolver.IsAssemblyInGac(assemblyFilePath);
            return (bool) _gacQueryCache[assemblyFilePath];
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        /// <summary>
        /// Holds the <see cref="AppDomain" /> in which assemblies will be loaded
        /// to determine whether they are in the Global Assembly Cache.
        /// </summary>
        private AppDomain _domain;

        /// <summary>
        /// Holds the <see cref="Project" /> context of the <see cref="GacCache" />.
        /// </summary>
        private Project _project;

        /// <summary>
        /// Holds a list of assembly files for which already has been determined 
        /// whether they are located in the Global Assembly Cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The key of the <see cref="Hashtable" /> is the full path to the 
        /// assembly file and the value is a <see cref="bool" /> indicating 
        /// whether the assembly is located in the Global Assembly Cache.
        /// </para>
        /// </remarks>
        private Hashtable _gacQueryCache;
        
        private bool _hasLoadedAssembly;

        private GacResolver _resolver;

        /// <summary>
        /// Holds a value indicating whether the object has been disposed.
        /// </summary>
        private bool _disposed;

        #endregion Private Instance Fields

        private class GacResolver : MarshalByRefObject {
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

            /// <summary>
            /// Determines whether an assembly is installed in the Global
            /// Assembly Cache given its file name or path.
            /// </summary>
            /// <param name="assemblyFile">The name or path of the file that contains the manifest of the assembly.</param>
            /// <returns>
            /// <see langword="true" /> if <paramref name="assemblyFile" /> is 
            /// installed in the Global Assembly Cache; otherwise, 
            /// <see langword="false" />.
            /// </returns>
            public bool IsAssemblyInGac(string assemblyFile) {
                try {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                    // the assembly can't be in the GAC if it has no public key
                    if (assemblyName.GetPublicKeyToken() == null) {
                        return false;
                    }

                    // load assembly
                    Assembly assembly = Assembly.Load(assemblyName);

                    // tests whether the specified assembly is loaded in the 
                    // global assembly cache
                    if (PlatformHelper.IsMono) {
                        // TODO: remove mono specific code when FromGlobalAccessCache
                        // is implemented
                        return assembly.GlobalAssemblyCache;
                    } else {
                        return RuntimeEnvironment.FromGlobalAccessCache(assembly);
                    }
                } catch {
                    return false;
                }
            }

            #endregion Public Instance Methods
        }
    }
}