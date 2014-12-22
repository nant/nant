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
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Globalization;
using System.Xml;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Provides the abstract base class for types.
    /// </summary>
    [Serializable()]
    public abstract class DataTypeBase : Element {
        #region Private Instance Fields

        private string _id;
        private string _refID;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The ID used to be referenced later.
        /// </summary>
        [TaskAttribute("id" )]
        public string ID {
            get { return _id; }
            set { _id = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The ID to use as the reference.
        /// </summary>
        [TaskAttribute("refid")]
        public string RefID {
            get { return _refID; }
            set { _refID = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets a value indicating whether a reference to the type can be
        /// defined.
        /// </summary>
        /// <remarks>
        /// Only types with an <see cref="ElementNameAttribute" /> assigned 
        /// to it, can be referenced.
        /// </remarks>
        public bool CanBeReferenced {
            get { return Name != null; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element
        
        /// <summary>
        /// Gets the name of the datatype.
        /// </summary>
        /// <value>
        /// The name of the datatype.
        /// </value>
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

        /// <summary>
        /// Derived classes should override to this method to provide extra
        /// initialization and validation not covered by the base class.
        /// </summary>
        /// <exception cref="BuildException">
        /// </exception>
        /// <remarks>
        /// Access to the <see cref="XmlNode" /> that was used to initialize
        /// this <see cref="Element" /> is available through <see cref="XmlNode" />.
        /// </remarks>
        protected override void Initialize() {
            if (Parent == null) {
                // output warning message
                Log(Level.Warning, "Parent property should be set on types" 
                    + " deriving from DataTypeBase to determine whether" 
                    + " the type is declared on a valid level.");

                // skip further tests
                return;
            }

            if (Parent.GetType() == typeof(Project) || Parent.GetType() == typeof(Target)) {
                if (String.IsNullOrEmpty(ID)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1010"), 
                        Name), Location);
                }
                if (!String.IsNullOrEmpty(RefID)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1009"), 
                        Name), Location);
                }
            } else {
                  if (!String.IsNullOrEmpty(ID)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1008") 
                        + " can only be declared at Project or Target level.", 
                        Name), Location);
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

        #region Protected Instance Methods

        /// <summary>
        /// Copies all instance data of the <see cref="DataTypeBase" /> to a given
        /// <see cref="DataTypeBase" />.
        /// </summary>
        protected void CopyTo(DataTypeBase clone) {
            base.CopyTo(clone);

            clone._id = _id;
            clone._refID = _refID;
        }

        #endregion Protected Instance Methods
    }
}