using System;

namespace NAnt.Core.Extensibility {
    public class PluginBuilder : ExtensionBuilder {
        public PluginBuilder(ExtensionAssembly extensionAssembly, Type pluginType) : base (extensionAssembly) {
            _pluginType = pluginType;
        }

        public Type PluginType {
            get { return _pluginType; }
        }

        public IPlugin CreatePlugin() {
            return (IPlugin) Activator.CreateInstance(PluginType);
        }

        private readonly Type _pluginType;
    }
}
