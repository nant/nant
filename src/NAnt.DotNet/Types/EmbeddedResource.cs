// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Represents an embedded resource.
    /// </summary>
    /// <remarks>
    /// Do not yet expose this to build authors.
    /// </remarks>
    public class EmbeddedResource {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResource" />
        /// with the specified file name and manifest resource name.
        /// </summary>
        /// <param name="file">The path of the compiled resource.</param>
        /// <param name="manifestResourceName">The manifest resource name of the embedded resource.</param>
        public EmbeddedResource(string file, string manifestResourceName) {
            _file = file;
            _manifestResourceName = manifestResourceName;
        }

        /// <summary>
        /// Gets the physical location of the resource to embed.
        /// </summary>
        /// <value>
        /// The physical location of the resource to embed.
        /// </value>
        public string File {
            get { return _file; }
        }

        /// <summary>
        /// Gets the manifest resource name to use when embedding the resource.
        /// </summary>
        /// <value>
        /// The manifest resource name to use when embedding the resource.
        /// </value>
        public string ManifestResourceName {
            get { return _manifestResourceName; }
        }

        private readonly string _file;
        private readonly string _manifestResourceName;
    }
}
