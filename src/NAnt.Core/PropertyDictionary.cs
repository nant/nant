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

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NAnt.Core {
    public class PropertyDictionary : DictionaryBase {
        #region Public Instance Properties

        /// <summary>
        /// Indexer property. 
        /// </summary>
        public virtual string this[string name] {
            get { return (string) Dictionary[(object) name]; }
            set {
                if (!_readOnlyProperties.Contains(name)) {
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
            if (!_readOnlyProperties.Contains(name)) {
                _readOnlyProperties.Add(name);
                Dictionary.Add(name, value);
            }
        }

        /// <summary>
        /// Adds a property to the collection.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to assign to the property.</param>
        public virtual void Add(string name, string value) {
            if (!_readOnlyProperties.Contains(name)) {
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
            if (!_readOnlyProperties.Contains(name)) {
                Dictionary[name] = value;
            } 
        }

        /// <summary>
        /// Determines whether the specified property is listed as read-only.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <c>true</c> if the property is listed as read-only; otherwise, 
        /// <c>false</c>.
        /// </returns>
        public virtual bool IsReadOnlyProperty(string name) {
            return _readOnlyProperties.Contains(name);
        }

        /// <summary>
        /// Inherits properties from an existing property dictionary Instance.
        /// </summary>
        /// <param name="source">Property list to inherit.</param>
        /// <param name="excludes">The list of properties to exclude during inheritance.</param>
        public virtual void Inherit(PropertyDictionary source, StringCollection excludes) {
            foreach (DictionaryEntry entry in source.Dictionary) {
                if (excludes != null && excludes.Contains((string) entry.Key)) {
                    continue;
                }

                Dictionary[entry.Key] = entry.Value;
                if (source.IsReadOnlyProperty((string)entry.Key)) {
                    _readOnlyProperties.Add((string)entry.Key);
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
            string output = input;
            if (input != null) {
                const string pattern = @"\$\{([^\}]*)\}";
                foreach (Match m in Regex.Matches(input, pattern)) {
                    if (m.Length > 0) {
                        string token = m.ToString();
                        string propertyName = m.Groups[1].Captures[0].Value;
                        string propertyValue = this[propertyName];

                        if (propertyValue != null) {
                            output = output.Replace(token, propertyValue);
                        } else {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Property '{0}' has not been set.", propertyName), location);
                        }
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Determines whether a property already exists.
        /// </summary>
        /// <param name="name">The name of the property to check.</param>
        /// <returns>
        /// <c>true</c> if the specified property already exists; otherwise,
        /// <c>false</c>.
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

        #region Private Instance Fields

        /// <summary>
        /// Maintains a list of the property names that are readonly.
        /// </summary>
        private StringCollection _readOnlyProperties = new StringCollection();

        #endregion Private Instance Fields
    }
}
