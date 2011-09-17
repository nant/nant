// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Represents a metadata file without assembly manifest.
    /// </summary>
    [Serializable]
    public class Module : Element {
        #region Public Instance Properties

        /// <summary>
        /// The path of the module.
        /// </summary>
        [TaskAttribute("file", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string File {
            get {
                if (ModuleSet != null && _file != null) {
                    return Path.Combine (ModuleSet.Dir.FullName, _file);
                }
                return _file;
            }
            set { _file = value; }
        }

        /// <summary>
        /// File name where the module should be copied to before it is compiled
        /// into an assembly.
        /// </summary>
        [TaskAttribute("target", Required=false)]
        public string Target {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="ModuleSet" /> that contains the module.
        /// </summary>
        public ModuleSet ModuleSet {
            get { return _moduleSet; }
            set { _moduleSet = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Object

        /// <summary>
        /// Returns a textual representation of the module, which can be used as
        /// argument for command-line tools.
        /// </summary>
        /// <returns>
        /// A textual representation of the path, file[,target].
        /// </returns>
        public override string ToString() {
            if (File == null) {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(File);
            if (Target != null) {
                sb.Append(",");
                sb.Append(Target);
            }
            return sb.ToString();
        }

        #endregion Override implementation of Object

        #region Private Instance Fields

        private string _file;
        private string _target;
        private ModuleSet _moduleSet;

        #endregion Private Instance Fields
    }
}
