// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Serge (serge@wildwestsoftware.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    ///   <para>
    ///   Processes a document via XSLT.</para>
    /// </summary>
    /// <example>
    ///   <para>Create a report in HTML.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="report.xsl" in="data.xml" out="report.html" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Create a report in HTML, with a param.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="report.xsl" in="data.xml" out="report.html">
    ///     <parameters>
    ///         <parameter name="reportType" namespaceuri="" value="Plain" />
    ///     </parameters>
    /// </style>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Create a report in HTML, with a expanded param.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="report.xsl" in="data.xml" out="report.html">
    ///     <parameters>
    ///         <parameter name="reportType" namespaceuri="" value="${report.type}" />
    ///     </parameters>
    /// </style>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    /// <para>Create some code based on a directory of templates.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="CodeGenerator.xsl" extension="java">
    ///     <infiles>
    ///         <includes name="*.xml" />
    ///     </infiles>
    ///     <parameters>
    ///         <parameter name="reportType" namespaceuri="" value="Plain" if="${report.plain}" />
    ///     </parameters>
    /// <style>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("style")]
    public class StyleTask : Task {
        #region Private Instance Fields
                
        private string _baseDir = null;
        private string _destDir = null;
        private string _extension = "html";
        private string _xsltFile = null;
        private string _srcFile = null;
        private string _outputFile = null;
        private FileSet _inFiles = new FileSet();
        private XsltParameterCollection _xsltParameters = new XsltParameterCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Where to find the source XML file, default is the project's basedir.
        /// </summary>
        [TaskAttribute("basedir", Required=false)]
        public string BaseDir {
            get { return _baseDir; }
            set { _baseDir = value; }
        }
        
        /// <summary>
        /// Directory in which to store the results.
        /// </summary>
        [TaskAttribute("destdir", Required=false)]
        public string DestDir {
            get { return _destDir; }
            set { _destDir = value; }
        }
        
        /// <summary>
        /// Desired file extension to be used for the targets. The default is 
        /// <c>html</c>.
        /// </summary>
        [TaskAttribute("extension", Required=false)]
        public string Extension {
            get { return _extension; }
            set { _extension = value; }
        }
        
        /// <summary>
        /// Name of the stylesheet to use - given either relative to the project's 
        /// basedir or as an absolute path.
        /// </summary>
        [TaskAttribute("style", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string StyleSheet {
            get { return _xsltFile; }
            set { _xsltFile = value; }
        }
        
        /// <summary>
        /// Specifies a single XML document to be styled. Should be used with 
        /// the <see cref="OutputFile" /> attribute.
        /// </summary>
        [TaskAttribute("in", Required=false)]
        public string SrcFile {
            get { return _srcFile; }
            set { _srcFile = value; }
        }
        
        /// <summary>
        /// Specifies the output name for the styled result from the <see cref="SrcFile" /> 
        /// attribute.
        /// </summary>
        [TaskAttribute("out", Required=false)]
        public string OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Specifies a group of input files to which to apply the stylesheet.
        /// </summary>
        [FileSet("infiles")]
        public FileSet InFiles {
            get { return _inFiles; }
            set { _inFiles = value; }
        }

        /// <summary>
        /// XSLT parameters to be passed to the XSLT transformation.
        /// </summary>
        [BuildElementCollection("parameters", "parameter")]
        public XsltParameterCollection Parameters {
            get { return _xsltParameters; }
        }

        #region Override implementation of Task

        protected override void InitializeTask(XmlNode taskNode) {
            // deprecated as of NAnt 0.8.4
            // TO-DO : remove this after NAnt 0.8.5 or so
            // Load parameters
            foreach (XmlNode node in taskNode) {
                if (node.LocalName.Equals("param")) {
                    Log(Level.Warning, "The usage of the <param> element is" 
                        + " deprecated. Please use the <parameters> collection" 
                        + " instead.");

                    // create and fill XsltParameter
                    XsltParameter xsltParameter = new XsltParameter();
                    xsltParameter.ParameterName = Project.ExpandProperties(node.Attributes["name"].Value, Location);
                    xsltParameter.Value = Project.ExpandProperties(node.Attributes["expression"].Value, Location);

                    // add parameter to collection
                    _xsltParameters.Add(xsltParameter);
                }
            }
        }

        protected override void ExecuteTask() {
            StringCollection srcFiles = null;
            if (!StringUtils.IsNullOrEmpty(SrcFile)) {
                srcFiles = new StringCollection();
                srcFiles.Add(SrcFile);
            } else if (InFiles.FileNames.Count > 0) {
                if (!StringUtils.IsNullOrEmpty(OutputFile)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "The \"out\" attribute is not allowed when \"infiles\" is used."), 
                        Location);
                }
                srcFiles = InFiles.FileNames;
            }

            if (srcFiles == null || srcFiles.Count == 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "No source files indicates; use \"in\" or \"infiles\"."), 
                    Location);
            }

            string basedirPath = Project.GetFullPath(BaseDir);
            string destdirPath = Project.GetFullPath(DestDir);
            string xsltPath = Path.GetFullPath(Path.Combine(basedirPath, StyleSheet));
            FileInfo xsltInfo = new FileInfo(xsltPath);
            if (!xsltInfo.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Unable to find stylesheet file {0}.", xsltPath), Location);
            }

            foreach (string srcFile in srcFiles) {
                string destFile = OutputFile;
                if (StringUtils.IsNullOrEmpty(destFile)) {
                    // TODO: use System.IO.Path (gs)
                    // append extension if necessary
                    string ext = Extension.IndexOf(".") > -1 ? Extension : "." + Extension;
                    int extPos = srcFile.LastIndexOf('.');
                    if (extPos == -1) {
                        destFile = srcFile + ext;
                    } else {
                        destFile = srcFile.Substring(0, extPos) + ext;
                    }
                    destFile = Path.GetFileName(destFile);
                }

                string srcPath  = Path.GetFullPath(Path.Combine(basedirPath, srcFile));
                string destPath = Path.GetFullPath(Path.Combine(destdirPath, destFile));
                FileInfo srcInfo  = new FileInfo(srcPath);
                FileInfo destInfo = new FileInfo(destPath);

                if (!srcInfo.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Unable to find source XML file {0}.", srcPath), Location);
                }

                bool destOutdated = !destInfo.Exists
                    || srcInfo.LastWriteTime  > destInfo.LastWriteTime
                    || xsltInfo.LastWriteTime > destInfo.LastWriteTime;

                if (destOutdated) {
                    XmlReader xmlReader = null;
                    XmlReader xslReader = null;
                    TextWriter writer = null;
    
                    try {
                        // load the xml that needs to be transformed
                        Log(Level.Verbose, LogPrefix + "Loading xml {0}.", 
                            srcPath);
                        xmlReader = CreateXmlReader(srcPath);
                        XPathDocument xml = new XPathDocument(xmlReader);
    
                        // load the stylesheet
                        Log(Level.Verbose, LogPrefix + "Loading stylesheet {0}.", 
                            xsltPath);
                        xslReader = CreateXmlReader(xsltPath);
                        XslTransform xslt = new XslTransform();
                        xslt.Load(xslReader);

                        // initialize xslt parameters
                        XsltArgumentList xsltArgs = new XsltArgumentList();

                        // set the xslt parameters
                        foreach (XsltParameter parameter in Parameters) {
                            if (IfDefined && !UnlessDefined) {
                                xsltArgs.AddParam(parameter.ParameterName, 
                                    parameter.NamespaceUri, parameter.Value);
                            }
                        }
    
                        // create writer for the destination xml
                        writer = CreateWriter(destPath);

                        // do the actual transformation 
                        Log(Level.Info, LogPrefix + "Processing {0} to {1}.", 
                            srcPath, destPath);
                        xslt.Transform(xml, xsltArgs, writer);
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Could not perform XSLT transformation of {0} using" 
                            + " stylesheet {1}.", srcPath, xsltPath), Location, 
                            ex);
                    } finally {
                        // ensure file handles are closed
                        if (xmlReader != null) {
                            xmlReader.Close();
                        }
                        if (xslReader != null) {
                            xslReader.Close();
                        }
                        if (writer != null) {
                            writer.Close();
                        }
                    }
                }
            }
        }

        #endregion Override implementation of Task

        #endregion Public Instance Properties

        #region Protected Instance Methods

        protected virtual XmlReader CreateXmlReader(string file) {
            XmlTextReader xmlReader = new XmlTextReader(new FileStream(file, FileMode.Open, FileAccess.Read));
            return xmlReader;
        }

        protected virtual TextWriter CreateWriter(string filepath) {
            string xmlPath = filepath;
            TextWriter writer = null;

            string targetDir = Path.GetDirectoryName(Path.GetFullPath(xmlPath));
            if (!StringUtils.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
            }
            // UTF-8 encoding will be used
            //xmlWriter = new XmlTextWriter(xmlPath, null);
            // Create text writer first
            writer = new StreamWriter(xmlPath);

            return writer;
        }

        #endregion Protected Instance Methods
    }
}
