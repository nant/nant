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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace NAnt.Core.Util {
    /// <summary>
    /// Represents a valid command-line argument.
    /// </summary>
    public class CommandLineArgument {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgument"/> class.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="propertyInfo">The property information.</param>
        public CommandLineArgument(CommandLineArgumentAttribute attribute, PropertyInfo propertyInfo) {
            _attribute = attribute;
            _propertyInfo = propertyInfo;
            _seenValue = false;

            _elementType = GetElementType(propertyInfo);
            _argumentType = GetArgumentType(attribute, propertyInfo);
           
            if (IsCollection || IsArray) {
                _collectionValues = new ArrayList();
            } else if (IsNameValueCollection) {
                _valuePairs = new NameValueCollection();
            }
            
            Debug.Assert(LongName != null && LongName.Length > 0);
            Debug.Assert((!IsCollection && !IsArray && !IsNameValueCollection) || AllowMultiple, "Collection and array arguments must have allow multiple");
            Debug.Assert(!Unique || (IsCollection || IsArray || IsNameValueCollection), "Unique only applicable to collection arguments");
        }
        
        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the property that backs the argument.
        /// </summary>
        /// <value>
        /// The property that backs the arguments.
        /// </value>
        public PropertyInfo Property {
            get { return _propertyInfo; }
        }

        /// <summary>
        /// Gets the underlying <see cref="Type" /> of the argument.
        /// </summary>
        /// <value>
        /// The underlying <see cref="Type" /> of the argument.
        /// </value>
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
        /// <see langword="true" /> if the argument is required; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public bool IsRequired {
            get { return 0 != (_argumentType & CommandLineArgumentTypes.Required); }
        }

        /// <summary>
        /// Gets a value indicating whether a mathing command-line argument 
        /// was already found.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a matching command-line argument was 
        /// already found; otherwise, <see langword="false" />.
        /// </value>
        public bool SeenValue {
            get { return _seenValue; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument can be specified multiple
        /// times.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument may be specified multiple 
        /// times; otherwise, <see langword="false" />.
        /// </value>
        public bool AllowMultiple {
            get { 
                return (IsCollection || IsArray || IsNameValueCollection) 
                    && (0 != (_argumentType & CommandLineArgumentTypes.Multiple)); 
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument can only be specified once
        /// with a certain value.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument should always have a unique 
        /// value; otherwise, <see langword="false" />.
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
        /// <see langword="true" /> if the argument is backed by a <see cref="Type" /> 
        /// that can be assigned to <see cref="ICollection" /> and is not backed 
        /// by a <see cref="Type" /> that can be assigned to 
        /// <see cref="NameValueCollection" />; otherwise, <see langword="false" />.
        /// </value>
        public bool IsCollection {
            get { return IsCollectionType(Type); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is a set of name/value
        /// pairs.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument is backed by a <see cref="Type" />
        /// that can be assigned to <see cref="NameValueCollection" />; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public bool IsNameValueCollection {
            get { return IsNameValueCollectionType(Type); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument is array-based.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument is backed by an array; 
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool IsArray {
            get { return IsArrayType(Type); }
        }
        
        /// <summary>
        /// Gets a value indicating whether the argument is the default argument.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument is the default argument; 
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool IsDefault {
            get { return (_attribute != null && _attribute is DefaultCommandLineArgumentAttribute); }
        }

        /// <summary>
        /// Gets a value indicating whether the argument cannot be combined with
        /// other arguments.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the argument cannot be combined with other 
        /// arguments; otherwise, <see langword="false" />.
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
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1171") 
                            + " but is not initialized and does not allow the"
                            + "collection to be initialized.", LongName));
                    }
                    object instance = Activator.CreateInstance(_propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    _propertyInfo.SetValue(destination, instance, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }
                
                object value = _propertyInfo.GetValue(destination, 
                    BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                 
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
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, ResourceUtils.GetString("NA1169"), LongName));
                } else {
                    try {
                        foreach (object item in _collectionValues) {
                            addMethod.Invoke(value, BindingFlags.Default, null, new object[] {item}, CultureInfo.InvariantCulture);
                        }
                    } catch (Exception ex) {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1173"),
                            LongName), ex);
                    }
                }
            } else if (IsNameValueCollection) {
                // If value of property is null, create new instance of collection 
                if (_propertyInfo.GetValue(destination, BindingFlags.Default, null, null, CultureInfo.InvariantCulture) == null) {
                    if (!_propertyInfo.CanWrite) {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1171") 
                            + " but is not initialized and does not allow the"
                            + "collection to be initialized.", LongName));
                    }
                    object instance = Activator.CreateInstance(_propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    _propertyInfo.SetValue(destination, instance, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }
                
                object value = _propertyInfo.GetValue(destination, 
                    BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                 
                MethodInfo addMethod = null;

                // Locate Add method with 2 string parameters
                foreach (MethodInfo method in value.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                    if (method.Name == "Add" && method.GetParameters().Length == 2) {
                        if (method.GetParameters()[0].ParameterType == typeof(string) &&
                            method.GetParameters()[1].ParameterType == typeof(string)) {
                            addMethod = method;
                            break;
                        }
                    }
                }

                if (addMethod == null) {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1169"), LongName));
                } else {
                    try {
                        foreach (string key in _valuePairs) {
                            addMethod.Invoke(value, BindingFlags.Default, null,
                                new object[] {key, _valuePairs.Get(key)}, 
                                CultureInfo.InvariantCulture);
                        }
                    } catch (Exception ex) {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1173"),
                            LongName), ex);
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
                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                ResourceUtils.GetString("NA1175"), LongName));
            }

            _seenValue = true;
            
            object newValue = ParseValue(ValueType, value);

            if (IsCollection || IsArray) {
                if (Unique && _collectionValues.Contains(newValue)) {
                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1172"), value, LongName));
                } else {
                    _collectionValues.Add(newValue);
                }
            } else if (IsNameValueCollection) {
                // name/value pair is added to collection in ParseValue
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
                    } else if (IsNameValueCollectionType(type)) {
                        Match match = Regex.Match(stringData, @"(\w+[^=]*)=(\w*.*)");
                        if (match.Success) {
                            string name = match.Groups[1].Value;
                            string value = match.Groups[2].Value;

                            if (Unique && _valuePairs.Get(name) != null) {
                                // we always assume we're dealing with properties
                                // here to make the message more clear
                                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA1174"), 
                                    name, LongName));
                            }
                            _valuePairs.Add(name, value);
                            return _valuePairs;
                        } else {
                            throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1170"), 
                                stringData, LongName), new ArgumentException(
                                "Expected name/value pair (<name>=<value>)."));
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
                                throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                                    message, stringData, LongName), ex);
                            }
                        } else {
                            // Make a guess that the there's a public static Parse method on the type of the property
                            // that will take an argument of type string to convert the string to the type 
                            // required by the property.
                            System.Reflection.MethodInfo parseMethod = type.GetMethod(
                                "Parse", BindingFlags.Public | BindingFlags.Static, 
                                null, CallingConventions.Standard, new Type[] {typeof(string)}, 
                                null);

                            if (parseMethod != null) {
                                // Call the Parse method
                                return parseMethod.Invoke(null, BindingFlags.Default, 
                                    null, new object[] {stringData}, CultureInfo.InvariantCulture);
                            } else if (type.IsClass) {
                                // Search for a constructor that takes a string argument
                                ConstructorInfo stringArgumentConstructor = 
                                    type.GetConstructor(new Type[] {typeof(string)});

                                if (stringArgumentConstructor != null) {
                                    return stringArgumentConstructor.Invoke(
                                        BindingFlags.Default, null, new object[] {stringData}, 
                                        CultureInfo.InvariantCulture);
                                }
                            }
                        }
                    }
                } catch (CommandLineArgumentException) {
                    throw;
                } catch (Exception ex) {
                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1170"), 
                        stringData, LongName), ex);
                }
            }

            throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, 
                ResourceUtils.GetString("NA1170"), stringData, 
                LongName));
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

        /// <summary>
        /// Indicates whether the specified <see cref="Type" /> is a 
        /// <see cref="NameValueCollection" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if <paramref name="type" /> can be assigned
        /// to <see cref="NameValueCollection" />; otherwise, <see langword="false" />.
        /// </value>
        private static bool IsNameValueCollectionType(Type type) {
            return typeof(NameValueCollection).IsAssignableFrom(type);
        }

        /// <summary>
        /// Indicates whether the specified <see cref="Type" /> is collection-based.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if <paramref name="type" /> can be assigned
        /// to <see cref="ICollection" /> and is not backed by a <see cref="Type" />
        /// that can be assigned to <see cref="NameValueCollection" />; 
        /// otherwise, <see langword="false" />.
        /// </value>
        private static bool IsCollectionType(Type type) {
            return typeof(ICollection).IsAssignableFrom(type) 
                && !IsNameValueCollectionType(type);
        }

        /// <summary>
        /// Indicates whether the specified <see cref="Type" /> is an array.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if <paramref name="type" /> is an array;
        /// otherwise, <see langword="false" />.
        /// </value>
        private static bool IsArrayType(Type type) {
            return type.IsArray;
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private Type _elementType;
        private bool _seenValue;
        private CommandLineArgumentTypes _argumentType;
        private PropertyInfo _propertyInfo;
        private CommandLineArgumentAttribute _attribute;

        private object _argumentValue;
        private ArrayList _collectionValues;
        private NameValueCollection _valuePairs;

        #endregion Private Instance Fields
    }
}
