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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Tomas Restrepo (tomasr@mvps.org)

namespace SourceForge.NAnt {

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;

    public class PropertyDictionary : DictionaryBase {

        /// <summary>
        /// Maintains a list of the property names that are readonly.
        /// </summary>
        StringCollection _readOnlyProperties = new StringCollection();

        /// <summary>
        /// Adds a property that cannot be changed.
        /// </summary>
        /// <remarks>
        /// Properties added with this method can never be changed.  Note that
        /// they are removed if the <c>Clear</c> method is called.
        /// </remarks>
        /// <param name="name">Name of property</param>
        /// <param name="value">Value of property</param>
        public virtual void AddReadOnly(string name, string value) {
            if (!_readOnlyProperties.Contains(name)) {
                _readOnlyProperties.Add(name);
                Dictionary.Add(name, value);
            }
        }

        /// <summary>
        /// Adds a property to the collection.
        /// </summary>
        /// <param name="name">Name of property</param>
        /// <param name="value">Value of property</param>
        public virtual void Add(string name, string value) {
            if (!_readOnlyProperties.Contains(name)) {
                Dictionary.Add(name, value);
            }
        }

        public virtual void SetValue(string name, string value) {
            if (!_readOnlyProperties.Contains(name)) {
                Dictionary[name] = value;
            } 
        }

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
                  throw new BuildException(String.Format("Property '{0}' is read-only!", name));
                }
*/
            }
        }

        /// <summary>
        /// Returns true if a property is listed as read only
        /// </summary>
        /// <param name="name">Property to check</param>
        /// <returns>true if readonly, false otherwise</returns>
        public virtual bool IsReadOnlyProperty(string name) {
           return _readOnlyProperties.Contains(name);
        }

        protected override void OnClear() {
            _readOnlyProperties.Clear();
        }

        /// <summary>
        /// Inherits Properties from an existing property
        /// dictionary Instance
        /// </summary>
        /// <param name="source">Property list to inherit</param>
        /// <param name="excludes">The list of properties to exclude during inheritance</param>
        public virtual void Inherit(PropertyDictionary source, StringCollection excludes) {
            foreach ( DictionaryEntry entry in source.Dictionary ) {
                if (excludes != null && excludes.Contains((string)entry.Key)) {
                    continue;
                }

                Dictionary[entry.Key] = entry.Value;
                if ( source.IsReadOnlyProperty((string)entry.Key) ) {
                    _readOnlyProperties.Add((string)entry.Key);
                }
            }
        }

        /// <summary>
        /// Expands a string from known properties
        /// </summary>
        /// <param name="input">The string with replacement tokens</param>
        /// <returns>The expanded and replaced string</returns>
        public string ExpandProperties(string input) {
            // Moved from Project.cs by Tomas Restrepo
            string output = input;
            if (input != null) {
                const string pattern = @"\$\{([^\}]*)\}";
                foreach (Match m in Regex.Matches(input, pattern)) {
                    if (m.Length > 0) {

                        string token         = m.ToString();
                        string propertyName  = m.Groups[1].Captures[0].Value;
                        string propertyValue = this[propertyName];

                        if (propertyValue != null) {
                            output = output.Replace(token, propertyValue);
                        }
                        else {
                            throw new BuildException(String.Format("Property '{0}' has not been set!", propertyName));
                        }
                    }
                }
            }
            return output;
        }

    
    }
}
