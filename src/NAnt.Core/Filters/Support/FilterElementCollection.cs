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


using System;
using System.Collections;
using NAnt.Core.Filters;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Sorted collection of filter elements
    /// </summary>
    /// <remarks>
    /// Sorted collection of filter elements.  Sorting is provided by the base class <see cref="SortedList"/>.
    /// </remarks>
    [Serializable]
    public class FilterElementCollection : SortedList {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public FilterElementCollection() {}


        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public FilterElement this[int index] {
            get { return ((FilterElement) base[index]); }
            set { base[index] = (FilterElement) value; }
        }

        /// <summary>
        /// Get a FilterElement given its physical index in the collection
        /// </summary>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public new FilterElement GetByIndex(int currentIndex) {
            return (FilterElement) base.GetByIndex(currentIndex);
        }

        /// <summary>
        /// Adds a <see cref="FilterElement"/> to the end of the collection.
        /// </summary>
        /// <param name="value">The The <see cref="FilterElement"/> index at which the value has been added.</param>
        /// <returns></returns>
        public void Add(FilterElement value) {
            try {
                base.Add(value.Order, value);
            } catch (ArgumentException e) {
                string message = "There is already a filter at location " + value.Order + " : Type = " + value.GetType().ToString();

                value.Log(Level.Error, message);
                throw new BuildException(message, value.Location, e);
            }
        }

        /// <summary>
        /// Returns the index at which a particular <see cref="FilterElement"/> resides in the collection.
        /// </summary>
        /// <param name="value"><see cref="FilterElement"/> to locate</param>
        /// <returns>Index</returns>
        public int IndexOf(FilterElement value) {
            return base.IndexOfKey(value.Order);
        }


        /// <summary>
        /// Removes a <see cref="FilterElement"/> for the collection gince its reference.
        /// </summary>
        /// <param name="value"><see cref="FilterElement"/> to remove</param>
        public void Remove(FilterElement value) {
            base.Remove(value.Order);
        }

        /// <summary>
        /// Indicates is the collection contains a particular <see cref="FilterElement"/>
        /// </summary>
        /// <param name="value"><see cref="FilterElement"/> to locate</param>
        /// <returns>true if the <see cref="FilterElement"/> is contained in the collection</returns>
        public bool Contains(FilterElement value) {
            return base.Contains(value.Order);
        }
    }
}
