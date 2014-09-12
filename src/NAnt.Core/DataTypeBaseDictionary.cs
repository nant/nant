// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
    /// Dictionary class to manage the projects types.
    /// </summary>
    public class DataTypeBaseDictionary : IDictionary, ICollection, IEnumerable, ICloneable {
        #region Private Instance Fields

        private Hashtable _innerHash;

        #endregion Private Instance Fields
        
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary" /> class.
        /// </summary>
        public DataTypeBaseDictionary() {
            _innerHash = new Hashtable();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="original">The original dictionary which well be copied.</param>
        public DataTypeBaseDictionary(DataTypeBaseDictionary original) {
            _innerHash = new Hashtable(original.InnerHash);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary which will be copied.</param>
        public DataTypeBaseDictionary(IDictionary dictionary) {
            _innerHash = new Hashtable (dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary" /> class
        /// with the specified capacity.
        /// </summary>
        public DataTypeBaseDictionary(int capacity) {
            _innerHash = new Hashtable(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="loadFactor">The load factor.</param>
        public DataTypeBaseDictionary(IDictionary dictionary, float loadFactor) {
            _innerHash = new Hashtable(dictionary, loadFactor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="codeProvider">The code provider.</param>
        /// <param name="comparer">The comparer.</param>
        public DataTypeBaseDictionary(IHashCodeProvider codeProvider, IComparer comparer) {
            _innerHash = new Hashtable(codeProvider, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="loadFactor">The load factor.</param>
        public DataTypeBaseDictionary(int capacity, int loadFactor) {
            _innerHash = new Hashtable(capacity, loadFactor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="codeProvider">The code provider.</param>
        /// <param name="comparer">The comparer.</param>
        public DataTypeBaseDictionary(IDictionary dictionary, IHashCodeProvider codeProvider, IComparer comparer) {
            _innerHash = new Hashtable (dictionary, codeProvider, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="codeProvider">The code provider.</param>
        /// <param name="comparer">The comparer.</param>
        public DataTypeBaseDictionary(int capacity, IHashCodeProvider codeProvider, IComparer comparer) {
            _innerHash = new Hashtable (capacity, codeProvider, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="loadFactor">The load factor.</param>
        /// <param name="codeProvider">The code provider.</param>
        /// <param name="comparer">The comparer.</param>
        public DataTypeBaseDictionary(IDictionary dictionary, float loadFactor, IHashCodeProvider codeProvider, IComparer comparer) {
            _innerHash = new Hashtable (dictionary, loadFactor, codeProvider, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeBaseDictionary"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="loadFactor">The load factor.</param>
        /// <param name="codeProvider">The code provider.</param>
        /// <param name="comparer">The comparer.</param>
        public DataTypeBaseDictionary(int capacity, float loadFactor, IHashCodeProvider codeProvider, IComparer comparer) {
            _innerHash = new Hashtable (capacity, loadFactor, codeProvider, comparer);
        }

        #endregion Public Instance Constructors

        #region Internal Instance Properties

        internal Hashtable InnerHash {
            get { return _innerHash; }
            set { _innerHash = value ; }
        }

        #endregion Internal Instance Properties

        #region Implementation of IDictionary

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>An enumerator object for the dictionary.</returns>
        public DataTypeBaseDictionaryEnumerator GetEnumerator() {
            return new DataTypeBaseDictionaryEnumerator(this);
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator" /> object for the <see cref="T:System.Collections.IDictionary" /> object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IDictionaryEnumerator" /> object for the <see cref="T:System.Collections.IDictionary" /> object.
        /// </returns>
        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new DataTypeBaseDictionaryEnumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        
      /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary" /> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(string key) {
            _innerHash.Remove(key);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary" /> object.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        void IDictionary.Remove(object key) {
            Remove((string) key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IDictionary" /> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary" /> object.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IDictionary" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool Contains(string key) {
            return _innerHash.Contains(key);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IDictionary" /> object contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary" /> object.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IDictionary" /> contains an element with the key; otherwise, false.
        /// </returns>
        bool IDictionary.Contains(object key) {
            return Contains((string)key);
        }

        /// <summary>
        /// Removes all keys and values from the Dictionary.
        /// </summary>
        public void Clear() {
            _innerHash.Clear();      
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(string key, DataTypeBase value) {
            _innerHash.Add (key, value);
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        void IDictionary.Add(object key, object value) {
            Add((string) key, (DataTypeBase) value);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary" /> object is read-only.
        /// </summary>
        public bool IsReadOnly {
            get { return _innerHash.IsReadOnly; }
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTypeBase"/> with the specified key.
        /// </summary>
        /// <value>
        /// The <see cref="DataTypeBase"/>.
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public DataTypeBase this[string key] {
            get { return (DataTypeBase) _innerHash[key]; }
            set { _innerHash[key] = value; }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        object IDictionary.this[object key] {
            get { return this[(string) key]; }
            set { this[(string) key] = (DataTypeBase) value; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection" /> object containing the values in the <see cref="T:System.Collections.IDictionary" /> object.
        /// </summary>
        public ICollection Values {
            get { return _innerHash.Values; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection" /> object containing the keys of the <see cref="T:System.Collections.IDictionary" /> object.
        /// </summary>
        public ICollection Keys {
            get { return _innerHash.Keys; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary" /> object has a fixed size.
        /// </summary>
        public bool IsFixedSize {
            get { return _innerHash.IsFixedSize; }
        }

        #endregion Implementation of IDictionary

        #region Implementation of ICollection

        void ICollection.CopyTo(Array array, int index) {
            _innerHash.CopyTo(array, index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
        /// </summary>
        public bool IsSynchronized {
            get { return _innerHash.IsSynchronized; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        public int Count {
            get { return _innerHash.Count; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        public object SyncRoot {
            get { return _innerHash.SyncRoot; }
        }

        /// <summary>
        /// Copies the Hashtable elements to a one-dimensional Array instance at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the DictionaryEntry objects copied from Hashtable. The Array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(DataTypeBase[] array, int index) {
            _innerHash.CopyTo(array, index);
        }

        #endregion Implementation of ICollection

        #region Implementation of ICloneable

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>The created clone.</returns>
        public DataTypeBaseDictionary Clone() {
            DataTypeBaseDictionary clone = new DataTypeBaseDictionary();
            clone.InnerHash = (Hashtable) _innerHash.Clone();
            return clone;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        object ICloneable.Clone() {
            return Clone();
        }

        #endregion Implementation of ICloneable
        
        #region HashTable Methods

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the dictionary contains the specified key; else <c>false.</c></returns>
        public bool ContainsKey (string key) {
            return _innerHash.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the dictionary contains the specified value; else <c>false.</c></returns>
        public bool ContainsValue(DataTypeBase value) {
            return _innerHash.ContainsValue(value);
        }

        /// <summary>
        /// Returns a synchronized (thread-safe) wrapper for the Hashtable.
        /// </summary>
        /// <param name="nonSync">The Hashtable to synchronize.</param>
        /// <returns>A synchronized (thread-safe) wrapper for the Hashtable.</returns>
        public static DataTypeBaseDictionary Synchronized(DataTypeBaseDictionary nonSync) {
            DataTypeBaseDictionary sync = new DataTypeBaseDictionary();
            sync.InnerHash = Hashtable.Synchronized(nonSync.InnerHash);
            return sync;
        }

        #endregion HashTable Methods
        
        /// <summary>
        /// Inherits Properties from an existing property
        /// dictionary Instance
        /// </summary>
        /// <param name="source">DataType list to inherit</param>       
        public virtual void Inherit(DataTypeBaseDictionary source) {
            foreach ( string key in source.Keys ){
                Add( key, source[key] );
                //this[key] = entry.Value;      
            }          
        }           
    }

    /// <summary>
    /// Enumerator class for a <see cref="DataTypeBaseDictionary"/>.
    /// </summary>
    public class DataTypeBaseDictionaryEnumerator : IDictionaryEnumerator {
        #region Private Instance Fields

        private IDictionaryEnumerator _innerEnumerator;

        #endregion Private Instance Fields

        #region Internal Instance Constructors

        internal DataTypeBaseDictionaryEnumerator(DataTypeBaseDictionary enumerable) {
            _innerEnumerator = enumerable.InnerHash.GetEnumerator();
        }

        #endregion Internal Instance Constructors

        #region Implementation of IDictionaryEnumerator

        /// <summary>
        /// Gets the key of the current dictionary entry.
        /// </summary>
        public string Key {
            get { return (string) _innerEnumerator.Key; }
        }

        /// <summary>
        /// Gets the key of the current dictionary entry.
        /// </summary>
        object IDictionaryEnumerator.Key {
            get { return Key; }
        }

        /// <summary>
        /// Gets the value of the current dictionary entry.
        /// </summary>
        public DataTypeBase Value {
            get { return (DataTypeBase) _innerEnumerator.Value; }
        }

        /// <summary>
        /// Gets the value of the current dictionary entry.
        /// </summary>
        object IDictionaryEnumerator.Value {
            get { return Value; }
        }

        /// <summary>
        /// Gets both the key and the value of the current dictionary entry.
        /// </summary>
        public DictionaryEntry Entry {
            get { return _innerEnumerator.Entry; }
        }

        #endregion Implementation of IDictionaryEnumerator

        #region Implementation of IEnumerator

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() {
            _innerEnumerator.Reset();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext() {
            return _innerEnumerator.MoveNext();
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object IEnumerator.Current {
            get { return _innerEnumerator.Current; }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public DataTypeBase Current {
            get { return (DataTypeBase) _innerEnumerator.Current; }
        }

        #endregion Implementation of IEnumerator
    }
}
