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
using System;
using System.IO;
using NAnt.Core.Types;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Base class for filters.
    /// </summary>
    /// <remarks>
    /// Base class for filters. All NAnt filters must be derived form this class. Filter provides
    /// support for parameters and provides a reference to the project. Filter's base class
    /// ChainableReader allows filters to be chained together.
    ///
    /// A <see cref="FilterElement"/> is used to create a Filter.
    /// </remarks>
    public abstract class Filter : ChainableReader {
        #region Public Instance Properties

        /// <summary>
        /// Current project.  Set after construction but before Initialize() is called.
        /// </summary>
        public Project Project {
            get { return _project; }
            set { _project = value;}
        }
        private Project _project = null;


        /// <summary>
        /// Collection of parameters that belong to the filter.
        /// Set after construction but before Initialize() is called.
        /// </summary>
        public FilterElementParameterCollection Parameters {
            get { return _parameters; }
            set { _parameters = value; }
        }
        FilterElementParameterCollection _parameters = null;

        /// <summary>
        /// Used to get and set the location of the filter.
        /// This fill be the location of the element that
        /// represents this filter.
        /// </summary>
        public Location Location {
            get { return _location; }
            set { _location = value; }
        }
        Location _location = null;

        #endregion Instance Properties

        #region Public Instance Constructors

        /// <summary>
        /// See ChainableReader(ChainableReader chainedReader)
        /// </summary>
        public Filter(ChainableReader chainedReader) : base(chainedReader) {}

        /// <summary>
        /// See ChainableReader(TextReader textReader)
        /// </summary>
        public Filter(TextReader textReader) : base(textReader) {}

        #endregion Public Instance Constructors

        #region Public Virtual Methods

        /// <summary>
        /// Called after construction and after properties are set. Allows
        /// for filter initialization based on properties such as Project and Properties.
        /// </summary>
        public virtual void Initialize() {}

        #endregion Public Virtual Methods

        #region PublicInstanceMethods

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the project.
        /// </remarks>
        public virtual void Log(Level messageLevel, string message) {
            if (Project != null) {
                Project.Log(messageLevel, message);
            }
        }
        #endregion PublicInstanceMethods
    }
}

