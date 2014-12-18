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
// Scott Hernandez (Scott Hernandez)

using System;
using System.Runtime.Serialization;

namespace Tests.NAnt.Core {
    /// <summary>
    /// Thrown whenever an error occurs during the build.
    /// </summary>
    [Serializable]
    public class TestBuildException : ApplicationException {
        #region Private Instance Fields

        private string _buildResults;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBuildException" />
        /// class.
        /// </summary>
        public TestBuildException() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBuildException" /> 
        /// class with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public TestBuildException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBuildException" /> 
        /// class with the specified descriptive message and inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public TestBuildException(string message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBuildException" /> 
        /// class with a descriptive message and the location in the build file 
        /// that caused the exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public TestBuildException(string message, string buildResult) : base(message) {
            _buildResults = buildResult;
        }

        /// <summary>
        /// Constructs an exception with the given descriptive message, the
        /// location in the build file and an instance of the Exception that
        /// is the cause of the current Exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public TestBuildException(string message, string buildResult, Exception innerException) : base(message, innerException) {
            _buildResults = buildResult;
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBuildException" /> 
        /// class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected TestBuildException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _buildResults = (string) info.GetValue("BuildResults", typeof(string));
        }

        #endregion Protected Instance Constructors

        #region Override implementation of ApplicationException

        public override string Message {
            get {
                if (_buildResults == null || _buildResults.Length == 0) {
                    return base.Message;
                } else {
                    return base.Message + Environment.NewLine + "Build Log:"
                        + Environment.NewLine + _buildResults;
                }
            }
        }

        /// <summary>
        /// Serializes this object into the <see cref="SerializationInfo" /> provided.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("BuildResults", _buildResults);
        }

        #endregion Override implementation of ApplicationException
    }
}

