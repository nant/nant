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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;

namespace NAnt.Core.Types {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="FilterSet"/> objects.
    /// </summary>
    [Serializable()]
    public class FilterSetCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSetCollection"/> class.
        /// </summary>
        public FilterSetCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSetCollection"/> class
        /// with the specified <see cref="FilterSetCollection"/> instance.
        /// </summary>
        public FilterSetCollection(FilterSetCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSetCollection"/> class
        /// with the specified array of <see cref="FilterSet"/> instances.
        /// </summary>
        public FilterSetCollection(FilterSet[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public FilterSet this[int index] {
            get {return ((FilterSet)(base.List[index]));}
            set {base.List[index] = value;}
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="FilterSet"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterSet"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(FilterSet item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="FilterSet"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="FilterSet"/> elements to be added to the end of the collection.</param> 
        public void AddRange(FilterSet[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="FilterSetCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="FilterSetCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(FilterSetCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="FilterSet"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterSet"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(FilterSet item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(FilterSet[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="FilterSet"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterSet"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="FilterSet"/>. If the <see cref="FilterSet"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(FilterSet item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="FilterSet"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="FilterSet"/> to insert.</param>
        public void Insert(int index, FilterSet item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="FilterSetEnumerator"/> for the entire collection.
        /// </returns>
        public new FilterSetEnumerator GetEnumerator() {
            return new FilterSetEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="FilterSet"/> to remove from the collection.</param>
        public void Remove(FilterSet item) {
            base.List.Remove(item);
        }

        /// <summary>
        /// Does replacement on the given string with token matching.
        /// </summary>
        /// <param name="line">The line to process the tokens in.</param>
        /// <returns>
        /// The line with the tokens replaced.
        /// </returns>
        public string ReplaceTokens(string line) {
            string replacedLine = line;

            foreach (FilterSet filterSet in base.List) {
                replacedLine = filterSet.ReplaceTokens(replacedLine);
            }

            return replacedLine;
        }
    
        /// <summary>
        /// Checks to see if there are filters in the collection of filtersets.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if there are filters in this collection of
        /// filtersets; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasFilters() {
            foreach (FilterSet filterSet in base.List) {
                if (filterSet.Filters.Count > 0) {
                    return true;
                }
            }

            return false;
        }

        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="FilterSet"/> elements of a <see cref="FilterSetCollection"/>.
    /// </summary>
    public class FilterSetEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSetEnumerator"/> class
        /// with the specified <see cref="FilterSetCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal FilterSetEnumerator(FilterSetCollection arguments) {
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
        public FilterSet Current {
            get { return (FilterSet) _baseEnumerator.Current; }
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
