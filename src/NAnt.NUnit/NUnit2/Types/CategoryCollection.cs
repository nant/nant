// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Contains a collection of <see cref="Category" /> elements.
    /// </summary>
    [Serializable()]
    public class CategoryCollection : Collection<Category> {

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class.
        /// </summary>
        public CategoryCollection()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="NAnt.NUnit2.Types.CategoryCollection"/> class as a wrapper for
        /// the specified list.
        /// </summary>
        /// <param name='value'>
        /// The list that is wrapped by the newly created instance.
        /// </param>
        public CategoryCollection(IList<Category> value)
            : base(value)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class
        /// with the specified <see cref="CategoryCollection"/> instance.
        /// </summary>
        /// <param name='value'>
        /// The collection to use to initialize the new instance with.
        /// </param>
        public CategoryCollection(CategoryCollection value)
            : base(value)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryCollection"/> class
        /// with the specified array of <see cref="Category"/> instances.
        /// </summary>
        /// <param name='value'>
        /// The collection to use to initialize the new instance with.
        /// </param>
        public CategoryCollection(Category[] value)
            : base()
        {
            AddRange(value);
        }

        #endregion Public Instance Constructors

        #region Override implementation of Object

        /// <summary>
        /// Returns a comma-delimited list of categories.
        /// </summary>
        /// <returns>
        /// A comma-delimited list of categories, or an empty 
        /// <see cref="string" /> if there are no categories.
        /// </returns>
        public override string ToString() {
            List<string> categories = new List<string>(this.Count);
            foreach (Category category in this)
            {
                if (category.IfDefined && !category.UnlessDefined)
                {
                    categories.Add(category.CategoryName);
                }
            }
            return String.Join(",", categories.ToArray());
        }

        #endregion Override implementation of Object
        
        #region Public Instance Properties

        /// <summary>
        /// Gets the <see cref="Category"/> with the specified name.
        /// </summary>
        /// <param name="value">The name of the <see cref="Category"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public Category this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (Category category in this) {
                        if (value.Equals(category.CategoryName)) {
                            return category;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Adds the elements of a <see cref="Category"/> array to the end of the
        /// collection.
        /// </summary>
        /// <param name="items">
        /// The array of <see cref="Category"/> elements to be added to the end of
        /// the collection.
        /// </param>
        public void AddRange(IEnumerable<Category> items)
        {
            foreach (Category item in items)
            {
                Add(item);
            }
        }
        
        #endregion Public Instance Methods
    }
}