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
// Gert Driesen (drieseng@users.sourceforge.net)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Configuration;
using NAnt.Core.Types;
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
    [Serializable()]
    public abstract class Element {
        #region Private Instance Fields

        private Location _location = Location.UnknownLocation;
        private Project _project;
        [NonSerialized()]
        private XmlNode _xmlNode;
        private object _parent;
        [NonSerialized()]
        private XmlNamespaceManager _nsMgr;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Element" /> class.
        /// </summary>
        protected Element() {
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
            _nsMgr = e._nsMgr;
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
        /// Gets the properties local to this <see cref="Element" /> and the 
        /// <see cref="Project" />.
        /// </summary>
        /// <value>
        /// The properties local to this <see cref="Element" /> and the <see cref="Project" />.
        /// </value>
        public virtual PropertyDictionary Properties {
            get { return Project.Properties; }
        }

        /// <summary>
        /// Gets or sets the <see cref="XmlNamespaceManager" />.
        /// </summary>
        /// <value>
        /// The <see cref="XmlNamespaceManager" />.
        /// </value>
        /// <remarks>
        /// The <see cref="NamespaceManager" /> defines the current namespace 
        /// scope and provides methods for looking up namespace information.
        /// </remarks>
        public XmlNamespaceManager NamespaceManager {
            get { return _nsMgr; }
            set { _nsMgr = value; }
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

        /// <summary>
        /// Gets a value indicating whether the element is performing additional
        /// processing using the <see cref="XmlNode" /> that was used to 
        /// initialize the element.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Elements that need to perform additional processing of the 
        /// <see cref="XmlNode" /> that was used to initialize the element, should
        /// override this property and return <see langword="true" />.
        /// </para>
        /// <para>
        /// When <see langword="true" />, no build errors will be reported for
        /// unknown nested build elements.
        /// </para>
        /// </remarks>
        protected virtual bool CustomXmlProcessing {
            get { return false; }
        }

        #endregion Protected Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Performs default initialization.
        /// </summary>
        /// <remarks>
        /// Derived classes that wish to add custom initialization should override 
        /// the <see cref="M:Initialize()" /> method.
        /// </remarks>
        public void Initialize(XmlNode elementNode) {
            Initialize(elementNode, Project.Properties, Project.TargetFramework);
        }
        
        /// <summary>
        /// Retrieves the location in the build file where the element is 
        /// defined.
        /// </summary>
        /// <returns>The element's build file location</returns>
        public Location GetLocation() {
            return Location;
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="format">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the project.
        /// </remarks>
        public virtual void Log(Level messageLevel, string format) {
            if (Project != null) {
                Project.Log(messageLevel, format);
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="format">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the project.
        /// </remarks>
        public virtual void Log(Level messageLevel, string format, params object[] args) {
            if (Project != null) {
                Project.Log(messageLevel, format, args);
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Derived classes should override to this method to provide extra 
        /// initialization and validation not covered by the base class.
        /// </summary>
        /// <param name="elementNode">The XML node of the element to use for initialization.</param>
        [Obsolete("Deprecated. Use Initialize() instead")]
        protected virtual void InitializeElement(XmlNode elementNode) {
        }

        /// <summary>
        /// Derived classes should override to this method to provide extra 
        /// initialization and validation not covered by the base class.
        /// </summary>
        /// <remarks>
        /// Access to the <see cref="XmlNode" /> that was used to initialize
        /// this <see cref="Element" /> is available through <see cref="XmlNode" />.
        /// </remarks>
        protected virtual void Initialize() {
        }

        /// <summary>
        /// Copies all instance data of the <see cref="Element" /> to a given
        /// <see cref="Element" />.
        /// </summary>
        protected void CopyTo(Element clone) {
            clone._location = _location;
            clone._nsMgr = _nsMgr;
            clone._parent = _parent;
            clone._project = _project;
            if (_xmlNode != null) {
                clone._xmlNode = _xmlNode.Clone();
            }
        }

        #endregion Protected Instance Methods

        #region Internal Instance Methods

        /// <summary>
        /// Performs initialization using the given set of properties.
        /// </summary>
        internal void Initialize(XmlNode elementNode, PropertyDictionary properties, FrameworkInfo framework) {
            if (Project == null) {
                throw new InvalidOperationException("Element has invalid Project property.");
            }

            // save position in buildfile for reporting useful error messages.
            try {
                _location = Project.LocationMap.GetLocation(elementNode);
            } catch (ArgumentException ex) {
                logger.Warn("Location of Element node could be located.", ex);
            }

            InitializeXml(elementNode, properties, framework);
            
            // If the current instance implements IConditional, check to make sure
            // that IfDefined is true and UnlessDefined is false before initializing
            // the rest of this instance
            IConditional c = this as IConditional;
            if (c != null && !(c.IfDefined && !c.UnlessDefined))
            {
                return;
            }
            
            // allow inherited classes a chance to do some custom initialization
            InitializeElement(elementNode);
            Initialize();
        }

        #endregion Internal Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Initializes all build attributes and child elements.
        /// </summary>
        protected virtual void InitializeXml(XmlNode elementNode, PropertyDictionary properties, FrameworkInfo framework) {
            _xmlNode = elementNode;

            IConditional conditional = this as IConditional;
            AttributeConfigurator configurator;

            if (conditional != null)
            {
                configurator =
                    new ConditionalConfigurator(this, elementNode, properties, framework);
            }
            else
            {
                configurator =
                    new AttributeConfigurator(this, elementNode, properties, framework);
            }
            configurator.Initialize();
        }

        /// <summary>
        /// Locates the XML node for the specified attribute in the project 
        /// configuration node.
        /// </summary>
        /// <param name="attributeName">The name of attribute for which the XML configuration node should be located.</param>
        /// <param name="framework">The framework to use to obtain framework specific information, or <see langword="null" /> if no framework specific information should be used.</param>
        /// <returns>
        /// The XML configuration node for the specified attribute, or 
        /// <see langword="null" /> if no corresponding XML node could be 
        /// located.
        /// </returns>
        /// <remarks>
        /// If there's a valid current framework, the configuration section for
        /// that framework will first be searched.  If no corresponding 
        /// configuration node can be located in that section, the framework-neutral
        /// section of the project configuration node will be searched.
        /// </remarks>
        protected virtual XmlNode GetAttributeConfigurationNode(FrameworkInfo framework, string attributeName) {
            return GetAttributeConfigurationNode(Project.ConfigurationNode,
                framework, attributeName);
        }

        #endregion Protected Instance Methods

        #region Protected Instance Instance Methods

        protected XmlNode GetAttributeConfigurationNode(XmlNode configSection, FrameworkInfo framework, string attributeName) {
            XmlNode attributeNode = null;
            string xpath = "";
            int level = 0;

            #region Construct XPath expression for locating configuration node

            Element parentElement = this as Element;

            while (parentElement != null) {
                if (parentElement is Task) {
                    xpath += " and parent::task[@name=\"" + parentElement.Name + "\""; 
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

            xpath = "descendant::attribute[@name=\"" + attributeName + "\"" + xpath;

            for (int counter = 0; counter < level; counter++) {
                xpath += "]";
            }

            xpath += "]";

            #endregion Construct XPath expression for locating configuration node

            #region Retrieve framework-specific configuration node

            if (framework != null) {
                // locate framework node for current framework
                XmlNode frameworkNode = configSection.SelectSingleNode("frameworks/platform[@name=\"" 
                    + Project.PlatformName + "\"]/framework[@name=\"" 
                    + framework.Name + "\"]", NamespaceManager);

                if (frameworkNode != null) {
                    // locate framework-specific configuration node
                    attributeNode = frameworkNode.SelectSingleNode(xpath, 
                        NamespaceManager);
                }
            }

            #endregion Retrieve framework-specific configuration node

            #region Retrieve framework-neutral configuration node

            if (attributeNode == null) {
                // locate framework-neutral node
                XmlNode frameworkNeutralNode = configSection.SelectSingleNode(
                    "frameworks/tasks", NamespaceManager);

                if (frameworkNeutralNode != null) {
                    // locate framework-neutral configuration node
                    attributeNode = frameworkNeutralNode.SelectSingleNode(xpath, 
                        NamespaceManager);
                }
            }

            #endregion Retrieve framework-neutral configuration node

            return attributeNode;
        }

        #endregion Protected Instance Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Initializes the build element.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="childNode">The child node.</param>
        /// <param name="buildElement">The build element.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <returns></returns>
        /// <exception cref="BuildException">If a datatype references contains an id attribute, a reference is not defined or a wrong reference type is used.
        /// </exception>
        public static Element InitializeBuildElement(Element parent, XmlNode childNode, Element buildElement, Type elementType) {
            // if subtype of DataTypeBase
            DataTypeBase dataType = buildElement as DataTypeBase;

            if (dataType != null && dataType.CanBeReferenced && childNode.Attributes["refid"] != null ) {
                dataType.RefID = childNode.Attributes["refid"].Value;

                if (!String.IsNullOrEmpty(dataType.ID)) {
                    // throw exception because of id and ref
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1183")),
                        parent.Project.LocationMap.GetLocation(childNode));
                }

                if (parent.Project.DataTypeReferences.Contains(dataType.RefID)) {
                    dataType = parent.Project.DataTypeReferences[dataType.RefID];
                    // clear any instance specific state
                    dataType.Reset();
                } else {
                    // reference not found exception
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1184"), dataType.Name, dataType.RefID), 
                        parent.Project.LocationMap.GetLocation(childNode));
                }
                if (!elementType.IsAssignableFrom(dataType.GetType())) {
                    // see if we have a valid copy constructor
                    ConstructorInfo constructor = elementType.GetConstructor(new Type[] {dataType.GetType()});
                    if (constructor != null) {
                        dataType = (DataTypeBase) constructor.Invoke(new object[] {dataType});
                    } else {
                        ElementNameAttribute dataTypeAttr = (ElementNameAttribute) 
                            Attribute.GetCustomAttribute(dataType.GetType(), typeof(ElementNameAttribute));
                        ElementNameAttribute elementTypeAttr = (ElementNameAttribute) 
                            Attribute.GetCustomAttribute(elementType, typeof(ElementNameAttribute));

                        // throw error wrong type definition
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1185"), 
                            dataTypeAttr.Name, elementTypeAttr.Name),
                            parent.Project.LocationMap.GetLocation(childNode));
                    }
                }
                // re-initialize the object with current context
                dataType.Project = parent.Project;
                dataType.Parent = parent;
                dataType.NamespaceManager = parent.NamespaceManager;
                dataType.Location = parent.Project.LocationMap.GetLocation(childNode);

                // return initialized data type
                return dataType;
            } else {
                // initialize the object with context
                buildElement.Project = parent.Project;
                buildElement.Parent = parent;
                buildElement.NamespaceManager = parent.NamespaceManager;

                // initialize element from XML
                buildElement.Initialize(childNode);

                // return initialize build element
                return buildElement;
            }
        }

        #endregion Public Static Methods

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
                Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute),
                false);

            if (elementNameAttribute != null) {
                return elementNameAttribute.Name;
            }

            return null;
        }

        #endregion Private Static Methods

        /// <summary>
        /// Configures an <see cref="Element" /> using meta-data provided by
        /// assigned attributes.
        /// </summary>
        public class AttributeConfigurator {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="AttributeConfigurator" />
            /// class for the given <see cref="Element" />.
            /// </summary>
            /// <param name="element">The <see cref="Element" /> for which an <see cref="AttributeConfigurator" /> should be created.</param>
            /// <param name="elementNode">The <see cref="XmlNode" /> to initialize the <see cref="Element" /> with.</param>
            /// <param name="properties">The <see cref="PropertyDictionary" /> to use for property expansion.</param>
            /// <param name="targetFramework">The framework that the <see cref="Element" /> should target.</param>
            /// <exception cref="ArgumentNullException">
            ///     <para><paramref name="element" /> is <see langword="null" />.</para>
            ///     <para>-or-</para>
            ///     <para><paramref name="elementNode" /> is <see langword="null" />.</para>
            ///     <para>-or-</para>
            ///     <para><paramref name="properties" /> is <see langword="null" />.</para>
            /// </exception>
            public AttributeConfigurator(Element element, XmlNode elementNode, PropertyDictionary properties, FrameworkInfo targetFramework) {
                if (element == null) {
                    throw new ArgumentNullException("element");
                }
                if (elementNode == null) {
                    throw new ArgumentNullException("elementNode");
                }
                if (properties == null) {
                    throw new ArgumentNullException("properties");
                }

                _element = element;
                _elementXml = elementNode;
                _properties = properties;
                _targetFramework = targetFramework;

                // collect a list of attributes, we will check to see if we use them all.
                _unprocessedAttributes = new StringCollection();
                foreach (XmlAttribute attribute in elementNode.Attributes) {
                    // skip non-nant namespace attributes
                    if (attribute.NamespaceURI.Length > 0 && !attribute.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant")) ) {
                        continue;
                    }

                    _unprocessedAttributes.Add(attribute.Name);
                }

                // create collection of node names
                _unprocessedChildNodes = new StringCollection();
                foreach (XmlNode childNode in elementNode) {
                    // skip non-nant namespace elements and special elements like comments, pis, text, etc.
                    if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                        continue;
                    }

                    // skip existing names as we only need unique names.
                    if (_unprocessedChildNodes.Contains(childNode.Name)) {
                        continue;
                    }

                    _unprocessedChildNodes.Add(childNode.Name);
                }
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            public Element Element {
                get { return _element; }
            }

            public Location Location {
                get { return Element.Location; }
            }

            public string Name {
                get { return Element.Name; }
            }

            public Project Project {
                get { return Element.Project; }
            }

            public XmlNode ElementXml {
                get { return _elementXml; }
            }

            public PropertyDictionary Properties {
                get { return _properties; }
            }

            public FrameworkInfo TargetFramework {
                get { return _targetFramework; }
            }

            public StringCollection UnprocessedAttributes {
                get { return _unprocessedAttributes; }
            }

            public StringCollection UnprocessedChildNodes {
                get { return _unprocessedChildNodes; }
            }

            /// <summary>
            /// Gets the <see cref="XmlNamespaceManager" />.
            /// </summary>
            /// <value>
            /// The <see cref="XmlNamespaceManager" />.
            /// </value>
            /// <remarks>
            /// The <see cref="NamespaceManager" /> defines the current namespace 
            /// scope and provides methods for looking up namespace information.
            /// </remarks>
            public XmlNamespaceManager NamespaceManager {
                get { return Element.NamespaceManager; }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            /// <summary>
            /// Initializes this instance.
            /// </summary>
            /// <exception cref="BuildException">If an unexpected attribute was found on this element.
            /// </exception>
            public void Initialize() {
                Type currentType = Element.GetType();

                PropertyInfo[] propertyInfoArray = currentType.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.NonPublic);

                // loop through all the properties in the derived class.
                foreach (PropertyInfo propertyInfo in propertyInfoArray) {
                    MethodInfo getter = null;
                    MethodInfo setter = null;

                    setter = propertyInfo.GetSetMethod(true);
                    if (setter != null && !(setter.IsPublic || setter.IsFamily)) {
                        setter = null;
                    }

                    getter = propertyInfo.GetGetMethod(true);
                    if (getter != null && !(getter.IsPublic || getter.IsFamily)) {
                        getter = null;
                    }

                    // skip properties that are not public or protected
                    if (getter == null && setter == null)
                        continue;

                    if (InitializeAttribute(propertyInfo)) {
                        continue;
                    }

                    if (InitializeBuildElementCollection(propertyInfo)) {
                        continue;
                    }

                    if (InitializeChildElement(propertyInfo, getter, setter)) {
                        continue;
                    }
                }

                // also support child elements that are backed by methods
                // we need this in order to support ordered child elements
                InitializeOrderedChildElements();
            
                // skip checking for anything in target
                if(!(currentType.Equals(typeof(Target)) || currentType.IsSubclassOf(typeof(Target)))) {
                    // check if there are unused attributes
                    if (UnprocessedAttributes.Count > 0) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1027"), 
                            UnprocessedAttributes[0], ElementXml.Name),
                            Location);
                    }

                    if (!Element.CustomXmlProcessing) {
                        // check if there are unused nested build elements
                        if (UnprocessedChildNodes.Count > 0) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1032"), ElementXml.Name,
                                UnprocessedChildNodes[0]), Location);
                        }
                    }
                }
            }

            /// <summary>
            /// Initializes the attribute.
            /// </summary>
            /// <param name="propertyInfo">The property information.</param>
            /// <returns></returns>
            /// <exception cref="BuildException">If an invalid value for an attribute was found.
            /// </exception>
            protected virtual bool InitializeAttribute(PropertyInfo propertyInfo) {
                XmlNode attributeNode = null;
                string attributeValue = null;
                XmlNode frameworkAttributeNode = null;

                #region Initialize property using framework configuration

                FrameworkConfigurableAttribute frameworkAttribute = (FrameworkConfigurableAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(FrameworkConfigurableAttribute),
                    false);

                if (frameworkAttribute != null) {
                    // locate XML configuration node for current attribute
                    frameworkAttributeNode = Element.GetAttributeConfigurationNode(
                        TargetFramework, frameworkAttribute.Name);

                    if (frameworkAttributeNode != null) {
                        // get the configured value
                        attributeValue = frameworkAttributeNode.InnerText;

                        if (frameworkAttribute.ExpandProperties && TargetFramework != null) {
                            try {
                                // expand attribute properties
                                attributeValue = TargetFramework.Project.Properties.ExpandProperties(
                                    attributeValue, Location);
                            } catch (BuildException ex) {
                                // throw BuildException if required
                                if (frameworkAttribute.Required) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        ResourceUtils.GetString("NA1015"),
                                        frameworkAttribute.Name, ElementXml.Name),
                                        Location, ex);
                                }

                                // set value to null
                                attributeValue = null;
                            }
                        }
                    } else {
                        // check if the attribute is required
                        if (frameworkAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1015"), 
                                frameworkAttribute.Name, ElementXml.Name),
                                Location);
                        }
                    }
                }

                #endregion Initialize property using framework configuration

                #region Initialize property with an assigned BuildAttribute

                // process all BuildAttribute attributes
                BuildAttributeAttribute buildAttribute = (BuildAttributeAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildAttributeAttribute),
                    false);

                if (buildAttribute != null) {
                    logger.DebugFormat(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("String_FoundAttribute"), 
                        buildAttribute.Name, propertyInfo.DeclaringType.FullName);

                    if (ElementXml != null) {
                        // locate attribute in build file
                        attributeNode = ElementXml.Attributes[buildAttribute.Name];
                    }

                    if (attributeNode != null) {
                        // remove processed attribute name
                        UnprocessedAttributes.Remove(attributeNode.Name);

                        // if we don't process the xml then skip on
                        if (!buildAttribute.ProcessXml) {
                            logger.DebugFormat(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("String_SkippingAttribute"), 
                                buildAttribute.Name, propertyInfo.DeclaringType.FullName);

                            // consider this property done
                            return true;
                        }

                        // get the configured value
                        attributeValue = attributeNode.Value;

                        if (buildAttribute.ExpandProperties) {
                            // expand attribute properites
                            attributeValue = Properties.ExpandProperties(attributeValue, Location);
                        }

                        // check if property is deprecated
                        ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ObsoleteAttribute));

                        // emit warning or error if attribute is deprecated
                        if (obsoleteAttribute != null) {
                            string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1014"), buildAttribute.Name,
                                ElementXml.Name, obsoleteAttribute.Message);
                            if (obsoleteAttribute.IsError) {
                                throw new BuildException(obsoleteMessage,
                                    Location);
                            } else {
                                Element.Log(Level.Warning, "{0} {1}", Location.ToString(), obsoleteMessage);
                            }
                        }
                    } else {
                        // check if attribute is required
                        if (buildAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1033"), 
                                buildAttribute.Name, ElementXml.Name), Location);
                        }
                    }
                }

                #endregion Initialize property with an assigned BuildAttribute

                if (attributeValue != null) {
                    // if attribute was not encountered in the build file, but
                    // still has a value, then it was configured in the framework
                    // section of the NAnt configuration file
                    if (attributeNode == null) {
                        attributeNode = frameworkAttributeNode;
                    }

                    logger.DebugFormat(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("String_SettingValue"), 
                        propertyInfo.Name, attributeValue, propertyInfo.DeclaringType.Name);

                    if (propertyInfo.CanWrite) {
                        // get the type of the property
                        Type propertyType = propertyInfo.PropertyType;

                        // validate attribute value with custom ValidatorAttribute(ors)
                        object[] validateAttributes = (ValidatorAttribute[]) 
                            Attribute.GetCustomAttributes(propertyInfo, typeof(ValidatorAttribute));
                        try {
                            foreach (ValidatorAttribute validator in validateAttributes) {
                                logger.InfoFormat(CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("String_ValidatingElement"), 
                                    validator.GetType().Name, ElementXml.Name, attributeNode.Name);

                                validator.Validate(attributeValue);
                            }
                        } catch (ValidationException ve) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                attributeValue, attributeNode.Name, ElementXml.Name), Location, ve);
                        }

                        // create an attribute setter for the type of the property
                        IAttributeSetter attributeSetter = CreateAttributeSetter(propertyType);

                        // set the property value
                        attributeSetter.Set(attributeNode, Element, propertyInfo, attributeValue);

                        // if a value is assigned to the property, we consider
                        // it done
                        return true;
                    }
                }

                // if a BuildAttribute was assigned to the property, then 
                // there's no need to try to initialize this property as a 
                // collection or nested element
                return (buildAttribute != null);
            }

            /// <summary>
            /// Initializes the build element collection.
            /// </summary>
            /// <param name="propertyInfo">The property information.</param>
            /// <returns></returns>
            /// <exception cref="BuildException">
            /// </exception>
            protected virtual bool InitializeBuildElementCollection(PropertyInfo propertyInfo) {
                BuildElementArrayAttribute buildElementArrayAttribute = null;
                BuildElementCollectionAttribute buildElementCollectionAttribute = null;

                // do build element arrays (assuming they are of a certain collection type.)
                buildElementArrayAttribute = (BuildElementArrayAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementArrayAttribute),
                    false);
                buildElementCollectionAttribute = (BuildElementCollectionAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementCollectionAttribute),
                    false);

                if (buildElementArrayAttribute == null && buildElementCollectionAttribute == null) {
                    // continue trying to initialize property
                    return false;
                }

                if (!propertyInfo.PropertyType.IsArray && !(typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType))) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1031"), buildElementArrayAttribute.Name, 
                        Name), Location);
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
                            ResourceUtils.GetString("NA1016"),
                            buildElementArrayAttribute.Name, Name),
                            Location);
                    }

                    if (elementType == null) {
                        elementType = propertyInfo.PropertyType.GetElementType();
                    }
                } else {
                    if (!propertyInfo.CanRead) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1019"),
                            buildElementArrayAttribute.Name, Name),
                            Location);
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
                        ResourceUtils.GetString("NA1140"), 
                        propertyInfo.PropertyType.FullName, propertyInfo.Name),
                        Location);
                }

                XmlNodeList collectionNodes = null;

                if (buildElementCollectionAttribute != null) {
                    collectionNodes = ElementXml.SelectNodes("nant:" 
                        + buildElementCollectionAttribute.Name, 
                        NamespaceManager);
                    
                    if (collectionNodes.Count == 0 && buildElementCollectionAttribute.Required) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1021"),
                            buildElementCollectionAttribute.Name, ElementXml.Name),
                            Location);
                    }

                    if (collectionNodes.Count == 1) {
                        // check if property is deprecated
                        ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ObsoleteAttribute));

                        // emit warning or error if attribute is deprecated
                        if (obsoleteAttribute != null) {
                            string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1034"),
                                buildElementCollectionAttribute.Name, ElementXml.Name,
                                obsoleteAttribute.Message);
                            if (obsoleteAttribute.IsError) {
                                throw new BuildException(obsoleteMessage,
                                    Location);
                            } else {
                                Element.Log(Level.Warning, "{0} {1}", Location.ToString(), obsoleteMessage);
                            }
                        }

                        // remove element from list of remaining items
                        UnprocessedChildNodes.Remove(collectionNodes[0].Name);

                        string elementName = buildElementCollectionAttribute.ChildElementName;
                        if (elementName == null) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1026"),
                                elementType.FullName, buildElementCollectionAttribute.Name,
                                Name), Location);
                        }

                        // get actual collection of element nodes
                        collectionNodes = collectionNodes[0].SelectNodes("nant:" 
                            + elementName, NamespaceManager);

                        // check if its required
                        if (collectionNodes.Count == 0 && buildElementCollectionAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1021"),
                                elementName, buildElementCollectionAttribute.Name),
                                Location);
                        }
                    } else if (collectionNodes.Count > 1) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1030"),
                            buildElementCollectionAttribute.Name,
                            Name), Location);
                    }
                } else {
                    collectionNodes = ElementXml.SelectNodes("nant:" 
                        + buildElementArrayAttribute.Name,
                        NamespaceManager);

                    if (collectionNodes.Count > 0) {
                        // check if property is deprecated
                        ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ObsoleteAttribute));

                        // emit warning or error if attribute is deprecated
                        if (obsoleteAttribute != null) {
                            string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1034"),
                                buildElementArrayAttribute.Name, ElementXml.Name,
                                obsoleteAttribute.Message);
                            if (obsoleteAttribute.IsError) {
                                throw new BuildException(obsoleteMessage,
                                    Location);
                            } else {
                                Element.Log(Level.Warning, "{0} {1}", Location.ToString(), obsoleteMessage);
                            }
                        }

                        // remove element from list of remaining items
                        UnprocessedChildNodes.Remove(collectionNodes[0].Name);
                    } else if (buildElementArrayAttribute.Required) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1035"),
                            buildElementArrayAttribute.Name, ElementXml.Name),
                            Location);
                    }
                }

                if (buildElementArrayAttribute != null) {
                    if (!buildElementArrayAttribute.ProcessXml) {
                        return true;
                    }
                } else if (!buildElementCollectionAttribute.ProcessXml) {
                    return true;
                }

                // create new array of the required size - even if size is 0
                Array list = Array.CreateInstance(elementType, collectionNodes.Count);

                int arrayIndex = 0;
                foreach (XmlNode childNode in collectionNodes) {
                    // skip non-nant namespace elements and special elements like comments, pis, text, etc.
                    if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                        continue;
                    }

                    // create and initialize child element (from XML or data type reference)
                    Element childElement = InitializeBuildElement(childNode, 
                        elementType);

                    // check if element should actually be added
                    IConditional conditional = childElement as IConditional;
                    if (conditional != null && !(conditional.IfDefined && !conditional.UnlessDefined))
                    {
                        continue;
                    }

                    // set element in array
                    list.SetValue(childElement, arrayIndex);

                    // move to next index position in array
                    arrayIndex++;
                }

                if (propertyInfo.PropertyType.IsArray) {
                    try {
                        if (arrayIndex != list.Length) {
                            // create a new array with a size that exactly matches
                            // the number of initialized elements
                            Array final = Array.CreateInstance(elementType, arrayIndex);
                            // copy initialized entries to new array
                            Array.Copy(list, 0, final, 0, arrayIndex);
                            // set the member array to our newly created array
                            propertyInfo.SetValue(Element, final, null);
                        } else {
                            // set the member array to our newly created array
                            propertyInfo.SetValue(Element, list, null);
                        }
                    } catch (TargetInvocationException ex) {
                        if (ex.InnerException is BuildException) {
                            throw ex.InnerException;
                        }
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1012"), 
                            elementType.FullName, propertyInfo.PropertyType.FullName, 
                            propertyInfo.Name, Name), Location, ex);
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
                            ResourceUtils.GetString("NA1020"),
                            elementType.FullName,
                            propertyInfo.PropertyType.FullName, propertyInfo.Name, Name),
                            Location);
                    }

                    // if value of property is null, create new instance of collection
                    object collection = propertyInfo.GetValue(Element, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                    if (collection == null) {
                        if (!propertyInfo.CanWrite) {
                            if (buildElementArrayAttribute != null) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA1093"),
                                    buildElementArrayAttribute.Name, Name),
                                    Location);
                            } else {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA1029"),
                                    buildElementCollectionAttribute.Name, Name),
                                    Location);
                            }
                        }

                        object instance = Activator.CreateInstance(
                            propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, 
                            null, null, CultureInfo.InvariantCulture);
                        propertyInfo.SetValue(Element, instance,
                            BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                    }

                    // add each element of the array to collection instance
                    for (int i = 0; i < arrayIndex; i++) {
                        object child = list.GetValue(i);
                        addMethod.Invoke(collection, BindingFlags.Default, null,
                            new object[] {child}, CultureInfo.InvariantCulture);
                    }
                }

                return true;
            }

            protected virtual bool InitializeChildElement(PropertyInfo propertyInfo, MethodInfo getter, MethodInfo setter) {
                // now do nested BuildElements
                BuildElementAttribute buildElementAttribute = (BuildElementAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementAttribute),
                    false);

                if (buildElementAttribute == null) {
                    return false;
                }

                // will hold the XML node
                XmlNode nestedElementNode;

                // when element is initialized from application configuration file,
                // there's no DocumentElement
                if (ElementXml.OwnerDocument.DocumentElement == null) {
                    nestedElementNode = ElementXml[buildElementAttribute.Name];
                } else {
                    nestedElementNode = ElementXml[buildElementAttribute.Name, ElementXml.OwnerDocument.DocumentElement.NamespaceURI]; 
                }

                // check if its required
                if (nestedElementNode == null && buildElementAttribute.Required) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1013"),
                        buildElementAttribute.Name, ElementXml.Name), Location);
                }

                if (nestedElementNode != null) {
                    //remove item from list. Used to account for each child xmlelement.
                    UnprocessedChildNodes.Remove(nestedElementNode.Name);

                    if (!buildElementAttribute.ProcessXml) {
                        return true;
                    }

                    // create the child build element; not needed directly. It will be assigned to the local property.
                    CreateChildBuildElement(propertyInfo, getter, setter,
                        nestedElementNode, Properties, TargetFramework);

                    // output warning to build log when multiple nested elements 
                    // were specified in the build file, as NAnt will only process
                    // the first element it encounters
                    if (ElementXml.SelectNodes("nant:" + buildElementAttribute.Name, NamespaceManager).Count > 1) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1186"),
                            ElementXml.Name, buildElementAttribute.Name),
                            Location);
                    }
                }

                return true;
            }

            /// <summary>
            /// Initializes the ordered child elements.
            /// </summary>
            /// <exception cref="BuildException">If an element is missing in the build file.</exception>
            protected virtual void InitializeOrderedChildElements() {
                // first, we'll fill a hashtable with public methods that take
                // a single argument and to which a BuildElementAttribute is 
                // assigned
                Hashtable childElementMethods = new Hashtable();

                // will hold list of required methods, in order to check missing
                // required elements
                Hashtable requiredMethods = new Hashtable();

                MethodInfo[] methods = Element.GetType().GetMethods(BindingFlags.Public 
                    | BindingFlags.Instance);

                foreach (MethodInfo method in methods) {
                    ParameterInfo[] parameters = method.GetParameters();

                    // we're only interested in methods with one argument, meaning
                    // the element
                    if (parameters.Length != 1) {
                        continue;
                    }

                    // ignore methods that do not have a BuildElementAttribute
                    // assigned to it
                    object[] buildElementAttributes = method.GetCustomAttributes(
                        typeof(BuildElementAttribute), true);
                    if (buildElementAttributes.Length == 0) {
                        continue;
                    }

                    BuildElementAttribute buildElementAttribute = (BuildElementAttribute)
                        buildElementAttributes[0];
                    childElementMethods.Add(buildElementAttribute.Name, method);

                    if (buildElementAttribute.Required) {
                        requiredMethods.Add(buildElementAttribute.Name, method);
                    }
                }

                // keep track of nodes that were processed as ordered build 
                // elements
                StringCollection processedNodes = new StringCollection();

                foreach (XmlNode childNode in ElementXml.ChildNodes) {
                    string elementName = childNode.Name;

                    // skip childnodes that have already been processed 
                    // (by other means)
                    if (!UnprocessedChildNodes.Contains(elementName)) {
                        continue;
                    }

                    // skip child nodes for which no init method exists
                    MethodInfo method = (MethodInfo) childElementMethods[elementName];
                    if (method == null) {
                        continue;
                    }

                    // mark node as processed (as an ordered build element)
                    if (!processedNodes.Contains(elementName)) {
                        processedNodes.Add(elementName);
                    }

                    // ensure method is marked processed
                    if (requiredMethods.ContainsKey(elementName)) {
                        requiredMethods.Remove(elementName);
                    }

                    // obtain buildelementattribute to check whether xml 
                    // should be processed
                    BuildElementAttribute buildElementAttribute = (BuildElementAttribute)
                        Attribute.GetCustomAttribute(method, typeof(BuildElementAttribute), 
                        false);
                    if (!buildElementAttribute.ProcessXml) {
                        continue;
                    }

                    // methods should have an argument of a type that derives 
                    // from Element
                    Type childElementType = method.GetParameters()[0].ParameterType;

                    // create and initialize the element (from XML or datatype reference)
                    Element element = InitializeBuildElement(childNode, 
                        childElementType);

                    try {
                        // invoke method, passing in the initialized element
                        method.Invoke(Element, BindingFlags.InvokeMethod, null,
                            new object[] {element}, CultureInfo.InvariantCulture);
                    } catch (TargetInvocationException ex) {
                        if (ex.InnerException != null) {
                            throw ex.InnerException;
                        }
                        throw;
                    }
                }

                // permanently mark nodes as processed
                foreach (string node in processedNodes) {
                    UnprocessedChildNodes.Remove(node);
                }

                // finally check if there are methods that are required, and for
                // which no element was specified in the build file
                if (requiredMethods.Count > 0) {
                    foreach (DictionaryEntry entry in requiredMethods) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1021"), (string) entry.Key,
                            ElementXml.Name), Location);
                    }
                }
            }

            protected virtual Element InitializeBuildElement(XmlNode childNode, Type elementType) {
                if (!typeof(Element).IsAssignableFrom(elementType)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1187"),
                        childNode.Name, elementType.FullName), Location);
                }

                // create a child element
                Element childElement = (Element) Activator.CreateInstance(
                    elementType, BindingFlags.Public | BindingFlags.NonPublic 
                    | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);

                // initialize the element
                return Element.InitializeBuildElement(Element, childNode, childElement, elementType);
            }

            #endregion Public Instance Methods

            #region Private Instance Methods

            /// <summary>
            /// Creates a child <see cref="Element" /> using property set/get methods.
            /// </summary>
            /// <param name="propInf">The <see cref="PropertyInfo" /> instance that represents the property of the current class.</param>
            /// <param name="getter">A <see cref="MethodInfo" /> representing the get accessor for the property.</param>
            /// <param name="setter">A <see cref="MethodInfo" /> representing the set accessor for the property.</param>
            /// <param name="xml">The <see cref="XmlNode" /> used to initialize the new <see cref="Element" /> instance.</param>
            /// <param name="properties">The collection of property values to use for macro expansion.</param>
            /// <param name="framework">The <see cref="FrameworkInfo" /> from which to obtain framework-specific information.</param>
            /// <returns>The <see cref="Element" /> child.</returns>
            private Element CreateChildBuildElement(PropertyInfo propInf, MethodInfo getter, MethodInfo setter, XmlNode xml, PropertyDictionary properties, FrameworkInfo framework) {
                Element childElement = null;
                Type elementType = null;

                // if there is a getter, then get the current instance of the object, and use that
                if (getter != null) {
                    try {
                        childElement = (Element) propInf.GetValue(Element, null);
                    } catch (InvalidCastException) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1188"), 
                            propInf.Name, Element.GetType().FullName, propInf.PropertyType.FullName, 
                            typeof(Element).FullName), Location);
                    }
                    if (childElement == null) {
                        if (setter == null) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1189"), propInf.Name, 
                                Element.GetType().FullName), Location);
                        } else {
                            // fake the getter as null so we process the rest like there is no getter
                            getter = null;
                            logger.InfoFormat(CultureInfo.InvariantCulture,
                                "{0}_get() returned null; will go the route of set method to populate.", 
                                propInf.Name);
                        }
                    } else {
                        elementType = childElement.GetType();
                    }
                }
            
                // create a new instance of the object if there is no get method
                // or the get object returned null
                if (getter == null) {
                    elementType = setter.GetParameters()[0].ParameterType;
                    if (elementType.IsAbstract) {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("String_AbstractType"), elementType.Name, propInf.Name, Name));
                    }
                    childElement = (Element) Activator.CreateInstance(elementType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null , CultureInfo.InvariantCulture);
                }

                // initialize the child element
                childElement = Element.InitializeBuildElement(Element, xml, childElement, elementType);
                
                // check if we're dealing with a reference to a data type
                DataTypeBase dataType = childElement as DataTypeBase;
                if (dataType != null && xml.Attributes["refid"] != null) {
                    // references to data type should be always be set
                    if (setter == null) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1190"), 
                            propInf.Name, this.GetType().FullName), Location);
                    }
                    // re-set the getter (for force the setter to be used)
                    getter = null;
                }

                // call the set method if we created the object
                if (setter != null && getter == null) {
                    setter.Invoke(Element, new object[] {childElement});
                }
            
                // return the new/used object
                return childElement;
            }
        
            /// <summary>
            /// Creates an <see cref="IAttributeSetter" /> for the given 
            /// <see cref="Type" />.
            /// </summary>
            /// <param name="attributeType">The <see cref="Type" /> for which an <see cref="IAttributeSetter" /> should be created.</param>
            /// <returns>
            /// An <see cref="IAttributeSetter" /> for the given <see cref="Type" />.
            /// </returns>
            private IAttributeSetter CreateAttributeSetter(Type attributeType) {
                if (AttributeSetters.ContainsKey(attributeType)) {
                    return (IAttributeSetter) AttributeSetters[attributeType];
                }

                IAttributeSetter attributeSetter = null;

                if (attributeType.IsEnum) {
                    attributeSetter = new EnumAttributeSetter();
                } else if (attributeType == typeof(Encoding)) {
                    attributeSetter = new EncodingAttributeSetter();
                } else if (attributeType == typeof(FileInfo)) {
                    attributeSetter = new FileAttributeSetter();
                } else if (attributeType == typeof(DirectoryInfo)) {
                    attributeSetter = new DirectoryAttributeSetter();
                } else if (attributeType == typeof(PathSet)) {
                    attributeSetter = new PathSetAttributeSetter();
                } else if (attributeType == typeof(Uri)) {
                    attributeSetter = new UriAttributeSetter();
                } else {
                    attributeSetter = new ConvertableAttributeSetter();
                }

                if (attributeSetter != null) {
                    AttributeSetters.Add(attributeType, attributeSetter);
                }

                return attributeSetter;
            }

            #endregion Private Instance Methods

            #region Private Instance Fields

            /// <summary>
            /// Holds the <see cref="Element" /> that should be initialized.
            /// </summary>
            private readonly Element _element;

            /// <summary>
            /// Holds the <see cref="XmlNode" /> that should be used to initialize
            /// the <see cref="Element" />.
            /// </summary>
            private readonly XmlNode _elementXml;

            /// <summary>
            /// Holds the dictionary that should be used for property 
            /// expansion.
            /// </summary>
            private readonly PropertyDictionary _properties;

            /// <summary>
            /// Holds the framework that should be targeted by the 
            /// <see cref="Element" /> that we're configuring, or
            /// <see langword="null" /> if there's no current target
            /// framework.
            /// </summary>
            private readonly FrameworkInfo _targetFramework;

            /// <summary>
            /// Holds the names of the attributes that still need to be 
            /// processed.
            /// </summary>
            private readonly StringCollection _unprocessedAttributes;

            /// <summary>
            /// Holds the names of the child nodes that still need to be 
            /// processed.
            /// </summary>
            private readonly StringCollection _unprocessedChildNodes;

            #endregion Private Instance Fields

            #region Private Static Fields

            /// <summary>
            /// Holds the logger for the current class.
            /// </summary>
            private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            /// <summary>
            /// Holds the cache of <see cref="IAttributeSetter" /> instances.
            /// </summary>
            private static Hashtable AttributeSetters = new Hashtable();

            #endregion Private Static Fields

            private class EnumAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    try {
                        object propertyValue;

                        // check for more specific type converter
                        TypeConverter tc = TypeDescriptor.GetConverter(property.PropertyType);
                        if (!(tc.GetType() == typeof(EnumConverter))) {
                            propertyValue = tc.ConvertFrom(value);
                        } else {
                            propertyValue = Enum.Parse(property.PropertyType, value);
                        }

                        property.SetValue(parent, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    } catch (FormatException) {
                        throw CreateBuildException(attributeNode, parent, 
                            property, value);
                    } catch (ArgumentException) {
                        throw CreateBuildException(attributeNode, parent, 
                            property, value);
                    }
                }

                private BuildException CreateBuildException(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    StringBuilder sb = new StringBuilder();

                    foreach (object field in Enum.GetValues(property.PropertyType)) {
                        if (sb.Length > 0) {
                            sb.Append(", ");
                        }
                        sb.Append(field.ToString());
                    }

                    string message = string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1023"),  value, attributeNode.Name, 
                        parent.Name, sb.ToString());

                    return new BuildException(message, parent.Location);
                }
            }

            private class EncodingAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    string encodingName = StringUtils.ConvertEmptyToNull(value);
                    if (encodingName == null) {
                        return;
                    }

                    Encoding encoding = null;

                    try {
                        encoding = System.Text.Encoding.GetEncoding(
                            encodingName);
                    } catch (ArgumentException) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1191"),
                            encodingName), parent.Location);
                    } catch (NotSupportedException) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1192"),
                            encodingName), parent.Location);
                    }

                    try {
                        property.SetValue(parent, encoding, BindingFlags.Public | 
                            BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1022"), 
                            value, attributeNode.Name, parent.Name), parent.Location, ex);
                    }
                }
            }

            private class FileAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    string path = StringUtils.ConvertEmptyToNull(value);
                    if (path != null) {
                        object propertyValue;

                        try {
                            propertyValue = new FileInfo(parent.Project.GetFullPath(value));
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                value, attributeNode.Name, parent.Name), parent.Location, ex);
                        }

                        try {
                            property.SetValue(parent, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                value, attributeNode.Name, parent.Name), parent.Location, ex);
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1193"), 
                            attributeNode.Name, parent.Name), parent.Location);
                    }
                }
            }

            private class DirectoryAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    string path = StringUtils.ConvertEmptyToNull(value);
                    if (path != null) {
                        try {
                            object propertyValue = new DirectoryInfo(
                                parent.Project.GetFullPath(value));
                            property.SetValue(parent, propertyValue, 
                                BindingFlags.Public | BindingFlags.Instance, 
                                null, null, CultureInfo.InvariantCulture);
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                value, attributeNode.Name, parent.Name), parent.Location, ex);
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                           ResourceUtils.GetString("NA1193"), 
                            attributeNode.Name, parent.Name), parent.Location);
                    }
                }
            }

            private class PathSetAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    try {
                        PathSet propertyValue = new PathSet(parent.Project, value);
                        property.SetValue(parent, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1022"), 
                            value, attributeNode.Name, parent.Name), parent.Location, ex);
                    }
                }
            }

            private class UriAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    string uri = StringUtils.ConvertEmptyToNull(value);
                    if (uri != null) {
                        Uri propertyValue;

                        // if uri does not contain a scheme, we'll consider it
                        // to be a normal path and as such we need to resolve
                        // it to an absolute path (relative to project base 
                        // directory
                        if (value.IndexOf(Uri.SchemeDelimiter) == -1) {
                            uri = parent.Project.GetFullPath(value);
                        }

                        try {
                            propertyValue = new Uri(uri);
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                value, attributeNode.Name, parent.Name), parent.Location, ex);
                        }

                        try {
                            property.SetValue(parent, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                        } catch (Exception ex) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA1022"), 
                                value, attributeNode.Name, parent.Name), parent.Location, ex);
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1193"), 
                            attributeNode.Name, parent.Name), parent.Location);
                    }
                }
            }

            private class ConvertableAttributeSetter : IAttributeSetter {
                public void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value) {
                    try {
                        object propertyValue = Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
                        property.SetValue(parent, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1022"), 
                            value, attributeNode.Name, parent.Name), parent.Location, ex);
                    }
                }
            }

            /// <summary>
            /// Internal interface used for setting element attributes. 
            /// </summary>
            private interface IAttributeSetter {
                void Set(XmlNode attributeNode, Element parent, PropertyInfo property, string value);
            }
        }


        private class ConditionalConfigurator : AttributeConfigurator
        {
            public ConditionalConfigurator(Element element, XmlNode elementNode, PropertyDictionary properties, FrameworkInfo targetFramework) :
                base(element, elementNode, properties, targetFramework)
            {
                IConditional conditional = element as IConditional;
                if (conditional == null) return;

                Type currentType = element.GetType();
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.Instance;
                
                PropertyInfo ifdefined = currentType.GetProperty("IfDefined", flags);

                InitializeAttribute(ifdefined);
                if (!conditional.IfDefined)
                {
                    _enabled = false;
                }
                else
                {
                    PropertyInfo unlessDefined = 
                        currentType.GetProperty("UnlessDefined", flags);
                    InitializeAttribute(unlessDefined);
                    _enabled = !conditional.UnlessDefined;
                }

                if (!_enabled)
                {
                    // since we will not be processing other attributes or
                    // child nodes, clear these collections to avoid
                    // errors for unrecognized attributes/elements
                    UnprocessedAttributes.Clear();
                    UnprocessedChildNodes.Clear();
                }
            }

            protected override bool InitializeAttribute(PropertyInfo propertyInfo)
            {
                if (!_enabled)
                    return true;
                return base.InitializeAttribute(propertyInfo);
            }

            protected override void InitializeOrderedChildElements()
            {
                if (!_enabled)
                    return;
                base.InitializeOrderedChildElements();
            }

            private readonly bool _enabled = true;
        }
    }
}
