// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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
    /// Contains a collection of <see cref="Module" /> items.
    /// </summary>
    /// <remarks>
    /// Do not yet expose this to build authors.
    /// </remarks>
    [Serializable()]
    public class ModuleCollection : IList, IEnumerable {
        #region Public Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleCollection" />
        /// for the specified <see cref="ModuleSet" />.
        /// </summary>
        /// <param name="moduleSet">The <see cref="ModuleSet" /> containing the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="moduleSet" /> is <see langword="true" />.</exception>
        public ModuleCollection(ModuleSet moduleSet) {
            if (moduleSet == null) {
                throw new ArgumentNullException("moduleSet");
            }
            _moduleSet = moduleSet;
            _list = new ArrayList();
        }

        #endregion Public Instance Constructor

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index" /> parameter is less than 0 or greater than or equal to the value of the <see cref="Count" /> property of the <see cref="ModuleCollection" />.</exception>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public Module this[int index] {
            get { 
                if (index < 0 || index >= Count) {
                    throw new ArgumentOutOfRangeException("index", index, "Invalid value.");
                }
                return (Module) List[index];
            }
            set {
                if (index < 0 || index >= Count) {
                    throw new ArgumentOutOfRangeException("index", index, "Invalid value.");
                }
                List[index] = value;
            }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the list of elements contained in the 
        /// <see cref="ModuleCollection" /> instance.
        /// </summary>
        /// <value>
        /// An <see cref="ArrayList" /> containing the elements of the 
        /// collection.
        /// </value>
        protected ArrayList List {
            get { return _list; }
        }

        #endregion Protected Instance Properties

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ModuleEnumerator"/> for the entire collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return List.GetEnumerator();
        }

        #endregion Implementation of IEnumerable

        #region Implementation of ICollection

        /// <summary>
        /// Gets a value indicating whether access to the collection is 
        /// synchronized (thread-safe).
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        bool ICollection.IsSynchronized {
            get { return false; }
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>
        /// The number of items in the collection.
        /// </value>
        public int Count {
            get { return List.Count; }
        }

        /// <summary>
        /// Copies the items of the collection to an <see cref="Array" />,
        /// starting at a particular index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array" /> that is the destination of the items copied from the collection. The <see cref="Array" /> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(Array array, int index) {
            List.CopyTo(array, index);
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the 
        /// collection.
        /// </summary>
        /// <value>
        /// An object that can be used to synchronize access to the collection.
        /// </value>
        object ICollection.SyncRoot {
            get { return this; }
        }

        #endregion Implementation of ICollection

        #region Implementation of IList

        /// <summary>
        /// Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        public bool IsFixedSize {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index" /> parameter is less than 0 or greater than or equal to the value of the <see cref="Count" /> property of the <see cref="ModuleCollection" />.</exception>
        object IList.this[int index] {
            get { return this[index]; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException ("value");
                }
                if (!(value is Module)) {
                    throw new ArgumentException ("Value is not a Module");
                }
                this[index] = (Module) value;
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear() {
            List.Clear();
        }

        /// <summary>
        /// Inserts a <see cref="Module" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="Module"/> to insert.</param>
        void IList.Insert(int index, object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }
            if (!(value is Module)) {
                throw new ArgumentException ("Value is not a Module");
            }
            Insert(index, (Module) value);
        }

        /// <summary>
        /// Removes the specified <see cref="Module"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to remove from the collection.</param>
        void IList.Remove(object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }
            if (!(value is Module)) {
                throw new ArgumentException ("Value is not a Module");
            }
            Remove((Module) value);
        }

        /// <summary>
        /// Removes an item at a specific index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index" /> parameter is less than 0 or greater than or equal to the value of the <see cref="Count" /> property of the <see cref="ModuleCollection" />.</exception>
        public void RemoveAt(int index) {
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException("index", index, "Invalid value.");
            }
            List.RemoveAt(index);
        }

        /// <summary>
        /// Determines whether a <see cref="Module"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        bool IList.Contains(object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }
            if (!(value is Module)) {
                throw new ArgumentException ("Value is not a Module");
            }
            return List.Contains((Module) value);
        }

        /// <summary>
        /// Gets the location of a <see cref="Module"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="Module" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Module"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        int IList.IndexOf(object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }
            if (!(value is Module)) {
                throw new ArgumentException ("Value is not a Module");
            }
            return IndexOf((Module) value);
        }

        /// <summary>
        /// Adds a <see cref="Module"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        int IList.Add(object value) {
            if (value == null) {
                throw new ArgumentNullException ("value");
            }
            if (!(value is Module)) {
                throw new ArgumentException ("Value is not a Module");
            }
            return Add((Module) value);
        }

        #endregion Implementation of IList

        #region Public Instance Methods

        /// <summary>
        /// Adds the items of a <see cref="ModuleCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="ModuleCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(ModuleCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds a <see cref="Module"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        public int Add(Module value) {
            if (value.ModuleSet != null) {
                throw new ArgumentException("Module is already linked to other ModuleSet.");
            }
            value.ModuleSet = _moduleSet;
            return List.Add(value);
        }

        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ModuleEnumerator"/> for the entire collection.
        /// </returns>
        public ModuleEnumerator GetEnumerator() {
            return new ModuleEnumerator(this);
        }

        /// <summary>
        /// Inserts a <see cref="Module" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="Module"/> to insert.</param>
        public void Insert(int index, Module value) {
            if (value.ModuleSet != null) {
                throw new ArgumentException("Module is already linked to other ModuleSet.");
            }
            value.ModuleSet = _moduleSet;
            List.Insert(index, value);
        }

        /// <summary>
        /// Removes the specified <see cref="Module"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to remove from the collection.</param>
        public void Remove(Module value) {
            List.Remove(value);
            if (value.ModuleSet == _moduleSet) {
                value.ModuleSet = null;
            }
        }

        /// <summary>
        /// Determines whether a <see cref="Module"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(Module value) {
            return List.Contains(value);
        }

        /// <summary>
        /// Gets the location of a <see cref="Module"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="Module"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="Module" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="Module"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        public int IndexOf(Module value) {
            return List.IndexOf(value);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private readonly ModuleSet _moduleSet;
        private readonly ArrayList _list;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Enumerates the <see cref="Module"/> items of a <see cref="ModuleCollection"/>.
    /// </summary>
    public class ModuleEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleEnumerator"/> class
        /// with the specified <see cref="ModuleCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal ModuleEnumerator(ModuleCollection arguments) {
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
        public Module Current {
            get { return (Module) _baseEnumerator.Current; }
        }

        /// <summary>
        /// Gets the current item in the collection.
        /// </summary>
        /// <returns>
        /// The current item in the collection.
        /// </returns>
        object IEnumerator.Current {
            get { return Current; }
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

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the 
        /// first item in the collection.
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
