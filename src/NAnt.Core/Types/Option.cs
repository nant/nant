// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (drieseng@users.sourceforge.net)

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an option.
    /// </summary>
    [ElementName("option")]
    public class Option : Element, IConditional {
        #region Private Instance Fields

        private string _name;
        private string _value;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        /// <summary>
        /// name, value constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public Option(string name, string value) {
            _name = name;
            _value = value;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Option() {}

        #region Public Instance Properties

        /// <summary>
        /// Name of the option.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string OptionName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Value of the option. The default is <see langword="null" />.
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Indicates if the option should be passed to the task. 
        /// If <see langword="true" /> then the option will be passed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the option should not be passed to the task.
        /// If <see langword="false" /> then the option will be passed; 
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
