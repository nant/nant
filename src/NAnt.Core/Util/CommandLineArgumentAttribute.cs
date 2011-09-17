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

namespace NAnt.Core.Util {
    /// <summary>
    /// Allows control of command line parsing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CommandLineArgumentAttribute : Attribute {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentAttribute" /> class
        /// with the specified argument type.
        /// </summary>
        /// <param name="argumentType">Specifies the checking to be done on the argument.</param>
        public CommandLineArgumentAttribute(CommandLineArgumentTypes argumentType) {
            _argumentType = argumentType;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the checking to be done on the argument.
        /// </summary>
        /// <value>The checking that should be done on the argument.</value>
        public CommandLineArgumentTypes Type {
            get { return _argumentType; }
        }

        /// <summary>
        /// Gets or sets the long name of the argument.
        /// </summary>
        /// <value>The long name of the argument.</value>
        public string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the short name of the argument.
        /// </summary>
        /// <value>The short name of the argument.</value>
        public string ShortName {
            get { return _shortName; }
            set { _shortName = value; }
        }

        /// <summary>
        /// Gets or sets the description of the argument.
        /// </summary>
        /// <value>The description of the argument.</value>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private CommandLineArgumentTypes _argumentType;
        private string _name;
        private string _shortName;
        private string _description;

        #endregion Private Instance Fields
    }
}

