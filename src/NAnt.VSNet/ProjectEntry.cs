// NAnt - A .NET build tool
// Copyright (C) 2001-2008 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net.be)

using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

using NAnt.Core;

namespace NAnt.VSNet {
    public class ProjectEntry {
        #region Private Instance Fields

        private readonly string _guid;
        private readonly string _path;
        private ProjectBase _project;
        private ConfigurationMap _buildConfigurations;

        #endregion Private Instance Fields

        #region Public Instance Constructors
            
        public ProjectEntry(string guid, string path) {
            if (guid == null) {
                throw new ArgumentNullException("guid");
            }
            if (path == null) {
                throw new ArgumentNullException("path");
            }

            _guid = guid;
            _path = path;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string Guid {
            get { return _guid; }
        }

        public string Path {
            get { return _path; }
        }

        /// <summary>
        /// Gets or sets the in memory representation of the project.
        /// </summary>
        /// <value>
        /// The in memory representation of the project, or <see langword="null" />
        /// if the project is not (yet) loaded.
        /// </value>
        /// <remarks>
        /// This property will always be <see langword="null" /> for
        /// projects that are not supported.
        /// </remarks>
        public ProjectBase Project {
            get { return _project; }
            set {
                if (value != null) {
                    // if the project GUID from the solution file doesn't match the 
                    // project GUID from the project file we will run into problems. 
                    // Alert the user to fix this as it is basically a corruption 
                    // probably caused by user manipulation of the solution file
                    // i.e. copy and paste
                    if (string.Compare(Guid, value.Guid, true, CultureInfo.InvariantCulture) != 0) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "GUID corruption detected for project '{0}'. GUID values" 
                            + " in project file and solution file do not match ('{1}'" 
                            + " and '{2}'). Please correct this manually.", value.Name, 
                            value.Guid, Guid), Location.UnknownLocation);
                    }
                }
                _project = value; 
            }
        }

        /// <summary>
        /// Return a mapping between the configurations defined in the
        /// solution file and the project build configurations.
        /// </summary>
        /// <value>
        /// Mapping between configurations defined in the solution file
        /// and the project build configurations, or <see langword="null" />
        /// if the project is not defined in a solution file.
        /// </value>
        /// <remarks>
        /// This mapping only includes project build configurations that
        /// are configured to be built for a given solution configuration.
        /// </remarks>
        public ConfigurationMap BuildConfigurations {
            get { return _buildConfigurations; }
            set { _buildConfigurations = value; }
        }

        #endregion Public Instance Properties
    }

    /// <summary>
    /// Contains a collection of <see cref="ProjectEntry" /> elements.
    /// </summary>
    [Serializable()]
    public class ProjectEntryCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class.
        /// </summary>
        public ProjectEntryCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class
        /// with the specified <see cref="ProjectEntryCollection"/> instance.
        /// </summary>
        public ProjectEntryCollection(ProjectEntryCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class
        /// with the specified array of <see cref="ProjectEntry"/> instances.
        /// </summary>
        public ProjectEntryCollection(ProjectEntry[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [IndexerName("Item")]
        public ProjectEntry this[int index] {
            get { return (ProjectEntry) base.List[index]; }
            set { base.List[index] = value; }
        }

        /// <summary>
        /// Gets the <see cref="ProjectEntry"/> with the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the <see cref="ProjectEntry"/> to get.</param>
        /// <remarks>
        /// Performs a case-insensitive lookup.
        /// </remarks>
        [IndexerName("Item")]
        public ProjectEntry this[string guid] {
            get {
                if (guid != null) {
                    // try to locate instance by guid (case-insensitive)
                    for (int i = 0; i < base.Count; i++) {
                        ProjectEntry projectEntry = (ProjectEntry) base.List[i];
                        if (string.Compare(projectEntry.Guid, guid, true, CultureInfo.InvariantCulture) == 0) {
                            return projectEntry;
                        }
                    }
                }
                return null;
            }
            set {
                if (guid == null) {
                    throw new ArgumentNullException ("guid");
                }
                if (value == null) {
                    throw new ArgumentNullException ("value");
                }

                if (!Contains (guid)) {
                    Add(value);
                }
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="ProjectEntry"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectEntry"/> to be added to the end of the collection.</param> 
        /// <returns>
        /// The position into which the new element was inserted.
        /// </returns>
        public int Add(ProjectEntry item) {
            if (item == null) {
                throw new ArgumentNullException("item");
            }

            // fail if a project with the same GUID exists in the collection
            ProjectEntry existingEntry = this[item.Guid];
            if (existingEntry != null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The GUIDs of projects \"{0}\" and \"{1}\" are identical."
                    + " Please correct this manually.", item.Path,
                    existingEntry.Path), Location.UnknownLocation);
            }

            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProjectEntry"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="ProjectEntry"/> elements to be added to the end of the collection.</param> 
        public void AddRange(ProjectEntry[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProjectEntryCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="ProjectEntryCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(ProjectEntryCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="ProjectEntry"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectEntry"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(ProjectEntry item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="ProjectEntry"/> with the specified
        /// GUID is in the collection, using a case-insensitive lookup.
        /// </summary>
        /// <param name="value">The GUID to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if a <see cref="ProjectEntry" /> with GUID 
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
        public void CopyTo(ProjectEntry[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="ProjectEntry"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectEntry"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="ProjectEntry"/>. If the <see cref="ProjectEntry"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(ProjectEntry item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="ProjectEntry"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="ProjectEntry"/> to insert.</param>
        public void Insert(int index, ProjectEntry item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ProjectEntryEnumerator"/> for the entire collection.
        /// </returns>
        public new ProjectEntryEnumerator GetEnumerator() {
            return new ProjectEntryEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProjectEntry"/> to remove from the collection.</param>
        public void Remove(ProjectEntry item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="ProjectEntry"/> elements of a <see cref="ProjectEntryCollection"/>.
    /// </summary>
    public class ProjectEntryEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectEntryEnumerator"/> class
        /// with the specified <see cref="ProjectEntryCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal ProjectEntryEnumerator(ProjectEntryCollection arguments) {
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
        public ProjectEntry Current {
            get { return (ProjectEntry) _baseEnumerator.Current; }
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
