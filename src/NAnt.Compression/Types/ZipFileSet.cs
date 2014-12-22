// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Compression.Tasks;

namespace NAnt.Compression.Types {
    /// <summary>
    /// A <see cref="ZipFileSet" /> is a <see cref="FileSet" /> with extra 
    /// attributes useful in the context of the <see cref="ZipTask" />.
    /// </summary>
    [ElementName("zipfileset")]
    public class ZipFileSet : FileSet {
        #region Private Instance Fields

        private string _prefix;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The top level directory prefix. If set, all file and directory paths 
        /// in the fileset will have this value prepended. Can either be a single 
        /// directory name or a "/" separated path.
        /// </summary>
        [TaskAttribute("prefix", Required=false)]
        public string Prefix {
            get { return _prefix; }
            set { 
                _prefix = StringUtils.ConvertEmptyToNull(value);
                if (_prefix != null && !_prefix.EndsWith("/") && !_prefix.EndsWith("\\")) {
                    _prefix += "/";
                }
            }
        }

        #endregion Public Instance Properties
    }
}
