// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
    /// Contains a collection of <see cref="EmbeddedResource" /> items.
    /// </summary>
    /// <remarks>
    /// Do not yet expose this to build authors.
    /// </remarks>
    [Serializable()]
    //[ElementName("embeddedresources")]
    public class EmbeddedResourceCollection : DataTypeCollectionBase, IList {
        #region Public Instance Properties

        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="EmbeddedResourceEnumerator"/> for the entire collection.
        /// </returns>
        public EmbeddedResourceEnumerator GetEnumerator() {
            return new EmbeddedResourceEnumerator(this);
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public EmbeddedResource this[int index] {
            get { 
                RangeCheck(index);
                return (EmbeddedResource) List[index];
            }
            set {
                this.RangeCheck(index);
                List[index] = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="EmbeddedResource"/> with the specified manifest
        /// resource name.
        /// </summary>
        /// <param name="value">The manifest resource name of the <see cref="EmbeddedResource"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public EmbeddedResource this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (EmbeddedResource embeddedResource in base.List) {
                        if (value.Equals(embeddedResource.ManifestResourceName)) {
                            return embeddedResource;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of DataTypeCollectionBase

        /// <summary>
        /// Gets the <see cref="Type" /> of the items in this collection.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of the items in this collection.
        /// </value>
        protected override Type ItemType {
            get { return typeof(EmbeddedResource); }
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
                this[index] = (EmbeddedResource) value;
            }
        }

        /// <summary>
        /// Inserts a <see cref="EmbeddedResource" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="EmbeddedResource"/> to insert.</param>
        void IList.Insert(int index, object value) {
            ValidateType(value);
            Insert(index, (EmbeddedResource) value);
        }

        /// <summary>
        /// Removes the specified <see cref="EmbeddedResource"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to remove from the collection.</param>
        void IList.Remove(object value) {
            ValidateType(value);
            Remove((EmbeddedResource) value);
        }

        /// <summary>
        /// Determines whether a <see cref="EmbeddedResource"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        bool IList.Contains(object value) {
            ValidateType(value);        
            return List.Contains((EmbeddedResource) value);
        }

        /// <summary>
        /// Gets the location of a <see cref="EmbeddedResource"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="EmbeddedResource" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="EmbeddedResource"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        int IList.IndexOf(object value) {
            ValidateType(value);
            return IndexOf((EmbeddedResource) value);
        }

        /// <summary>
        /// Adds a <see cref="EmbeddedResource"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        int IList.Add(object value) {
            ValidateType(value);
            return Add((EmbeddedResource) value);
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Adds the items of a <see cref="EmbeddedResourceCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="EmbeddedResourceCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(EmbeddedResourceCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds a <see cref="EmbeddedResource"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        //[BuildElement("import")]
        public int Add(EmbeddedResource value) {
            return List.Add(value);
        }

        /// <summary>
        /// Inserts a <see cref="EmbeddedResource" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="EmbeddedResource"/> to insert.</param>
        public void Insert(int index, EmbeddedResource value) {
            List.Insert(index, value);
        }

        /// <summary>
        /// Removes the specified <see cref="EmbeddedResource"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to remove from the collection.</param>
        public void Remove(EmbeddedResource value) {
            List.Remove(value);
        }

        /// <summary>
        /// Determines whether a <see cref="EmbeddedResource"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(EmbeddedResource value) {
            return List.Contains(value);
        }

        /// <summary>
        /// Gets the location of a <see cref="EmbeddedResource"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="EmbeddedResource"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="EmbeddedResource" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="EmbeddedResource"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        public int IndexOf(EmbeddedResource value) {
            return List.IndexOf(value);
        }

        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="EmbeddedResource"/> items of a <see cref="EmbeddedResourceCollection"/>.
    /// </summary>
    public class EmbeddedResourceEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedResourceEnumerator"/> class
        /// with the specified <see cref="EmbeddedResourceCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal EmbeddedResourceEnumerator(EmbeddedResourceCollection arguments) {
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
        public EmbeddedResource Current {
            get { return (EmbeddedResource) _baseEnumerator.Current; }
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
