// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

namespace NAnt.Core.Util {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="CommandLineArgument"/> objects.
    /// </summary>
    [Serializable]
    public class CommandLineArgumentCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentCollection"/> class.
        /// </summary>
        public CommandLineArgumentCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentCollection"/> class
        /// with the specified <see cref="CommandLineArgumentCollection"/> instance.
        /// </summary>
        public CommandLineArgumentCollection(CommandLineArgumentCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentCollection"/> class
        /// with the specified array of <see cref="CommandLineArgument"/> instances.
        /// </summary>
        public CommandLineArgumentCollection(CommandLineArgument[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public CommandLineArgument this[int index] {
            get {return ((CommandLineArgument)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="CommandLineArgument"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="CommandLineArgument"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public CommandLineArgument this[string name] {
            get {
                if (name != null) {
                    // Try to locate instance using LongName
                    foreach (CommandLineArgument CommandLineArgument in base.List) {
                        if (name.Equals(CommandLineArgument.LongName)) {
                            return CommandLineArgument;
                        }
                    }

                    // Try to locate instance using ShortName
                    foreach (CommandLineArgument CommandLineArgument in base.List) {
                        if (name.Equals(CommandLineArgument.ShortName)) {
                            return CommandLineArgument;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="CommandLineArgument"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="CommandLineArgument"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(CommandLineArgument item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="CommandLineArgument"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="CommandLineArgument"/> elements to be added to the end of the collection.</param> 
        public void AddRange(CommandLineArgument[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="CommandLineArgumentCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="CommandLineArgumentCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(CommandLineArgumentCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="CommandLineArgument"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="CommandLineArgument"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(CommandLineArgument item) {
            return base.List.Contains(item);
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(CommandLineArgument[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="CommandLineArgument"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="CommandLineArgument"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="CommandLineArgument"/>. If the <see cref="CommandLineArgument"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(CommandLineArgument item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="CommandLineArgument"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="CommandLineArgument"/> to insert.</param>
        public void Insert(int index, CommandLineArgument item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="CommandLineArgumentEnumerator"/> for the entire collection.
        /// </returns>
        public new CommandLineArgumentEnumerator GetEnumerator() {
            return new CommandLineArgumentEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="CommandLineArgument"/> to remove from the collection.</param>
        public void Remove(CommandLineArgument item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="CommandLineArgument"/> elements of a <see cref="CommandLineArgumentCollection"/>.
    /// </summary>
    public class CommandLineArgumentEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentEnumerator"/> class
        /// with the specified <see cref="CommandLineArgumentCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal CommandLineArgumentEnumerator(CommandLineArgumentCollection arguments) {
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
        public CommandLineArgument Current {
            get { return (CommandLineArgument) _baseEnumerator.Current; }
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
