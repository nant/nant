// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;

namespace NAnt.Core {
    /// <summary>
    /// Provides the abstract base class for tasks.
    /// </summary>
    /// <remarks>A task is a piece of code that can be executed.</remarks>
    public abstract class DataTypeBase : Element {
        #region Private Instance Fields

        private string _id;
        private string _refID;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>The base of the directory of this file set.  Default is project base directory.</summary>
        [TaskAttribute("id" )]
        public string ID {
            get { return _id; }
            set { _id = value; }
        }

        // todo if ref has value then load it from collection ...
        [TaskAttribute("refid")]
        public string RefID {
            get { return _refID; }
            set { _refID = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element
        
        /// <summary>
        /// Gets the name of the datatype.
        /// </summary>
        /// <value>The name of the datatype.</value>
        public override string Name {
            get {
                string name = null;
                ElementNameAttribute elementName = (ElementNameAttribute) Attribute.GetCustomAttribute(GetType(), typeof(ElementNameAttribute));
                if (elementName != null) {
                    name = elementName.Name;
                }
                return name;
            }
        }

        protected override void InitializeElement(XmlNode elementNode) {
            if (Parent.GetType() == typeof(Project) || Parent.GetType() == typeof(Target)) {
                if (ID == null || ID.Length == 0) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "'id' is a required attribute for a <{0}> datatype declaration.", Name);
                    throw new BuildException(msg, Location);
                }
                if (RefID != null && RefID.Length > 0) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "'refid' attribute is invalid for a <{0}> datatype declaration.", Name);
                    throw new BuildException(msg, Location);
                }
            } else {
                  if (ID != null && ID.Length > 0 ) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "'id' is an invalid attribute for a <{0}> tag. Datatypes can only be declared at Project or Task level.", Name);
                    throw new BuildException(msg, Location);
                }
            }
        }

        #endregion Override implementation of Element

        #region Public Instance Methods

        /// <summary>
        /// Should be overridden by derived classes. clones the referenced types 
        /// data into the current instance.
        /// </summary>
        public virtual void Reset( ) {
        }

        #endregion Public Instance Methods
    }
}