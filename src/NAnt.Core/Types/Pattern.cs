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

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Pattern which is used by a <see cref="PatternSet"/> to include or exclude specific files.
    /// </summary>
    public class Pattern : Element, IConditional {
        #region Private Instance Fields

        private string _patternName;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Pattern" /> class.
        /// </summary>
        public Pattern() {
        }

        #endregion Public Instance Constructors

        #region Internal Instance Constructors

        internal Pattern(Project project, string patternName) {
            PatternName = patternName;
            Project = project;
        }

        #endregion Internal Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name pattern to include/exclude.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public virtual string PatternName {
            get { return _patternName; }
            set { _patternName = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the pattern will be used; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if", Required=false)]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// If <see langword="false" /> then the pattern will be used;
        /// otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless", Required=false)]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        internal bool Enabled {
            get { return IfDefined && !UnlessDefined; }
        }

        #endregion Internal Instance Properties
    }
}
