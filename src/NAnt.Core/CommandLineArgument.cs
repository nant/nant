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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

namespace SourceForge.NAnt {
    /// <summary>
    /// Represents a valid command-line argument.
    /// </summary>
    public class CommandLineArgument {
        #region Public Instance Constructors

        public CommandLineArgument(CommandLineArgumentAttribute attribute, PropertyInfo propertyInfo) {
            _attribute = attribute;
            _propertyInfo = propertyInfo;
            _seenValue = false;

            _elementType = GetElementType(propertyInfo);
            _argumentType = GetArgumentType(attribute, propertyInfo);
           
            if (IsCollection || IsArray) {
                _collectionValues = new ArrayList();
            }
            
            Debug.Assert(LongName != null && LongName.Length > 0);
            Debug.Assert((!IsCollection && !IsArray) || AllowMultiple, "Collection and array arguments must have allow multiple");
            Debug.Assert(!Unique || (IsCollection || IsArray), "Unique only applicable to collection arguments");
        }
        
        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the underlying <see cref="Type" /> of the argument.
        /// </summary>
        /// <value>The underlying <see cref="Type" /> of the argument.</value>
        /// <remarks>
        /// If the <see cref="Type" /> of the argument is a collection type,
        /// this property will returns the underlying type of that collection.
        /// </remarks>
        public Type ValueType {
            get { return IsCollection || IsArray ? _elementType : Type; }
        }
        
        /// <summary>
        /// Gets the long name of the argument.
        /// </summary>
        /// <value>The long name of the argument.</value>
        public string LongName {
            get { 
                if (_attribute != null && _attribute.Name != null) {
                    return _attribute.Name;
                } else {
                    return _propertyInfo.Name;
                }
            }
        }

