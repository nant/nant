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
    /// Contains a collection of <see cref="EnvironmentVariable" /> elements.
    /// </summary>
    [Serializable()]
    public class EnvironmentVariableCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableCollection"/> class.
        /// </summary>
        public EnvironmentVariableCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableCollection"/> class
        /// with the specified <see cref="EnvironmentVariableCollection"/> instance.
        /// </summary>
        public EnvironmentVariableCollection(EnvironmentVariableCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableCollection"/> class
        /// with the specified array of <see cref="EnvironmentVariable"/> instances.
        /// </summary>
        public EnvironmentVariableCollection(EnvironmentVariable[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public EnvironmentVariable this[int index] {
            get {return ((EnvironmentVariable)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="EnvironmentVariable"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="EnvironmentVariable"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public EnvironmentVariable this[string name] {
            get {
                if (name != null) {
                    // try to locate instance by name
                    foreach (EnvironmentVariable environmentVariable in base.List) {
                        if (environmentVariable.VariableName == name) {
                            return environmentVariable;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="EnvironmentVariable"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="EnvironmentVariable"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(EnvironmentVariable item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="EnvironmentVariable"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="EnvironmentVariable"/> elements to be added to the end of the collection.</param> 
        public void AddRange(EnvironmentVariable[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="EnvironmentVariableCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="EnvironmentVariableCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(EnvironmentVariableCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="EnvironmentVariable"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="EnvironmentVariable"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(EnvironmentVariable item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="EnvironmentVariable"/> with the specified
        /// value is in the collection.
        /// </summary>
        /// <param name="value">The argument value to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="EnvironmentVariable" /> with value 
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
        public void CopyTo(EnvironmentVariable[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="EnvironmentVariable"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="EnvironmentVariable"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="EnvironmentVariable"/>. If the <see cref="EnvironmentVariable"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(EnvironmentVariable item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="EnvironmentVariable"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="EnvironmentVariable"/> to insert.</param>
        public void Insert(int index, EnvironmentVariable item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="EnvironmentVariableEnumerator"/> for the entire collection.
        /// </returns>
        public new EnvironmentVariableEnumerator GetEnumerator() {
            return new EnvironmentVariableEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="EnvironmentVariable"/> to remove from the collection.</param>
        public void Remove(EnvironmentVariable item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="EnvironmentVariable"/> elements of a <see cref="EnvironmentVariableCollection"/>.
    /// </summary>
    public class EnvironmentVariableEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentVariableEnumerator"/> class
        /// with the specified <see cref="EnvironmentVariableCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal EnvironmentVariableEnumerator(EnvironmentVariableCollection arguments) {
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
        public EnvironmentVariable Current {
            get { return (EnvironmentVariable) _baseEnumerator.Current; }
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
