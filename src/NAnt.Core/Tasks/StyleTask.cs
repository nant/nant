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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// <para>
    ///     Processes a document via XSLT.</para>
    /// <para>
    ///     This is useful for building views of XML based documentation, or in generating code.</para>
    /// <para>
    ///     Note: <![CDATA[<param name="" expression=""/>]]> are allowed.
    /// </para>
    /// 
    /// </summary>
    /// <example>
    ///   <para>Create a report in HTML.</para>
    ///   <code>&lt;style style="report.xsl" in="data.xml" out="report.html" /&gt;</code>
    /// </example>
    /// <example>
    ///   <para>Create a report in HTML, with a param.</para>
    ///   <code><![CDATA[
    ///     <style style="report.xsl" in="data.xml" out="report.html">
    ///         <param name="reportType" expression="Plain"/>
    ///     </style>
    ///   ]]></code>
    /// </example>
    /// <example>
    ///   <para>Create a report in HTML, with a expanded param.</para>
    ///   <code><![CDATA[
    ///     <style style="report.xsl" in="data.xml" out="report.html">
    ///         <param name="reportType" expression="${report.type}"/>
    ///     </style>
    ///   ]]></code>
    /// </example>
    /// <example>
    /// <para>Create a some code based on a directory of templates.</para>
    ///   <code><![CDATA[
    /// <style style="CodeGenerator.xsl" extension="java">
    ///   <infiles>
    ///     <includes name="*.xml"/>
    ///   </infiles>
    /// <style>
    /// ]]></code>
    /// </example> 
    [TaskName("style")]
    public class StyleTask : Task {
        #region Private Instance Fields
                
        string _baseDir = null;
        string _destDir = null;
        string _extension = "html";
        string _xsltFile = null;
        string _srcFile = null;
        string _outputFile = null;
        FileSet _inFiles = new FileSet();

        Hashtable _params = new Hashtable(); // TODO sort this out with an attribute

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>Where to find the source XML file, default is the project's basedir.</summary>
        [TaskAttribute("basedir", Required=false)]
        public string BaseDir                  { get { return _baseDir; } set { _baseDir = value; } }
        
        /// <summary>Directory in which to store the results.</summary>
        [TaskAttribute("destdir", Required=false)]
        public string DestDir                  { get { return _destDir; } set { _destDir = value; } }
        
        /// <summary>Desired file extension to be used for the targets. The default is "html".</summary>
        [TaskAttribute("extension", Required=false)]
        public string Extension                { get { return _extension; } set { _extension = value; } }
        
        /// <summary>Name of the stylesheet to use - given either relative to the project's basedir or as an absolute path.</summary>
        [TaskAttribute("style", Required=true)]
        public string StyleSheet               { get { return _xsltFile; } set { _xsltFile = value; } }
        
        /// <summary>Specifies a single XML document to be styled. Should be used with the out attribute.</summary>
        [TaskAttribute("in", Required=false)]
        public string SrcFile                  { get { return _srcFile; } set { _srcFile = value; } }
        
        /// <summary>Specifies the output name for the styled result from the in attribute.</summary>
        [TaskAttribute("out", Required=false)]
        public string OutputFile               { get { return _outputFile; } set { _outputFile = value; } }

       
        /// <summary>Specifies a group of input files to which to apply the stylesheet.</summary>
        [FileSet("infiles")]
        public FileSet InFiles                 { get { return _inFiles; } }

        #region Override implementation of Task

        ///<param name="taskNode"> taskNode used to define this task instance </param>
        protected override void InitializeTask(XmlNode taskNode) {
            // Load parameters
            foreach (XmlNode node in taskNode) {
                if(node.Name.Equals("param")) {
                    string paramname = Project.ExpandProperties(node.Attributes["name"].Value, Location );
                    string paramval = Project.ExpandProperties(node.Attributes["expression"].Value, Location);
                    _params[paramname] = paramval;
                }
            }
        }

        protected override void ExecuteTask() {
            StringCollection srcFiles = null;
            if(SrcFile != null && SrcFile.Length > 0) {
                srcFiles = new StringCollection();
                srcFiles.Add(SrcFile);
            } else if(InFiles.FileNames.Count > 0) {
                if(OutputFile != null && OutputFile.Trim().Length > 0) {
                    string msg = String.Format(CultureInfo.InvariantCulture, "The \"out\" attribute is not allowed when \"infiles\" is used.");
                    throw new BuildException(msg, Location);
                }
                srcFiles = InFiles.FileNames;
            }

            if(srcFiles == null || srcFiles.Count == 0) {
                string msg = String.Format(CultureInfo.InvariantCulture, "No source files indicates; use \"in\" or \"infiles\".");
                throw new BuildException(msg, Location);
            }

            string basedirPath = Project.GetFullPath(BaseDir);
            string destdirPath = Project.GetFullPath(DestDir);
            string xsltPath = Path.GetFullPath(Path.Combine(basedirPath, StyleSheet));
            FileInfo xsltInfo = new FileInfo(xsltPath);
            if (!xsltInfo.Exists) {
                string msg = String.Format(CultureInfo.InvariantCulture, "Unable to find stylesheet file {0}", xsltPath);
                throw new BuildException(msg, Location);
            }           
            foreach(string srcFile in srcFiles) {
                string destFile = OutputFile;
                if (destFile == null || destFile.Length == 0) {
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
                    string msg = String.Format(CultureInfo.InvariantCulture, "Unable to find source XML file {0}", srcPath);
                    throw new BuildException(msg, Location);
                }

                bool destOutdated = !destInfo.Exists
                    || srcInfo.LastWriteTime  > destInfo.LastWriteTime
                    || xsltInfo.LastWriteTime > destInfo.LastWriteTime;

                if (destOutdated) {
                    XmlReader xmlReader = null;
                    XmlReader xslReader = null;
                    TextWriter writer = null;
    
                    try {
                        xmlReader = CreateXmlReader(srcPath);
                        xslReader = CreateXmlReader(xsltPath);
                        writer = CreateWriter(destPath);
    
                        if (Verbose) {
                            Log.WriteLine(LogPrefix + "Transforming into " + destdirPath );
                        }
    
                        XslTransform xslt = new XslTransform();
                        XPathDocument xml = new XPathDocument(xmlReader);
                        XsltArgumentList scriptargs = new XsltArgumentList();
    
                        if (Verbose) {
                            Log.WriteLine(LogPrefix + "Loading stylesheet " + Path.GetFullPath(xsltPath));
                        }
    
                        xslt.Load(xslReader);
    
                        // Load paramaters
                        foreach (string key in _params.Keys) {
                            scriptargs.AddParam(key, "", (string) _params[key]);
                        }
    
                        Log.WriteLine(LogPrefix + "Processing " + Path.GetFullPath(srcPath) + " to " + Path.GetFullPath(destPath));
                        xslt.Transform(xml, scriptargs, writer);
    
                    } catch (Exception e) {
                        throw new BuildException("Could not perform XSLT transformation.", Location, e);
                    } finally {
                        // Ensure file handles are closed
                        if (xmlReader != null) { xmlReader.Close(); }
                        if (xslReader != null) { xslReader.Close(); }
                        if (writer != null) { writer.Close(); }
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
            if (targetDir != null && targetDir.Length > 0 && !Directory.Exists(targetDir)) {
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
