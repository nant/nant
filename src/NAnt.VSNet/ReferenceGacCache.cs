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
// Matt Mastracci <mmastrac@canada.com>

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;

using NAnt.Core;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET projects.
    /// </summary>
    public sealed class ReferenceGacCache : IDisposable {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceGacCache"/>
        /// class.
        /// </summary>
        public ReferenceGacCache() {
            _appDomain = AppDomain.CreateDomain("temporaryDomain");
            _gacResolver = 
                ((GacResolver) _appDomain.CreateInstanceFrom(Assembly.GetExecutingAssembly().Location,
                typeof(GacResolver).FullName).Unwrap());
            _gacQueryCache = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }
        #endregion Public Instance Constructors

        #region Public Instance Destructors

        ~ReferenceGacCache() {
            Dispose();
        }

        #endregion Public Instance Destructors

        #region Implementation of IDisposable

        public void Dispose() {
            if (_appDomain != null) {
                AppDomain.Unload(_appDomain);
                _appDomain = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion Implementation of IDisposable

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
        /// <remarks>
        /// To determine whether the specified assembly is installed in the 
        /// Global Assembly Cache, the assembly is loaded into a separate
        /// <see cref="AppDomain" />.
        /// </remarks>
        public bool IsAssemblyInGac(string assemblyFile) {
            string assemblyFilePath = Path.GetFullPath(assemblyFile);
            if (_gacQueryCache.Contains(assemblyFilePath)) {
                return (bool) _gacQueryCache[assemblyFilePath];
            }

            _gacQueryCache[assemblyFilePath] = _gacResolver.IsAssemblyInGac(assemblyFilePath);
            return (bool) _gacQueryCache[assemblyFilePath];
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private AppDomain _appDomain;

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

        private GacResolver _gacResolver;

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
                    Assembly assembly = Assembly.Load(assemblyName);
                    return assembly.GlobalAssemblyCache;
                } catch {
                    return false;
                }
            }

            #endregion Public Instance Methods
        }
    }
}