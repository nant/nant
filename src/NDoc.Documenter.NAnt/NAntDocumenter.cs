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
// Gert Driesen (drieseng@users.sourceforge.net)
// Scott Hernandez (ScottHernandez_hotmail_com)

using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using NAnt.Core;
using NDoc.Core;
using NDoc.Core.Reflection;
using NAnt.Core.Attributes;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// NDoc Documenter for building custom NAnt User documentation.
    /// </summary>
    public class NAntDocumenter : BaseReflectionDocumenter {
        #region Private Instance Fields

        private XslTransform _xsltTaskIndex;
        private XslTransform _xsltTypeIndex;
        private XslTransform _xsltFunctionIndex;
        private XslTransform _xsltFilterIndex;
        private XslTransform _xsltTypeDoc;
        private XslTransform _xsltFunctionDoc;
        private XmlDocument _xmlDocumentation;
        private string _resourceDirectory;
        private StringDictionary _writtenFiles = new StringDictionary();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NAntDocumenter" /> class.
        /// </summary>
        public NAntDocumenter() : base("NAnt") {
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
                return ((NAntDocumenterConfig) Config).OutputDirectory;
            } 
        }

        /// <summary>
        /// Gets or sets the root namespace to document.
        /// </summary>
        /// <value>
        /// The root namespace to document, or a empty <see cref="string" />
        /// if no restriction should be set on the namespace to document.
        /// </value>
        public string NamespaceFilter {
            get {
                return ((NAntDocumenterConfig) Config).NamespaceFilter;
            }
        }

        /// <summary>
        /// Gets the name of the product for which documentation should be
        /// generated.
        /// </summary>
        /// <value>
        /// The name of the product for which documentation should be
        /// generated.
        /// </value>
        public string ProductName {
            get {
                return ((NAntDocumenterConfig) Config).ProductName;
            } 
        }

        /// <summary>
        /// Gets the version of the product for which documentation should be
        /// generated.
        /// </summary>
        /// <value>
        /// The version of the product for which documentation should be
        /// generated.
        /// </value>
        public string ProductVersion {
            get {
                return ((NAntDocumenterConfig) Config).ProductVersion;
            } 
        }

        /// <summary>
        /// Gets the URL of the website of the product for which documentation 
        /// should be generated.
        /// </summary>
        /// <value>
        /// The URL of the website of the product for which documentation should 
        /// be generated.
        /// </value>
        public string ProductUrl {
            get {
                return ((NAntDocumenterConfig) Config).ProductUrl;
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
            Config = new NAntDocumenterConfig();
        }

        /// <summary>
        /// Builds the documentation.
        /// </summary>
        public override void Build(NDoc.Core.Project project) {
            int buildStepProgress = 0;
            OnDocBuildingStep(buildStepProgress, "Initializing...");

            _resourceDirectory = Path.Combine(Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "NDoc"), "NAnt");

            // get assembly in which documenter is defined
            Assembly assembly = this.GetType().Module.Assembly;

            // write xslt files to resource directory
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.xslt", 
                Path.Combine(_resourceDirectory, "xslt"));

            // create the html output directories
            try {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "elements"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "functions"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "types"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "tasks"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "enums"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "filters"));
            } catch (Exception ex) {
                throw new DocumenterException("The output directories could not" 
                    + " be created.", ex);
            }

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Merging XML documentation...");

            // load the stylesheets that will convert the master xml into html pages
            MakeTransforms();

            // will hold the file name containing the NDoc generated XML
            string tempFile = null;

            try {
                // determine temporary file name
                tempFile = Path.GetTempFileName();

                // create the master XML document
                MakeXmlFile(project, tempFile);

                // create a xml document that will be transformed using xslt
                using (FileStream fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    _xmlDocumentation = new XmlDocument();
                    _xmlDocumentation.Load(fs);
                }
            } finally {
                // ensure temporary file is removed
                if (tempFile != null) {
                    File.Delete(tempFile);
                }
            }

            // build the file mapping
            buildStepProgress += 15;
            OnDocBuildingStep(buildStepProgress, "Building mapping...");

            // create arguments for nant index page transform
            XsltArgumentList indexArguments = CreateXsltArgumentList();

            // add extension object for NAnt utilities
            NAntXsltUtilities indexUtilities = NAntXsltUtilities.CreateInstance(
                _xmlDocumentation, (NAntDocumenterConfig) Config);

            // add extension object to Xslt arguments
            indexArguments.AddExtensionObject("urn:NAntUtil", indexUtilities);

            buildStepProgress += 15;
            OnDocBuildingStep(buildStepProgress, "Creating Task Index Page...");

            // transform nant task index page transform
            TransformAndWriteResult(_xsltTaskIndex, indexArguments, "tasks/index.html");

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Creating Type Index Page...");

            // transform nant type index page transform
            TransformAndWriteResult(_xsltTypeIndex, indexArguments, "types/index.html");

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Creating Filter Index Page...");

            // transform nant type index page transform
            TransformAndWriteResult(_xsltFilterIndex, indexArguments, "filters/index.html");

            OnDocBuildingStep(buildStepProgress, "Creating Function Index Page...");
            // transform nant function index page transform
            TransformAndWriteResult(_xsltFunctionIndex, indexArguments, "functions/index.html");

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Generating Task Documents...");

            // generate a page for each marked task
            XmlNodeList typeNodes = _xmlDocumentation.SelectNodes("//class[starts-with(substring(@id, 3, string-length(@id) - 2), '" + NamespaceFilter + "')]");
            foreach (XmlNode typeNode in typeNodes) {
                ElementDocType elementDocType = indexUtilities.GetElementDocType(typeNode);
                DocumentType(typeNode, elementDocType, indexUtilities);
            }

            OnDocBuildingStep(buildStepProgress, "Generating Function Documents...");
            
            // generate a page for each function - TODO - change the XPath expression to select more functions
            XmlNodeList functionNodes = _xmlDocumentation.SelectNodes("//method[attribute/@name = 'NAnt.Core.Attributes.FunctionAttribute' and ancestor::class[starts-with(substring(@id, 3, string-length(@id) - 2), '" + NamespaceFilter + "')]]");
            foreach (XmlElement function in functionNodes) {
                DocumentFunction(function, indexUtilities);
            }

            OnDocBuildingStep(100, "Complete");
        }

        #endregion Override implementation of IDocumenter

        #region Private Instance Methods

        private void DocumentType(XmlNode typeNode, ElementDocType docType, NAntXsltUtilities utilities) {
            if (typeNode == null) {
                throw new ArgumentNullException("typeNode");
            }

            if (docType == ElementDocType.None || docType == ElementDocType.FunctionSet) {
                // we don't need to document this type
                return;
            }

            string classID = typeNode.Attributes["id"].Value;
            if (!classID.Substring(2).StartsWith(NamespaceFilter)) {
                // we don't need to types in this namespace
                return;
            }

            string filename = utilities.GetFileNameForType(typeNode, false);
            if (filename == null) {
                // we should never get here, but just in case ...
                return;
            }

            if (_writtenFiles.ContainsValue(classID)) {
                return;
            } else {
                _writtenFiles.Add(filename, classID);
            }

            // create arguments for nant task page transform (valid args are class-id, refType, imagePath, relPathAdjust)
            XsltArgumentList arguments = CreateXsltArgumentList();
            arguments.AddParam("class-id", String.Empty, classID);

            string refTypeString;
            switch (docType) {
                case ElementDocType.DataTypeElement:
                    refTypeString = "Type";
                    break;
                case ElementDocType.Element:
                    refTypeString = "Element";
                    break;
                case ElementDocType.Task:
                    refTypeString = "Task";
                    break;
                case ElementDocType.Enum:
                    refTypeString = "Enum";
                    break;
                case ElementDocType.Filter:
                    refTypeString = "Filter";
                    break;
                default:
                    refTypeString = "Other?";
                    break;
            }

            arguments.AddParam("refType", string.Empty, refTypeString);

            // add extension object to Xslt arguments
            arguments.AddExtensionObject("urn:NAntUtil", utilities);

            // Process all sub-elements and generate docs for them. :)
            // Just look for properties with attributes to narrow down the foreach loop. 
            // (This is a restriction of NAnt.Core.Attributes.BuildElementAttribute)
            foreach (XmlNode propertyNode in typeNode.SelectNodes("property[attribute]")) {
                //get the xml element
                string elementName = utilities.GetElementNameForProperty(propertyNode);
                if (elementName != null) {
                    // try to get attribute info if it is an array/collection.
                    // strip the array brakets "[]" to get the type
                    string elementType = "T:" + propertyNode.Attributes["type"].Value.Replace("[]","");

                    // check whether property is an element array
                    XmlNode nestedElementNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(BuildElementArrayAttribute).FullName + "']");
                    if (nestedElementNode == null) {
                        // check whether property is an element collection
                        nestedElementNode = propertyNode.SelectSingleNode("attribute[@name='" + typeof(BuildElementCollectionAttribute).FullName + "']");
                    }

                    // if property is either array or collection type element
                    if (nestedElementNode != null) {
                        // select the item type in the collection
                        XmlAttribute elementTypeAttribute = _xmlDocumentation.SelectSingleNode("//class[@id='" + elementType + "']/method[@name='Add']/parameter/@type") as XmlAttribute;
                        if (elementTypeAttribute != null) {
                            // get type of collection elements
                            elementType = "T:" + elementTypeAttribute.Value;
                        }

                        // if it contains a ElementType attribute then it is an array or collection
                        // if it is a collection, then we care about the child type.
                        XmlNode explicitElementType = propertyNode.SelectSingleNode("attribute/property[@ElementType]");
                        if (explicitElementType != null) {
                            // ndoc is inconsistent about how classes are named.
                            elementType = explicitElementType.Attributes["value"].Value.Replace("+","."); 
                        }
                    }

                    XmlNode elementTypeNode = utilities.GetTypeNodeByID(elementType);
                    if (elementTypeNode != null) {
                        ElementDocType elementDocType = utilities.GetElementDocType(elementTypeNode);
                        if (elementDocType != ElementDocType.None) {
                            DocumentType(elementTypeNode, elementDocType, 
                                utilities);
                        }
                    }
                }
            }

            // create the page
            TransformAndWriteResult(_xsltTypeDoc, arguments, filename);
        }

        private void DocumentFunction(XmlElement functionElement, NAntXsltUtilities utilities) {
            if (functionElement == null) {
                throw new ArgumentNullException("functionElement");
            }

            string methodID = functionElement.GetAttribute("id");
            string filename = utilities.GetFileNameForFunction(functionElement, false);

            XsltArgumentList arguments = CreateXsltArgumentList();

            arguments.AddParam("method-id", string.Empty, methodID);
            arguments.AddParam("refType", string.Empty, "Function");
            arguments.AddParam("functionName", string.Empty, functionElement.GetAttribute("name"));

            // add extension object to Xslt arguments
            arguments.AddExtensionObject("urn:NAntUtil", utilities);

            // document parameter types
            foreach (XmlAttribute paramTypeAttribute in functionElement.SelectNodes("parameter/@type")) {
                string paramType = "T:" + paramTypeAttribute.Value;
                XmlNode typeNode = utilities.GetTypeNodeByID(paramType);
                if (typeNode != null) {
                    ElementDocType paramDocType = utilities.GetElementDocType(typeNode);
                    if (paramDocType != ElementDocType.None) {
                        DocumentType(typeNode, paramDocType, utilities);
                    }
                }
            }

            // document return type
            XmlAttribute returnTypeAttribute = functionElement.Attributes["returnType"];
            if (returnTypeAttribute != null) {
                string returnType = "T:" + returnTypeAttribute.Value;
                XmlNode returnTypeNode = utilities.GetTypeNodeByID(returnType);
                if (returnTypeNode != null) {
                    ElementDocType returnDocType = utilities.GetElementDocType(returnTypeNode);
                    if (returnDocType != ElementDocType.None) {
                        DocumentType(returnTypeNode, returnDocType, utilities);
                    }
                }
            }

            // create the page
            TransformAndWriteResult(_xsltFunctionDoc, arguments, filename);
        }

        private void MakeTransforms() {
            OnDocBuildingProgress(0);

            _xsltTaskIndex = new XslTransform();
            _xsltTypeIndex = new XslTransform();
            _xsltFunctionIndex = new XslTransform();
            _xsltFilterIndex = new XslTransform();
            _xsltTypeDoc = new XslTransform();
            _xsltFunctionDoc = new XslTransform();

            MakeTransform(_xsltTaskIndex, "task-index.xslt");
            OnDocBuildingProgress(20);
            MakeTransform(_xsltTypeIndex, "type-index.xslt");
            OnDocBuildingProgress(40);
            MakeTransform(_xsltFilterIndex, "filter-index.xslt");
            OnDocBuildingProgress(50);
            MakeTransform(_xsltFunctionIndex, "function-index.xslt");
            OnDocBuildingProgress(60);
            MakeTransform(_xsltTypeDoc, "type-doc.xslt");
            OnDocBuildingProgress(80);
            MakeTransform(_xsltFunctionDoc, "function-doc.xslt");
            OnDocBuildingProgress(100);
        }

        private void MakeTransform(XslTransform transform, string fileName) {
            transform.Load(Path.Combine(Path.Combine(_resourceDirectory, "xslt"), 
                fileName));
        }

        private XsltArgumentList CreateXsltArgumentList() {
            XsltArgumentList arguments = new XsltArgumentList();
            arguments.AddParam("productName", string.Empty, ProductName);
            arguments.AddParam("productVersion", string.Empty, ProductVersion);
            arguments.AddParam("productUrl", string.Empty, ProductUrl);
            return arguments;
        }

        private void TransformAndWriteResult(XslTransform transform, XsltArgumentList arguments, string filename) {
            string path = Path.Combine(OutputDirectory, filename);
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8)) {
                transform.Transform(_xmlDocumentation, arguments, writer);
            }
        }

        #endregion Private Instance Methods
    }

    /// <summary>
    /// Enumeration of possible types of a node.
    /// </summary>
    public enum ElementDocType {
        /// <summary>
        /// The node is an unknown node.
        /// </summary>
        None = 0,
        /// <summary>
        /// The node is a <see cref="Task"/> node.
        /// </summary>
        Task = 1,
        /// <summary>
        /// The node is a <see cref="DataTypeBase"/> node.
        /// </summary>
        DataTypeElement = 2,
        /// <summary>
        /// The node is an <see cref="Element"/> node.
        /// </summary>
        Element = 3,
        /// <summary>
        /// The node is an enumeration node.
        /// </summary>
        Enum = 4,
        /// <summary>
        /// The node is a <see cref="Filter"/> node.
        /// </summary>
        Filter = 5,
        /// <summary>
        /// The node is a <see cref="FunctionSet"/> node.
        /// </summary>
        FunctionSet = 6
    }
}
