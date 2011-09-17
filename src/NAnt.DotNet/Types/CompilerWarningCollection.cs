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

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Contains a collection of <see cref="CompilerWarning"/> elements.
    /// </summary>
    [Serializable()]
    public class CompilerWarningCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerWarningCollection"/> class.
        /// </summary>
        public CompilerWarningCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerWarningCollection"/> class
        /// with the specified <see cref="CompilerWarningCollection"/> instance.
        /// </summary>
        public CompilerWarningCollection(CompilerWarningCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerWarningCollection"/> class
        /// with the specified array of <see cref="CompilerWarning"/> instances.
        /// </summary>
        public CompilerWarningCollection(CompilerWarning[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public CompilerWarning this[int index] {
            get {return ((CompilerWarning)(base.List[index]));}
            set {base.List[index] = value;}
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="CompilerWarning"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="CompilerWarning"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(CompilerWarning item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="CompilerWarning"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="CompilerWarning"/> elements to be added to the end of the collection.</param> 
        public void AddRange(CompilerWarning[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="CompilerWarningCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="CompilerWarningCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(CompilerWarningCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="CompilerWarning"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="CompilerWarning"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(CompilerWarning item) {
            return base.List.Contains(item);
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(CompilerWarning[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="CompilerWarning"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="CompilerWarning"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="CompilerWarning"/>. If the <see cref="CompilerWarning"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(CompilerWarning item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="CompilerWarning"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="CompilerWarning"/> to insert.</param>
        public void Insert(int index, CompilerWarning item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="CompilerWarningEnumerator"/> for the entire collection.
        /// </returns>
        public new CompilerWarningEnumerator GetEnumerator() {
            return new CompilerWarningEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="CompilerWarning"/> to remove from the collection.</param>
        public void Remove(CompilerWarning item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="CompilerWarning"/> elements of a <see cref="CompilerWarningCollection"/>.
    /// </summary>
    public class CompilerWarningEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerWarningEnumerator"/> class
        /// with the specified <see cref="CompilerWarningCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal CompilerWarningEnumerator(CompilerWarningCollection arguments) {
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
        public CompilerWarning Current {
            get { return (CompilerWarning) _baseEnumerator.Current; }
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
