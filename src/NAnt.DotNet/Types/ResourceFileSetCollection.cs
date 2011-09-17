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

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="ResourceFileSet"/> objects.
    /// </summary>
    [Serializable]
    public class ResourceFileSetCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFileSetCollection"/> class.
        /// </summary>
        public ResourceFileSetCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFileSetCollection"/> class
        /// with the specified <see cref="ResourceFileSetCollection"/> instance.
        /// </summary>
        public ResourceFileSetCollection(ResourceFileSetCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFileSetCollection"/> class
        /// with the specified array of <see cref="ResourceFileSet"/> instances.
        /// </summary>
        public ResourceFileSetCollection(ResourceFileSet[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public ResourceFileSet this[int index] {
            get {return ((ResourceFileSet)(base.List[index]));}
            set {base.List[index] = value;}
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="ResourceFileSet"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="ResourceFileSet"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(ResourceFileSet item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="ResourceFileSet"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="ResourceFileSet"/> elements to be added to the end of the collection.</param> 
        public void AddRange(ResourceFileSet[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="ResourceFileSetCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="ResourceFileSetCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(ResourceFileSetCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="ResourceFileSet"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ResourceFileSet"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(ResourceFileSet item) {
            return base.List.Contains(item);
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(ResourceFileSet[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="ResourceFileSet"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ResourceFileSet"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="ResourceFileSet"/>. If the <see cref="ResourceFileSet"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(ResourceFileSet item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="ResourceFileSet"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="ResourceFileSet"/> to insert.</param>
        public void Insert(int index, ResourceFileSet item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ResourceFileSetEnumerator"/> for the entire collection.
        /// </returns>
        public new ResourceFileSetEnumerator GetEnumerator() {
            return new ResourceFileSetEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="ResourceFileSet"/> to remove from the collection.</param>
        public void Remove(ResourceFileSet item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="ResourceFileSet"/> elements of a <see cref="ResourceFileSetCollection"/>.
    /// </summary>
    public class ResourceFileSetEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFileSetEnumerator"/> class
        /// with the specified <see cref="ResourceFileSetCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal ResourceFileSetEnumerator(ResourceFileSetCollection arguments) {
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
        public ResourceFileSet Current {
            get { return (ResourceFileSet) _baseEnumerator.Current; }
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
