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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    [FunctionSet("property", "NAnt")]
    public class ExpressionEvaluator : ExpressionEvalBase {
        #region Private Instance Fields

        private PropertyDictionary _properties;
        private Hashtable _state;
        private Stack _visiting;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionEvaluator"/> class.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="properties">The projects properties.</param>
        /// <param name="state">The state.</param>
        /// <param name="visiting">The visiting.</param>
        public ExpressionEvaluator(Project project, PropertyDictionary properties, Hashtable state, Stack visiting)
            : base(project) {
            _properties = properties;
            _state = state;
            _visiting = visiting;
        }

        #endregion Public Instance Constructors

        #region Override implementation of ExpressionEvalBase

        protected override object EvaluateProperty(string propertyName) {
            return GetPropertyValue(propertyName);
        }

        protected override object EvaluateFunction(MethodInfo methodInfo, object[] args) {
            try {
                if (methodInfo.IsStatic) {
                    return methodInfo.Invoke(null, args);
                } else if (methodInfo.DeclaringType.IsAssignableFrom(typeof(ExpressionEvaluator))) {
                    return methodInfo.Invoke(this, args);
                } else {
                    // create new instance.
                    ConstructorInfo constructor = methodInfo.DeclaringType.GetConstructor(new Type[] {typeof(Project), typeof(PropertyDictionary)});
                    object o = constructor.Invoke(new object[] {Project, _properties});

                    return methodInfo.Invoke(o, args);
                }
            } catch (TargetInvocationException ex) {
                if (ex.InnerException != null) {
                    // throw actual exception
                    throw ex.InnerException;
                }
                // re-throw exception
                throw;
            }
        }

        #endregion Override implementation of ExpressionEvalBase

        #region Public Instance Methods

        /// <summary>
        /// Gets the value of the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get the value of.</param>
        /// <returns>
        /// The value of the specified property.
        /// </returns>
        [Function("get-value")]
        public string GetPropertyValue(string propertyName) {
            if (_properties.IsDynamicProperty(propertyName)) {
                string currentState = (string)_state[propertyName];

                // check for circular references
                if (currentState == PropertyDictionary.Visiting) {
                    // Currently visiting this node, so have a cycle
                    throw PropertyDictionary.CreateCircularException(propertyName, _visiting);
                }

                _visiting.Push(propertyName);
                _state[propertyName] = PropertyDictionary.Visiting;

                string propertyValue = _properties.GetPropertyValue(propertyName);
                if (propertyValue == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1053"), propertyName));
                }

                Location propertyLocation = Location.UnknownLocation;

                // TODO - get the proper location of the property declaration
                
                propertyValue = _properties.ExpandProperties(propertyValue, 
                    propertyLocation, _state, _visiting);

                _visiting.Pop();
                _state[propertyName] = PropertyDictionary.Visited;
                return propertyValue;
            } else {
                string propertyValue = _properties.GetPropertyValue(propertyName);
                if (propertyValue == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1053"), propertyName));
                }

                return propertyValue;
            }
        }

        #endregion Public Instance Methods
    }
}
