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
// Gert Driesen (drieseng@users.sourceforge.net)
// Scott Hernandez ScottHernandez_At_hOtmail.d.o.t.com

using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Filters;
using NDoc.Core.Reflection;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// Provides an extension object for the XSLT transformations.
    /// </summary>
    public class NAntXsltUtilities {
        #region Private Instance Fields

        private string _sdkDocBaseUrl; 
        private string _sdkDocExt; 
        private StringDictionary _elementNames = new StringDictionary();
        private XmlDocument _doc;
        private NAntDocumenterConfig _config;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string SdkDoc10BaseUrl = "ms-help://MS.NETFrameworkSDK/cpref/html/frlrf";
        private const string SdkDoc11BaseUrl = "ms-help://MS.NETFrameworkSDKv1.1/cpref/html/frlrf";
        private const string SdkDocPageExt = ".htm";
        private const string MsdnOnlineSdkBaseUrl = "http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf";
        private const string MsdnOnlineSdkPageExt = ".asp";
        private const string SystemPrefix = "System.";
        private const string MicrosoftWin32Prefix = "Microsoft.Win32.";
        
        private static ArrayList Instances = new ArrayList(3);

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NAntXsltUtilities" />
        /// class.
        /// </summary>
        private NAntXsltUtilities(XmlDocument doc, NAntDocumenterConfig config) {
            _doc = doc;
            _config = config;

            if (config.SdkLinksOnWeb) {
                _sdkDocBaseUrl = MsdnOnlineSdkBaseUrl;
                _sdkDocExt = MsdnOnlineSdkPageExt;
            } else {
                switch (config.SdkDocVersion) {
                    case SdkVersion.SDK_v1_0:
                        _sdkDocBaseUrl = SdkDoc10BaseUrl;
                        _sdkDocExt = SdkDocPageExt;
                        break;
                    case SdkVersion.SDK_v1_1:
                        _sdkDocBaseUrl = SdkDoc11BaseUrl;
                        _sdkDocExt = SdkDocPageExt;
                        break;
                }
            }

            // create a list of element names by id
            XmlNodeList types = Document.SelectNodes("//class");
            foreach (XmlElement typeNode in types) {
                string typeId = typeNode.Attributes["id"].Value;
                _elementNames[typeId] = GetElementNameForType(typeNode);

                XmlNodeList members = typeNode.SelectNodes("*[@id]");
                foreach (XmlElement memberNode in members) {
                    string id = memberNode.Attributes["id"].Value;
                    switch (memberNode.Name) {
                        case "constructor":
                            _elementNames[id] = _elementNames[typeId];
                            break;
                        case "field":
                            _elementNames[id] = memberNode.Attributes["name"].Value;
                            break;
                        case "property":
                            _elementNames[id] = GetElementNameForProperty(memberNode);
                            break;
                        case "method":
                            _elementNames[id] = GetElementNameForMethod(memberNode);
                            break;
                        case "operator":
                            _elementNames[id] = memberNode.Attributes["name"].Value;
                            break;
                        case "event":
                            _elementNames[id] = memberNode.Attributes["name"].Value;
                            break;
                    }
                }
            }
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the base url for links to system types.
        /// </summary>
        /// <value>
        /// The base url for links to system types.
        /// </value>
        public string SdkDocBaseUrl {
            get { return _sdkDocBaseUrl; }
        }

        /// <summary>
        /// Gets the page file extension for links to system types.
        /// </summary>
        public string SdkDocExt {
            get { return _sdkDocExt; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private XmlDocument Document {
            get { return _doc; }
        }

        private NAntDocumenterConfig Config {
            get { return _config; }
        }

        #endregion Private Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Gets the root namespace to document.
        /// </summary>
        /// <returns>
        /// The root namespace to document, or a empty <see cref="string" />
        /// if no restriction should be set on the namespace to document.
        /// </returns>
        public string GetNamespaceFilter() {
            return Config.NamespaceFilter;
        }

        /// <summary>
        /// Searches the document for the <c>&lt;class&gt;</c> node with the 
        /// given id.
        /// </summary>
        /// <param name="id">Type.FullName of class to return</param>
        /// <returns>
        /// The <c>&lt;class&gt;</c> node with the given id, or <see langword="null" />
        /// if the node does not exist.
        /// </returns>
        public XPathNodeIterator GetClassNode(string id) {
            if (!id.StartsWith("T:")) {
                id = "T:" + id;
            }
            XmlNode typeNode = Document.SelectSingleNode("//class[@id='" + id + "']");
            if (typeNode == null) {
                return null;
            }
            return typeNode.CreateNavigator().Select(".");
        }

        /// <summary>
        /// Returns an href for a cref.
        /// </summary>
        /// <param name="cref">The cref for which the href will be looked up.</param>
        /// <returns>
        /// The href for the specified cref.
        /// </returns>
        public string GetHRef(string cref) {
            if ((cref.Length < 2) || (cref[1] != ':')) {
                return string.Empty;
            }
            
            // get the underlying type of the array
            if (cref.EndsWith("[]")){
                cref=cref.Replace("[]","");
            }

            // check if the ref is for a system namespaced element or not
            if (cref.Length < 9 || (!cref.Substring(2).StartsWith(SystemPrefix) && !cref.Substring(2).StartsWith(MicrosoftWin32Prefix))) {
                // not a system one.

                // will hold the filename to link to
                string fileName = null;

                switch (cref.Substring(0, 2)) {
                    case "T:":
                        fileName = GetFileNameForType(cref, true);
                        break;
                    case "M:":
                        fileName = GetFileNameForFunction(cref, true);
                        break;
                }

                if (fileName == null) {
                    return string.Empty;
                } else {
                    if (cref.Substring(2).StartsWith("NAnt.") && !cref.Substring(2).StartsWith("NAnt.Contrib")) {
                        return Config.NAntBaseUri + fileName;
                    } else {
                        return "../" + fileName;
                    }
                }
            } else {
                // a system cref
                switch (cref.Substring(0, 2)) {
                    case "N:":  // Namespace
                        return SdkDocBaseUrl + cref.Substring(2).Replace(".", "") + SdkDocExt;
                    case "T:":  // Type: class, interface, struct, enum, delegate
                        return SdkDocBaseUrl + cref.Substring(2).Replace(".", "") + "ClassTopic" + SdkDocExt;
                    case "F:":  // Field
                        // do not generate href for fields, as the .NET SDK does 
                        // not have separate pages for enum fields, and we have no
                        // way of knowing whether it's a reference to an enum field 
                        // or class field.
                        return string.Empty;
                    case "P:":  // Property
                    case "M:":  // Method
                    case "E:":  // Event
                        return this.GetFilenameForSystemMember(cref);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Returns the name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for which the name will be looked up.</param>
        /// <returns>
        /// The name for the specified cref.
        /// </returns>
        public string GetName(string cref) {
            if (cref.Length < 2) {
                return cref;
            }

            if (cref[1] == ':') {
                if (cref.Length < 9 || (!cref.Substring(2).StartsWith(SystemPrefix) && !cref.Substring(2).StartsWith(MicrosoftWin32Prefix))) {
                    //what name should be found?
                    string name = _elementNames[cref];
                    if (name != null) {
                        return name;
                    }
                }

                int index;
                if ((index = cref.IndexOf(".#c")) >= 0) {
                    cref = cref.Substring(2, index - 2);
                } else if ((index = cref.IndexOf("(")) >= 0) {
                    cref = cref.Substring(2, index - 2);
                } else {
                    cref = cref.Substring(2);
                }
            }

            return cref.Substring(cref.LastIndexOf(".") + 1);
        }

        /// <summary>
        /// Returns the NAnt task name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for the task name will be looked up.</param>
        /// <returns>
        /// The NAnt task name for the specified cref.
        /// </returns>
        public string GetTaskName(string cref) {
            return GetTaskNameForType(GetTypeNodeByID(cref));
        }

        /// <summary>
        /// Returns the NAnt element name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for the element name will be looked up.</param>
        /// <returns>
        /// The NAnt element name for the specified cref.
        /// </returns>
        public string GetElementName(string cref) {
            return GetElementNameForType(GetTypeNodeByID(cref));
        }

        /// <summary>
        /// Gets the type node of the specivied identifier.
        /// </summary>
        /// <param name="cref">The identifier.</param>
        /// <returns>The type node.</returns>
        public XmlNode GetTypeNodeByID(string cref) {
            if (cref[1] == ':' && !cref.StartsWith("T:")) {
                return null;
            }

            if (!cref.StartsWith("T:")) {
                cref = "T:" + cref;
            }
            
            XmlNode typeNode = Document.SelectSingleNode("//class[@id='" + cref + "']");
            if (typeNode == null) {
                typeNode = Document.SelectSingleNode("//enumeration[@id='" + cref + "']");
            }
            return typeNode;
        }

        /// <summary>
        /// Gets the method node by identifier.
        /// </summary>
        /// <param name="cref">The mehtod identifier.</param>
        /// <returns>The method node.</returns>
        public XmlNode GetMethodNodeByID(string cref) {
            if (cref[1] == ':' && !cref.StartsWith("M:")) {
                return null;
            }

            if (!cref.StartsWith("M:")) {
                cref = "M:" + cref;
            }

            return Document.SelectSingleNode("//method[@id='" + cref + "']");
        }

        /// <summary>
        /// Determines the <see cref="ElementDocType" /> of the given type node.
        /// </summary>
        /// <param name="typeNode">The type node for which to determine the <see cref="ElementDocType" />.</param>
        /// <returns>
        /// The <see cref="ElementDocType" /> of the given type node.
        /// </returns>
        public ElementDocType GetElementDocType(XmlNode typeNode) {
            if (typeNode == null) {
                return ElementDocType.None;
            }

            // check if type is an enum
            if (typeNode.LocalName == "enumeration") {
                return ElementDocType.Enum;
            }

            // check if type is a task
            if (GetTaskNameForType(typeNode) != null) {
                return ElementDocType.Task;
            }

            // check if type is a datatype
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(DataTypeBase).FullName + "']") != null) {
                if (typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']") != null) {
                    return ElementDocType.DataTypeElement;
                } else {
                    return ElementDocType.Element;
                }
            }

            // check if type is a filter
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Filter).FullName + "']") != null) {
                if (typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']") != null) {
                    return ElementDocType.Filter;
                } else {
                    return ElementDocType.Element;
                }
            }

            // check if type is an element
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Element).FullName + "']") != null) { 
                return ElementDocType.Element;
            }

            // check if type is a functionset
            if (typeNode.SelectSingleNode("attribute[@name='" + typeof(FunctionSetAttribute).FullName + "']/property[@name='Prefix']/@value") != null) {
                return ElementDocType.FunctionSet;
            }

            return ElementDocType.None;
        }

        /// <summary>
        /// Determines the <see cref="ElementDocType" /> of the type to which
        /// the given cref points.
        /// </summary>
        /// <param name="cref">The cref for which to determine the <see cref="ElementDocType" />.</param>
        /// <returns>
        /// The <see cref="ElementDocType" /> of the type to which the given
        /// cref points.
        /// </returns>
        public ElementDocType GetElementDocTypeByID(string cref) {
            return GetElementDocType(GetTypeNodeByID(cref));
        }

        /// <summary>
        /// Determines whether the given cref points to a <c>datatype</c>.
        /// </summary>
        /// <param name="cref">The cref to check.</param>
        /// <returns>
        /// <see langword="true" /> if the given cref points to a <c>datatype</c>;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool IsDataType(string cref) {
            return GetElementDocTypeByID(cref) == ElementDocType.DataTypeElement;
        }

        /// <summary>
        /// Determines whether the given cref points to an <c>element</c>.
        /// </summary>
        /// <param name="cref">The cref to check.</param>
        /// <returns>
        /// <see langword="true" /> if the given cref points to an <c>element</c>;
        /// otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// When the cref points to a <see cref="Task" /> or <see cref="DataTypeBase" />
        /// this method returns <see langword="false" />.
        /// </remarks>
        public bool IsElement(string cref) {
            return GetElementDocTypeByID(cref) == ElementDocType.Element;
        }

        /// <summary>
        /// Determines whether the given cref points to a <c>datatype</c>.
        /// </summary>
        /// <param name="cref">The cref to check.</param>
        /// <returns>
        /// <see langword="true" /> if the given cref points to a <c>datatype</c>;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool IsFilter(string cref) {
            return GetElementDocTypeByID(cref) == ElementDocType.Filter;
        }

        /// <summary>
        /// Determines whether the given cref points to a <c>task</c>.
        /// </summary>
        /// <param name="cref">The cref to check.</param>
        /// <returns>
        /// <see langword="true" /> if the given cref points to a <c>task</c>;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool IsTask(string cref) {
            return GetElementDocTypeByID(cref) == ElementDocType.Task;
        }

        /// <summary>
        /// Determines whether the given cref points to a <c>functionset</c>.
        /// </summary>
        /// <param name="cref">The cref to check.</param>
        /// <returns>
        /// <see langword="true" /> if the given cref points to a <c>functionset</c>;
        /// otherwise, <see langword="false" />.
        /// </returns>
        public bool IsFunctionSet(string cref) {
            return GetElementDocTypeByID(cref) == ElementDocType.FunctionSet;
        }

        /// <summary>
        /// Encodes a URL string using <see cref="Encoding.UTF8" /> for reliable
        /// HTTP transmission from the Web server to a client.
        /// </summary>
        /// <param name="value">The text to encode.</param>
        /// <returns>
        /// The encoded string.
        /// </returns>
        public string UrlEncode(string value) {
            return HttpUtility.UrlEncode(value, Encoding.UTF8);
        }

        #endregion Public Instance Methods

        #region Internal Instance Methods

        /// <summary>
        /// Gets the BuildElementAttribute name for the "class/property" XmlNode
        /// </summary>
        /// <param name="propertyNode">The XmlNode to look for a name.</param>
        /// <returns>The BuildElementAttrbute.Name if the attribute exists for the node.</returns>
        internal string GetElementNameForProperty(XmlNode propertyNode) {
            // check whether property is a task attribute
            XmlAttribute taskAttributeNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(TaskAttributeAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (taskAttributeNode != null) {
                return taskAttributeNode.Value;
            }

            // check whether property is a element array
            XmlAttribute elementArrayNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(BuildElementArrayAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (elementArrayNode != null) {
                return elementArrayNode.Value;
            }

            // check whether property is a element collection
            XmlAttribute elementCollectionNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(BuildElementCollectionAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (elementCollectionNode != null) {
                return elementCollectionNode.Value;
            }

            // check whether property is an xml element
            XmlAttribute buildElementNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(BuildElementAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (buildElementNode != null) {
                return buildElementNode.Value;
            }

            // check whether property is a Framework configurable attribute
            XmlAttribute frameworkConfigAttributeNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(FrameworkConfigurableAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (frameworkConfigAttributeNode != null) {
                return frameworkConfigAttributeNode.Value;
            }

            return null;
        }

        /// <summary>
        /// Returns the filename to use for the given function XmlElement
        /// </summary>
        /// <param name="functionNode">The "method" element to find the filename for.</param>
        /// <param name="urlEncode">Specified whether to URLencode the filename.</param>
        internal string GetFileNameForFunction(XmlNode functionNode, bool urlEncode) {
            StringBuilder sb = new StringBuilder();
            XmlNode n = functionNode.SelectSingleNode("../attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Prefix']/@value");
            if (n != null && n.InnerText != "") {
                sb.Append(n.InnerText);
                sb.Append('.');
            }
            n = functionNode.SelectSingleNode("attribute[@name='NAnt.Core.Attributes.FunctionAttribute']/property[@name='Name']/@value");
            if (n != null && n.InnerText != "") {
                sb.Append(n.InnerText);
            } else {
                sb.Append(functionNode.Attributes["name"].Value);
            }

            sb.Append('(');

            XmlNodeList parameters = functionNode.SelectNodes("parameter");
            for (int i = 0; i < parameters.Count; i++) {
                if (i > 0)
                    sb.Append(',');
                XmlElement param = (XmlElement) parameters[i];
                sb.Append(param.GetAttribute("type"));
            }

            sb.Append(')');

            string file = sb.ToString ();
            return string.Concat("functions/", (urlEncode ? UrlEncode (file) :  file), ".html");
        }

        /// <summary>
        /// Returns the filename to use for the given class XmlNode
        /// </summary>
        /// <param name="typeNode">The "Class" element to find the filename for.</param>
        /// <param name="urlEncode">if set to <c>true</c>, the URL will be encoded using UTF8 encoding.</param>
        /// <returns>
        /// The relative path and filename where this type is stored in the
        /// documentation.
        /// </returns>
        /// <remarks>
        /// For a type that is neither a task, enum, global type, filter or
        /// functionset, the returned filename will point to the SDK docs for
        /// that type.
        /// </remarks>
        internal string GetFileNameForType(XmlNode typeNode, bool urlEncode) {
            if (typeNode == null) {
                return null;
            }

            string partialURL = null;

            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return "tasks/" + (urlEncode ? UrlEncode(taskName) : taskName) + ".html";
            }

            // check if type is an enum
            if (typeNode.LocalName == "enumeration") {
                string enumFile = typeNode.Attributes["id"].Value.Substring(2);
                return "enums/" + (urlEncode ? UrlEncode(enumFile) : enumFile) + ".html";
            }

            // check if type derives from NAnt.Core.DataTypeBase
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(DataTypeBase).FullName + "']") != null) {
                // make sure the type has a ElementName assigned to it
                XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
                if (elementNameAttribute != null) {
                    string dtFile = elementNameAttribute.Value;
                    return "types/" + (urlEncode ? UrlEncode(dtFile) : dtFile) + ".html";
                }
            }

            // check if type derives from NAnt.Core.Filters.Filter
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Filter).FullName + "']") != null) {
                // make sure the type has a ElementName assigned to it
                XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
                if (elementNameAttribute != null) {
                    string filterFile = elementNameAttribute.Value;
                    return "filters/" + (urlEncode ? UrlEncode(filterFile) : filterFile) + ".html";
                }
            }

            // check if type is a functionset
            partialURL = GetHRefForFunctionSet(typeNode);
            if (partialURL != null) {
                return partialURL;
            }

            // check if type derives from NAnt.Core.Element
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Element).FullName + "']") != null) {
                string elementFile = typeNode.Attributes["id"].Value.Substring(2);
                return "elements/" + (urlEncode ? UrlEncode(elementFile) : elementFile) + ".html";
            }

            string sdkFile = typeNode.Attributes["id"].Value.Substring(2);
            return "../sdk/" + (urlEncode ? UrlEncode(sdkFile) : sdkFile) + ".html";
        }

        #endregion Internal Instance Methods

        #region Private Instance Methods

        private string GetElementNameForType(XmlNode typeNode) {
            if (typeNode == null) {
                return string.Empty;
            }

            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return "<" + taskName + ">";
            }

            // make sure the type has a ElementNameAttribute assigned to it
            XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (elementNameAttribute != null) {
                // check if we're dealing with a data type
                if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(DataTypeBase).FullName + "']")!= null) {
                    return "<" + elementNameAttribute.Value + ">";
                }

                // check if we're dealing with a filter
                if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Filter).FullName + "']")!= null) {
                    return "<" + elementNameAttribute.Value + ">";
                }
            }

            // if we're dealing with a FunctionSet, use category as name instead
            // of prefix
            XmlAttribute categoryNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(FunctionSetAttribute).FullName + "']/property[@name='Category']/@value") as XmlAttribute;
            if (categoryNameAttribute != null) {
                return categoryNameAttribute.Value;
            }

            return null;
        }

        private string GetFileNameForFunction(string type, bool urlEncode) {
            XmlNode functionNode = GetMethodNodeByID(type);
            if (functionNode != null) {
                return GetFileNameForFunction(functionNode, true);
            }
            return null;
        }

        private string GetFileNameForType(string type, bool urlEncode) {
            return GetFileNameForType(GetTypeNodeByID(type), urlEncode);
        }

        private string GetFilenameForSystemMember(string cref) {
            string crefName;
            int index;
            if ((index = cref.IndexOf(".#c")) >= 0) {
                crefName = cref.Substring(2, index - 2) + ".ctor";
            } else if ((index = cref.IndexOf("(")) >= 0) {
                crefName = cref.Substring(2, index - 2);
            } else {
                crefName = cref.Substring(2);
            }
            index = crefName.LastIndexOf(".");
            string crefType = crefName.Substring(0, index);
            string crefMember = crefName.Substring(index + 1);
            return SdkDocBaseUrl + crefType.Replace(".", "") + "Class" + crefMember + "Topic" + SdkDocExt;
        }

        /// <summary>
        /// Gets the TaskNameAttrbute name for the "class" XmlNode
        /// </summary>
        /// <param name="typeNode">The XmlNode to look for a name.</param>
        /// <returns>The <see cref="ElementNameAttribute.Name" /> if the attribute exists for the node.</returns>
        /// <remarks>
        /// The class is also checked to make sure it is derived from <see cref="Task" />
        /// </remarks>
        private string GetTaskNameForType(XmlNode typeNode) {
            if (typeNode == null) {
                return null;
            }

            // make sure the type actually derives from NAnt.Core.Task
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Task).FullName + "']") != null) {
                // make sure the type has a TaskNameAttribute assigned to it
                XmlAttribute taskNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(TaskNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
                if (taskNameAttribute != null) {
                    return taskNameAttribute.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the function name for methods that represent a NAtn function.
        /// </summary>
        /// <param name="methodNode">The XmlNode to look for a name.</param>
        /// <returns>
        /// The function name if <paramref name="methodNode" /> represent a
        /// NAnt function.
        /// </returns>
        private string GetElementNameForMethod(XmlNode methodNode) {
            XmlNode functionNameAttribute = methodNode.SelectSingleNode("attribute[@name='" + typeof(FunctionAttribute).FullName + "']/property[@name='Name']/@value");
            if (functionNameAttribute == null) {
                return methodNode.Attributes["name"].Value;
            }

            XmlNode prefixAttribute = methodNode.SelectSingleNode("../attribute[@name='" + typeof(FunctionSetAttribute).FullName + "']/property[@name='Prefix']/@value");
            if (prefixAttribute == null) {
                return methodNode.Attributes["name"].Value;
            }

            return prefixAttribute.InnerText + "::" + functionNameAttribute.InnerText + "()";
        }

        /// <summary>
        /// Returns a partial URL to link to the functionset in the function index.
        /// </summary>
        /// <param name="functionNode">The "Class" element to find the filename for.</param>
        private string GetHRefForFunctionSet(XmlNode functionNode) {
            XmlAttribute categoryValueAttribute = functionNode.SelectSingleNode("attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value") as XmlAttribute;
            if (categoryValueAttribute != null && categoryValueAttribute.Value != "") {
                return "functions/index.html#" + UrlEncode(categoryValueAttribute.Value);
            }

            return null;
        }

        #endregion Private Instance Methods

        #region Internal Static Methods

        internal static NAntXsltUtilities CreateInstance(XmlDocument doc, NAntDocumenterConfig config){
            // just in case... but we should never see this happen.
            lock (Instances) {
                foreach (NAntXsltUtilities util in Instances) {
                    if (util.Document == doc && util.Config.SdkDocVersion.Equals(config.SdkDocVersion) && util.Config.SdkLinksOnWeb == config.SdkLinksOnWeb) {
                        return util;
                    }
                }
                NAntXsltUtilities inst = new NAntXsltUtilities(doc, config);
                Instances.Add(inst);
                return inst;
            }
        }

        #endregion Internal Static Methods
    }
}
