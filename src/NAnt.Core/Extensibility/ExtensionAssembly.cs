using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace NAnt.Core.Extensibility {
    /// <summary>
    /// Represents an <see cref="Assembly" /> in which one or more extensions
    /// are found.
    /// </summary>
    public class ExtensionAssembly {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionAssembly" />
        /// class for a given <see cref="Assembly" />.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly" /> for which to construct an <see cref="ExtensionAssembly" />.</param>
        public ExtensionAssembly(Assembly assembly) {
            _assembly = assembly;
        }

        /// <summary>
        /// Gets the <see cref="Assembly" /> containing extensions.
        /// </summary>
        public Assembly Assembly {
            get { return _assembly; }
        }

        internal XmlNode ConfigurationSection {
            get {
                if (_configurationInit) {
                    return _configurationSection;
                }

                try {
                    Stream s = _assembly.GetManifestResourceStream ("NAnt.Extension.config");
                    if (s != null) {
                        XmlDocument doc = new XmlDocument ();
                        doc.Load (s);
                        _configurationSection = doc.DocumentElement;
                    }
                    return _configurationSection;
                } finally {
                    _configurationInit = true;
                }
            }
        }

        private readonly Assembly _assembly;
        private XmlNode _configurationSection;
        private bool _configurationInit;
    }
}
