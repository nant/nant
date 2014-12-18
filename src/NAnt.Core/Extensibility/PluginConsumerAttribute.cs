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