        /// <summary>
        /// Gets the short name of the argument.
        /// </summary>
        /// <value>The short name of the argument.</value>
        public string ShortName {
            get { 
                if (_attribute != null) {
                    return _attribute.ShortName;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the description of the argument.
        /// </summary>
        /// <value>The description of the argument.</value>
        public string Description {
            get { 
                if (_attribute != null) {
                    return _attribute.Description;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequired {
            get { return 0 != (_argumentType & CommandLineArgumentTypes.Required); }
        }

        /// <summary>
        /// Gets a value indicating whether a mathing command-line argument 
        /// was already found.
        /// </summary>
        /// <value>
        /// <c>true</c> if a matching command-line argument was already
        /// found; otherwise, <c>false</c>.
        /// </value>
        public bool SeenValue {
            get { return _seenValue; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument can be specified multiple
        /// times.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument may be specified multiple times;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool AllowMultiple {
            get { return (IsCollection || IsArray) && (0 != (_argumentType & CommandLineArgumentTypes.Multiple)); }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument can only be specified once
        /// with a certain value.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument should always have a unique value;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool Unique {
            get { return 0 != (_argumentType & CommandLineArgumentTypes.Unique); }
        }

        /// <summary>
        /// Gets the <see cref="Type" /> of the property to which the argument
        /// is applied.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of the property to which the argument is
        /// applied.
        /// </value>
        public Type Type {
            get { return _propertyInfo.PropertyType; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument is collection-based.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument is collection-based; otherwise, <c>false</c>.
        /// </value>
        public bool IsCollection {
            get { return IsCollectionType(Type); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is array-nased.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument is array-based; otherwise, <c>false</c>.
        /// </value>
        public bool IsArray {
            get { return IsArrayType(Type); }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument is the default argument.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument is the default argument; otherwise, <c>false</c>.
        /// </value>
        public bool IsDefault {
            get { return (_attribute != null && _attribute is DefaultCommandLineArgumentAttribute); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument cannot be combined with
        /// other arguments.
        /// </summary>
        /// <value>
        /// <c>true</c> if the argument cannot be combined with other arguments; 
        /// otherwise, <c>false</c>.
        /// </value>
        public bool IsExclusive {
            get { return 0 != (_argumentType & CommandLineArgumentTypes.Exclusive); }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Sets the value of the argument on the specified object.
        /// </summary>
        /// <param name="destination">The object on which the value of the argument should be set.</param>
        /// <exception cref="CommandLineArgumentException">The argument is required and no value was specified.</exception>
        /// <exception cref="NotSupportedException">
        /// <para>
        /// The matching property is collection-based, but is not initialized 
        /// and cannot be written to.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        /// The matching property is collection-based, but has no strongly-typed
        /// Add method.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        /// The matching property is collection-based, but the signature of the 
        /// Add method is not supported.
        /// </para>
        /// </exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public void Finish(object destination) {
            if (IsRequired && !SeenValue) {
                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Missing required argument '-{0}'.", LongName));
            }

            if (IsArray) {
                _propertyInfo.SetValue(destination, _collectionValues.ToArray(_elementType), BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            } else if (IsCollection) {
                // If value of property is null, create new instance of collection 
                if (_propertyInfo.GetValue(destination, BindingFlags.Default, null, null, CultureInfo.InvariantCulture) == null) {
                    if (!_propertyInfo.CanWrite) {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Command-line argument '-{0}' is collection-based, but is not initialized and does not allow the collection to be initialized.", LongName));
                    }
                    object instance = Activator.CreateInstance(_propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    _propertyInfo.SetValue(destination, instance, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }
                
                object value = _propertyInfo.GetValue(destination, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                 
                MethodInfo addMethod = null;

                // Locate Add method with 1 parameter
                foreach (MethodInfo method in value.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                    if (method.Name == "Add" && method.GetParameters().Length == 1) {
                        ParameterInfo parameter = method.GetParameters()[0];
                        if (parameter.ParameterType != typeof(object)) {
                            addMethod = method;
                            break;
                        }
                    }
                }

                if (addMethod == null) {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Collection-based command-line argument '-{0}' has no strongly-typed Add method.", LongName));
                } else {
                    try {
                        foreach (object item in _collectionValues) {
                            addMethod.Invoke(value, BindingFlags.Default, null, new object[] {item}, CultureInfo.InvariantCulture);
                        }
                    } catch (Exception ex) {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "The signature of the Add method for the collection-based command-line argument '-{0}' is not supported.", LongName), ex);
                    }
                }
            } else {
                // this fails on mono if the _argumentValue is null
                if (_argumentValue != null) {
                    _propertyInfo.SetValue(destination, _argumentValue, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Assigns the specified value to the argument.
        /// </summary>
        /// <param name="value">The value that should be assigned to the argument.</param>
        /// <exception cref="CommandLineArgumentException">
        /// <para>Duplicate argument.</para>
        /// <para>-or-</para>
        /// <para>Invalid value.</para>
        /// </exception>
        public void SetValue(string value) {
            if (SeenValue && !AllowMultiple) {
                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Duplicate command-line argument '-{0}'.", LongName));
            }

            _seenValue = true;
            
            object newValue = ParseValue(ValueType, value);

            if (IsCollection || IsArray) {
                if (Unique && _collectionValues.Contains(newValue)) {
                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Duplicate value '-{0}' for command-line argument '{1}'.", value, LongName));
                } else {
                    _collectionValues.Add(newValue);
                }
            } else {
                _argumentValue = newValue;
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private object ParseValue(Type type, string stringData) {
            // null is only valid for bool variables
            // empty string is never valid
            if ((stringData != null || type == typeof(bool)) && (stringData == null || stringData.Length > 0)) {
                try {
                    if (type == typeof(string)) {
                        return stringData;
                    } else if (type == typeof(bool)) {
                        if (stringData == null || stringData == "+") {
                            return true;
                        } else if (stringData == "-") {
                            return false;
                        }
                    } else {
                        if (type.IsEnum) {
                            try {
                                return Enum.Parse(type, stringData, true);
                            } catch(ArgumentException ex) {
                                string message = "Invalid value {0} for command-line argument '-{1}'. Valid values are: ";
                                foreach (object value in Enum.GetValues(type)) {
                                    message += value.ToString() + ", ";
                                }
                                // strip last ,
                                message = message.Substring(0, message.Length - 2) + ".";
                                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, message, stringData, LongName), ex);
                            }
                        } else {
                            // Make a guess that the there's a public static Parse method on the type of the property
                            // that will take an argument of type string to convert the string to the type 
                            // required by the property.
                            System.Reflection.MethodInfo parseMethod = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Standard, new Type[] {typeof(string)}, null);
                            if (parseMethod != null) {
                                // Call the Parse method
                                return parseMethod.Invoke(null, BindingFlags.Default, null, new object[] {stringData}, CultureInfo.InvariantCulture);
                            } else if (type.IsClass) {
                                // Search for a constructor that takes a string argument
                                ConstructorInfo stringArgumentConstructor = type.GetConstructor(new Type[] {typeof(string)});

                                if (stringArgumentConstructor != null) {
                                    return stringArgumentConstructor.Invoke(BindingFlags.Default, null, new object[] {stringData}, CultureInfo.InvariantCulture);
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid value '{0}' for command-line argument '-{1}'.", stringData, LongName), ex);
                }
            }

            throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid value '{0}' for command-line argument '-{1}'.", stringData, LongName));
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        private static CommandLineArgumentTypes GetArgumentType(CommandLineArgumentAttribute attribute, PropertyInfo propertyInfo) {
            if (attribute != null) {
                return attribute.Type;
            } else if (IsCollectionType(propertyInfo.PropertyType)) {
                return CommandLineArgumentTypes.MultipleUnique;
            } else {
                return CommandLineArgumentTypes.AtMostOnce;
            }
        }

        private static Type GetElementType(PropertyInfo propertyInfo) {
            Type elementType = null;

            if (propertyInfo.PropertyType.IsArray) {
                elementType = propertyInfo.PropertyType.GetElementType();
                if (elementType == typeof(object)) {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Property {0} is not a strong-typed array.", propertyInfo.Name));
                }
            } else if (typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType)) {
                // Locate Add method with 1 parameter
                foreach (MethodInfo method in propertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                    if (method.Name == "Add" && method.GetParameters().Length == 1) {
                        ParameterInfo parameter = method.GetParameters()[0];
                        if (parameter.ParameterType == typeof(object)) {
                            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Property {0} is not a strong-typed collection.", propertyInfo.Name));
                        } else {
                            elementType = parameter.ParameterType;
                            break;
                        }
                    }
                }

                if (elementType == null) {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Invalid commandline argument type for property {0}.", propertyInfo.Name));
                }
            }

            return elementType;
        }

        private static bool IsCollectionType(Type type) {
            return typeof(ICollection).IsAssignableFrom(type);
        }

        private static bool IsArrayType(Type type) {
            return type.IsArray;
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private Type _elementType;
        private bool _seenValue;
        private CommandLineArgumentTypes _argumentType;
        private object _argumentValue;
        private ArrayList _collectionValues;
        private PropertyInfo _propertyInfo;
        private CommandLineArgumentAttribute _attribute;

        #endregion Private Instance Fields
    }
}
