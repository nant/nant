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
using System.Reflection;

namespace NAnt.Core.Extensibility {
    public abstract class ExtensionBuilder {
        /// <summary>
        /// Initializes a instance of the <see cref="ExtensionBuilder" /> 
        /// class for an extension in a given <see cref="ExtensionAssembly" />.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> in which the extension is found.</param>
        /// <exception cref="ArgumentNullException"><paramref name="extensionAssembly" /> is <see langword="null" />.</exception>
        internal ExtensionBuilder(ExtensionAssembly extensionAssembly) {
            if (extensionAssembly == null) {
                throw new ArgumentNullException("extensionAssembly");
            }
            _extensionAssembly = extensionAssembly;
        }

        /// <summary>
        /// Initializes a instance of the <see cref="ExtensionBuilder" /> 
        /// class for an extension in a given <see cref="Assembly" />.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly" /> in which the extension is found.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
        protected ExtensionBuilder(Assembly assembly)
            : this (ExtensionAssembly.Create (assembly)) {
        }

        /// <summary>
        /// Gets the <see cref="ExtensionAssembly" /> in which the extension
        /// was found.
        /// </summary>
        internal ExtensionAssembly ExtensionAssembly {
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
