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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NAnt.Core {
    /// <summary>
    /// Thrown whenever an error occurs during the build.
    /// </summary>
    [Serializable]
    public class BuildException : ApplicationException {
        #region Private Instance Fields

        /// <summary>
        /// The location of the exception in the build document (xml file).
        /// </summary>
        private Location _location = Location.UnknownLocation;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class.
        /// </summary>
        public BuildException() : base() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class 
        /// with a descriptive message.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        public BuildException(String message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class
        /// with the specified descriptive message and inner exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public BuildException(String message, Exception innerException) : base(message, innerException) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class
        /// with a descriptive message and the location in the build file that 
        /// caused the exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="location">The location in the build file where the exception occured.</param>
        public BuildException(String message, Location location) : base(message) {
            _location = location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class
        /// with a descriptive message, the location in the build file and an 
        /// instance of the exception that is the cause of the current exception.
        /// </summary>
        /// <param name="message">A descriptive message to include with the exception.</param>
        /// <param name="location">The location in the build file where the exception occured.</param>
        /// <param name="innerException">A nested exception that is the cause of the current exception.</param>
        public BuildException(String message, Location location, Exception innerException) : base(message, innerException) {
            _location = location;
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildException" /> class 
        /// with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected BuildException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _location = info.GetValue("Location", _location.GetType()) as Location;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the raw message as specified when the exception was 
        /// constructed.
        /// </summary>
        /// <value>
        /// The raw message as specified when the exception was 
        /// constructed.
        /// </value>
        public string RawMessage {
            get { return base.Message; }
        }

        /// <summary>
        /// Gets the location in the build file of the element from which the
        /// exception originated.
        /// </summary>
        /// <value>
        /// The location in the build file of the element from which the
        /// exception originated.
        /// </value>
        public Location Location {
            get { return _location; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ISerializable

        /// <summary>
        /// Serializes this object into the <see cref="SerializationInfo" /> provided.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
        /// <param name="context">The destination for this serialization.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("Location", _location);      
        }

        #endregion Override implementation of ISerializable

        #region Override implementation of ApplicationException

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value>
        /// The error message that explains the reason for the exception.
        /// </value>
        /// <remarks>
        /// Adds location information to the message, if available.
        /// </remarks>
        public override string Message {
            get {
                string message = base.Message;
                // only include location string if not empty
                string locationString = string.Empty;
                if (_location != null) {
                    locationString = _location.ToString();
                }
                
                if (!String.IsNullOrEmpty(locationString)) {
                    message = locationString + Environment.NewLine + message;
                }
                return message; 
            }
        }       
        
        #endregion Override implementation of ApplicationException
        
        #region Override implementation of Object

        /// <summary>
        /// Creates and returns a string representation of the current 
        /// exception.
        /// </summary>
        /// <returns>
        /// A string representation of the current exception.
        /// </returns>
        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}{2}", 
                Message, Environment.NewLine, base.ToString());
        }

        #endregion Override implementation of Object
    }
}
