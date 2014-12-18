// NAnt - A .NET build tool
// Copyright (C) 2007 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class ManagedExecutionMode : Element {
        private RuntimeEngine _engine;
        private EnvironmentSet _environmentSet = new EnvironmentSet();

        [BuildElement("engine")]
        public RuntimeEngine Engine {
            get { return _engine; }
            set { _engine = value; }
        }

        /// <summary>
        /// Gets the collection of environment variables that should be passed
        /// to external programs that are launched.
        /// </summary>
        /// <value>
        /// <summary>
        /// The collection of environment variables that should be passed
        /// to external programs that are launched.
        /// </summary>
        /// </value>
        [BuildElement("environment")]
        public EnvironmentSet Environment {
            get { return _environmentSet; }
        }
    }
}
