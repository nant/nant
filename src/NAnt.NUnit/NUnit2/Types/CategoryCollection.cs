// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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

using NAnt.Core.Util;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Contains a collection of <see cref="Category" /> elements.
    /// </summary>
    [Serializable()]
    public class CategoryCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class.
        /// </summary>
        public CategoryCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class
        /// with the specified <see cref="CategoryCollection"/> instance.
        /// </summary>
        public CategoryCollection(CategoryCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class
        /// with the specified array of <see cref="Category"/> instances.
        /// </summary>
        public CategoryCollection(Category[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors

        #region Override implementation of Object

        /// <summary>
        /// Returns a comma-delimited list of categories.
        /// </summary>
        /// <returns>
        /// A comma-delimited list of categories, or an empty 
        /// <see cref="string" /> if there are no categories.
        /// </returns>
        public override string ToString() {
            string categories = string.Empty;

            foreach (Category category in base.List) {
                if (category.IfDefined && !category.UnlessDefined) {
                    // add comma delimited if its not the first category
                    if (!StringUtils.IsNullOrEmpty(categories)) {
                        categories += ",";
                    }

                    categories += category.CategoryName;
                }
            }

            return categories;
        }

        #endregion Override implementation of Object
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public Category this[int index] {
            get {return ((Category)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="Category"/> with the specified name.
        /// </summary>
        /// <param name="value">The name of the <see cref="Category"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public Category this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (Category category in base.List) {
                        if (value.Equals(category.CategoryName)) {
                            return category;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="Category"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="Category"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(Category item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="Category"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="Category"/> elements to be added to the end of the collection.</param> 
        public void AddRange(Category[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="CategoryCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="CategoryCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(CategoryCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="Category"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="Category"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(Category item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="Category"/> with the specified
        /// value is in the collection.
        /// </summary>
        /// <param name="value">The argument value to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="Category" /> with value 
        /// <paramref name="value"/> is found in the collection; otherwise, 
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
        public void CopyTo(Category[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="Category"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="Category"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="Category"/>. If the <see cref="Category"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(Category item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="Category"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="Category"/> to insert.</param>
        public void Insert(int index, Category item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="CategoryEnumerator"/> for the entire collection.
        /// </returns>
        public new CategoryEnumerator GetEnumerator() {
            return new CategoryEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="Category"/> to remove from the collection.</param>
        public void Remove(Category item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="Category"/> elements of a <see cref="CategoryCollection"/>.
    /// </summary>
    public class CategoryEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryEnumerator"/> class
        /// with the specified <see cref="CategoryCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal CategoryEnumerator(CategoryCollection arguments) {
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
        public Category Current {
            get { return (Category) _baseEnumerator.Current; }
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
