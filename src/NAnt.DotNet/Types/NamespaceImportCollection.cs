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
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Base class for collections that needs to be globally referencable.
    /// </summary>
    public abstract class DataTypeCollectionBase : DataTypeBase, ICollection {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeCollectionBase" />
        /// class.
        /// </summary>
        protected DataTypeCollectionBase() {
            _list = new ArrayList();
        }

        #endregion Protected Instance Constructors

        #region ICollection Members

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

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that can iterate through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator" /> that can be used to iterate through 
        /// the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return List.GetEnumerator();
        }

        #endregion

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
        /// Removes an item at a specific index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index) {
            RangeCheck(index);
            List.RemoveAt(index);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear() {
            List.Clear();
        }

        #endregion Implementation of IList

        #region Protected Instance Properties

        /// <summary>
        /// Gets the list of elements contained in the 
        /// <see cref="DataTypeCollectionBase" /> instance.
        /// </summary>
        /// <value>
        /// An <see cref="ArrayList" /> containing the elements of the 
        /// collection.
        /// </value>
        protected ArrayList List {
            get { return _list; }
        }

        /// <summary>
        /// Gets the <see cref="Type" /> of the items in this collection.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of the items in this collection.
        /// </value>
        protected abstract Type ItemType {
            get;
        }

        #endregion Protected Instance Properties

        #region Private Instance Methods

        /// <summary>
        /// Used by methods that take <see cref="object" /> instances as argument
        /// to verify whether the instance is valid for the collection class.
        /// </summary>
        /// <param name="value">The instance to verify.</param>
        protected void ValidateType(object value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }

            if (!this.ItemType.IsInstanceOfType(value)) {
                throw new ArgumentException ("Specified value is not an instance"
                    + " of " + this.ItemType.FullName + ".");
            }
        }

        /// <summary>
        /// Checks whether the specified index is within the range of this
        /// collection.
        /// </summary>
        /// <param name="index">The index to check.</param>
        protected void RangeCheck(int index) {
            if (index < 0 || index >= Count) {
                throw new ArgumentOutOfRangeException("index", index, "Index "
                    + "must be greater than or equal to zero, and less than "
                    + "the number of items in the collection.");
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private ArrayList _list;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Contains a collection of <see cref="NamespaceImport" /> items.
    /// </summary>
    /// <example>
    ///   <para>Define a reference with name &quot;system.imports&quot;.</para>
    ///   <code>
    ///     <![CDATA[
    /// <namespaceimports id="system.imports">
    ///     <import namespace="System" />
    ///     <import namespace="System.Data" />
    /// </namespaceimports>
    ///     ]]>
    ///   </code>
    ///   <para>Use the predefined set of imports to compile a VB.NET assembly.</para>
    ///   <code>
    ///     <![CDATA[
    /// <vbc target="exe" output="HelloWorld.exe" rootnamespace="HelloWorld">
    ///     <imports refid="system.imports" />
    ///     <sources>
    ///         <include name="**/*.vb" />
    ///     </sources>
    ///     <references>
    ///         <include name="System.dll" />
    ///         <include name="System.Data.dll" />
    ///     </references>
    /// </vbc>
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [ElementName("namespaceimports")]
    public class NamespaceImportCollection : DataTypeCollectionBase, IList {
        #region Public Instance Properties

        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="NamespaceImportEnumerator"/> for the entire collection.
        /// </returns>
        public NamespaceImportEnumerator GetEnumerator() {
            return new NamespaceImportEnumerator(this);
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public NamespaceImport this[int index] {
            get { 
                RangeCheck(index);
                return (NamespaceImport) List[index];
            }
            set {
                this.RangeCheck(index);
                List[index] = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="NamespaceImport"/> with the specified namespace.
        /// </summary>
        /// <param name="value">The namespace of the <see cref="NamespaceImport"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public NamespaceImport this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (NamespaceImport NamespaceImport in base.List) {
                        if (value.Equals(NamespaceImport.Namespace)) {
                            return NamespaceImport;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Object

        /// <summary>
        /// Returns a comma-delimited list of namespace imports.
        /// </summary>
        /// <returns>
        /// A comma-delimited list of namespace imports, or an empty 
        /// <see cref="string" /> if there are no namespace imports.
        /// </returns>
        /// <remarks>
        /// Each namespace import is quoted individually.
        /// </remarks>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            foreach (NamespaceImport import in base.List) {
                if (import.IfDefined && !import.UnlessDefined) {
                    // users might using a single NamespaceImport element to 
                    // import multiple namespaces
                    string[] imports = import.Namespace.Split(',');
                    foreach (string ns in imports) {
                        // add comma delimited if its not the first import
                        if (sb.Length > 0) {
                            sb.Append(',');
                        }

                        sb.AppendFormat("\"{0}\"", ns);
                    }
                }
            }

            return sb.ToString();
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
            get { return typeof(NamespaceImport); }
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
                this[index] = (NamespaceImport) value;
            }
        }

        /// <summary>
        /// Inserts a <see cref="NamespaceImport" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="NamespaceImport"/> to insert.</param>
        void IList.Insert(int index, object value) {
            ValidateType(value);
            Insert(index, (NamespaceImport) value);
        }

        /// <summary>
        /// Removes the specified <see cref="NamespaceImport"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to remove from the collection.</param>
        void IList.Remove(object value) {
            ValidateType(value);
            Remove((NamespaceImport) value);
        }

        /// <summary>
        /// Determines whether a <see cref="NamespaceImport"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        bool IList.Contains(object value) {
            ValidateType(value);
            return List.Contains((NamespaceImport) value);
        }

        /// <summary>
        /// Gets the location of a <see cref="NamespaceImport"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="NamespaceImport" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="NamespaceImport"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        int IList.IndexOf(object value) {
            ValidateType(value);
            return IndexOf((NamespaceImport) value);
        }

        /// <summary>
        /// Adds a <see cref="NamespaceImport"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        int IList.Add(object value) {
            ValidateType(value);
            return Add((NamespaceImport) value);
        }

        #endregion

        #region Public Instance Methods

        /// <summary>
        /// Adds the items of a <see cref="NamespaceImportCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="NamespaceImportCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(NamespaceImportCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds a <see cref="NamespaceImport"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new item was inserted.
        /// </returns>
        [BuildElement("import")]
        public int Add(NamespaceImport value) {
            return List.Add(value);
        }

        /// <summary>
        /// Inserts a <see cref="NamespaceImport" /> into the collection at the
        /// specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
        /// <param name="value">The <see cref="NamespaceImport"/> to insert.</param>
        public void Insert(int index, NamespaceImport value) {
            List.Insert(index, value);
        }

        /// <summary>
        /// Removes the specified <see cref="NamespaceImport"/> from the
        /// collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to remove from the collection.</param>
        public void Remove(NamespaceImport value) {
            List.Remove(value);
        }

        /// <summary>
        /// Determines whether a <see cref="NamespaceImport"/> is in the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="value" /> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(NamespaceImport value) {
            return List.Contains(value);
        }

        /// <summary>
        /// Gets the location of a <see cref="NamespaceImport"/> in the collection.
        /// </summary>
        /// <param name="value">The <see cref="NamespaceImport"/> object to locate.</param> 
        /// <returns>
        /// The zero-based location of the <see cref="NamespaceImport" /> in the
        /// collection.
        /// </returns>
        /// <remarks>
        /// If the <see cref="NamespaceImport"/> is not currently a member of 
        /// the collection, -1 is returned.
        /// </remarks>
        public int IndexOf(NamespaceImport value) {
            return List.IndexOf(value);
        }

        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="NamespaceImport"/> items of a <see cref="NamespaceImportCollection"/>.
    /// </summary>
    public class NamespaceImportEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceImportEnumerator"/> class
        /// with the specified <see cref="NamespaceImportCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal NamespaceImportEnumerator(NamespaceImportCollection arguments) {
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
        public NamespaceImport Current {
            get { return (NamespaceImport) _baseEnumerator.Current; }
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
