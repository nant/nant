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
// Ian MacLean (ian@maclean.ms)
// Scott Hernandez (ScottHernandez@hotmail.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;
 
namespace NAnt.Core {
    /// <summary>
    /// Models a NAnt XML element in the build file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Automatically validates attributes in the element based on attributes 
    /// applied to members in derived classes.
    /// </para>
    /// </remarks>
    public abstract class Element {
        #region Private Instance Fields

        private Location _location = Location.UnknownLocation;
        private Project _project = null;
        private XmlNode _xmlNode = null;
        private object _parent = null;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Element" /> class.
        /// </summary>
        protected Element(){
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Element" /> class
        /// from the specified element.
        /// </summary>
        /// <param name="e">The element that should be used to create a new instance of the <see cref="Element" /> class.</param>
        protected Element(Element e) : this() {
            _location = e._location;
            _project = e._project;
            _xmlNode = e._xmlNode;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the parent of the element.
        /// </summary>
        /// <value>
        /// The parent of the element.
        /// </value>
        /// <remarks>
        /// This will be the parent <see cref="Task" />, <see cref="Target" />, or 
        /// <see cref="Project" /> depending on where the element is defined.
        /// </remarks>
        public object Parent {
            get { return _parent; } 
            set { _parent = value; } 
        }

        /// <summary>
        /// Gets the name of the XML element used to initialize this element.
        /// </summary>
        /// <value>
        /// The name of the XML element used to initialize this element.
        /// </value>
        public virtual string Name {
            get { return Element.GetElementNameFromType(GetType()); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Project" /> to which this element belongs.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> to which this element belongs.
        /// </value>
        public virtual Project Project {
            get { return _project; }
            set { _project = value; }
        }

        /// <summary>
        /// Gets the properties local to this <see cref="Element" /> and the <see cref="Project" />.
        /// </summary>
        /// <value>
        /// The properties local to this <see cref="Element" /> and the <see cref="Project" />.
        /// </value>
        public virtual PropertyDictionary Properties {
            get { return Project.Properties; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets or sets the XML node of the element.
        /// </summary>
        /// <value>
        /// The XML node of the element.
        /// </value>
        protected virtual XmlNode XmlNode {
            get { return _xmlNode; }
            set { _xmlNode = value; }
        }

        /// <summary>
        /// Gets or sets the location in the build file where the element is 
        /// defined.
        /// </summary>
        /// <value>
        /// The location in the build file where the element is defined.
        /// </value>
        protected virtual Location Location {
            get { return _location; }
            set { _location = value; }
        }

        #endregion Protected Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Performs default initialization.
        /// </summary>
        /// <remarks>
        /// Derived classes that wish to add custom initialization should override 
        /// the <see cref="InitializeElement" /> method.
        /// </remarks>
        public void Initialize(XmlNode elementNode) {
            if (Project == null) {
                throw new InvalidOperationException("Element has invalid Project property.");
            }

            // Save position in buildfile for reporting useful error messages.
            try {
                _location = Project.LocationMap.GetLocation(elementNode);
            } catch (ArgumentException ex) {
                logger.Warn("Location of Element node could be located.", ex);
            }

            InitializeXml(elementNode);

            // Allow inherited classes a chance to do some custom initialization.
            InitializeElement(elementNode);
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the project.
        /// </remarks>
        public virtual void Log(Level messageLevel, string message) {
            if (Project != null) {
                Project.Log(messageLevel, message);
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the project.
        /// </remarks>
        public virtual void Log(Level messageLevel, string message, params object[] args) {
            if (Project != null) {
                Project.Log(messageLevel, message, args);
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Derived classes should override to this method to provide extra 
        /// initialization and validation not covered by the base class.
        /// </summary>
        /// <param name="elementNode">The XML node of the element to use for initialization.</param>
        protected virtual void InitializeElement(XmlNode elementNode) {
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Initializes all build attributes and child elements.
        /// </summary>
        private void InitializeXml(XmlNode elementNode) {
            // This is a bit of a monster function but if you look at it 
            // carefully this is what it does:
            // * Looking for task attributes to initialize.
            // * For each BuildAttribute try to find the XML attribute that corresponds to it.
            // * Next process all the nested elements, same idea, look at what is supposed to
            //   be there from the attributes on the class/properties and then get
            //   the values from the XML node to set the instance properties.
            
            //* Removed the inheritance walking as it isn't necessary for extraction of public properties
            XmlNode = elementNode;

            Type currentType = GetType();
            
            PropertyInfo[] propertyInfoArray = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            #region Create Collections for Attributes and Element Names Tracking

            // collect a list of attributes, we will check to see if we use them all.
            StringCollection attribs = new StringCollection();
            foreach (XmlAttribute xmlattr in XmlNode.Attributes) {
                attribs.Add(xmlattr.Name);
            }

            // create collection of element names. We will remove 
            StringCollection childElementsRemaining = new StringCollection();
            foreach (XmlNode childNode in XmlNode) {
                // skip existing names as we only need unique names.
                if(childElementsRemaining.Contains(childNode.Name)) {
                    continue;
                }

                childElementsRemaining.Add(childNode.Name);
            }

            #endregion Create Collections for Attributes and Element Names Tracking

            // loop through all the properties in the derived class.
            foreach (PropertyInfo propertyInfo in propertyInfoArray) {
                #region Initialize all properties with an assigned FrameworkConfigurableAttribute

                XmlNode attributeNode = null;
                string attributeValue = null;
                XmlNode frameworkAttributeNode = null;

                FrameworkConfigurableAttribute frameworkAttribute = (FrameworkConfigurableAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(FrameworkConfigurableAttribute));

                if (frameworkAttribute != null) {
                    // locate XML configuration node for current attribute
                    frameworkAttributeNode = GetAttributeConfigurationNode(frameworkAttribute.Name);

                    if (frameworkAttributeNode != null) {
                        // get the configured value
                        attributeValue = frameworkAttributeNode.InnerText;

                        if (frameworkAttribute.ExpandProperties && Project.CurrentFramework != null) {
                            try {
                                // expand attribute properties
                                attributeValue = Project.CurrentFramework.Properties.ExpandProperties(attributeValue, Location);
                            } catch (BuildException ex) {
                                // throw BuildException if required
                                if (frameworkAttribute.Required) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        "'{0}' is a required framework configuration setting for <{1} ... />.", 
                                        frameworkAttribute.Name, this.Name), Location, ex);
                                }

                                // set value to null
                                attributeValue = null;
                            }
                        }
                    } else {
                        // check if the attribute is required
                        if (frameworkAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "'{0}' is a required framework configuration setting for <{1} ... />.", 
                                frameworkAttribute.Name, this.Name), Location);
                        }
                    }
                }

                #endregion Initialize all properties with an assigned FrameworkConfigurableAttribute

                #region Initialize all properties with an assigned BuildAttribute

                // process all BuildAttribute attributes
                BuildAttributeAttribute buildAttribute = (BuildAttributeAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildAttributeAttribute));

                if (buildAttribute != null) {
                    logger.Debug(string.Format(
                        CultureInfo.InvariantCulture,
                        "Found {0} <attribute> for {1}", 
                        buildAttribute.Name, 
                        propertyInfo.DeclaringType.FullName));

                    // locate attribute in build file
                    attributeNode = XmlNode.Attributes[buildAttribute.Name];

                    if (attributeNode != null) {
                        // get the configured value
                        attributeValue = attributeNode.Value;

                        if (buildAttribute.ExpandProperties) {
                            // expand attribute properites
                            attributeValue = Project.ExpandProperties(attributeValue, Location);
                        }

                        //remove processed attribute name
                        attribs.Remove(attributeNode.Name);

                        // check if property is deprecated
                        ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ObsoleteAttribute));

                        // emit warning or error if attribute is deprecated
                        if (obsoleteAttribute != null) {
                            string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                                "{0} Attribute '{1}' for <{2} ... /> is deprecated.  {3}", 
                                Location, buildAttribute.Name, Name, obsoleteAttribute.Message);
                            if (obsoleteAttribute.IsError) {
                                Log(Level.Error, obsoleteMessage);
                            } else {
                                Log(Level.Warning, obsoleteMessage);
                            }
                        }
                    } else {
                        // check if attribute is required
                        if (buildAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "'{0}' is a required attribute of <{1} ... />.", 
                                buildAttribute.Name, this.Name), Location);
                        }
                    }
                }

                if (attributeValue != null) {
                    // if attribute was not encountered in the build file, but
                    // still has a value, then it was configured in the framework
                    // section of the NAnt configuration file
                    if (attributeNode == null) {
                        attributeNode = frameworkAttributeNode;
                    }

                    logger.Debug(string.Format(
                        CultureInfo.InvariantCulture,
                        "Setting value: {2}.{0} = {1}", 
                        propertyInfo.Name, 
                        attributeValue,
                        propertyInfo.DeclaringType.Name));

                    if (propertyInfo.CanWrite) {
                        // get the type of the property
                        Type propertyType = propertyInfo.PropertyType;

                        // validate attribute value with custom ValidatorAttribute(ors)
                        object[] validateAttributes = (ValidatorAttribute[]) 
                            Attribute.GetCustomAttributes(propertyInfo, typeof(ValidatorAttribute));
                        try {
                            foreach (ValidatorAttribute validator in validateAttributes) {
                                logger.Info(string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Validating <{1} {2}='...'> with {0}", 
                                    validator.GetType().Name, XmlNode.Name, attributeNode.Name));

                                validator.Validate(attributeValue);
                            }
                        } catch (ValidationException ve) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "'{0}' is not a valid value for attribute '{1}' of <{2} ... />.", 
                                attributeValue, attributeNode.Name, this.Name), Location, ve);
                        }

                        // holds the attribute value converted to the property type
                        object propertyValue = null;

                        // check if the object is an emum
                        if (propertyType.IsEnum) {
                            try {
                                propertyValue = Enum.Parse(propertyType, attributeValue);
                            } catch (ArgumentException) {
                                string message = string.Format(CultureInfo.InvariantCulture, 
                                    "'{0}' is not a valid value for attribute" +
                                    " '{1}' of <{2} ... />. Valid values are: ", 
                                    attributeValue, attributeNode.Name, this.Name);
                                foreach (object value in Enum.GetValues(propertyType)) {
                                    message += value.ToString() + ", ";
                                }
                                // strip last ,
                                message = message.Substring(0, message.Length - 2);
                                throw new BuildException(message, Location);
                            }
                        } else {
                            try {
                                propertyValue = Convert.ChangeType(attributeValue, propertyInfo.PropertyType, CultureInfo.InvariantCulture);
                            } catch (Exception ex) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                    "'{0}' is not a valid value for attribute '{1}' of <{2} ... />.", 
                                    attributeValue, attributeNode.Name, this.Name), Location, ex);
                            }
                        }

                        // set value
                        propertyInfo.SetValue(this, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    }
                }

                #endregion Initialize all properties with an assigned BuildAttribute

                #region Initiliaze the Nested BuildElementArray and BuildElementCollection (Child xmlnodes)

                BuildElementArrayAttribute buildElementArrayAttribute = null;
                BuildElementCollectionAttribute buildElementCollectionAttribute = null;

                // do build element arrays (assuming they are of a certain collection type.)
                buildElementArrayAttribute = (BuildElementArrayAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementArrayAttribute));
                buildElementCollectionAttribute = (BuildElementCollectionAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementCollectionAttribute));

                if (buildElementArrayAttribute != null || buildElementCollectionAttribute != null) {
                    if (!propertyInfo.PropertyType.IsArray && !(typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType))) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "BuildElementArrayAttribute and BuildElementCollection attributes" +
                            " must be applied to array or collection-based types '{0}' element" +
                            "for <{1} ...//>.", buildElementArrayAttribute.Name, this.Name), 
                            Location);
                    }
                    
                    Type elementType = null;

                    // determine type of child elements
                    if (buildElementArrayAttribute != null) {
                        elementType = buildElementArrayAttribute.ElementType;
                    } else {
                        elementType = buildElementCollectionAttribute.ElementType;
                    }

                    if (propertyInfo.PropertyType.IsArray) {
                        if (!propertyInfo.CanWrite) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "BuildElementArrayAttribute cannot be applied to read-only" +
                                " array-based properties. '{0}' element for <{1} ...//>.", 
                                buildElementArrayAttribute.Name, this.Name), Location);
                        }

                        if (elementType == null) {
                            elementType = propertyInfo.PropertyType.GetElementType();
                        }
                    } else {
                        if (!propertyInfo.CanRead) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "BuildElementArrayAttribute cannot be applied to write-only" +
                                " collection-based properties. '{0}' element for <{1} ...//>.", 
                                buildElementArrayAttribute.Name, this.Name), Location);
                        }

                        if (elementType == null) {
                            // locate Add method with 1 parameter, type of that parameter is parameter type
                            foreach (MethodInfo method in propertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                                if (method.Name == "Add" && method.GetParameters().Length == 1) {
                                    ParameterInfo parameter = method.GetParameters()[0];
                                    elementType = parameter.ParameterType;
                                    break;
                                }
                            }
                        }
                    }

                    // make sure the element is strongly typed
                    if (elementType == null || !typeof(Element).IsAssignableFrom(elementType)) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "BuildElementArrayAttribute and BuildElementCollectionAttribute" +
                            " should have an element type assigned that derives from Element" +
                            " for <{0} ...//>.", this.Name), Location);
                    }

                    XmlNodeList collectionNodes = null;

                    if (buildElementCollectionAttribute != null) {
                        collectionNodes = elementNode.SelectNodes("nant:" + buildElementCollectionAttribute.Name, Project.NamespaceManager);
                        
                        if (collectionNodes.Count == 0 && buildElementCollectionAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Element Required! There must be a least one '{0}' element for <{1} ...//>.", 
                                buildElementCollectionAttribute.Name, this.Name), Location);
                        }

                        if (collectionNodes.Count == 1) {
                            // remove element from list of remaining items
                            childElementsRemaining.Remove(collectionNodes[0].Name);

                            string elementName = buildElementCollectionAttribute.ChildElementName;
                            if (elementName == null) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    "No name was assigned to the base element '{0}' for collection element {1} for <{2} ...//>.", 
                                    elementType.FullName, buildElementCollectionAttribute.Name, this.Name), Location);
                            }

                            // get actual collection of element nodes
                            collectionNodes = collectionNodes[0].SelectNodes("nant:" + elementName, Project.NamespaceManager);

                            // check if its required
                            if (collectionNodes.Count == 0 && buildElementCollectionAttribute.Required) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    "Element Required! There must be a least one '{0}' element for <{1} ... />.", 
                                    elementName, buildElementCollectionAttribute.Name), Location);
                            }
                        } else if (collectionNodes.Count > 1) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Use BuildElementArrayAttributes to have array like elements!" +
                                " There must be at most one '{0}' element for <{1} ... />.", 
                                buildElementCollectionAttribute.Name, this.Name), Location);
                        }
                    } else {
                        collectionNodes = elementNode.SelectNodes("nant:" + buildElementArrayAttribute.Name, Project.NamespaceManager);

                        if (collectionNodes.Count > 0) {
                            // remove element from list of remaining items
                            childElementsRemaining.Remove(collectionNodes[0].Name);
                        } else if (buildElementArrayAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Element Required! There must be a least one '{0}' element for <{1} ... />.", 
                                buildElementArrayAttribute.Name, this.Name), Location);
                        }
                    }

                    // create new array of the required size - even if size is 0
                    Array list = Array.CreateInstance(elementType, collectionNodes.Count);

                    int arrayIndex = 0;
                    foreach (XmlNode childNode in collectionNodes) {
                        //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                        if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(Project.Document.DocumentElement.NamespaceURI)) {
                            continue;
                        }

                        // Create a child element
                        Element childElement = (Element) Activator.CreateInstance(elementType); 

                        childElement.Project = Project;
                        childElement.Parent = this;
                        
                        // if subtype of DataTypeBase
                        DataTypeBase dataType = childElement as DataTypeBase;
                        
                        if (dataType != null  && childNode.Attributes["refid"] != null ) {
                            dataType.RefID = childNode.Attributes["refid"].Value;
                            // we have a datatype reference 
                            childElement.Project = Project;
                            childElement.Parent = this;
                            childElement = InitDataTypeBase(dataType );
                            if( childElement.GetType() != (elementType)) {
                                ElementNameAttribute childElemAttr = (ElementNameAttribute) 
                                    Attribute.GetCustomAttribute(childElement.GetType(), typeof(ElementNameAttribute));
                                ElementNameAttribute elementTypeAttr = (ElementNameAttribute) 
                                    Attribute.GetCustomAttribute(elementType, typeof(ElementNameAttribute));
                                
                                // throw error wrong type definition
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                            "Attempting to use a <{0}> reference where a <{1}> is required", elementTypeAttr.Name, childElemAttr.Name ), Location);
                            }
                            childElement.Location = Project.LocationMap.GetLocation(elementNode);
                        } else {
                            childElement.Initialize(childNode);
                        }
                        list.SetValue(childElement, arrayIndex);
                        arrayIndex ++;
                    }

                    // check if property is deprecated
                    ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ObsoleteAttribute));

                    // emit warning or error if attribute is deprecated
                    if (obsoleteAttribute != null) {
                        string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                            "{0} Attribute '{1}' for <{2}... /> is deprecated. {3}", 
                            Location, buildAttribute.Name, Name, obsoleteAttribute.Message);
                        if (obsoleteAttribute.IsError) {
                            Log(Level.Error, obsoleteMessage);
                        } else {
                            Log(Level.Warning, obsoleteMessage);
                        }
                    }
                    
                    if (propertyInfo.PropertyType.IsArray) {
                        try {
                            // set the member array to our newly created array
                            propertyInfo.SetValue(this, list, null);
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Child element type {0} is not assignable to the type" +
                                " of the underlying array ({1} for property {2} for <{3} ... />.", 
                                elementType.FullName, propertyInfo.PropertyType.FullName, 
                                propertyInfo.Name, this.Name), Location, ex);
                        }
                    } else {
                        MethodInfo addMethod = null;

                        // get array of public instance methods
                        MethodInfo[] addMethods = propertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                        // search for a method called 'Add' which accepts a parameter
                        // to which the element type is assignable
                        foreach (MethodInfo method in addMethods) {
                            if (method.Name == "Add" && method.GetParameters().Length == 1) {
                                ParameterInfo parameter = method.GetParameters()[0];
                                if (parameter.ParameterType.IsAssignableFrom(elementType)) {
                                    addMethod = method;
                                    break;
                                }
                            }
                        }

                        if (addMethod == null) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Child element type {0} cannot be added to collection" +
                                " {1} for underlying property {2} for <{3} ... />.", elementType.FullName,
                                propertyInfo.PropertyType.FullName, propertyInfo.Name, this.Name),
                                Location);
                        }

                        // if value of property is null, create new instance of collection
                        object collection = propertyInfo.GetValue(this, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                        if (collection == null) {
                            if (!propertyInfo.CanWrite) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    "BuildElementArrayAttribute cannot be applied to read-only property with" +
                                    " uninitialized collection-based value '{0}' element for <{1} ... />.", 
                                    buildElementArrayAttribute.Name, this.Name), Location);
                            }
                            object instance = Activator.CreateInstance(propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                            propertyInfo.SetValue(this, instance, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                        }

                        // add each element of the array to collection instance
                        foreach (object childElement in list) {
                            addMethod.Invoke(collection, BindingFlags.Default, null, new object[] {childElement}, CultureInfo.InvariantCulture);
                        }
                    }

                }

                #endregion Initiliaze the Nested BuildArrayElements (Child xmlnodes)

                #region Initiliaze the Nested BuildElements (Child xmlnodes)

                // now do nested BuildElements
                BuildElementAttribute buildElementAttribute = (BuildElementAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementAttribute));

                if (buildElementAttribute != null && buildElementArrayAttribute == null && buildElementCollectionAttribute == null) { // if we're not an array element either
                    // get value from XML node
                    XmlNode nestedElementNode = elementNode[buildElementAttribute.Name, elementNode.OwnerDocument.DocumentElement.NamespaceURI]; 

                    // check if its required
                    if (nestedElementNode == null && buildElementAttribute.Required) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "'{0}' is a required element of <{1} ... />.", 
                            buildElementAttribute.Name, this.Name), Location);
                    }
                    if (nestedElementNode != null) {
                        //remove item from list. Used to account for each child xmlelement.
                        childElementsRemaining.Remove(nestedElementNode.Name);
                        
                        //create the child build element; not needed directly. It will be assigned to the local property.
                        CreateChildBuildElement(propertyInfo, nestedElementNode);

                        // output warning to build log when multiple nested elements 
                        // were specified in the build file, as NAnt will only process
                        // the first element it encounters
                        if (elementNode.SelectNodes("nant:" + buildElementAttribute.Name, Project.NamespaceManager).Count > 1) {
                            Log(Level.Warning, string.Format(CultureInfo.InvariantCulture,
                                "<{0} ... /> does not support multiple '{1}' child elements." +
                                " Only the first element will be processed.", this.Name,
                                buildElementAttribute.Name));
                        }
                    }
                }

                #endregion Initiliaze the Nested BuildElements (Child xmlnodes)
            }
            
            // skip checking for anything in target
            if(!(currentType.Equals(typeof(Target)) || currentType.IsSubclassOf(typeof(Target)))) {
                #region Check Tracking Collections for Attribute and Element use

                foreach (string attr in attribs) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "{2}:Did not use {0} of <{1} ... />?", attr, currentType.Name, Location);
                    logger.Info(msg);
                }
                foreach (string element in childElementsRemaining) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "Did not use <{0} ... /> under <{1} />?", element, currentType.Name);
                    logger.Info(msg);
                }

                #endregion Check Tracking Collections for Attribute and Element use
            }

        }

        /// <summary>
        /// Creates a child <see cref="Element" /> using property set/get methods.
        /// </summary>
        /// <param name="propInf">The <see cref="PropertyInfo" /> instance that represents the property of the current class.</param>
        /// <param name="xml">The <see cref="XmlNode" /> used to initialize the new <see cref="Element" /> instance.</param>
        /// <returns>The <see cref="Element" /> child.</returns>
        private Element CreateChildBuildElement(PropertyInfo propInf, XmlNode xml) {
            MethodInfo getter = null;
            MethodInfo setter = null;
            Element childElement = null;

            setter = propInf.GetSetMethod(true);
            getter = propInf.GetGetMethod(true);
           
            // if there is a getter, then get the current instance of the object, and use that
            if (getter != null) {
                childElement = (Element) propInf.GetValue(this, null);
                if (childElement == null && setter == null) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "Property {0} cannot return null (if there is no set method) for class {1}", propInf.Name, this.GetType().FullName);
                    logger.Error(msg);
                    throw new BuildException(msg, Location);
                } else if (childElement == null && setter != null) {
                    // fake the getter as null so we process the rest like there is no getter
                    getter = null;
                    logger.Info(string.Format(CultureInfo.InvariantCulture,"{0}_get() returned null; will go the route of set method to populate.", propInf.Name));
                }
            }
            
            // create a new instance of the object if there is not a get method. (or the get object returned null... see above)
            if (getter == null && setter != null) {
                Type elemType = setter.GetParameters()[0].ParameterType;
                if (elemType.IsAbstract) {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "abstract type: {0} for {2}.{1}", elemType.Name, propInf.Name, this.Name));
                }
                childElement = (Element) Activator.CreateInstance(elemType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null , CultureInfo.InvariantCulture);
            }

            // initialize the object with context
            childElement.Project = Project;
            childElement.Parent = this;
            DataTypeBase dataType = childElement as DataTypeBase;
            if (dataType != null  && xml.Attributes["refid"] != null ) {
                
                if (setter == null){
                    string msg = string.Format(CultureInfo.InvariantCulture, "DataType child element {0} in class {1} must define a set method", propInf.Name, this.GetType().FullName);
                    logger.Error(msg);
                    throw new BuildException(msg, Location);
                }
                dataType.RefID = xml.Attributes["refid"].Value;           
                // we have a datatype reference
                childElement = InitDataTypeBase(dataType);
                Type elemType = setter.GetParameters()[0].ParameterType;
                
                if( childElement.GetType() != (elemType ) ) {
                    ElementNameAttribute childElemAttr = (ElementNameAttribute) 
                        Attribute.GetCustomAttribute(childElement.GetType(), typeof(ElementNameAttribute));
                    ElementNameAttribute elementTypeAttr = (ElementNameAttribute) 
                        Attribute.GetCustomAttribute(elemType, typeof(ElementNameAttribute));
                        
                    // throw error wrong type definition
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Attempting to use a <{0}> reference where a <{1}> is required", elementTypeAttr.Name, childElemAttr.Name ), Location);
                }
                // re-set the getter
                getter = null;
                childElement.Project = Project;
                childElement.Parent = this;
            } else {
                childElement.Initialize(xml);
            }
            

            // call the set method if we created the object
            if (setter != null && getter == null) {
                setter.Invoke(this, new object[] {childElement});
            }
            
            // return the new/used object
            return childElement;
        }
        
        private DataTypeBase InitDataTypeBase(DataTypeBase reference) {
            DataTypeBase refType = null;
            if (!StringUtils.IsNullOrEmpty(reference.ID)) {
                // throw exception because of id and ref
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Datatype references cannot contain an id attribute."),
                    reference.Location);
            }
            if (Project.DataTypeReferences.Contains(reference.RefID)) {
                refType = Project.DataTypeReferences[reference.RefID];
                // clear any instance specific state
                refType.Reset();
            } else {
                // reference not found exception
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "{0} reference '{1}' not defined.", reference.Name, reference.RefID), 
                    reference.Location);
            }
            return refType;
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Returns the <see cref="ElementNameAttribute.Name" /> of the 
        /// <see cref="ElementNameAttribute" /> assigned to the specified
        /// <see cref="Type" />.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> of which the assigned <see cref="ElementNameAttribute.Name" /> should be retrieved.</param>
        /// <returns>
        /// The <see cref="ElementNameAttribute.Name" /> assigned to the specified 
        /// <see cref="Type" /> or a null reference is no <see cref="ElementNameAttribute.Name" />
        /// is assigned to the <paramref name="type" />.
        /// </returns>
        private static string GetElementNameFromType(Type type) {
            if (type == null) {
                throw new ArgumentNullException("type");
            }

            ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

            if (elementNameAttribute != null) {
                return elementNameAttribute.Name;
            }

            return null;
        }

        /// <summary>
        /// Locates the XML node for the specified attribute in NAnt configuration
        /// file.
        /// </summary>
        /// <param name="attributeName">The name of attribute for which the XML configuration node should be located.</param>
        /// <returns>
        /// The XML configuration node for the specified attribute, or 
        /// <see langword="null" /> if no corresponding XML node could be 
        /// located.
        /// </returns>
        /// <remarks>
        /// If there's a valid current framework, the configuration section for
        /// that framework will first be searched.  If no corresponding 
        /// configuration node can be located in that section, the framework-neutral
        /// section of NAnt configuration file will be searched.
        /// </remarks>
        protected XmlNode GetAttributeConfigurationNode(string attributeName) {
            XmlNode attributeNode = null;
            XmlNode nantSettingsNode = ConfigurationSettings.GetConfig("nant") as XmlNode;

            string xpath = "";
            int level = 0;

            if (nantSettingsNode != null) { 
                #region Construct XPATH expression for locating configuration node

                Element parentElement = this as Element;

                while (parentElement != null) {
                    if (parentElement is Task) {
                        xpath += " and parent::nant:task[@name=\"" + parentElement.Name + "\""; 
                        level++;
                        break;
                    }

                    // For now do not support framework configurable attributes 
                    // on nested types.
                    /*
                    } else if (!(parentElement is Target)) {
                        if (parentElement.XmlNode != null) {
                            // perform lookup using name of the node
                            xpath += " and parent::element[@name=\"" + parentElement.XmlNode.Name + "\""; 
                        } else {
                            // perform lookup using name of the element
                            xpath += " and parent::element[@name=\"" + parentElement.Name + "\""; 
                        }
                        level++;
                    }
                    */

                    parentElement = parentElement.Parent as Element;
                }

                xpath = "descendant::nant:attribute[@name=\"" + attributeName + "\"" + xpath;

                for (int counter = 0; counter < level; counter++) {
                    xpath += "]";
                }

                xpath += "]";

                #endregion Construct XPATH expression for locating configuration node

                #region Retrieve framework-specific configuration node

                if (Project.CurrentFramework != null) {
                    // locate framework node for current framework
                     XmlNode frameworkNode = nantSettingsNode.SelectSingleNode("nant:frameworks/nant:platform[@name=\"" + Project.PlatformName + "\"]/nant:framework[@name=\"" + Project.CurrentFramework.Name + "\"]", Project.NamespaceManager);

                    if (frameworkNode != null) {
                        // locate framework-specific configuration node
                        attributeNode = frameworkNode.SelectSingleNode(xpath, Project.NamespaceManager);
                    }
                }

                #endregion Retrieve framework-specific configuration node

                #region Retrieve framework-neutral configuration node

                if (attributeNode == null) {
                    // locate framework-neutral node
                    XmlNode frameworkNeutralNode = nantSettingsNode.SelectSingleNode("nant:frameworks/nant:tasks", Project.NamespaceManager);

                    if (frameworkNeutralNode != null) {
                        // locate framework-neutral configuration node
                        attributeNode = frameworkNeutralNode.SelectSingleNode(xpath, Project.NamespaceManager);
                    }
                }

                #endregion Retrieve framework-neutral configuration node
            }

            return attributeNode;
        }

        #endregion Private Static Methods
    }
}
