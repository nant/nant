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

namespace SourceForge.NAnt.Attributes {
    /// <summary>
    /// Indicates that property should be treated as a xml arrayList for the task.
    /// </summary>
    /// <remarks>
    /// Should only be applied to properties exposing strongly typed arrays or 
    /// strongly typed collections.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited=true)]
    public sealed class BuildElementArrayAttribute : BuildElementAttribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildElementArrayAttribute" /> with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        public BuildElementArrayAttribute(string name) : base(name) {
        }                

        #endregion Public Instance Constructors
    }
}