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

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

using NDoc.Core;

namespace Sourceforge.NAnt.Documenter {

    /// <summary>NDoc Documenter for building custom NAnt User documentation.</summary>
    public class NAntTaskDocumenter : BaseDocumenter {

        XslTransform _xsltTaskIndex;
        XslTransform _xsltTaskDoc;
        XmlDocument _xmlDocumentation;
        string _resourceDirectory;

        public readonly string IndexFileName = "index.html";

        public NAntTaskDocumenter() : base("NAntTask") {
            Clear();
        }

        //------------------------------------------------------------------
        // Private helper members
        //------------------------------------------------------------------
        
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
                String msg = String.Format("Error compiling the '{0}' stylesheet:\n{1}", fileName, e.Message);
                throw new DocumenterException(msg, e);
            }
        }

        //------------------------------------------------------------------
        // IDocumenter Implementation
        //------------------------------------------------------------------

        /// <summary>See IDocumenter.</summary>
        public override void View() {}

        /// <summary>See IDocumenter.</summary>

        public override void Clear() {
            Config = new NAntTaskDocumenterConfig();
        }

        /// <summary>See IDocumenter.</summary>
        public override void Build(Project project) {

            OnDocBuildingStep(0, "Initializing...");
            // Define this when you want to edit the stylesheets
            // without having to shutdown the application to rebuild.
            #if NO_RESOURCES

            string mainModuleDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //_resourceDirectory = Path.GetFullPath(Path.Combine(mainModuleDirectory, @"..\..\..\Documenter\NAntTask\"));
            #else

            _resourceDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData) +
                "\\NDoc\\NAntTasks\\";

            EmbeddedResources.WriteEmbeddedResources(
                this.GetType().Module.Assembly,
                "Documenter.css",
                _resourceDirectory + "css\\");

            EmbeddedResources.WriteEmbeddedResources(
                this.GetType().Module.Assembly,
                "Documenter.xslt",
                _resourceDirectory + "xslt\\");

            EmbeddedResources.WriteEmbeddedResources(
                this.GetType().Module.Assembly,
                "Documenter.html",
                _resourceDirectory + "html\\");
            #endif

            // Create the html output directory if it doesn't exist.
            if (!Directory.Exists(MyConfig.OutputDirectory)) {
                Directory.CreateDirectory(MyConfig.OutputDirectory);
            }

            // Copy our cascading style sheet to the html output directory
            /*
            File.Copy(
            _resourceDirectory + @"css\NAntDoc.css",
            MyConfig.OutputDirectory + "\\NAntDoc.css",
            true);
            File.Copy(
            _resourceDirectory + @"html\index.html",
            MyConfig.OutputDirectory + "\\index.html",
            true);
            File.Copy(
            _resourceDirectory + @"html\NAntTaskRef.html",
            MyConfig.OutputDirectory + "\\NAntTaskRef.html",
            true);
            */

            OnDocBuildingStep(10, "Merging XML documentation...");

            MakeXml(project);

            // Load the xslt
            MakeTransforms();

            // Now the individual tasks
            // Load the XML documentation into a DOM.
            _xmlDocumentation = new XmlDocument();
            _xmlDocumentation.LoadXml(Document.OuterXml);

            // Generate the Index ..
            XsltArgumentList arguments = new XsltArgumentList();

            TransformAndWriteResult(_xsltTaskIndex, arguments, IndexFileName);

            XmlNodeList taskAttrNodes = _xmlDocumentation.SelectNodes("/ndoc/assembly/module/namespace/class/attribute[@name = 'SourceForge.NAnt.Attributes.TaskNameAttribute']");
            foreach (XmlNode node in taskAttrNodes) {

                XmlNode taskNode = node.ParentNode ;
                XmlNode propNode = node.SelectSingleNode("property[@name='Name']");
                // Get the string
                string classID = taskNode.Attributes["id"].Value;
                string className = taskNode.Attributes["name"].Value;
                XsltArgumentList docArguments = new XsltArgumentList();
                docArguments.AddParam("class-id", String.Empty, classID);
                
                string taskFileName = propNode.Attributes["value"].Value.ToLower() + "Task.html";;    
                TransformAndWriteResult(_xsltTaskDoc, docArguments, taskFileName);
            }
        }

        private void TransformAndWriteResult(XslTransform transform, XsltArgumentList arguments, string filename) {
            StreamWriter streamWriter = null;
            try {
                streamWriter = new StreamWriter(
                    File.Open(Path.Combine(MyConfig.OutputDirectory, filename), FileMode.Create),
                    new ASCIIEncoding());
                    //new UTF8Encoding(true));
                transform.Transform(_xmlDocumentation, arguments, streamWriter);
            } finally {
                if (streamWriter != null) {
                    streamWriter.Close();
                }
            }
        }

        private NAntTaskDocumenterConfig MyConfig {
            get {
                return (NAntTaskDocumenterConfig) Config;
            }
        }

        public override string MainOutputFile { get {return "";} }
    }
}
