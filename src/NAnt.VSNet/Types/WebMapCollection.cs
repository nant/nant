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
using System.Globalization;

namespace NAnt.VSNet.Types {
    /// <summary>
    /// Contains a strongly typed collection of <see cref="WebMap" /> 
    /// objects.
    /// </summary>
    [Serializable()]
    public class WebMapCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebMapCollection"/> class.
        /// </summary>
        public WebMapCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WebMapCollection"/> class
        /// with the specified <see cref="WebMapCollection"/> instance.
        /// </summary>
        public WebMapCollection(WebMapCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WebMapCollection"/> class
        /// with the specified array of <see cref="WebMap"/> instances.
        /// </summary>
        public WebMapCollection(WebMap[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public WebMap this[int index] {
            get { return ((WebMap)(base.List[index])); }
            set { base.List[index] = value; }
        }

        /// <summary>
        /// Gets the <see cref="WebMap"/> with the specified value.
        /// </summary>
        /// <param name="value">The value of the <see cref="WebMap"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public WebMap this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (WebMap WebMap in base.List) {
                        if (string.Compare(WebMap.Url, value, !WebMap.CaseSensitive, CultureInfo.InvariantCulture) == 0) {
                            return WebMap;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Find the best matching <see cref="WebMap"/> for the given Uri.
        /// </summary>
        /// <param name="uri">The value to match against the <see cref="WebMap" /> objects in the collection.</param>
        public string FindBestMatch(string uri)
        {
            string bestMatch = null;
            int bestMatchLength = Int32.MinValue;

            foreach (WebMap webMap in base.List) {
                if (!webMap.IfDefined || webMap.UnlessDefined)
                    continue;

                string testSubject = webMap.CaseSensitive ? uri : uri.ToUpper(CultureInfo.InvariantCulture);
                string testTarget = webMap.CaseSensitive ? webMap.Url : webMap.Url.ToUpper(CultureInfo.InvariantCulture);

                if (testSubject.StartsWith(testTarget) && testTarget.Length > bestMatchLength) {
                    bestMatchLength = testTarget.Length;
                    bestMatch = webMap.Path.FullName + uri.Substring(testTarget.Length);
                }
            }

            return bestMatch;
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="WebMap"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="WebMap"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(WebMap item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="WebMap"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="WebMap"/> elements to be added to the end of the collection.</param> 
        public void AddRange(WebMap[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="WebMapCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="WebMapCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(WebMapCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="WebMap"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="WebMap"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(WebMap item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="WebMap"/> with the specified
        /// value is in the collection.
        /// </summary>
        /// <param name="value">The argument value to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="WebMap" /> with value 
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
        public void CopyTo(WebMap[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="WebMap"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="WebMap"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="WebMap"/>. If the <see cref="WebMap"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(WebMap item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="WebMap"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="WebMap"/> to insert.</param>
        public void Insert(int index, WebMap item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="WebMapEnumerator"/> for the entire collection.
        /// </returns>
        public new WebMapEnumerator GetEnumerator() {
            return new WebMapEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="WebMap"/> to remove from the collection.</param>
        public void Remove(WebMap item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="WebMap"/> elements of a <see cref="WebMapCollection"/>.
    /// </summary>
    public class WebMapEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebMapEnumerator"/> class
        /// with the specified <see cref="WebMapCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal WebMapEnumerator(WebMapCollection arguments) {
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
        public WebMap Current {
            get { return (WebMap) _baseEnumerator.Current; }
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
