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

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Contains a collection of <see cref="Category" /> items.
    /// </summary>
    [Serializable()]
    public class CategoryCollection : DataTypeCollectionBase, IList {
        #region Public Instance Properties

        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="CategoryEnumerator"/> for the entire collection.
        /// </returns>
        public CategoryEnumerator GetEnumerator() {
            return new CategoryEnumerator(this);
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public Category this[int index] {
            get { 
                RangeCheck(index);
                return (Category) List[index];
            }
            set {
                this.RangeCheck(index);
                List[index] = value;
            }
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

        #region Override implementation of DataTypeCollectionBase

        /// <summary>
        /// Gets the <see cref="Type" /> of the items in this collection.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of the items in this collection.
        /// </value>
        protected override Type ItemType {
            get { return typeof(Category); }
        }

        #endregion Override implementation of DataTypeCollectionBase

        #region IList Members

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        object IList.this[int index] {
            get { return this[index]; }
            set { 
                ValidateType(value);
                this[index] = (Category) value;
            }
        }

        /// <summary>
        /// Inserts a <see cref="Category" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="value">The <see cref="Category"/> to insert.</param>
        void IList.Insert(int index, object value) {
            ValidateType(value);
            Insert(index, (Category) value);
        }

        /// <summary>
        /// Removes the specified <see cref="Category"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to remove from the collection.</param>
        void IList.Remove(object value) {
            ValidateType(value);
            Remove((Category) value);
        }

        /// <summary>
        /// Determines whether a <see cref="Category"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        bool IList.Contains(object value) {
            ValidateType(value);        
            return List.Contains((Category) value);
        }

        /// <summary>
        /// Gets the location of a <see cref="Category"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="Category" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Category"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        int IList.IndexOf(object value) {
            ValidateType(value);
            return IndexOf((Category) value);
        }

        /// <summary>
        /// Adds a <see cref="Category"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        int IList.Add(object value) {
            ValidateType(value);
            return Add((Category) value);
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Adds the items of a <see cref="CategoryCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="CategoryCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(CategoryCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds a <see cref="Category"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        [BuildElement("import")]
        public int Add(Category value) {
            return List.Add(value);
        }

        /// <summary>
        /// Inserts a <see cref="Category" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="value">The <see cref="Category"/> to insert.</param>
        public void Insert(int index, Category value) {
            List.Insert(index, value);
        }

        /// <summary>
        /// Removes the specified <see cref="Category"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to remove from the collection.</param>
        public void Remove(Category value) {
            List.Remove(value);
        }

        /// <summary>
        /// Determines whether a <see cref="Category"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(Category value) {
            return List.Contains(value);
        }

        /// <summary>
        /// Gets the location of a <see cref="Category"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Category"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="Category" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Category"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        public int IndexOf(Category value) {
            return List.IndexOf(value);
        }

        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="Category"/> items of a <see cref="CategoryCollection"/>.
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
        /// Gets the current item in the collection.
        /// </summary>
        /// <returns>
        /// The current item in the collection.
        /// </returns>
        public Category Current {
            get { return (Category) _baseEnumerator.Current; }
        }

        /// <summary>
        /// Gets the current item in the collection.
        /// </summary>
        /// <returns>
        /// The current item in the collection.
        /// </returns>
        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next item of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced 
        /// to the next item; <see langword="false" /> if the enumerator has 
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
        /// first item in the collection.
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
