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

// Tomas Restrepo (tomasr@mvps.org)

using System.Collections;
using System.Xml;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {
    /// <summary>
    /// Represents an option in an optionSet
    /// </summary>
    public struct OptionValue {
        #region Private Instance Fields

        private string _name;
        private string _value;

        #endregion Private Instance Fields

        #region Internal Instance Constructors

        internal OptionValue(string name, string value) {
            _name = name;
            _value = value;
        }
        
        #endregion Internal Instance Constructors

        #region Public Instance Properties

        public string Name { 
            get { return _name; }
        }

        public string Value { 
            get { return _value; }
        }

        #endregion Public Instance Properties
    }

    /// <summary>
    /// Handles a set of options as a name/value collection.
    /// </summary>
    public class OptionSet : Element, IEnumerable {
        #region Private Instance Fields

        private ArrayList _options;
        private OptionElement[] _optionElements;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSet" /> class.
        /// </summary>
        public OptionSet() {
            _options = new ArrayList();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Indexer, based on option index.
        /// </summary>
        public OptionValue this[int index] {
            get { return (OptionValue)_options[index]; }
        }

        /// <summary>
        /// Number of options in the set
        /// </summary>
        public int Count {
            get { return _options.Count; }
        }

        /// <summary>        /// The options.        /// </summary>
        [BuildElementArray("option")]
        public OptionElement[] SetOptions {
            get { return _optionElements; }
            set { _optionElements = value; }
        }
        
        /// <param name="elementNode"></param>
        protected override void InitializeElement(XmlNode elementNode) {
            // Convert everything to optionValues
            foreach (OptionElement element in _optionElements) {
                _options.Add(new OptionValue(element.OptionName, element.Value));
            }
        }
        #endregion Public Instance Properties

        #region Implementation of IEnumerable

        public IEnumerator GetEnumerator() {
            return _options.GetEnumerator();
        }

        #endregion Implementation of IEnumerable
    }

    [ElementName("option")]
    public class OptionElement : Element {
        #region Private Instance Fields

        private string _name = null;
        private string _value = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of this property
        /// </summary>
        [TaskAttribute("name", Required=true)]
        public string OptionName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Value of this property. Default is null;
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        #endregion Public Instance Properties
    }
}
 