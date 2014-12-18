// NAnt - A .NET build tool
// Copyright (C) 2001-2008 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net.be)

using System;
using System.Collections;

namespace NAnt.Core.Extensibility {
    /// <summary>
    /// Responsible for scanning types for plugins, and maintaining a cache of
    /// <see cref="PluginBuilder" /> instances.
    /// </summary>
    internal class PluginScanner {
        private readonly ArrayList _pluginBuilders = new ArrayList();

        /// <summary>
        /// Scans a given <see cref="Type" /> for plugins.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="Type" /> to scan.</param>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// <see cref="IPlugin" />; otherwise, <see langword="false" />.
        /// </returns>
        public bool ScanTypeForPlugins(ExtensionAssembly extensionAssembly, Type type, Task task) {
            if (type.IsAbstract)
                return false;
            try {
                bool isplugin = typeof(IPlugin).IsAssignableFrom(type);
                if (!isplugin) {
                    return false;
                }

                PluginBuilder pb = new PluginBuilder(extensionAssembly, type);
                _pluginBuilders.Add(pb);
                return true;
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for plugins.",
                    type.AssemblyQualifiedName);
                throw;
            }
        }

        /// <summary>
        /// Registers matching plugins for the specified <see cref="IPluginConsumer" />.
        /// </summary>
        /// <param name="consumer">The <see cref="IPluginConsumer" /> which plugins must be registered for.</param>
        /// <exception cref="ArgumentNullException"><paramref name="consumer" /> is <see langword="null" />.</exception>
        public void RegisterPlugins (IPluginConsumer consumer) {
            if (consumer == null) {
                throw new ArgumentNullException ("consumer");
            }

            object[] consumes = consumer.GetType().GetCustomAttributes(
                typeof(PluginConsumerAttribute), false);
            if (consumes.Length == 0) {
                return;
            }

            foreach (PluginBuilder pb in _pluginBuilders) {
                foreach (PluginConsumerAttribute c in consumes) {
                    if (c.PluginType.IsAssignableFrom (pb.PluginType)) {
                        consumer.ConsumePlugin(pb.CreatePlugin());
                        break;
                    }
                }
            }
        }
    }
}
