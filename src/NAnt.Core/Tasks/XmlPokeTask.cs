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
// Ian McLean (ianm@activestate.com)
// Mitch Denny (mitch.denny@monash.net)

using System;
using System.Globalization;
using System.IO;
using System.Xml;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Replaces text in an XML file at the location specified by an XPath 
    /// expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The location specified by the XPath expression must exist, it will
    /// not create the parent elements for you. However, provided you have
    /// a root element you could use a series of the tasks to build the
    /// XML file up if necessary.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Change the <c>server</c> setting in the configuration from <c>testhost.somecompany.com</c> 
    ///   to <c>productionhost.somecompany.com</c>.
    ///   </para>
    ///   <para>XML file:</para>
    ///   <code>
    ///     <![CDATA[
    /// <?xml version="1.0" encoding="utf-8" ?>
    /// <configuration>
    ///     <appSettings>
    ///         <add key="server" value="testhost.somecompany.com" />
    ///     </appSettings>
    /// </configuration>
    ///     ]]>
    ///   </code>
    ///   <para>Build fragment:</para>
    ///   <code>
    ///     <![CDATA[
    /// <xmlpoke
    ///     file="App.config"
    ///     xpath="/configuration/appSettings/add[@key = 'server']/@value"
    ///     value="productionhost.somecompany.com" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Modify the <c>noNamespaceSchemaLocation</c> in an XML file.
    ///   </para>
    ///   <para>XML file:</para>
    ///   <code>
    ///     <![CDATA[
    /// <?xml version="1.0" encoding="utf-8" ?>
    /// <Commands xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="Path Value">
    /// </Commands>
    ///     ]]>
    ///   </code>
    ///   <para>Build fragment:</para>
    ///   <code>
    ///     <![CDATA[
    /// <xmlpoke file="test.xml" xpath="/Commands/@xsi:noNamespaceSchemaLocation" value="d:\Commands.xsd">
    ///     <namespaces>
    ///         <namespace prefix="xsi" uri="http://www.w3.org/2001/XMLSchema-instance" />
    ///     </namespaces>
    /// </xmlpoke>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("xmlpoke")]
    public class XmlPokeTask : Task {
        #region Private Instance Fields
        
        private FileInfo _xmlFile;
        private string _value;
        private string _xPathExpression;
        private bool _preserveWhitespace;
        private XmlNamespaceCollection _namespaces = new XmlNamespaceCollection();
        
        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the file that contains the XML document that is going 
        /// to be poked.
        /// </summary>
        [TaskAttribute("file", Required=true)]
        public FileInfo XmlFile {
            get { return _xmlFile; }
            set { _xmlFile = value; }
        }

        /// <summary>
        /// The XPath expression used to select which nodes are to be modified.
        /// </summary>
        [TaskAttribute("xpath", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string XPath {
            get { return _xPathExpression; }
            set { _xPathExpression = value; }
        }

        /// <summary>
        /// The value that replaces the contents of the selected nodes.
        /// </summary>
        [TaskAttribute("value", Required=true)]
        [StringValidator(AllowEmpty=true)]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Namespace definitions to resolve prefixes in the XPath expression.
        /// </summary>
        [BuildElementCollection("namespaces", "namespace")]
        public XmlNamespaceCollection Namespaces {
            get { return _namespaces; }
            set { _namespaces = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the whitespace in the resulting
        /// document will be preserved; otherwise the whitespace will be removed.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("preserveWhitespace", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public bool PreserveWhitespace
        {
            get { return _preserveWhitespace; }
            set { _preserveWhitespace = value; }
        }



        #endregion Public Instance Properties
        
        #region Override implementation of Task
         
        /// <summary>
        /// Executes the XML poke task.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure the specified xml file exists
            if (!XmlFile.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1154"), XmlFile.FullName), Location);
            }

            try {
                XmlDocument document = LoadDocument(XmlFile.FullName, PreserveWhitespace);

                XmlNamespaceManager nsMgr = new XmlNamespaceManager(document.NameTable);
                foreach (XmlNamespace xmlNamespace in Namespaces) {
                    if (xmlNamespace.IfDefined && !xmlNamespace.UnlessDefined) {
                        nsMgr.AddNamespace(xmlNamespace.Prefix, xmlNamespace.Uri);
                    }
                }

                XmlNodeList nodes = SelectNodes(XPath, document, nsMgr);

                // don't bother trying to update any nodes or save the
                // file if no nodes were found in the first place.
                if (nodes.Count > 0) {
                    UpdateNodes(nodes, Value);
                    SaveDocument(document, XmlFile.FullName);
                } 
            } catch (BuildException ex) {
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                                       ResourceUtils.GetString("NA1159"), XmlFile.FullName), 
                    Location, ex);
            }
        }
        #endregion Override implementation of Task
        
        #region Private Instance Methods

        /// <summary>
        /// Loads an XML document from a file on disk.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the file to load the XML document from.
        /// </param>
        /// <param name="preserveWhitespace">
        /// Value for XmlDocument.PreserveWhitespace that is set before the xml is loaded.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Xml.XmlDocument" /> containing
        /// the document object model representing the file.
        /// </returns>
        private XmlDocument LoadDocument(string fileName, bool preserveWhitespace) {
            XmlDocument document = null;

            try {
                Log(Level.Verbose, "Attempting to load XML document" 
                    + " in file '{0}'.", fileName);

                document = new XmlDocument();
                document.PreserveWhitespace = preserveWhitespace;
                document.Load(fileName);

                Log(Level.Verbose, "XML document in file '{0}' loaded" 
                    + " successfully.", fileName);
                return document;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1158"), fileName), Location,
                    ex);
            }
        }

        /// <summary>
        /// Given an XML document and an expression, returns a list of nodes
        /// which match the expression criteria.
        /// </summary>
        /// <param name="xpath">
        /// The XPath expression used to select the nodes.
        /// </param>
        /// <param name="document">
        /// The XML document that is searched.
        /// </param>
        /// <param name="nsMgr">
        /// An <see cref="XmlNamespaceManager" /> to use for resolving namespaces 
        /// for prefixes in the XPath expression.
        /// </param>
        /// <returns>
        /// An <see cref="XmlNodeList" /> containing references to the nodes 
        /// that matched the XPath expression.
        /// </returns>
        private XmlNodeList SelectNodes(string xpath, XmlDocument document, XmlNamespaceManager nsMgr) {
            XmlNodeList nodes = null;

            try {
                Log(Level.Verbose, "Selecting nodes with XPath" 
                    + " expression '{0}'.", xpath);

                nodes = document.SelectNodes(xpath, nsMgr);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1161"),
                    xpath), Location, ex);
            }
            // throw exception if no nodes found for XPath (handled gracefully if failonerror=false)
            if (nodes == null || nodes.Count == 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                                       ResourceUtils.GetString("NA1156"),
                                                       xpath), Location);
            }
            // report back how many we found.
            Log(Level.Info, "Found '{0}' nodes matching"
                    + " XPath expression '{1}'.", nodes.Count, xpath);
            return nodes;
        }

        /// <summary>
        /// Given a node list, replaces the XML within those nodes.
        /// </summary>
        /// <param name="nodes">
        /// The list of nodes to replace the contents of.
        /// </param>
        /// <param name="value">
        /// The text to replace the contents with.
        /// </param>
        private void UpdateNodes(XmlNodeList nodes, string value) {
            Log(Level.Verbose, "Updating nodes with value '{0}'.",
                value);
                
            int index = 0;
            foreach (XmlNode node in nodes) {
                Log(Level.Verbose, "Updating node '{0}'.", index);
                node.InnerXml = value;
                index ++;
            }

            Log( Level.Verbose, "Updated all nodes successfully.",
                value);
        }
        
        /// <summary>
        /// Saves the XML document to a file.
        /// </summary>
        /// <param name="document">The XML document to be saved.</param>
        /// <param name="fileName">The file name to save the XML document under.</param>
        private void SaveDocument(XmlDocument document, string fileName) {
            try {
                Log(Level.Verbose, "Attempting to save XML document" 
                    + " to '{0}'.", fileName);

                document.Save(fileName);
                
                Log(Level.Verbose, "XML document successfully saved" 
                    + " to '{0}'.", fileName);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1162"), fileName), 
                    Location, ex);
            }
        }

        #endregion Private Instance Methods
    }
}