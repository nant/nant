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
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Collections;

namespace NAnt.Core {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="DataTypeBaseBuilder"/> objects.
    /// </summary>
    [Serializable]
    public class DataTypeBaseBuilderCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseBuilderCollection"/> class.
        /// </summary>
        public DataTypeBaseBuilderCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseBuilderCollection"/> class
        /// with the specified <see cref="DataTypeBaseBuilderCollection"/> instance.
        /// </summary>
        public DataTypeBaseBuilderCollection(DataTypeBaseBuilderCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseBuilderCollection"/> class
        /// with the specified array of <see cref="DataTypeBaseBuilder"/> instances.
        /// </summary>
        public DataTypeBaseBuilderCollection(DataTypeBaseBuilder[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public DataTypeBaseBuilder this[int index] {
            get {return ((DataTypeBaseBuilder)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="DataTypeBaseBuilder"/> for the specified task.
        /// </summary>
        /// <param name="dataTypeName">The name of task for which the <see cref="DataTypeBaseBuilder" /> should be located in the collection.</param> 
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public DataTypeBaseBuilder this[string dataTypeName] {
            get {
                if (dataTypeName != null) {
                    // Try to locate instance using TaskName
                    foreach (DataTypeBaseBuilder DataTypeBaseBuilder in base.List) {
                        if (dataTypeName.Equals(DataTypeBaseBuilder.DataTypeName)) {
                            return DataTypeBaseBuilder;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="DataTypeBaseBuilder"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="DataTypeBaseBuilder"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(DataTypeBaseBuilder item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="DataTypeBaseBuilder"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="DataTypeBaseBuilder"/> elements to be added to the end of the collection.</param> 
        public void AddRange(DataTypeBaseBuilder[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="DataTypeBaseBuilderCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="DataTypeBaseBuilderCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(DataTypeBaseBuilderCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="DataTypeBaseBuilder"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="DataTypeBaseBuilder"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(DataTypeBaseBuilder item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="DataTypeBaseBuilder"/> for the specified 
        /// task is in the collection.
        /// </summary>
        /// <param name="taskName">The name of task for which the <see cref="DataTypeBaseBuilder" /> should be located in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="DataTypeBaseBuilder" /> for 
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
        public void CopyTo(DataTypeBaseBuilder[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="DataTypeBaseBuilder"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="DataTypeBaseBuilder"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="DataTypeBaseBuilder"/>. If the <see cref="DataTypeBaseBuilder"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(DataTypeBaseBuilder item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="DataTypeBaseBuilder"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="DataTypeBaseBuilder"/> to insert.</param>
        public void Insert(int index, DataTypeBaseBuilder item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="DataTypeBaseBuilderEnumerator"/> for the entire collection.
        /// </returns>
        public new DataTypeBaseBuilderEnumerator GetEnumerator() {
            return new DataTypeBaseBuilderEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="DataTypeBaseBuilder"/> to remove from the collection.</param>
        public void Remove(DataTypeBaseBuilder item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="DataTypeBaseBuilder"/> elements of a <see cref="DataTypeBaseBuilderCollection"/>.
    /// </summary>
    public class DataTypeBaseBuilderEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseBuilderEnumerator"/> class
        /// with the specified <see cref="DataTypeBaseBuilderCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal DataTypeBaseBuilderEnumerator(DataTypeBaseBuilderCollection arguments) {
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
        public DataTypeBaseBuilder Current {
            get { return (DataTypeBaseBuilder) _baseEnumerator.Current; }
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
