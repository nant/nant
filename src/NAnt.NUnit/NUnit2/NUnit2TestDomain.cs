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
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;

using NUnit.Core;

namespace NAnt.NUnit2.Tasks {
    /// <summary>
    /// Custom TestDomain, similar to the one included with NUnit, in order 
    /// to workaround some limitations in it.
    /// </summary>
    internal class NUnit2TestDomain {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnit2TestDomain" />
        /// class.
        /// </summary>
        public NUnit2TestDomain() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Runs a single testcase.
        /// </summary>
        /// <param name="assemblyFile">The test assembly.</param>
        /// <param name="configFile">The application configuration file for the test domain.</param>
        /// <returns>
        /// The result of the test.
        /// </returns>
        public TestRunner CreateRunner(FileInfo assemblyFile, FileInfo configFile) {
            // create test domain
            _domain = CreateDomain(assemblyFile.Directory, assemblyFile, 
                configFile);

            // assemble directories which can be probed for missing unresolved 
            // assembly references
            StringCollection probePaths = new StringCollection();
            
            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath != null) {
                foreach (string path in AppDomain.CurrentDomain.SetupInformation.PrivateBinPath.Split(';')) {
                    // path is relative to base directory of AppDomain, so 
                    // resolve to full path
                    probePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
                }
            }

            // add base directory of current AppDomain as probe path
            probePaths.Add(AppDomain.CurrentDomain.BaseDirectory);

            // create an instance of our custom Assembly Resolver in the target domain.
            _domain.CreateInstanceFrom(Assembly.GetExecutingAssembly().CodeBase, 
                    typeof(AssemblyResolveHandler).FullName,
                    false, 
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new object[] {probePaths},
                    CultureInfo.InvariantCulture,
                    null,
                    AppDomain.CurrentDomain.Evidence);     
            
            // create testrunner
            return CreateTestRunner(_domain);
        }

        public void Unload() {
            if (_domain != null) {
                try {
                    AppDomain.Unload(_domain);
                } catch (CannotUnloadAppDomainException) {
                    // ignore exceptions during unload, this matches the 
                    // behaviour of the NUnit TestDomain
                } finally {
                    _domain = null;
                }
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private AppDomain CreateDomain(DirectoryInfo basedir, FileInfo assemblyFile, FileInfo configFile) {
            // spawn new domain in specified directory
            AppDomainSetup domSetup = new AppDomainSetup();
            domSetup.ApplicationBase = basedir.FullName;

            // use explicitly specified configuration file, or fall back to
            // configuration file for given assembly file
            string configurationFile = null;
            if (configFile != null) {
                configurationFile = configFile.FullName;                    
            } else {
                configurationFile = assemblyFile.FullName + ".config";
            }

            // only set configuration file if it actually exists
            if (File.Exists(configurationFile)) {
                domSetup.ConfigurationFile = configurationFile;
            }
                
            domSetup.ApplicationName = "NAnt NUnit Remote Domain";
         
            return AppDomain.CreateDomain( 
                domSetup.ApplicationName, 
                AppDomain.CurrentDomain.Evidence, 
                domSetup
                );
        }

        private RemoteTestRunner CreateTestRunner(AppDomain domain) {
            ObjectHandle oh;
            Type rtrType = typeof(RemoteTestRunner);

            oh = domain.CreateInstance(
                rtrType.Assembly.FullName,
                rtrType.FullName,
                false, 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                null,
                CultureInfo.InvariantCulture,
                null,
                AppDomain.CurrentDomain.Evidence);
            return (RemoteTestRunner) oh.Unwrap();
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private AppDomain _domain;

        #endregion Private Instance Fields

        /// <summary>
        /// Helper class called when an assembly resolve event is raised.
        /// </summary>
        [Serializable()]
        private class AssemblyResolveHandler {
            #region Public Instance Constructors

            /// <summary> 
            /// Initializes an instanse of the <see cref="AssemblyResolveHandler" /> 
            /// class.
            /// </summary>
            public AssemblyResolveHandler(StringCollection probePaths) {
                _assemblyCache = new Hashtable();
                _probePaths = probePaths;
            
                // attach handlers for the current domain.
                AppDomain.CurrentDomain.AssemblyResolve += 
                    new ResolveEventHandler(ResolveAssembly);
                AppDomain.CurrentDomain.AssemblyLoad += 
                    new AssemblyLoadEventHandler(AssemblyLoad);

            }

            #endregion Public Instance Constructors

            #region Public Instance Methods

            /// <summary>
            /// Called back when the CLR cannot resolve a given assembly.
            /// </summary>
            /// <param name="sender">The source of the event.</param>
            /// <param name="args">A <see cref="ResolveEventArgs" /> that contains the event data.</param>
            /// <returns>
            /// The <c>nunit.framework</c> we know to be in NAnts bin directory, if 
            /// that is the assembly that needs to be resolved; otherwise, 
            /// <see langword="null" />.
            /// </returns>
            public Assembly ResolveAssembly(Object sender, ResolveEventArgs args) {
                bool isFullName = args.Name.IndexOf("Version=") != -1;

                // find assembly in cache
                if (isFullName) {
                    if (_assemblyCache.Contains(args.Name)) {
                        // return assembly from cache
                        return (Assembly) _assemblyCache[args.Name];
                    }
                } else {
                    foreach (Assembly assembly in _assemblyCache.Values) {
                        if (assembly.GetName(false).Name == args.Name) {
                            // return assembly from cache
                            return assembly;
                        }
                    }
                }

                // find assembly in probe paths
                foreach (string path in _probePaths) {
                    if (!Directory.Exists(path)) {
                        continue;
                    }

                    string[] assemblies = Directory.GetFiles(path, "*.dll");

                    foreach (string assemblyFile in assemblies) {
                        try {
                            AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                            if (isFullName) {
                                if (assemblyName.FullName == args.Name) {
                                    return Assembly.LoadFrom(assemblyFile);
                                }
                            } else {
                                if (assemblyName.Name == args.Name) {
                                    return Assembly.LoadFrom(assemblyFile);
                                }
                            }
                        } catch {}
                    }
                }

                // assembly reference could not be resolved
                return null;
            }

            /// <summary>
            /// Occurs when an assembly is loaded. The loaded assembly is added 
            /// to the assembly cache.
            /// </summary>
            /// <param name="sender">The source of the event.</param>
            /// <param name="args">An <see cref="AssemblyLoadEventArgs" /> that contains the event data.</param>
            private void AssemblyLoad(object sender, AssemblyLoadEventArgs args) {
                // store assembly in cache
                _assemblyCache[args.LoadedAssembly.FullName] = args.LoadedAssembly;
            }

            #endregion Public Instance Methods

            #region Private Instance Fields

            /// <summary>
            /// Holds the list of directories that will be scanned for missing
            /// assembly references.
            /// </summary>
            private StringCollection _probePaths;

            /// <summary>
            /// Holds the loaded assemblies.
            /// </summary>
            private Hashtable _assemblyCache;

            #endregion Private Instance Fields
        }
    }
}
