// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Tomas Restrepo (tomasr@mvps.org)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NAnt.Core {
    [Serializable()]
    public class PropertyDictionary : DictionaryBase {
        #region Public Instance Properties

        /// <summary>
        /// Indexer property. 
        /// </summary>
        public virtual string this[string name] {
            get {
                string value = (string) Dictionary[(object) name];

                if (IsDynamicProperty(name)) {
                    return ExpandProperties(value, Location.UnknownLocation);
                } else {
                    return value;
                }
            }
            set {
                if (!IsReadOnlyProperty(name)) {
                    Dictionary[name] = value;
                } 
                /* // tomasr: Should this throw an error? I think so
                                else {
                                  throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Property '{0}' is read-only!", name));
                                }
                */
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Adds a property that cannot be changed.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <remarks>
        /// Properties added with this method can never be changed.  Note that
        /// they are removed if the <see cref="DictionaryBase.Clear" /> method is called.
        /// </remarks>
        public virtual void AddReadOnly(string name, string value) {
            if (!IsReadOnlyProperty(name)) {
                _readOnlyProperties.Add(name);
                Dictionary.Add(name, value);
            }
        }

        /// <summary>
        /// Marks a property as a property of which the value is expanded at 
        /// execution time.
        /// </summary>
        /// <param name="name">The name of the property to mark as dynamic.</param>
        public virtual void MarkDynamic(string name) {
            if (!IsDynamicProperty(name)) {
                _dynamicProperties.Add(name);
            }
        }

        /// <summary>
        /// Adds a property to the collection.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        public virtual void Add(string name, string value) {
            if (!IsReadOnlyProperty(name)) {
                Dictionary.Add(name, value);
            }
        }

        /// <summary>
        /// Sets the specified property to the given value.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <remarks>
        /// For read-only properties, the value will not be changed.
        /// </remarks>
        public virtual void SetValue(string name, string value) {
            if (!IsReadOnlyProperty(name)) {
                Dictionary[name] = value;
            } 
        }

        /// <summary>
        /// Determines whether the specified property is listed as read-only.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the property is listed as read-only; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool IsReadOnlyProperty(string name) {
            return _readOnlyProperties.Contains(name);
        }

        /// <summary>
        /// Determines whether the specified property is listed as dynamic.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the property is listed as dynamic; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool IsDynamicProperty(string name) {
            return _dynamicProperties.Contains(name);
        }

        /// <summary>
        /// Inherits properties from an existing property dictionary Instance.
        /// </summary>
        /// <param name="source">Property list to inherit.</param>
        /// <param name="excludes">The list of properties to exclude during inheritance.</param>
        public virtual void Inherit(PropertyDictionary source, StringCollection excludes) {
            foreach (DictionaryEntry entry in source.Dictionary) {
                string propertyName = (string) entry.Key;

                if (excludes != null && excludes.Contains(propertyName)) {
                    continue;
                }

                // do not overwrite an existing read-only property
                if (IsReadOnlyProperty(propertyName)) {
                    continue;
                }

                // add property to dictionary
                Dictionary[propertyName] = entry.Value;

                // if property is readonly, add to collection of readonly properties
                if (source.IsReadOnlyProperty(propertyName)) {
                    _readOnlyProperties.Add(propertyName);
                }

                // if property is dynamic, add to collection of dynamic properties
                // if it was not already in that collection
                if (source.IsDynamicProperty(propertyName) && !IsDynamicProperty(propertyName)) {
                    _dynamicProperties.Add(propertyName);
                }
            }
        }

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The replacement tokens.</param>
        /// <param name="location">The <see cref="Location" /> to pass through for any exceptions.</param>
        /// <returns>The expanded and replaced string.</returns>
        public string ExpandProperties(string input, Location location) {
            Hashtable state = new Hashtable();
            Stack visiting = new Stack();
            return ExpandProperties(input, location, state, visiting);
        }

        /// <summary>
        /// Determines whether a property already exists.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <see langword="true" /> if the specified property already exists; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool Contains(string name) {
            return Dictionary.Contains(name) || IsReadOnlyProperty(name);
        }

        #endregion Public Instance Methods

        #region Override implementation of DictionaryBase

        protected override void OnClear() {
            _readOnlyProperties.Clear();
        }

        #endregion Override implementation of DictionaryBase

        #region Private Instance Methods

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The replacement tokens.</param>
        /// <param name="location">The <see cref="Location" /> to pass through for any exceptions.</param>
        /// <param name="state">A mapping from properties to states. The states in question are "VISITING" and "VISITED". Must not be <see langword="null" />.</param>
        /// <param name="visiting">A stack of properties which are currently being visited. Must not be <see langword="null" />.</param>
        /// <returns>The expanded and replaced string.</returns>
        private string ExpandProperties(string input, Location location, Hashtable state, Stack visiting) {
            string output = input;
            if (input != null) {
                const string pattern = @"\$\{([^\}]*)\}";
                foreach (Match m in Regex.Matches(input, pattern)) {
                    if (m.Length > 0) {
                        string token = m.ToString();
                        string propertyName = m.Groups[1].Captures[0].Value;

                        string currentState = (string) state[propertyName];

                        // check for circular references
                        if (currentState == PropertyDictionary.Visiting) {
                            // Currently visiting this node, so have a cycle
                            throw CreateCircularException(propertyName, visiting);
                        }

                        visiting.Push(propertyName);

                        state[propertyName] = PropertyDictionary.Visiting;

                        string propertyValue = (string) Dictionary[(object) propertyName];

                        if (propertyValue != null) {
                            if (IsDynamicProperty(propertyName)) {
                                propertyValue = ExpandProperties(propertyValue, location, state, visiting);
                            }

                            output = output.Replace(token, propertyValue);
                        } else {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Property '{0}' has not been set.", propertyName), location);
                        }

                        visiting.Pop();
                        state[propertyName] = PropertyDictionary.Visited;
                    }
                }
            }
            return output;
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Builds an appropriate exception detailing a specified circular
        /// reference.
        /// </summary>
        /// <param name="end">The property reference to stop at. Must not be <see langword="null" />.</param>
        /// <param name="stack">A stack of property references. Must not be <see langword="null" />.</param>
        /// <returns>
        /// A <see cref="BuildException" /> detailing the specified circular 
        /// dependency.
        /// </returns>
        private static BuildException CreateCircularException(string end, Stack stack) {
            StringBuilder sb = new StringBuilder("Circular property reference: ");
            sb.Append(end);

            string c;

            do {
                c = (string) stack.Pop();
                sb.Append(" <- ");
                sb.Append(c);
            } while (!c.Equals(end));

            return new BuildException(sb.ToString());
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        /// <summary>
        /// Maintains a list of the property names that are readonly.
        /// </summary>
        private StringCollection _readOnlyProperties = new StringCollection();

        /// <summary>
        /// Maintains a list of the property names of which the value is expanded
        /// on usage, not at initalization.
        /// </summary>
        private StringCollection _dynamicProperties = new StringCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// Constant for the "visiting" state, used when traversing a DFS of 
        /// property references.
        /// </summary>
        private const string Visiting = "VISITING";

        /// <summary>
        /// Constant for the "visited" state, used when travesing a DFS of 
        /// property references.
        /// </summary>
        private const string Visited = "VISITED";

        #endregion Private Static Fields
    }
}
