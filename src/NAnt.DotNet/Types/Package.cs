// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Represents a package.
    /// </summary>
    public class Package : Element, IConditional {
        #region Private Instance Fields

        private string _name;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of the package to reference. Multiple package can be specified
        /// with a single element as a semi-colon separated list of 
        /// package names.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string PackageName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Indicates if the package should be passed to the task. 
        /// If <see langword="true" /> then the package will be passed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the package should not be passed to the task.
        /// If <see langword="false" /> then the package will be passed; 
        /// otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
