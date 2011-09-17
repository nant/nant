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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;

namespace NAnt.Core.Types {
    /// <summary>
    /// Contains a collection of <see cref="XsltParameter" /> elements.
    /// </summary>
    [Serializable()]
    public class XsltParameterCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XsltParameterCollection"/> class.
        /// </summary>
        public XsltParameterCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="XsltParameterCollection"/> class
        /// with the specified <see cref="XsltParameterCollection"/> instance.
        /// </summary>
        public XsltParameterCollection(XsltParameterCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="XsltParameterCollection"/> class
        /// with the specified array of <see cref="XsltParameter"/> instances.
        /// </summary>
        public XsltParameterCollection(XsltParameter[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public XsltParameter this[int index] {
            get { return (XsltParameter) base.List[index]; }
            set { base.List[index] = value; }
        }

        /// <summary>
        /// Gets the <see cref="XsltParameter"/> with the specified name.
        /// </summary>
        /// <param name="value">The name of the <see cref="XsltParameter"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public XsltParameter this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (XsltParameter parameter in base.List) {
                        if (parameter.Name == value) {
                            return parameter;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="XsltParameter"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="XsltParameter"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(XsltParameter item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="XsltParameter"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="XsltParameter"/> elements to be added to the end of the collection.</param> 
        public void AddRange(XsltParameter[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="XsltParameterCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="XsltParameterCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(XsltParameterCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="XsltParameter"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="XsltParameter"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(XsltParameter item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="XsltParameter"/> with the specified
        /// value is in the collection.
        /// </summary>
        /// <param name="value">The argument value to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="XsltParameter" /> with 
        /// value <paramref name="value"/> is found in the collection; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        public bool Contains(string value) {
            return this[value] != null;
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(XsltParameter[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="XsltParameter"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="XsltParameter"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="XsltParameter"/>. If the <see cref="XsltParameter"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(XsltParameter item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="XsltParameter"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="XsltParameter"/> to insert.</param>
        public void Insert(int index, XsltParameter item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="XsltParameterEnumerator"/> for the entire collection.
        /// </returns>
        public new XsltParameterEnumerator GetEnumerator() {
            return new XsltParameterEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="XsltParameter"/> to remove from the collection.</param>
        public void Remove(XsltParameter item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="XsltParameter"/> elements of a <see cref="XsltParameterCollection"/>.
    /// </summary>
    public class XsltParameterEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XsltParameterEnumerator"/> class
        /// with the specified <see cref="XsltParameterCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal XsltParameterEnumerator(XsltParameterCollection arguments) {
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
        public XsltParameter Current {
            get { return (XsltParameter) _baseEnumerator.Current; }
        }

        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced 
        /// to the next element; <see langword="false" /> if the enumerator has 
        /// passed the end of the collection.
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
