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
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {
    /// <summary>
    /// Represents an option.
    /// </summary>
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

    [Serializable()]
    public class OptionElementCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionElementCollection"/> class.
        /// </summary>
        public OptionElementCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionElementCollection"/> class
        /// with the specified <see cref="OptionElementCollection"/> instance.
        /// </summary>
        public OptionElementCollection(OptionElementCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionElementCollection"/> class
        /// with the specified array of <see cref="OptionElement"/> instances.
        /// </summary>
        public OptionElementCollection(OptionElement[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public OptionElement this[int index] {
            get {return ((OptionElement)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="OptionElement"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the option that should be located in the collection.</param> 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public OptionElement this[string name] {
            get {
                if (name != null) {
                    // Try to locate instance using OptionName
                    foreach (OptionElement OptionElement in base.List) {
                        if (name.Equals(OptionElement.OptionName)) {
                            return OptionElement;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="OptionElement"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="OptionElement"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(OptionElement item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="OptionElement"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="OptionElement"/> elements to be added to the end of the collection.</param> 
        public void AddRange(OptionElement[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="OptionElementCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="OptionElementCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(OptionElementCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="OptionElement"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="OptionElement"/> to locate in the collection.</param> 
        /// <returns>
        /// <c>true</c> if <paramref name="item"/> is found in the collection;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(OptionElement item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="OptionElement"/> for the specified 
        /// task is in the collection.
        /// </summary>
        /// <param name="taskName">The name of task for which the <see cref="OptionElement" /> should be located in the collection.</param> 
        /// <returns>
        /// <c>true</c> if a <see cref="OptionElement" /> for the specified task
        ///is found in the collection; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string taskName) {
            return this[taskName] != null;
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(OptionElement[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="OptionElement"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="OptionElement"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="OptionElement"/>. If the <see cref="OptionElement"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(OptionElement item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="OptionElement"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="OptionElement"/> to insert.</param>
        public void Insert(int index, OptionElement item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="OptionElementEnumerator"/> for the entire collection.
        /// </returns>
        public new OptionElementEnumerator GetEnumerator() {
            return new OptionElementEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="OptionElement"/> to remove from the collection.</param>
        public void Remove(OptionElement item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="OptionElement"/> elements of a <see cref="OptionElementCollection"/>.
    /// </summary>
    public class OptionElementEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionElementEnumerator"/> class
        /// with the specified <see cref="OptionElementCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal OptionElementEnumerator(OptionElementCollection arguments) {
            IEnumerable temp = (IEnumerable) (arguments);
            _baseEnumerator = temp.GetEnumerator();
        }

        #endregion Internal Instance Constructors

        #region Implementation of IEnumerator
            
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        public OptionElement Current {
            get { return (OptionElement) _baseEnumerator.Current; }
        }

        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next element; 
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext() {
            return _baseEnumerator.MoveNext();
        }

        bool IEnumerator.MoveNext() {
            return _baseEnumerator.MoveNext();
        }
            
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the 
        /// first element in the collection.
        /// </summary>
        public void Reset() {
            _baseEnumerator.Reset();
        }
            
        void IEnumerator.Reset() {
            _baseEnumerator.Reset();
        }

        #endregion Implementation of IEnumerator

        #region Private Instance Fields
    
        private IEnumerator _baseEnumerator;

        #endregion Private Instance Fields
    }
}
