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
        #region public Constructors
        
        public PropertyDictionary() {
        }

        public PropertyDictionary(Project project){
            _project = project;
        }

        #endregion public Constructors

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
                ValidatePropertyName(name, Location.UnknownLocation);
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
                ValidatePropertyName(name, Location.UnknownLocation);
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
                ValidatePropertyName(name, Location.UnknownLocation);
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
                ValidatePropertyName(name, Location.UnknownLocation);
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
                ValidatePropertyName(name, Location.UnknownLocation);
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
                ValidatePropertyName(propertyName, Location.UnknownLocation);
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

        protected override void OnInsert(Object key, Object value)  {
            string propertyName = key as string;
            if (propertyName == null)
                throw new ArgumentException("Property name must be a string.", "key");

            if (value != null) {
                if (!(value is string))
                    throw new ArgumentException("Property value must be a string, was " + value.GetType(), "value");
            } else {
                // TODO: verify this
                // throw new ArgumentException("Property value '" + propertyName + "' must not be null", "value");
                return;
            }

            ValidatePropertyName(propertyName, Location.UnknownLocation);
        }
        #endregion Override implementation of DictionaryBase

        #region Private Instance Methods

        public static void ValidatePropertyName(string propertyName, Location location) {
            const string propertyNamePattern = "^[_A-Za-z0-9][_A-Za-z0-9\\-.]*$";

            // validate property name
            //
            if (!Regex.IsMatch(propertyName, propertyNamePattern)) {
                throw new BuildException("Property name '" + propertyName + "' is invalid", location);
            }
            if (propertyName.EndsWith("-") || propertyName.EndsWith(".")) {
                // this additional rule helps simplify the regex pattern
                throw new BuildException("Property name '" + propertyName + "' is invalid", location);
            }
        }

        internal string GetPropertyValue(string propertyName) {
                return (string)Dictionary[propertyName];
        }

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The replacement tokens.</param>
        /// <param name="location">The <see cref="Location" /> to pass through for any exceptions.</param>
        /// <param name="state">A mapping from properties to states. The states in question are "VISITING" and "VISITED". Must not be <see langword="null" />.</param>
        /// <param name="visiting">A stack of properties which are currently being visited. Must not be <see langword="null" />.</param>
        /// <returns>The expanded and replaced string.</returns>
        internal string ExpandProperties(string input, Location location, Hashtable state, Stack visiting) {
            if (!DisableExpressionEvaluator) {
                return EvaluateEmbeddedExpressions(input, location, state, visiting);
            }
            
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
                        } else {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Property '{0}' has not been set.", propertyName), location);
                        }
                        output = output.Replace(token, propertyValue);

                        visiting.Pop();
                        state[propertyName] = PropertyDictionary.Visited;
                    }
                }
            }
            return output;
        }

        private string EvaluateEmbeddedExpressions(string input, Location location, Hashtable state, Stack visiting) {
            if (input == null) {
                return null;
            }

            if (input.IndexOf('$') < 0) {
                return input;
            }

            try {
                StringBuilder output = new StringBuilder(input.Length);

                ExpressionTokenizer tokenizer = new ExpressionTokenizer();
                ExpressionEvaluator eval = new ExpressionEvaluator(_project, this, location, state, visiting);

                tokenizer.IgnoreWhitespace = false;
                tokenizer.SingleCharacterMode = true;
                tokenizer.InitTokenizer(input);

                while (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                    if (tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Dollar) {
                        tokenizer.GetNextToken();
                        if (tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LeftCurlyBrace) {
                            tokenizer.IgnoreWhitespace = true;
                            tokenizer.SingleCharacterMode = false;
                            tokenizer.GetNextToken();

                            string val = Convert.ToString(eval.Evaluate(tokenizer), CultureInfo.InvariantCulture);
                            output.Append(val);
                            tokenizer.IgnoreWhitespace = false;

                            if (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightCurlyBrace) {
                                throw new ExpressionParseException("'}' expected", tokenizer.CurrentPosition.CharIndex);
                            }
                            tokenizer.SingleCharacterMode = true;
                            tokenizer.GetNextToken();
                        } else {
                            output.Append('$');
                            if (tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                                output.Append(tokenizer.TokenText);
                                tokenizer.GetNextToken();
                            }
                        }
                    } else {
                        output.Append(tokenizer.TokenText);
                        tokenizer.GetNextToken();
                    }
                }
                return output.ToString();
            } catch (ExpressionParseException ex) {
                StringBuilder errorMessage = new StringBuilder();
                string reformattedInput = input;
                reformattedInput = reformattedInput.Replace('\n', ' '); // replace CR, LF and TAB with a space
                reformattedInput = reformattedInput.Replace('\r', ' ');
                reformattedInput = reformattedInput.Replace('\t', ' ');

                errorMessage.Append("Error: ");
                errorMessage.Append(ex.Message);
                errorMessage.Append(Environment.NewLine);

                string label = "Expression: ";

                errorMessage.Append(label);
                errorMessage.Append(reformattedInput);
                errorMessage.Append(Environment.NewLine);

                int p0 = ex.StartPos;
                int p1 = ex.EndPos;

                if (p0 != -1 || p1 != -1) {
                    if (p1 == -1)
                        p1 = p0 + 1;

                    for (int i = 0; i < p0 + label.Length; ++i)
                        errorMessage.Append(' ');
                    for (int i = p0; i < p1; ++i)
                        errorMessage.Append('^');

                    errorMessage.Append(Environment.NewLine);
                }

                throw new BuildException(errorMessage.ToString(), location, null);
            }
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
        internal static BuildException CreateCircularException(string end, Stack stack) {
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

        Project _project = null;

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// A global flag to disable expression evaluator. Useful to revert to pre-EE 
        /// NAnt behaviour.
        /// </summary>
        internal static bool DisableExpressionEvaluator = false;

        /// <summary>
        /// Constant for the "visiting" state, used when traversing a DFS of 
        /// property references.
        /// </summary>
        internal const string Visiting = "VISITING";

        /// <summary>
        /// Constant for the "visited" state, used when travesing a DFS of 
        /// property references.
        /// </summary>
        internal const string Visited = "VISITED";

        #endregion Private Static Fields
    }
}
