using System;
using System.Reflection;

namespace NAnt.Core.Extensibility {
    public abstract class ExtensionBuilder {
        /// <summary>
        /// Initializes a instance of the <see cref="ExtensionBuilder" /> 
        /// class for an extension in a given <see cref="ExtensionAssembly" />.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> in which the extension is found.</param>
        protected ExtensionBuilder(ExtensionAssembly extensionAssembly) {
            _extensionAssembly = extensionAssembly;
        }

        /// <summary>
        /// Gets the <see cref="ExtensionAssembly" /> in which the extension
        /// was found.
        /// </summary>
        public ExtensionAssembly ExtensionAssembly {
            get { return _extensionAssembly; }
        }

        /// <summary>
        /// Gets the <see cref="Assembly" /> from which the extension will 
        /// be created.
        /// </summary>
        /// <value>
        /// The <see cref="Assembly" /> containing the extension.
        /// </value>
        protected internal Assembly Assembly {
            get { return ExtensionAssembly.Assembly; }
        }

        private readonly ExtensionAssembly _extensionAssembly;
    }
}
