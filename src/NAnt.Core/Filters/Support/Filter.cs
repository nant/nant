// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

using NAnt.Core.Attributes;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Base class for filters.
    /// </summary>
    /// <remarks>
    /// Base class for filters. All NAnt filters must be derived from this class. Filter provides
    /// support for parameters and provides a reference to the project. Filter's base class
    /// ChainableReader allows filters to be chained together.
    /// </remarks>
    public abstract class Filter : ChainableReader {
        #region Private Instance Fields

        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// If <see langword="true" /> then the filter will be used; otherwise, 
        /// skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of <see cref="IfDefined" />. If <see langword="false" /> 
        /// then the filter will be executed; otherwise, skipped. The default 
        /// is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Called after construction and after properties are set. Allows
        /// for filter initialization.
        /// </summary>
        public virtual void InitializeFilter() {
        }

        #endregion Public Instance Methods
    }
}

