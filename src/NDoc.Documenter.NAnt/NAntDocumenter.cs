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

// File Maintainers:
// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using NDoc.Core;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// NDoc Documenter for building custom NAnt User documentation.
    /// </summary>
    public class NAntTaskDocumenter : BaseDocumenter {
        #region Private Instance Fields

        private XslTransform _xsltTaskIndex;
        private XslTransform _xsltTaskDoc;
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
        /// <value>The documenter's output directory.</value>
        public string OutputDirectory { 
            get {
                return ((NAntTaskDocumenterConfig) Config).OutputDirectory;
            } 
        }

        #endregion Public Instance Properties

        #region Override implementation of IDocumenter

        /// <summary>
        /// Gets the documenter's main output file.
        /// </summary>
        /// <value>The documenter's main output file</value>
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
        public override void Build(Project project) {
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
            _xmlDocumentation.LoadXml(Document.OuterXml); 

            // build the file mapping
            OnDocBuildingStep(25, "Building mapping...");
            MakeFilenames(_xmlDocumentation);

            // transform nant task index page transform (requires no arguments)
            TransformAndWriteResult(_xsltTaskIndex, "index.html");

            // generate a page for each marked task
            XmlNodeList taskAttrNodes = _xmlDocumentation.SelectNodes("/ndoc/assembly/module/namespace/class/attribute[@name = 'NAnt.Core.Attributes.TaskNameAttribute']");
            foreach (XmlNode node in taskAttrNodes) {
                // create arguments for nant task page transform
                XsltArgumentList arguments = new XsltArgumentList();
                string classID = node.ParentNode.Attributes["id"].Value;
                arguments.AddParam("class-id", String.Empty, classID);

                // add extension object for NAnt utilities
                NAntXsltUtilities utilities = new NAntXsltUtilities(_fileNames, _elementNames, _namespaceNames, _assemblyNames, _taskNames);
                arguments.AddExtensionObject("urn:NAntUtil", utilities);

                // generate filename for page
                XmlNode propNode = node.SelectSingleNode("property[@name='Name']");
                string filename = propNode.Attributes["value"].Value.ToLower(CultureInfo.InvariantCulture) + "task.html";;    

                // create the page
                TransformAndWriteResult(_xsltTaskDoc, arguments, filename);
            }
        }

        #endregion Override implementation of IDocumenter

        #region Private Instance Methods

        private void MakeTransforms() {
            OnDocBuildingProgress(0);

            _xsltTaskIndex = new XslTransform();
            _xsltTaskDoc = new XslTransform();

            OnDocBuildingProgress(50);
            MakeTransform(_xsltTaskIndex, "task-index.xslt");

            OnDocBuildingProgress(100);
            MakeTransform(_xsltTaskDoc, "task-doc.xslt");
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
            NAntXsltUtilities utilities = new NAntXsltUtilities(_fileNames, _elementNames, _namespaceNames, _assemblyNames, _taskNames);
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
//                    fileNames[typeId] = GetFilenameForType(typeNode);
                    _elementNames[typeId] = typeNode.Attributes["name"].Value;
                    _namespaceNames[typeId] = namespaceName;
                    _assemblyNames[typeId] = assemblyName;

                    // check whether the type actually derives from NAnt.Core.Task
                    if (typeNode.SelectSingleNode("descendant::base[@id='T:NAnt.Core.Task']") != null) {
                        // get the actual task name
                        XmlAttribute taskNameAttribute = typeNode.SelectSingleNode("attribute/property[@name='Name']/@value") as XmlAttribute;
                        if (taskNameAttribute != null) {
                            _taskNames[typeId] = taskNameAttribute.Value;
                        }
                    }

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
                                _elementNames[id] = memberNode.Attributes["name"].Value;
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

        #endregion Private Instance Methods
    }
}
