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
// Gert Driesen (gert.driesen@ardatis.com)
// Scott Hernandez ScottHernandez_At_hOtmail.d.o.t.com

using System.Collections.Specialized;
using System.Xml;
using System.Xml.XPath;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// Provides an extension object for the Xslt transformations.
    /// </summary>
    public class NAntXsltUtilities {

        #region Private Instance Fields

        private SdkDocVersion _linkToSdkDocVersion;
        private string _sdkDocBaseUrl; 
        private string _sdkDocExt; 
        private StringDictionary _elementNames = new StringDictionary();
        private XmlDocument _doc;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string SdkDoc10BaseUrl = "ms-help://MS.NETFrameworkSDK/cpref/html/frlrf";
        private const string SdkDoc11BaseUrl = "ms-help://MS.NETFrameworkSDKv1.1/cpref/html/frlrf";
        private const string SdkDocPageExt = ".htm";
        private const string MsdnOnlineSdkBaseUrl = "http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf";
        private const string MsdnOnlineSdkPageExt = ".asp";
        private const string SystemPrefix = "System.";
        
        private static System.Collections.ArrayList Instances = new System.Collections.ArrayList(3);

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NAntXsltUtilities" />
        /// class.
        /// </summary>
        private NAntXsltUtilities(XmlDocument doc, SdkDocVersion linkToSdkDocVersion) {
            _doc = doc;
            _linkToSdkDocVersion = linkToSdkDocVersion;

            switch (linkToSdkDocVersion) {
                case SdkDocVersion.SDK_v1_0:
                    _sdkDocBaseUrl = SdkDoc10BaseUrl;
                    _sdkDocExt = SdkDocPageExt;
                    break;
                case SdkDocVersion.SDK_v1_1:
                    _sdkDocBaseUrl = SdkDoc11BaseUrl;
                    _sdkDocExt = SdkDocPageExt;
                    break;
                case SdkDocVersion.MsdnOnline:
                    _sdkDocBaseUrl = MsdnOnlineSdkBaseUrl;
                    _sdkDocExt = MsdnOnlineSdkPageExt;
                    break;
            }

            //create a list of element names by id
            XmlNodeList types = _doc.SelectNodes("//class");
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
                            _elementNames[id] = memberNode.Attributes["name"].Value;
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

        #region Public Instance Methods

        /// <summary>
        /// Searches the document for a 
        /// </summary>
        /// <param name="type">Type.FullName of class to return</param>
        /// <returns></returns>
        public XPathNodeIterator GetClassNode(string id){
            if(!id.StartsWith("T:")){
                id = "T:" + id;
            }
            XmlNode typeNode = _doc.SelectSingleNode("//class[@id='" + id + "']");
            if(typeNode == null) {
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
            //System.Console.WriteLine("looking up:" + cref);
            if ((cref.Length < 2) || (cref[1] != ':')) {
                return string.Empty;
            }
            
            //get the underlying type of the array
            if(cref.EndsWith("[]")){
                cref=cref.Replace("[]","");
            }

            //check if the ref is for a system namespaced element or not
            if (cref.Length < 9 || cref.Substring(2, 7) != SystemPrefix) {
                //not a system one.
                if(!cref.StartsWith("T:")){
                    return string.Empty;
                }

                string fileName = GetFileNameForType(cref);

                if (fileName == null) {
                    return string.Empty;
                } else {
                    return fileName;
                }
            } else {
                //a system cref
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
                if (cref.Length < 9 || cref.Substring(2, 7) != SystemPrefix) {
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

            string returnName = cref.Substring(cref.LastIndexOf(".") + 1);
            //System.Console.WriteLine("GetName: {0} = {1}", cref, returnName);
            return returnName;
        }


        /// <summary>
        /// Returns the NAnt task name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for the task name will be looked up.</param>
        /// <returns>
        /// The NAnt task name for the specified cref.
        /// </returns>
        public string GetTaskName(string cref) {
            return GetTaskNameForType(GetTypeByID(cref));
        }

        /// <summary>
        /// Returns the NAnt element name for a given cref.
        /// </summary>
        /// <param name="cref">The cref for the element name will be looked up.</param>
        /// <returns>
        /// The NAnt element name for the specified cref.
        /// </returns>
        public string GetElementName(string cref) {
            return GetElementNameForType(GetTypeByID(cref));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods
        private string GetElementNameForType(string id) {
            return GetElementNameForType(GetTypeByID(id));
        }

        private string GetElementNameForType(XmlNode typeNode) {
            if(typeNode == null) return string.Empty;

            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return "<" + taskName + ">";
            }

        
            // make sure the type has a ElementNameAttribute assigned to it
            XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (elementNameAttribute != null && 
                (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(DataTypeBase).FullName + "']")!= null)) {
                return elementNameAttribute.Value;
            }

            // null
            //Console.WriteLine("no element name for: " + typeNode.Attributes["id"].Value);
            return null;
        }

        private string GetFileNameForType(string type) {
            return GetFileNameForType(GetTypeByID(type));
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

        private XmlNode GetTypeByID(string id){
           
            /*
            // if it is a property, field, method or such, remove the last element name, and search for the parent type.
            // ie. P:NAnt.Core.Types.FileSet.ExcludesElement.IfDefined becomes T:NAnt.Core.Types.FileSet.ExcludesElement
            switch (id.Substring(0, 2)) {
                case "T:":  // Type: class, interface, struct, enum, delegate
                    break;
                case "F:":  // Field
                case "P:":  // Property
                case "M:":  // Method
                case "E:": {  // Event
                    //should convert P:NAnt.Core.Types.FileSet.ExcludesElement.IfDefined to T:NAnt.Core.Types.FileSet.ExcludesElement
                    id = "T:" + id.Substring(2,id.LastIndexOf(".") - 2);
                    break;
                }
            }
            */

            if(id[1] == ':' && !id.StartsWith("T:")){
                return null;
                //throw new System.ArgumentException("Cannot lookup type: " + id, "id");
            }

            if(!id.StartsWith("T:")){
                id = "T:" + id;
            }
            
            XmlNode classNode = _doc.SelectSingleNode("//class[@id='" + id + "']");
            if(classNode == null) {
                //System.Console.WriteLine("Could not find: {0}", id);
            }
            return classNode;
        }
        #endregion Private Instance Methods

        #region Internal Static Methods

        /// <summary>
        /// Gets the TaskNameAttrbute name for the "class" XmlNode
        /// </summary>
        /// <param name="propertyNode">The XmlNode to look for a name.</param>
        /// <returns>The <see cref="TaskNameAttribute.Name"/> if the attribute exists for the node.</returns>
        /// <remarks>
        /// The class is also checked to make sure it is derived from <see cref="Task"/>
        /// </remarks>
        internal static string GetTaskNameForType(XmlNode typeNode) {
            if(typeNode == null) return string.Empty;

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
        /// Gets the BuildElementAttrbute name for the "class/property" XmlNode
        /// </summary>
        /// <param name="propertyNode">The XmlNode to look for a name.</param>
        /// <returns>The BuildElementAttrbute.Name if the attribute exists for the node.</returns>
        internal static string GetElementNameForProperty(XmlNode propertyNode) {
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

            // check whether property is a FileSet
            XmlAttribute fileSetNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(FileSetAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (fileSetNode != null) {
                return fileSetNode.Value;
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
        /// Returns the filename to use for the given class XmlNode
        /// </summary>
        /// <param name="typeNode">The "Class" element to find the filename for.</param>
        /// <returns>
        ///     <para>The relative path+filename where this type is stored in the documentation.</para>
        ///     <para>Note: Types default to the 'elements' dir if they don't go into 'tasks' or 'types' directories</para>
        /// </returns>
        internal static string GetFileNameForType(XmlNode typeNode) {
            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return "tasks/" + taskName + ".html";
            }

            // check if type derives from NAnt.Core.DataTypeBase
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(DataTypeBase).FullName + "']") != null) {
                // make sure the type has a ElementName assigned to it
                XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
                if (elementNameAttribute != null) {
                    return "types/" + elementNameAttribute.Value + ".html";
                }
            }


            return "elements/" + typeNode.Attributes["id"].Value.Substring(2) + ".html";
        }
                

        
        internal static NAntXsltUtilities CreateInstance(XmlDocument doc, SdkDocVersion linkToSdkDocVersion){
            //just in case... but we should never see this happen.
            lock(typeof(NAntXsltUtilities)) {
                foreach(NAntXsltUtilities util in Instances){
                    if(util._doc == doc && util._linkToSdkDocVersion.Equals(linkToSdkDocVersion)) {
                        return util;
                    }
                }
                NAntXsltUtilities inst = new NAntXsltUtilities(doc, linkToSdkDocVersion);
                Instances.Add(inst);
                return inst;
            }
        }
        #endregion
    }
}
