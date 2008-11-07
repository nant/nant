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
// Gert Driesen (gert.driesen@cegeka.be)

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
