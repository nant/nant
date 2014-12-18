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
// Tim Noll (tim.noll@gmail.com)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Processes a document via XSLT.
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
    ///   <para>Create some code based on a directory of templates.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="CodeGenerator.xsl" extension="java">
    ///     <infiles>
    ///         <include name="*.xml" />
    ///     </infiles>
    ///     <parameters>
    ///         <parameter name="reportType" namespaceuri="" value="Plain" if="${report.plain}" />
    ///     </parameters>
    /// <style>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Create a report in HTML, with an extension object.</para>
    ///   <code>
    ///     <![CDATA[
    /// <style style="report.xsl" in="data.xml" out="report.html">
    ///     <extensionobjects>
    ///         <extensionobject namespaceuri="urn:Formatter" typename="XsltExtensionObjects.Formatter" assembly="XsltExtensionObjects.dll" />
    ///     </extensionobjects>
    /// </style>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("style")]
    public class StyleTask : Task {
        #region Private Instance Fields
                
        private DirectoryInfo _destDir;
        private string _extension = "html";
        private Uri _xsltFile;
        private FileInfo _srcFile;
        private FileInfo _outputFile;
        private FileSet _inFiles = new FileSet();
        private XsltParameterCollection _xsltParameters = new XsltParameterCollection();
        private XsltExtensionObjectCollection _xsltExtensions = new XsltExtensionObjectCollection();
        private Proxy _proxy;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Directory in which to store the results. The default is the project
        /// base directory.
        /// </summary>
        [TaskAttribute("destdir", Required=false)]
        public DirectoryInfo DestDir {
            get { 
                if (_destDir == null) {
                    return new DirectoryInfo(Project.BaseDirectory);
                }
                return _destDir; 
            }
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
        /// URI or path that points to the stylesheet to use. If given as path, it can
        /// be relative to the project's basedir or absolute.
        /// </summary>
        [TaskAttribute("style", Required=true)]
        public Uri XsltFile {
            get { return _xsltFile; }
            set { _xsltFile = value; }
        }
        
        /// <summary>
        /// Specifies a single XML document to be styled. Should be used with 
        /// the <see cref="OutputFile" /> attribute.
        /// </summary>
        [TaskAttribute("in", Required=false)]
        public FileInfo SrcFile {
            get { return _srcFile; }
            set { _srcFile = value; }
        }
        
        /// <summary>
        /// Specifies the output name for the styled result from the <see cref="SrcFile" /> 
        /// attribute.
        /// </summary>
        [TaskAttribute("out", Required=false)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Specifies a group of input files to which to apply the stylesheet.
        /// </summary>
        [BuildElement("infiles")]
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

        /// <summary>
        /// XSLT extension objects to be passed to the XSLT transformation.
        /// </summary>
        [BuildElementCollection("extensionobjects", "extensionobject")]
        public XsltExtensionObjectCollection ExtensionObjects {
            get { return _xsltExtensions; }
        }

        /// <summary>
        /// The network proxy to use to access the Internet resource.
        /// </summary>
        [BuildElement("proxy")]
        public Proxy Proxy {
            get { return _proxy; }
            set { _proxy = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">
        /// <list type="bullet">
        /// <item>
        /// <description>If the <see cref="OutputFile"/> attribute is set and the <see cref="InFiles"/> element is used.</description>
        /// </item>
        /// <item>
        /// <description>If no source files are present.</description>
        /// </item>
        /// <item>
        /// <description>If the xslt file does not exist.</description>
        /// </item>
        /// <item>
        /// <description>If the xslt file cannot be retrieved by the specified URI.</description>
        /// </item>
        /// </list>
        /// </exception>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (InFiles.BaseDirectory == null) {
                InFiles.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            StringCollection srcFiles = null;
            if (SrcFile != null) {
                srcFiles = new StringCollection();
                srcFiles.Add(SrcFile.FullName);
            } else if (InFiles.FileNames.Count > 0) {
                if (OutputFile != null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1148")), 
                        Location);
                }
                srcFiles = InFiles.FileNames;
            }

            if (srcFiles == null || srcFiles.Count == 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1147")), 
                    Location);
            }

            if (XsltFile.IsFile) {
                FileInfo fileInfo = new FileInfo(XsltFile.LocalPath);

                if (!fileInfo.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1149"), fileInfo.FullName), 
                        Location);
                }
            } else {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(XsltFile);
                if (Proxy != null) {
                    request.Proxy = Proxy.GetWebProxy();
                }

                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1149"), XsltFile), 
                        Location);
                }
            }

            foreach (string srcFile in srcFiles) {
                string destFile = null;

                if (OutputFile != null) {
                    destFile = OutputFile.FullName;
                }

                if (String.IsNullOrEmpty(destFile)) {
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

                FileInfo srcInfo = new FileInfo(srcFile);
                FileInfo destInfo = new FileInfo(Path.GetFullPath(Path.Combine(
                    DestDir.FullName, destFile)));

                if (!srcInfo.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1150"), srcInfo.FullName), 
                        Location);
                }

                bool destOutdated = !destInfo.Exists
                    || srcInfo.LastWriteTime > destInfo.LastWriteTime;

                if (!destOutdated && XsltFile.IsFile) {
                    FileInfo fileInfo = new FileInfo(XsltFile.LocalPath);
                    destOutdated |= fileInfo.LastWriteTime > destInfo.LastWriteTime;
                }

                if (destOutdated) {
                    XmlReader xmlReader = null;
                    XmlReader xslReader = null;
                    TextWriter writer = null;
    
                    try {
                        // store current directory
                        string originalCurrentDirectory = Directory.GetCurrentDirectory();

                        // initialize XPath document holding input XML
                        XPathDocument xml = null;

                        try {
                            // change current directory to directory containing
                            // XSLT file, to allow includes to be resolved 
                            // correctly
                            Directory.SetCurrentDirectory(srcInfo.DirectoryName);

                            // load the xml that needs to be transformed
                            Log(Level.Verbose, "Loading XML file '{0}'.", 
                                srcInfo.FullName);
                            xmlReader = CreateXmlReader(new Uri(srcInfo.FullName));
                            xml = new XPathDocument(xmlReader);
                        } finally {
                            // restore original current directory
                            Directory.SetCurrentDirectory(originalCurrentDirectory);
                        }

                        // initialize xslt parameters
                        XsltArgumentList xsltArgs = new XsltArgumentList();

                        // set the xslt parameters
                        foreach (XsltParameter parameter in Parameters) {
                            if (IfDefined && !UnlessDefined) {
                                xsltArgs.AddParam(parameter.ParameterName, 
                                    parameter.NamespaceUri, parameter.Value);
                            }
                        }
                         
                        // create extension objects
                        foreach (XsltExtensionObject extensionObject in ExtensionObjects) {
                            if (extensionObject.IfDefined && !extensionObject.UnlessDefined) {
                                object extensionInstance = extensionObject.CreateInstance();
                                xsltArgs.AddExtensionObject(extensionObject.NamespaceUri,
                                    extensionInstance);
                            }
                        }

                        try {
                            if (XsltFile.IsFile) {
                                // change current directory to directory containing
                                // XSLT file, to allow includes to be resolved 
                                // correctly
                                FileInfo fileInfo = new FileInfo(XsltFile.LocalPath);
                                Directory.SetCurrentDirectory(fileInfo.DirectoryName);
                            }

                            // load the stylesheet
                            Log(Level.Verbose, "Loading stylesheet '{0}'.", XsltFile);
                            xslReader = CreateXmlReader(XsltFile);

                            // create writer for the destination xml
                            writer = CreateWriter(destInfo.FullName);

                            // do the actual transformation 
                            Log(Level.Info, "Processing '{0}' to '{1}'.", 
                                srcInfo.FullName, destInfo.FullName);

                            XslCompiledTransform xslt = new XslCompiledTransform();
                            string xslEngineName = xslt.GetType().Name;
                                
                            Log(Level.Verbose, "Using {0} to load '{1}'.",
                                xslEngineName, XsltFile);
                            xslt.Load(xslReader, new XsltSettings(true, true), new XmlUrlResolver() );
                                
                            Log(Level.Verbose, "Using {0} to process '{1}' to '{2}'.",
                                xslEngineName, srcInfo.FullName, destInfo.FullName);
                            xslt.Transform(xml, xsltArgs, writer);
                        } finally {
                            // restore original current directory
                            Directory.SetCurrentDirectory(originalCurrentDirectory);
                        }
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1151"), srcInfo.FullName, XsltFile), 
                            Location, ex);
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

        #region Protected Instance Methods

        protected virtual XmlReader CreateXmlReader(Uri uri) {
            Stream stream = null;
            XmlUrlResolver resolver = null;

            if (uri.IsFile) {
                stream = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read);
            } else {
                resolver = new XmlUrlResolver();
                HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(uri);
                if (Proxy != null) {
                    request.Proxy = Proxy.GetWebProxy();
                    resolver.Credentials = Proxy.Credentials.GetCredential();
                } else {
                    resolver.Credentials = CredentialCache.DefaultCredentials;
                }
                stream = request.GetResponse().GetResponseStream();
            }

            XmlTextReader xmlReader = new XmlTextReader(uri.ToString(), stream);
            xmlReader.XmlResolver = resolver;
            return new XmlValidatingReader(xmlReader);
        }

        protected virtual TextWriter CreateWriter(string filepath) {
            string targetDir = Path.GetDirectoryName(Path.GetFullPath(filepath));
            if (!String.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                Directory.CreateDirectory(targetDir);
            }
            return new StreamWriter(filepath);
        }

        #endregion Protected Instance Methods
    }
}

