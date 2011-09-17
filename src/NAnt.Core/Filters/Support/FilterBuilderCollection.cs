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

namespace NAnt.Core.Filters {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="FilterBuilder"/> objects.
    /// </summary>
    [Serializable]
    public class FilterBuilderCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterBuilderCollection"/> class.
        /// </summary>
        public FilterBuilderCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterBuilderCollection"/> class
        /// with the specified <see cref="FilterBuilderCollection"/> instance.
        /// </summary>
        public FilterBuilderCollection(FilterBuilderCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterBuilderCollection"/> class
        /// with the specified array of <see cref="FilterBuilder"/> instances.
        /// </summary>
        public FilterBuilderCollection(FilterBuilder[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public FilterBuilder this[int index] {
            get {return ((FilterBuilder)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="FilterBuilder"/> for the specified task.
        /// </summary>
        /// <param name="filterName">The name of the filter for which the <see cref="FilterBuilder" /> should be located in the collection.</param> 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public FilterBuilder this[string filterName] {
            get {
                if (filterName != null) {
                    // try to locate instance by name
                    foreach (FilterBuilder filterBuilder in base.List) {
                        if (filterName.Equals(filterBuilder.FilterName)) {
                            return filterBuilder;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="FilterBuilder"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterBuilder"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(FilterBuilder item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="FilterBuilder"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="FilterBuilder"/> elements to be added to the end of the collection.</param> 
        public void AddRange(FilterBuilder[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="FilterBuilderCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="FilterBuilderCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(FilterBuilderCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="FilterBuilder"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterBuilder"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(FilterBuilder item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="FilterBuilder"/> for the specified 
        /// task is in the collection.
        /// </summary>
        /// <param name="taskName">The name of task for which the <see cref="FilterBuilder" /> should be located in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="FilterBuilder" /> for 
        /// the specified task is found in the collection; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        public bool Contains(string taskName) {
            return this[taskName] != null;
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(FilterBuilder[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="FilterBuilder"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterBuilder"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="FilterBuilder"/>. If the <see cref="FilterBuilder"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(FilterBuilder item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="FilterBuilder"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="FilterBuilder"/> to insert.</param>
        public void Insert(int index, FilterBuilder item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="FilterBuilderEnumerator"/> for the entire collection.
        /// </returns>
        public new FilterBuilderEnumerator GetEnumerator() {
            return new FilterBuilderEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterBuilder"/> to remove from the collection.</param>
        public void Remove(FilterBuilder item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="FilterBuilder"/> elements of a <see cref="FilterBuilderCollection"/>.
    /// </summary>
    public class FilterBuilderEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterBuilderEnumerator"/> class
        /// with the specified <see cref="FilterBuilderCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal FilterBuilderEnumerator(FilterBuilderCollection arguments) {
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
        public FilterBuilder Current {
            get { return (FilterBuilder) _baseEnumerator.Current; }
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
