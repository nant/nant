// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Ian MacLean (ian@maclean.ms)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {
    /// <summary>Models a NAnt XML element in the build file.</summary>
    /// <remarks>
    ///   <para>Automatically validates attributes in the element based on Attribute settings in the derived class.</para>
    /// </remarks>
    public class Element {
        #region Private Instance Fields

        private Location _location = Location.UnknownLocation;
        private Project _project = null;
        private XmlNode _xmlNode = null;
        private object _parent = null;
        #endregion Private Instance Fields        #region Private Static Fields
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Element" /> class.
        /// </summary>
        public Element(){
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Element" /> class
        /// from the specified element.
        /// </summary>
        /// <param name="e">The element that should be used to create a new instance of the <see cref="Element" /> class.</param>
        protected Element(Element e) : this() {
            this._location = e._location;
            this._project = e._project;
            this._xmlNode = e._xmlNode;
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
            get {
                ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                    Attribute.GetCustomAttribute(GetType(), typeof(ElementNameAttribute));

                string name = null;
                if (elementNameAttribute != null) {
                    name = elementNameAttribute.Name;
                }
                return name;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Project"/> to which this element belongs.
        /// </summary>
        /// <value>
        /// The <see cref="Project"/> to which this element belongs.
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
            get { 
                return Project.Properties;
            }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets or sets the xml node of the element.
        /// </summary>
        /// <value>
        /// The xml node of the element.
        /// </value>
        protected virtual XmlNode XmlNode {
            get { return _xmlNode; }
            set { _xmlNode = value; }
        }

        /// <summary>
        /// Gets or sets the location in the build file where the element is defined.
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
        /// <para>Derived classes that wish to add custom initialization should override 
        /// the <see cref="InitializeElement"/> method.
        /// </para>
        /// </remarks>
        public void Initialize(XmlNode elementNode) {
            if (Project == null) {
                throw new InvalidOperationException("Element has invalid Project property.");
            }

            // Save position in buildfile for reporting useful error messages.
            try {
                _location = Project.LocationMap.GetLocation(elementNode);
            }
            catch(ArgumentException ae) {
                Log.WriteLineIf(Project.Verbose, ae.ToString());
                //ignore
            }

            InitializeXml(elementNode);

            // Allow inherited classes a chance to do some custom initialization.
            InitializeElement(elementNode);
        }

        #endregion Public Instance Methods            

        #region Protected Instance Methods

        /// <summary>
        /// Derived classes should override to this method to provide extra initialization 
        /// and validation not covered by the base class.
        /// </summary>
        /// <param name="elementNode">The xml node of the element to use for initialization.</param>
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
            // * For each BuildAttribute try to find the xml attribute that corresponds to it.
            // * Next process all the nested elements, same idea, look at what is supposed to
            //   be there from the attributes on the class/properties and then get
            //   the values from the xml node to set the instance properties.
            
            //* Removed the inheritance walking as it isn't necessary for extraction of public properties          
            _xmlNode = elementNode;

            Type currentType = GetType();
            
            PropertyInfo[] propertyInfoArray = currentType.GetProperties(BindingFlags.Public|BindingFlags.Instance);

            #region Create Collections for Attributes and Element Names Tracking
            //collect a list of attributes, we will check to see if we use them all.
            System.Collections.Specialized.StringCollection attribs = new System.Collections.Specialized.StringCollection();
            foreach(XmlAttribute xmlattr in _xmlNode.Attributes)
                attribs.Add(xmlattr.Name);


            //create collection of element names. We will remove 
            System.Collections.Specialized.StringCollection childElementsRemaining = new System.Collections.Specialized.StringCollection();
            foreach(XmlNode childNode in _xmlNode) {
                //skip existing names. We only need unique names.
                if(childElementsRemaining.Contains(childNode.Name))
                    continue;

                childElementsRemaining.Add(childNode.Name);
            }            
            #endregion Create Collections for Attributes and Element Names Tracking

            //Loop through all the properties in the derived class.
            foreach (PropertyInfo propertyInfo in propertyInfoArray ) {

                #region Initiliaze all the Attributes

                // process all BuildAttribute attributes
                BuildAttributeAttribute buildAttribute = (BuildAttributeAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildAttributeAttribute));

                if (buildAttribute != null) {
                    XmlAttribute attributeNode = _xmlNode.Attributes[buildAttribute.Name];


                    logger.Debug(string.Format(
                        CultureInfo.InvariantCulture,
                        "Found {0} <attribute> for {1}", 
                        buildAttribute.Name, 
                        propertyInfo.DeclaringType.FullName));

                    // check if its required
                    if (attributeNode == null && buildAttribute.Required) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, "'{0}' is a required attribute of <{1} ... \\>.", buildAttribute.Name, this.Name), Location);
                    }

                    if (attributeNode != null) {
                        
                        //remove processed attribute name
                        attribs.Remove(attributeNode.Name);
                        
                        string attrValue = attributeNode.Value;
                        if (buildAttribute.ExpandProperties) {
                            // expand attribute properites
                            attrValue = Project.ExpandProperties(attrValue, this.Location );
                            
                        }

                        logger.Debug(string.Format(
                            CultureInfo.InvariantCulture,
                            "Setting value: {3}.{0} = {2}({1})", 
                            buildAttribute.Name, 
                            attrValue,
                            attributeNode.Value,
                            propertyInfo.DeclaringType.Name));

                        if (propertyInfo.CanWrite) {
                            // set the property value instead
                            MethodInfo info = propertyInfo.GetSetMethod();
                            object[] paramaters = new object[1];

                            // If the object is an emum
                            Type propertyType = propertyInfo.PropertyType;

                            //validate attribute value with custom ValidatorAttribute(ors)
                            object[] validateAttributes = (ValidatorAttribute[]) 
                                Attribute.GetCustomAttributes(propertyInfo, typeof(ValidatorAttribute));
                            try {
                                foreach(ValidatorAttribute validator in validateAttributes) {
                                    logger.Info(string.Format(
                                        CultureInfo.InvariantCulture,
                                        "Validating <{1} {2}='...'> with {0}", 
                                        validator.GetType().Name, _xmlNode.Name, attributeNode.Name));

                                    validator.Validate(attrValue);
                                }
                            } catch(ValidationException ve) {
                                logger.Error("Validation Exception", ve);
                                throw new ValidationException("Validation failed on" + propertyInfo.DeclaringType.FullName, Location, ve);
                            }
                            
                            //set paramaters[0] to value.
                            if (propertyType.IsSubclassOf(Type.GetType("System.Enum"))) {
                                try {
                                    paramaters[0] = Enum.Parse(propertyType, attrValue);
                                } catch (Exception) {
                                    // catch type conversion exceptions here
                                    string message = "Invalid value \"" + attrValue + "\". Valid values for this attribute are: ";
                                    foreach (object value in Enum.GetValues(propertyType)) {
                                        message += value.ToString() + ", ";
                                    }
                                    // strip last ,
                                    message = message.Substring(0, message.Length - 2);
                                    throw new BuildException(message, Location);
                                }
                            } else {
                                paramaters[0] = Convert.ChangeType(attrValue, propertyInfo.PropertyType, CultureInfo.InvariantCulture);
                            }
                            //set value
                            info.Invoke(this, paramaters);
                        }
                    }
                }
                #endregion Initiliaze all the Attributes

                #region Initiliaze the Nested BuildArrayElements (Child xmlnodes)

                // Do build Element Arrays ( assuming they are of a certain collection type.)
                BuildElementArrayAttribute buildElementArrayAttribute = (BuildElementArrayAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementArrayAttribute));
                if (buildElementArrayAttribute != null) {
                    if(!propertyInfo.PropertyType.IsArray) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, " BuildElementArrayAttribute must be applied to array types '{0}' element for <{1} ...//>.", buildElementArrayAttribute.Name, this.Name), Location);
                    }
                    
                    // get collection of nodes  ( TODO - do this without using xpath )
                    XmlNodeList nodes  = elementNode.SelectNodes( "nant:" + buildElementArrayAttribute.Name, Project.NamespaceManager);

                    //remove this node name from the list. This name has been accounted for.
                    if(nodes != null && nodes.Count > 1)
                        childElementsRemaining.Remove(nodes[0].Name);

                    // check if its required
                    if (nodes == null && buildElementArrayAttribute.Required) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, " Element Required! There must be a least one '{0}' element for <{1} ...//>.", buildElementArrayAttribute.Name, this.Name), Location);
                    }

                    // get the type of the array elements
                    Type elementType = propertyInfo.PropertyType.GetElementType();
                    // create new array of the required size - even if size is 0
                    System.Array list = Array.CreateInstance(elementType, nodes.Count);

                    int arrayIndex =0;
                    foreach ( XmlNode childNode in nodes ) {
                        // Create a child element
                        Element childElement = (Element) Activator.CreateInstance(elementType); 
                        
                        childElement.Project = Project;
                        childElement.Initialize(childNode);
                        list.SetValue(childElement, arrayIndex);
                        arrayIndex ++;
                    }
                    
                    // set the memvber array to our newly created array
                    propertyInfo.SetValue(this, list, null);
                }
                #endregion Initiliaze the Nested BuildArrayElements (Child xmlnodes)

                #region Initiliaze the Nested BuildElements (Child xmlnodes)
                // now do nested BuildElements
                BuildElementAttribute buildElementAttribute = (BuildElementAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementAttribute));

                if (buildElementAttribute != null && buildElementArrayAttribute == null ) { // if we're not an array element either
                    // get value from xml node
                    XmlNode nestedElementNode = elementNode[buildElementAttribute.Name, elementNode.OwnerDocument.DocumentElement.NamespaceURI]; 

                    // check if its required
                    if (nestedElementNode == null && buildElementAttribute.Required) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, "'{0}' is a required element of <{1} ...//>.", buildElementAttribute.Name, this.Name), Location);
                    }
                    if (nestedElementNode != null) {
                        
                        //remove item from list. Used to account for each child xmlelement.
                        childElementsRemaining.Remove(nestedElementNode.Name);
                        
                        //create the child build element; not needed directly. It will be assigned to the local property.
                        CreateChildBuildElement(propertyInfo, nestedElementNode);
                    }                        
                }
                #endregion Initiliaze the Nested BuildElements (Child xmlnodes)

            }
            
            //skip checking for anything in target.
            if( !(currentType.Equals(typeof(Target)) || currentType.IsSubclassOf(typeof(Target))) ) {
                #region Check Tracking Collections for Attribute and Element use
                foreach(string attr in attribs) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "{2}:Did not use {0} of <{1} ...>?", attr, currentType.Name, Location);
                    //Log.WriteLineIf(Project.Verbose, msg);
                    logger.Info(msg);
                }
                foreach(string element in childElementsRemaining) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "Did not use <{0} .../> under <{1}/>?", element, currentType.Name);
                    //Log.WriteLine(msg);
                    logger.Info(msg);
                }        
                #endregion Check Tracking Collections for Attribute and Element use
            }

        }

        /// <summary>
        /// Creates a child BuildElement using property set/get methods.
        /// </summary>
        /// <param name="propInf">The PropertyInfo object that represents the property of the current class</param>
        /// <param name="xml">The XMLNode used to init the new object</param>
        /// <returns>Returns the Element child.</returns>
        private Element CreateChildBuildElement(PropertyInfo propInf, XmlNode xml) {
            MethodInfo setter, getter;
            setter = propInf.GetSetMethod(true);
            getter = propInf.GetGetMethod(true);

            Element childElement = null;
            
            
            //if there is a getter, then get the current instance of the object, and use that.
            if(getter != null) {
                childElement = (Element)propInf.GetValue(this, null);
                if (childElement == null && setter == null){
                    string msg = string.Format(CultureInfo.InvariantCulture, "Property {0} cannot return null (if there is no set method) for class {1}", propInf.Name, this.GetType().FullName);
                    logger.Error(msg);
                    throw new BuildException(msg, Location);
                }else if (childElement == null && setter != null){
                    //fake the getter as null so we process the rest like there is no getter.
                    getter = null;
                    logger.Info(string.Format(CultureInfo.InvariantCulture,"{0}_get() returned null; will go the route of set method to populate.", propInf.Name));
                }

            }
            
            //create a new instance of the object if there is not a get method. (or the get object returned null... see above)
            if(getter == null && setter != null) {
                Type elemType = setter.GetParameters()[0].ParameterType;
                if(elemType.IsAbstract)
                    throw new InvalidOperationException(string.Format("abstract type: {0} for {2}.{1}", elemType.Name, propInf.Name, this.Name));
                childElement = (Element)Activator.CreateInstance(elemType, true);
            }

            //initialize the object with context.
            childElement.Project = Project;
            childElement.Parent = this;
            childElement.Initialize(xml);

            //call the set method if we created the object
            if(setter != null && getter == null)
                setter.Invoke(this,new object[] {childElement});
            
            //return the new/used object
            return childElement;
        }
        #endregion Private Instance Methods            
    }
}
