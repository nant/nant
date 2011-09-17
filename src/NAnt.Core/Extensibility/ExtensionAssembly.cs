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
using System.IO;
using System.Reflection;
using System.Xml;

namespace NAnt.Core.Extensibility {
    /// <summary>
    /// Represents an <see cref="Assembly" /> in which one or more extensions
    /// are found.
    /// </summary>
    internal class ExtensionAssembly {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionAssembly" />
        /// class for a given <see cref="Assembly" />.
        /// </summary>
        /// <remarks>
        /// The <see cref="ExtensionAssembly" /> instance is not cached for
        /// future use. If this is required, use <see cref="Create(Assembly)" />.
        /// </remarks>
        /// <param name="assembly">The <see cref="Assembly" /> for which to construct an <see cref="ExtensionAssembly" />.</param>
        internal ExtensionAssembly(Assembly assembly) {
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
                if (_configurationInit)
                    return _configurationSection;

                try {
                    Stream s = _assembly.GetManifestResourceStream ("NAnt.Extension.config");
                    if (s != null) {
                        try {
                            XmlDocument doc = new XmlDocument ();
                            doc.Load (s);
                            _configurationSection = doc.DocumentElement;
                        } finally {
                            s.Close ();
                        }
                    }
                    return _configurationSection;
                } finally {
                    _configurationInit = true;
                }
            }
        }

        /// <summary>
        /// Creates an  <see cref="ExtensionAssembly" /> for the specified
        /// <see cref="Assembly" /> and caches it for future use.
        /// </summary>
        /// <remarks>
        /// If an <see cref="ExtensionAssembly" /> for the same assembly is
        /// available in the cache, then this cached instance is returned.
        /// </remarks>
        /// <param name="assembly">The <see cref="Assembly" /> for which to construct an <see cref="ExtensionAssembly" />.</param>
        /// <returns>
        /// The <see cref="ExtensionAssembly" /> for the specified <see cref="Assembly" />.
        /// </returns>
        public static ExtensionAssembly Create (Assembly assembly) {
            if (assembly == null)
                throw new ArgumentNullException ("assembly");

            string aname = assembly.FullName;
            ExtensionAssembly ext = _extensionAssemblies [aname]
                as ExtensionAssembly;
            if (ext == null) {
                ext = new ExtensionAssembly (assembly);
                _extensionAssemblies [aname] = assembly;
            }
            return ext;
        }

        private static Hashtable _extensionAssemblies = new Hashtable ();

        private readonly Assembly _assembly;
        private XmlNode _configurationSection;
        private bool _configurationInit;
    }
}
