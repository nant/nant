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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Runtime.Serialization;

namespace NAnt.Core.Util {
    /// <summary>
    /// The exception that is thrown when one of the command-line arguments provided 
    /// is not valid.
    /// </summary>
    [Serializable()]
    public sealed class CommandLineArgumentException : ArgumentException {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class.
        /// </summary>
        public CommandLineArgumentException() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class
        /// with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public CommandLineArgumentException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class
        /// with a descriptive message and an inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public CommandLineArgumentException(string message, Exception innerException) : base(message, innerException) {
        }

        #endregion Public Instance Constructors

        #region Private Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentException" /> class 
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        private CommandLineArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        #endregion Private Instance Constructors
    }
}
