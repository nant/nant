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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using NDoc.Core;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// NDoc Documenter for building custom NAnt User documentation.
    /// </summary>
    public class NAntTaskDocumenter : BaseDocumenter {
        #region Private Instance Fields

        private XslTransform _xsltTaskIndex;
        private XslTransform _xsltTaskDoc;
        private XslTransform _xsltTypeIndex;
        private XslTransform _xsltTypeDoc;        
        private XmlDocument _xmlDocumentation;
        private string _resourceDirectory;
        private StringDictionary _fileNames = new StringDictionary();
        private StringDictionary _elementNames = new StringDictionary();
        private StringDictionary _namespaceNames = new StringDictionary();
        private StringDictionary _assemblyNames = new StringDictionary();
        private StringDictionary _taskNames = new StringDictionary();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NAntTaskDocumenter" /> class.
        /// </summary>
        public NAntTaskDocumenter() : base("NAntTask") {
            Clear();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the documenter's output directory.
        /// </summary>
        /// <value>
        /// The documenter's output directory.
        /// </value>
        public string OutputDirectory { 
            get {
                return ((NAntTaskDocumenterConfig) Config).OutputDirectory;
            } 
        }

        /// <summary>
        /// Gets the .NET Framework SDK version to provide links to for system 
        /// types.
        /// </summary>
        /// <value>
        /// The .NET Framework SDK version to provide links to for system types.
        /// The default is <see cref="F:SdkDocVersion.MsdnOnline" />.
        /// </value>
        public SdkDocVersion LinkToSdkDocVersion {
            get {
                return ((NAntTaskDocumenterConfig) Config).LinkToSdkDocVersion;
            } 
        }

        #endregion Public Instance Properties

        #region Override implementation of IDocumenter

        /// <summary>
        /// Gets the documenter's main output file.
        /// </summary>
        /// <value>
        /// The documenter's main output file.
        /// </value>
        public override string MainOutputFile { 
            get { return ""; } 
        }

        /// <summary>
        /// Resets the documenter to a clean state.
        /// </summary>
        public override void Clear() {
            Config = new NAntTaskDocumenterConfig();
        }

        /// <summary>
        /// Builds the documentation.
        /// </summary>
        public override void Build(NDoc.Core.Project project) {
            OnDocBuildingStep(0, "Initializing...");

            _resourceDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\NDoc\\NAntTasks\\";

            System.Reflection.Assembly assembly = this.GetType().Module.Assembly;
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.css", _resourceDirectory + "css\\");
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.xslt", _resourceDirectory + "xslt\\");
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.html", _resourceDirectory + "html\\");

            // create the html output directory if it doesn't exist.
            if (!Directory.Exists(OutputDirectory)) {
                Directory.CreateDirectory(OutputDirectory);
            }

            OnDocBuildingStep(10, "Merging XML documentation...");

            // crate the master xml document that contains all the documentation
            MakeXml(project);

            // load the stylesheets that will convert the master xml into html pages
            MakeTransforms();

            // create a xml document that will get transformed by xslt
            _xmlDocumentation = new XmlDocument();
            _xmlDocumentation.LoadXml( XmlBuffer); 

            // build the file mapping
            OnDocBuildingStep(25, "Building mapping...");
            MakeFilenames(_xmlDocumentation);

            // create arguments for nant index page transform
            XsltArgumentList indexArguments = new XsltArgumentList();

            // add extension object for NAnt utilities
            NAntXsltUtilities indexUtilities = new NAntXsltUtilities(_fileNames, 
                _elementNames, _namespaceNames, _assemblyNames, _taskNames, 
                LinkToSdkDocVersion);

            // add extension object to Xslt arguments
            indexArguments.AddExtensionObject("urn:NAntUtil", indexUtilities);

            // transform nant task index page transform
            TransformAndWriteResult(_xsltTaskIndex, indexArguments, "index.html");
            
            // transform nant type index page transform
            TransformAndWriteResult(_xsltTypeIndex, indexArguments, "type-index.html");

            // generate a page for each marked task
            XmlNodeList taskAttrNodes = _xmlDocumentation.SelectNodes("/ndoc/assembly/module/namespace/class/attribute[@name = 'NAnt.Core.Attributes.TaskNameAttribute']");
            foreach (XmlNode node in taskAttrNodes) {
                // create arguments for nant task page transform
                XsltArgumentList arguments = new XsltArgumentList();
                string classID = node.ParentNode.Attributes["id"].Value;
                arguments.AddParam("class-id", String.Empty, classID);

                // add extension object for NAnt utilities
                NAntXsltUtilities utilities = new NAntXsltUtilities(_fileNames, 
                    _elementNames, _namespaceNames, _assemblyNames, _taskNames, 
                    LinkToSdkDocVersion);

                // add extension object to Xslt arguments
                arguments.AddExtensionObject("urn:NAntUtil", utilities);

                // generate filename for page
                XmlNode propNode = node.SelectSingleNode("property[@name='Name']");
                string filename = propNode.Attributes["value"].Value.ToLower(CultureInfo.InvariantCulture) + "task.html";;    

                // create the page
                TransformAndWriteResult(_xsltTaskDoc, arguments, filename);
            }
            
            // generate a page for each marked type
            XmlNodeList typeAttrNodes = _xmlDocumentation.SelectNodes("/ndoc/assembly/module/namespace/class/attribute[@name = 'NAnt.Core.Attributes.ElementNameAttribute']");
            foreach (XmlNode node in typeAttrNodes) {
                // create arguments for nant type page transform
                XsltArgumentList arguments = new XsltArgumentList();
                string classID = node.ParentNode.Attributes["id"].Value;
                arguments.AddParam("class-id", String.Empty, classID);
                
                // add extension object for NAnt utilities
                NAntXsltUtilities utilities = new NAntXsltUtilities(_fileNames, 
                    _elementNames, _namespaceNames, _assemblyNames, _taskNames, 
                    LinkToSdkDocVersion);

                // add extension object to Xslt arguments
                arguments.AddExtensionObject("urn:NAntUtil", utilities);
                
                // generate filename for page
                XmlNode propNode = node.SelectSingleNode("property[@name='Name']");
                string filename = propNode.Attributes["value"].Value.ToLower(CultureInfo.InvariantCulture) + "type.html";;    

                // create the page
                TransformAndWriteResult(_xsltTypeDoc, arguments, filename);
            }
        }

        #endregion Override implementation of IDocumenter

        #region Private Instance Methods

        private void MakeTransforms() {
            OnDocBuildingProgress(0);

            _xsltTaskIndex = new XslTransform();
            _xsltTaskDoc = new XslTransform();
            _xsltTypeIndex = new XslTransform();
            _xsltTypeDoc = new XslTransform();

            OnDocBuildingProgress(25);
            MakeTransform(_xsltTaskIndex, "task-index.xslt");

            OnDocBuildingProgress(50);
            MakeTransform(_xsltTaskDoc, "task-doc.xslt");
            
            OnDocBuildingProgress(75);
            MakeTransform(_xsltTypeIndex, "type-index.xslt");

            OnDocBuildingProgress(100);
            MakeTransform(_xsltTypeDoc, "type-doc.xslt");
        }


        private void MakeTransform(XslTransform transform, string fileName) {
            try {
                transform.Load(_resourceDirectory + "xslt/" + fileName);
            } catch (Exception e) {
                String msg = String.Format(CultureInfo.InvariantCulture, "Error compiling the '{0}' stylesheet:\n{1}", fileName, e.Message);
                throw new DocumenterException(msg, e);
            }
        }

        private void TransformAndWriteResult(XslTransform transform, string filename) {
            XsltArgumentList arguments = new XsltArgumentList();

            // add extension object for NAnt utilities
            NAntXsltUtilities utilities = new NAntXsltUtilities(_fileNames, _elementNames, _namespaceNames, _assemblyNames, _taskNames, LinkToSdkDocVersion);
            arguments.AddExtensionObject("urn:NAntUtil", utilities);

            TransformAndWriteResult(transform, arguments, filename);
        }

        private void TransformAndWriteResult(XslTransform transform, XsltArgumentList arguments, string filename) {
            string path = Path.Combine(OutputDirectory, filename);
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.ASCII)) {
                transform.Transform(_xmlDocumentation, arguments, writer);
            }
        }

        private void MakeFilenames(XmlNode documentation) {
            XmlNodeList namespaces = documentation.SelectNodes("/ndoc/assembly/module/namespace");
            foreach (XmlElement namespaceNode in namespaces) {
                string assemblyName = namespaceNode.SelectSingleNode("../../@name").Value;
                string namespaceName = namespaceNode.Attributes["name"].Value;
                string namespaceId = "N:" + namespaceName;
                _elementNames[namespaceId] = namespaceName;

                XmlNodeList types = namespaceNode.SelectNodes("*[@id]");
                foreach (XmlElement typeNode in types) {
                    string typeId = typeNode.Attributes["id"].Value;
                    _fileNames[typeId] = GetFileNameForType(typeNode);
                    _elementNames[typeId] = GetElementNameForType(typeNode);
                    _namespaceNames[typeId] = namespaceName;
                    _assemblyNames[typeId] = assemblyName;
                    _taskNames[typeId] = GetTaskNameForType(typeNode);

                    XmlNodeList members = typeNode.SelectNodes("*[@id]");
                    foreach (XmlElement memberNode in members) {
                        string id = memberNode.Attributes["id"].Value;
                        switch (memberNode.Name) {
                            case "constructor":
//                                fileNames[id] = GetFilenameForConstructor(memberNode);
                                _elementNames[id] = _elementNames[typeId];
                                break;
                            case "field":
                                if (typeNode.Name == "enumeration") {
//                                    fileNames[id] = GetFilenameForType(typeNode);
                                } else {
//                                    fileNames[id] = GetFilenameForField(memberNode);
                                }
                                _elementNames[id] = memberNode.Attributes["name"].Value;
                                break;
                            case "property":
//                                fileNames[id] = GetFilenameForProperty(memberNode);
                                _elementNames[id] = GetElementNameForProperty(memberNode);
                                break;
                            case "method":
//                                fileNames[id] = GetFilenameForMethod(memberNode);
                                _elementNames[id] = memberNode.Attributes["name"].Value;
                                break;
                            case "operator":
//                                fileNames[id] = GetFilenameForOperator(memberNode);
                                _elementNames[id] = memberNode.Attributes["name"].Value;
                                break;
                            case "event":
//                                fileNames[id] = GetFilenameForEvent(memberNode);
                                _elementNames[id] = memberNode.Attributes["name"].Value;
                                break;
                        }

                        _namespaceNames[id] = namespaceName;
                        _assemblyNames[id] = assemblyName;
                    }
                }
            }
        }

        private string GetTaskNameForType(XmlNode typeNode) {
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

        private string GetElementNameForType(XmlNode typeNode) {
            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return "<" + taskName + ">";
            }

            // use name of type
            return typeNode.Attributes["name"].Value;
        }

        private string GetElementNameForProperty(XmlNode propertyNode) {
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

            // check whether property is a Framework configurable attribute
            XmlAttribute frameworkConfigAttributeNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(FrameworkConfigurableAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
            if (frameworkConfigAttributeNode != null) {
                return frameworkConfigAttributeNode.Value;
            }

            return propertyNode.Attributes["name"].Value;
        }

        private string GetFileNameForType(XmlNode typeNode) {
            // if type is task use name set using TaskNameAttribute
            string taskName = GetTaskNameForType(typeNode);
            if (taskName != null) {
                return taskName + "task.html";
            }

            /*
            // check if type derives from NAnt.Core.Element
            if (typeNode.SelectSingleNode("descendant::base[@id='T:" + typeof(Element).FullName + "']") != null) {
                // make sure the type has a ElementName assigned to it
                XmlAttribute elementNameAttribute = typeNode.SelectSingleNode("attribute[@name='" + typeof(ElementNameAttribute).FullName + "']/property[@name='Name']/@value") as XmlAttribute;
                if (elementNameAttribute != null) {
                    return elementNameAttribute.Value + "type.html";
                }
            }
            */

            return null;
        }

        #endregion Private Instance Methods
    }

    /// <summary>
    /// Specifies a version of the .NET Framework documentation.
    /// </summary>
    public enum SdkDocVersion {
        /// <summary>
        /// The SDK version 1.0.
        /// </summary>
        SDK_v1_0,

        /// <summary>
        /// The SDK version 1.1.
        /// </summary>
        SDK_v1_1,

        /// <summary>
        /// The online version of the SDK documentation.
        /// </summary>
        MsdnOnline
    }
}
