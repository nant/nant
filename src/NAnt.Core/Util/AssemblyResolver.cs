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
using System.Collections;
using System.Reflection;

namespace NAnt.Core.Util {
    /// <summary> 
    /// Resolves assemblies by caching assemblies that were loaded.
    /// </summary>
    [Serializable()]
    public sealed class AssemblyResolver {
        #region Public Instance Constructors

        /// <summary> 
        /// Initializes an instanse of the <see cref="AssemblyResolver" /> 
        /// class.
        /// </summary>
        public AssemblyResolver() {
            _assemblyCache = new Hashtable();
        }

        /// <summary> 
        /// Initializes an instanse of the <see cref="AssemblyResolver" /> 
        /// class in the context of the given <see cref="Task" />.
        /// </summary>
        public AssemblyResolver(Task task) : this() {
            _task = task;
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

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

        #endregion Public Instance Methods

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
            bool isFullName = args.Name.IndexOf("Version=") != -1;

            // first try to find an already loaded assembly
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies) {
                if (isFullName) {
                    if (assembly.FullName == args.Name) {
                        // output debug message
                        Log(Level.Debug, "Resolved assembly '{0}' from" 
                            + " loaded assemblies using full name.", args.Name);
                        // return assembly from AppDomain
                        return assembly;
                    }
                } else if (assembly.GetName(false).Name == args.Name) {
                    // output debug message
                    Log(Level.Debug, "Resolved assembly '{0}' from" 
                        + " loaded assemblies using name.", args.Name);
                    // return assembly from AppDomain
                    return assembly;
                }
            }

            // find assembly in cache
            if (isFullName) {
                if (_assemblyCache.Contains(args.Name)) {
                    // output debug message
                    Log(Level.Debug, "Resolved assembly '{0}' from"
                        + " cache using full name.", args.Name);
                    // return assembly from cache
                    return (Assembly) _assemblyCache[args.Name];
                }
            } else {
                foreach (Assembly assembly in _assemblyCache.Values) {
                    if (assembly.GetName(false).Name == args.Name) {
                        // output debug message
                        Log(Level.Debug, "Resolved assembly '{0}'"
                            + " from cache using name.", args.Name);
                        // return assembly from cache
                        return assembly;
                    }
                }
            }

            // output debug message
            Log(Level.Debug, "Assembly '{0}' could not be located.", 
                args.Name);

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
            // output debug message
            Log(Level.Debug, "Added assembly '{0}' to assembly cache.", 
                args.LoadedAssembly.FullName);
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the <see cref="Task" /> in which 
        /// the <see cref="AssemblyResolver" /> is executing 
        /// </remarks>
        private void Log(Level messageLevel, string message, params object[] args) {
            if (_task != null) {
                _task.Log(messageLevel, message, args);
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        /// <summary>
        /// Holds the loaded assemblies.
        /// </summary>
        private Hashtable _assemblyCache;

        /// <summary>
        /// Holds the <see cref="Task" /> in which the <see cref="AssemblyResolver" /> 
        /// is executing.
        /// </summary>
        /// <value>
        /// The <see cref="Task" /> in which the <see cref="AssemblyResolver" /> 
        /// is executing or <see langword="null" /> if the <see cref="AssemblyResolver" />
        /// is not executing in the context of a <see cref="Task" />.
        /// </value>
        private Task _task;

        #endregion Private Instance Fields
    }
}
