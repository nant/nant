// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Runtime.Serialization;

namespace NAnt.Core {
    [Serializable()]
    public class ValidationException : BuildException {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class.
        /// </summary>
        public ValidationException() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public ValidationException(String message) : base(message) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class with the specified descriptive message and inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public ValidationException(String message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class with a descriptive message and the location in the build file 
        /// that caused the exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="location">The location in the build file where the exception occured.</param>
        public ValidationException(String message, Location location) : base(message, location) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class with a descriptive message, the location in the build file and 
        /// an instance of the exception that is the cause of the current 
        /// exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="location">The location in the build file where the exception occured.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public ValidationException(String message, Location location, Exception innerException) : base(message, location, innerException) {
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException" /> 
        /// class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        #endregion Protected Instance Constructors
      }
}
