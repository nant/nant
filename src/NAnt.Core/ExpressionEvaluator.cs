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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Core {
    [FunctionSet("nant", "NAnt")]
    public class ExpressionEvaluator : ExpressionEvalBase {
        #region Private Instance Fields

        private PropertyDictionary _propDict;
        private Location _location;
        private Hashtable _state;
        private Stack _visiting;
        private Project _project;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        public ExpressionEvaluator(Project project, PropertyDictionary propDict, Location location, Hashtable state, Stack visiting) {            
            _project = project;
            _propDict = propDict;
            _location = location;
            _state = state;
            _visiting = visiting;
        }

        #endregion Public Instance Constructors

        #region Override implementation of ExpressionEvalBase

        protected override object EvaluateProperty(string propertyName) {
            return GetPropertyValue(propertyName);
        }

        protected override void ValidateProperty(string propertyName) {
            if (!_propDict.Contains(propertyName)) {
                ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                    "Property '{0}' has not been set.", propertyName));
            }
        }

        protected override object EvaluateFunction(string functionName, ArrayList args) {
            MethodInfo methodInfo = TypeFactory.LookupFunction(functionName);
            if (methodInfo == null) {
                return ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown function '{0}'.", functionName));
            }

            if (methodInfo.IsStatic) {
                return methodInfo.Invoke(null, args.ToArray());
            } else if (methodInfo.DeclaringType.IsAssignableFrom(typeof(ExpressionEvaluator))) {
                return methodInfo.Invoke(this, args.ToArray());
            } else {
                // create new instance.
                ConstructorInfo constructor = methodInfo.DeclaringType.GetConstructor(new Type[] {typeof(Project), typeof(PropertyDictionary)});
                object o = constructor.Invoke(new object[] {_project, _propDict});
                return methodInfo.Invoke(o, args.ToArray());
            }
        }

        protected override void ValidateFunction(string functionName, int argCount) {
            MethodInfo methodInfo = TypeFactory.LookupFunction(functionName);
            if (methodInfo == null) {
                ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown function '{0}'.", functionName));
            }
            if (methodInfo.GetParameters().Length != argCount) {
                ReportParseError(string.Format(CultureInfo.InvariantCulture,
                    "Function '{0}' expects {1} arguments while {2} were supplied.", 
                    functionName, methodInfo.GetParameters().Length, argCount));
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
        [Function("get-property-value")]
        public string GetPropertyValue(string propertyName) {
            if (_propDict.IsDynamicProperty(propertyName)) {
                string currentState = (string)_state[propertyName];

                // check for circular references
                if (currentState == PropertyDictionary.Visiting) {
                    // Currently visiting this node, so have a cycle
                    throw PropertyDictionary.CreateCircularException(propertyName, _visiting);
                }

                _visiting.Push(propertyName);
                _state[propertyName] = PropertyDictionary.Visiting;

                string propertyValue = _propDict.GetPropertyValue(propertyName);
                if (propertyValue == null) {
                    ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Property '{0}' has not been set.", propertyName));
                }

                propertyValue = _propDict.ExpandProperties(propertyValue, _location, _state, _visiting);
                _visiting.Pop();
                _state[propertyName] = PropertyDictionary.Visited;
                return propertyValue;
            } else {
                string propertyValue = _propDict.GetPropertyValue(propertyName);
                if (propertyValue == null) {
                    ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Property '{0}' has not been set.", propertyName));
                }

                return propertyValue;
            }
        }

        #endregion Public Instance Methods
    }
}
