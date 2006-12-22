using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace NAnt.Core.Extensibility {
    internal class PluginScanner {
        private readonly ArrayList _plugins = new ArrayList();

        /// <summary>
        /// Scans a given <see cref="Type" /> for data type.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// data type; otherwise, <see langword="false" />.
        /// </returns>
        public bool ScanTypeForPlugins(Type type, Task task) {
            if (type.IsAbstract)
                return false;
            try {
                bool isplugin = typeof(IPlugin).IsAssignableFrom(type);
                if (!isplugin) {
                    return false;
                }

                _plugins.Add(type);
                return true;
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for plugins.",
                    type.AssemblyQualifiedName);
                throw;
            }
        }

        public void RegisterPlugins (IPluginConsumer consumer) {
            if (consumer == null) {
                throw new ArgumentNullException ("consumer");
            }

            object[] consumes = consumer.GetType().GetCustomAttributes(
                typeof(PluginConsumerAttribute), false);
            if (consumes.Length == 0) {
                return;
            }

            foreach (Type type in _plugins) {
                foreach (PluginConsumerAttribute c in consumes) {
                    if (c.PluginType.IsAssignableFrom (type)) {
                        consumer.ConsumePlugin(type);
                        break;
                    }
                }
            }
        }
    }
}
