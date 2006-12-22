using System;

using NAnt.Core.Attributes;

namespace NAnt.Core.Extensibility {
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=true)]
    public sealed class PluginConsumerAttribute : Attribute {
        private Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConsumerAttribute" /> 
        /// with the specified type.
        /// </summary>
        /// <param name="type">The type of the <see cref="IPlugin" /> to consume.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        public PluginConsumerAttribute(Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }
            _type = type;
        }

        public Type PluginType {
            get { return _type; }
        }
    }
}
