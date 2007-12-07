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
// Ian MacLean ( ian@maclean.ms )

using System;

using NAnt.Core.Tasks;

namespace NAnt.Core.Attributes {

    /// <summary>
    /// Defines possible locations in which a task executable can be located.
    /// </summary>
    public enum LocationType {
        /// <summary>
        /// Locates the task executable in the current Framework directory.
        /// </summary>
        FrameworkDir,

        /// <summary>
        /// Locates the task executable in the current Framework SDK directory.
        /// </summary>
        FrameworkSdkDir
    }
    
    /// <summary>
    /// Indicates the location that a task executable can be located in.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When applied to a task deriving from <see cref="ExternalProgramBase" />,
    ///   the program to execute will first be searched for in the designated
    ///   location.
    ///   </para>
    ///   <para>
    ///   If the program does not exist in that location, and the file name is
    ///   not an absolute path then the list of tool paths of the current
    ///   target framework will be searched (in the order in which they are
    ///   defined in the NAnt configuration file).
    ///   </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public class ProgramLocationAttribute : Attribute {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramLocationAttribute" /> 
        /// with the specified location.
        /// </summary>
        /// <param type="type">The <see cref="LocationType" /> of the attribute.</param>
        public ProgramLocationAttribute(LocationType type) {
            LocationType = type;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the <see cref="LocationType" /> of the task.
        /// </summary>
        /// <value>
        /// The location type of the task to which the attribute is assigned.
        /// </value>
        public LocationType LocationType {
            get { return _locationType; }
            set { _locationType = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private LocationType _locationType;

        #endregion Private Instance Fields
    }
}