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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Globalization;

using NDoc.Core;

namespace Sourceforge.NAnt.Documenter {

    /// <summary>NDoc Documenter for building custom NAnt User documentation.</summary>
    public class NAntTaskDocumenter : BaseDocumenter {

        XslTransform _xsltTaskIndex;
        XslTransform _xsltTaskDoc;
        XmlDocument _xmlDocumentation;
        string _resourceDirectory;

        public NAntTaskDocumenter() : base("NAntTask") {
            Clear();
        }

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

        // IDocumenter Implementation
        public override string MainOutputFile { 
            get { return ""; } 
        }

        /// <summary>See IDocumenter.</summary>
        /// 
        public override void Clear() {
            Config = new NAntTaskDocumenterConfig();
        }

        public string OutputDirectory { 
            get {
                return ((NAntTaskDocumenterConfig) Config).OutputDirectory;
            } 
        }

        /// <summary>See IDocumenter.</summary>
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

            // transform nant task index page transform (requires no arguments)
            TransformAndWriteResult(_xsltTaskIndex, "index.html");

            // generate a page for each marked task
            XmlNodeList taskAttrNodes = _xmlDocumentation.SelectNodes("/ndoc/assembly/module/namespace/class/attribute[@name = 'SourceForge.NAnt.Attributes.TaskNameAttribute']");
            foreach (XmlNode node in taskAttrNodes) {
                // create arguments for nant task page transform
                XsltArgumentList arguments = new XsltArgumentList();
                string classID = node.ParentNode.Attributes["id"].Value;
                arguments.AddParam("class-id", String.Empty, classID);

                // generate filename for page
                XmlNode propNode = node.SelectSingleNode("property[@name='Name']");
                string filename = propNode.Attributes["value"].Value.ToLower(CultureInfo.InvariantCulture) + "task.html";;    

                // create the page
                TransformAndWriteResult(_xsltTaskDoc, arguments, filename);
            }
        }

        private void TransformAndWriteResult(XslTransform transform, string filename) {
            XsltArgumentList arguments = new XsltArgumentList();
            TransformAndWriteResult(transform, arguments, filename);
        }

        private void TransformAndWriteResult(XslTransform transform, XsltArgumentList arguments, string filename) {
            string path = Path.Combine(OutputDirectory, filename);
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.ASCII)) {
                transform.Transform(_xmlDocumentation, arguments, writer);
            }
        }
    }
}
