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
// Scott Hernandez (ScottHernandez_hotmail_com)

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
        private XslTransform _xsltTypeIndex;
        private XslTransform _xsltTypeDoc;        
        private XmlDocument _xmlDocumentation;
        private string _resourceDirectory;
        private StringDictionary _writtenFiles = new StringDictionary();

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
            int buildStepProgress = 0;
            OnDocBuildingStep(buildStepProgress, "Initializing...");

            _resourceDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\NDoc\\NAntTasks\\";

            System.Reflection.Assembly assembly = this.GetType().Module.Assembly;
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.css", _resourceDirectory + "css\\");
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.xslt", _resourceDirectory + "xslt\\");
            EmbeddedResources.WriteEmbeddedResources(assembly, "Documenter.html", _resourceDirectory + "html\\");

            // create the html output directory.
            try {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "types"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "tasks"));
                Directory.CreateDirectory(Path.Combine(OutputDirectory, "elements"));
            } catch (Exception ex) {
                throw new DocumenterException("The output directories could not" 
                    + " be created.", ex);
            }

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Merging XML documentation...");

            // crate the master xml document that contains all the documentation
            MakeXml(project);

            // load the stylesheets that will convert the master xml into html pages
            MakeTransforms();

            // create a xml document that will get transformed by xslt
            _xmlDocumentation = new XmlDocument();
            _xmlDocumentation.LoadXml( XmlBuffer); 

            // build the file mapping
            buildStepProgress += 15;
            OnDocBuildingStep(buildStepProgress, "Building mapping...");
            //MakeFilenames(_xmlDocumentation);

            // create arguments for nant index page transform
            XsltArgumentList indexArguments = new XsltArgumentList();

            // add extension object for NAnt utilities
            NAntXsltUtilities indexUtilities = NAntXsltUtilities.CreateInstance(_xmlDocumentation, LinkToSdkDocVersion);

            // add extension object to Xslt arguments
            indexArguments.AddExtensionObject("urn:NAntUtil", indexUtilities);

            buildStepProgress += 15;
            OnDocBuildingStep(buildStepProgress, "Creating Task Index Page...");

            // transform nant task index page transform
            TransformAndWriteResult(_xsltTaskIndex, indexArguments, "tasks.html");

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Creating Type Index Page...");

            // transform nant type index page transform
            TransformAndWriteResult(_xsltTypeIndex, indexArguments, "types.html");

            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Generating Task Documents...");

            // generate a page for each marked task
            XmlNodeList taskAttrNodes = _xmlDocumentation.SelectNodes("//class[attribute/@name = 'NAnt.Core.Attributes.TaskNameAttribute']");
            foreach (XmlNode taskNode in taskAttrNodes) {
                //OnDocBuildingStep(buildStepProgress++, "Doc'n Task:" + taskNode.Attributes["id"].Value);
                DocumentType(taskNode, ElementDocType.Task);
            }
            
            buildStepProgress += 10;
            OnDocBuildingStep(buildStepProgress, "Generating Type Documents...");
            // generate a page for each marked type
            XmlNodeList typeAttrNodes = _xmlDocumentation.SelectNodes("//class[descendant::base/@id='T:" + typeof(DataTypeBase).FullName + "']");
            foreach (XmlNode typeNode in typeAttrNodes) {
                //OnDocBuildingStep(buildStepProgress++, "Doc'n DataType:" + typeNode.Attributes["id"].Value);
                DocumentType(typeNode, ElementDocType.DataTypeElement);
            }
            OnDocBuildingStep(100, "Complete");

        }

        #endregion Override implementation of IDocumenter

        #region Private Instance Methods
        
        private void DocumentType(XmlNode typeNode, ElementDocType docType) {
            if (typeNode == null) {
                throw new ArgumentNullException("typeNode");
            }

            // do not document tasks that are deprecated and have the IsError 
            // property of ObsoleteAttribute set to "true"
            XmlNode obsoleteErrorNode = typeNode.SelectSingleNode("attribute[@name = 'System.ObsoleteAttribute']/property[@name='IsError']");
            if (obsoleteErrorNode != null) {
                if (Convert.ToBoolean(obsoleteErrorNode.Attributes["value"].Value)) {
                    return;
                }
            }
            
            string classID = typeNode.Attributes["id"].Value;
            string filename = NAntXsltUtilities.GetFileNameForType(typeNode);
            if (_writtenFiles.ContainsValue(classID)) {
                return;
            } else {
                _writtenFiles.Add(filename, classID);
            }

            //Console.WriteLine(classID + " --> " + filename);


            // create arguments for nant task page transform (valid args are class-id, refType, imagePath, relPathAdjust)
            XsltArgumentList arguments = new XsltArgumentList();
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
                default:
                    refTypeString = "Other?";
                    break;
            }

            arguments.AddParam("refType", string.Empty, refTypeString);

            // add extension object for NAnt utilities
            NAntXsltUtilities utilities = NAntXsltUtilities.CreateInstance(_xmlDocumentation, LinkToSdkDocVersion);

            // add extension object to Xslt arguments
            arguments.AddExtensionObject("urn:NAntUtil", utilities);

            // generate filename for page
            //Console.Write(classID);
            //Console.WriteLine(" filename is " + filename);

            // Process all sub-elements and generate docs for them. :)
            // Just look for properties with attributes to narrow down the foreach loop. 
            // (This is a restriction of NAnt.Core.Attributes.BuildElementAttribute)
            foreach (XmlNode propertyNode in typeNode.SelectNodes("property[attribute]")) {
                //get the xml element
                string elementName = NAntXsltUtilities.GetElementNameForProperty(propertyNode);
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
                            // HACK : make sure the collection class is documented too
                            XmlNode collectionNode = _xmlDocumentation.SelectSingleNode("//class[@id='" + elementType + "']");
                            if (collectionNode != null) {
                                DocumentType(collectionNode, ElementDocType.Element);
                            }

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

                    //ElementDocType type = _elementNames[elementType] == null ? ElementDocType.DataTypeElement : ElementDocType.Element;
                    XmlNode elementNode = _xmlDocumentation.SelectSingleNode("//class[@id='" + elementType + "']");
                    if (elementNode == null) {
                        //Console.WriteLine(elementType + " not found in document");
                    } else {
                        DocumentType(elementNode, ElementDocType.Element);
                    }
                }
            }

            // create the page
            TransformAndWriteResult(_xsltTypeDoc, arguments, filename);
        }
        private void MakeTransforms() {
            OnDocBuildingProgress(0);

            _xsltTaskIndex = new XslTransform();
            _xsltTypeIndex = new XslTransform();
            _xsltTypeDoc = new XslTransform();

            OnDocBuildingProgress(25);
            MakeTransform(_xsltTaskIndex, "task-index.xslt");
            
            OnDocBuildingProgress(75);
            MakeTransform(_xsltTypeIndex, "type-index.xslt");

            OnDocBuildingProgress(100);
            MakeTransform(_xsltTypeDoc, "type-doc.xslt");
        }


        private void MakeTransform(XslTransform transform, string fileName) {
            transform.Load(_resourceDirectory + "xslt/" + fileName);
        }

        private void TransformAndWriteResult(XslTransform transform, string filename) {
            XsltArgumentList arguments = new XsltArgumentList();

            // add extension object for NAnt utilities
            NAntXsltUtilities utilities = NAntXsltUtilities.CreateInstance(_xmlDocumentation, LinkToSdkDocVersion);

            arguments.AddExtensionObject("urn:NAntUtil", utilities);

            TransformAndWriteResult(transform, arguments, filename);
        }

        private void TransformAndWriteResult(XslTransform transform, XsltArgumentList arguments, string filename) {
            string path = Path.Combine(OutputDirectory, filename);
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.ASCII)) {
                transform.Transform(_xmlDocumentation, arguments, writer);
            }
        }
        #endregion
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
    public enum ElementDocType {
        Task,
        DataTypeElement,
        Element,
        Inline
    }
}
