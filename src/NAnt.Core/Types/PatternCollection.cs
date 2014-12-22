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
    /// Contains a collection of <see cref="Pattern" /> elements.
    /// </summary>
    [Serializable()]
    public class PatternCollection : IList {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternCollection"/> class.
        /// </summary>
        public PatternCollection() {
            _list = new ArrayList ();
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public Pattern this[int index] {
            get { return (Pattern) List[index] ;}
            set { List[index] = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private ArrayList List {
            get { return _list; }
        }

        #endregion Private Instance Properties

        #region Implementation of ICollection

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        public int Count {
            get { return List.Count; }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        void ICollection.CopyTo (Array array, int index) {
            List.CopyTo(array, index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
        /// </summary>
        bool ICollection.IsSynchronized {
            get { return List.IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        object ICollection.SyncRoot {
            get { return List.SyncRoot; }
        }

        #endregion Implementation of ICollection

        #region Implementation of IEnumerable

        IEnumerator IEnumerable.GetEnumerator() {
            return List.GetEnumerator();
        }

        #endregion Implementation of IEnumerable

        #region Implementation of IList

        object IList.this [int index] {
            get {
                return this [index];
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }

                if (!(value is Pattern)) {
                    throw new ArgumentException ("Specified value is not an instance"
                        + " of " + typeof (Pattern).FullName + ".");
                }

                this [index] = (Pattern) value;
            }
        }

        bool IList.IsFixedSize {
            get { return false; }
        }

        bool IList.IsReadOnly {
            get { return false; }
        }

        int IList.Add (object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }

            if (!(value is Pattern)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + typeof (Pattern).FullName + ".");
            }

            return Add ((Pattern) value);
        }

        bool IList.Contains (object value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (!(value is Pattern)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + typeof (Pattern).FullName + ".");
            }

            return Contains ((Pattern) value);
        }

        /// <summary>
        /// Removes all items from the <see cref="PatternCollection" />.
        /// </summary>
        public void Clear () {
            List.Clear ();
        }

        int IList.IndexOf(object value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (!(value is Pattern)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + typeof (Pattern).FullName + ".");
            }

            return IndexOf ((Pattern) value);
        }

        void IList.Insert(int index, object value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (!(value is Pattern)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + typeof (Pattern).FullName + ".");
            }

            Insert(index, (Pattern) value);
        }

        void IList.Remove(object value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (!(value is Pattern)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + typeof (Pattern).FullName + ".");
            }

            Remove((Pattern) value);
        }

        void IList.RemoveAt (int index) {
            List.RemoveAt(index);
        }

        #endregion Implementation of IList

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="Pattern"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="Pattern"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(Pattern item) {
            return List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="Pattern"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="Pattern"/> elements to be added to the end of the collection.</param> 
        public void AddRange(Pattern[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="PatternCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="PatternCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(PatternCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="Pattern"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="Pattern"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(Pattern item) {
            return List.Contains(item);
        }

        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array,
        /// starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(Pattern[] array, int index) {
            List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="Pattern"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="Pattern"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="Pattern"/>. If the <see cref="Pattern"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(Pattern item) {
            return List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="Pattern"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="Pattern"/> to insert.</param>
        public void Insert(int index, Pattern item) {
            List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="PatternEnumerator"/> for the entire collection.
        /// </returns>
        public PatternEnumerator GetEnumerator() {
            return new PatternEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="Pattern"/> to remove from the collection.</param>
        public void Remove(Pattern item) {
            List.Remove(item);
        }
        
        #endregion Public Instance Methods

        #region Private Instance Fields

        private readonly ArrayList _list;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Enumerates the <see cref="Pattern"/> elements of a <see cref="PatternCollection"/>.
    /// </summary>
    public class PatternEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternEnumerator"/> class
        /// with the specified <see cref="PatternCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal PatternEnumerator(PatternCollection arguments) {
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
        public Pattern Current {
            get { return (Pattern) _baseEnumerator.Current; }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
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

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the 
        /// first element in the collection.
        /// </summary>
        public void Reset() {
            _baseEnumerator.Reset();
        }

        #endregion Implementation of IEnumerator

        #region Private Instance Fields

        private IEnumerator _baseEnumerator;

        #endregion Private Instance Fields
    }
}
