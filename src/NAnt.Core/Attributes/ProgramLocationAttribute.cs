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

namespace NAnt.Core.Attributes {
        
    public enum LocationType {
        FrameworkDir,
        FrameworkSdkDir
    }
    
    /// <summary>
    /// Indicates that the location that a task can be located in.
    /// Use the enum above to mark a task as being from the frameworkdir or 
    /// FrameworkSdkDir
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
    public class ProgramLocationAttribute : Attribute {
        #region Private Instance Fields

        LocationType _locationType;        

        #endregion Private Instance Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramLocationAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param type="name">The LocationType of the attribute.</param>
        public ProgramLocationAttribute(LocationType type) {
            LocationType = type;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the LocationType of the attribute.
        /// </summary>
        /// <value>The location type.</value>
        public LocationType LocationType {
            get { return _locationType; }
            set { _locationType = value; }
        }        

        #endregion Public Instance Properties
    }
}