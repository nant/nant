// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez
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

namespace SourceForge.NAnt {
    /// <summary>
    /// Summary description for ValidationException.
    /// </summary>

    [Serializable]
    public class ValidationException : BuildException {
        /// <summary>
        /// Constructs a build exception with no descriptive information.
        /// </summary>
        public ValidationException() : base() {}

        /// <summary>
        /// Constructs an exception with a descriptive message.
        /// </summary>
        public ValidationException(String message) : base(message) {}

        /// <summary>
        /// Constructs an exception with a descriptive message and an
        /// instance of the Exception that is the cause of the current Exception.
        /// </summary>
        public ValidationException(String message, Exception e) : base(message, e) {}

        /// <summary>
        /// Constructs an exception with a descriptive message and location
        /// in the build file that caused the exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="location">Location in the build file where the exception occured.</param>
        public ValidationException(String message, Location location) : base(message, location) {}

        /// <summary>
        /// Constructs an exception with the given descriptive message, the
        /// location in the build file and an instance of the Exception that
        /// is the cause of the current Exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="location">Location in the build file where the exception occured.</param>
        /// <param name="e">An instance of Exception that is the cause of the current Exception.</param>
        public ValidationException(String message, Location location, Exception e) : base(message, location, e) {}

        /// <summary>Initializes a new instance of the ValidationException class with serialized data.</summary>
        public ValidationException(SerializationInfo info, StreamingContext context) : base(info, context) {}

        /// <summary>Sets the SerializationInfo object with information about the exception.</summary>
        /// <param name="info">The object that holds the serialized object data. </param>
        /// <param name="context">The contextual information about the source or destination. </param>
        /// <remarks>For more information, see SerializationInfo in the Microsoft documentation.</remarks>
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {}

        public override string Message {
            get {
                return base.Message;
            }
        }
    }
}
