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
// Charles Chan (cchan_qa@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Extracts text from an XML file at the location specified by an XPath 
    /// expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the XPath expression specifies multiple nodes the node index is used 
    /// to determine which of the nodes' text is returned.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   The example provided assumes that the following XML file (App.config)
    ///   exists in the current build directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <?xml version="1.0" encoding="utf-8" ?>
    /// <configuration xmlns="http://www.gordic.cz/shared/project-config/v_1.0.0.0">
    ///     <appSettings>
    ///         <add key="server" value="testhost.somecompany.com" />
    ///     </appSettings>
    /// </configuration>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   The example will read the server value from the above
    ///   configuration file.
    ///   </para>
    ///   <para>
    ///   NOTE: The example below shows that the default namespace needs to also be declared. Simply set any prefix and use that prefix in the xpath.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <xmlpeek
    ///     file="App.config"
    ///     xpath="/x:configuration/x:appSettings/x:add[@key = 'server']/@value"
    ///     property="configuration.server">
    ///     <namespaces>
    ///         <namespace prefix="x" uri="http://www.gordic.cz/shared/project-config/v_1.0.0.0" />
    ///     </namespaces>
    /// </xmlpeek>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("xmlpeek")]
    public class XmlPeekTask : Task {
        #region Private Instance Fields

        private FileInfo _xmlFile;
        private int _nodeIndex = 0;
        private string _property;
        private string _xPath;
        private XmlNamespaceCollection _namespaces = new XmlNamespaceCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties
        
        /// <summary>
        /// The name of the file that contains the XML document
        /// that is going to be peeked at.
        /// </summary>
        [TaskAttribute("file", Required=true)]
        public FileInfo XmlFile {
            get { return _xmlFile; }
            set { _xmlFile = value; }
        }

        /// <summary>
        /// The index of the node that gets its text returned when the query 
        /// returns multiple nodes.
        /// </summary>
        [TaskAttribute("nodeindex", Required=false)]
        [Int32Validator(0, Int32.MaxValue)]
        public int NodeIndex {
            get { return _nodeIndex; }
            set { _nodeIndex = value; }
        }

        /// <summary>
        /// The property that receives the text representation of the XML inside 
        /// the node returned from the XPath expression.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Property {
            get { return _property; }
            set { _property = value; }
        }

        /// <summary>
        /// The XPath expression used to select which node to read.
        /// </summary>
        [TaskAttribute("xpath", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string XPath {
            get { return _xPath; }
            set { _xPath = value; }
        }

        /// <summary>
        /// Namespace definitions to resolve prefixes in the XPath expression.
        /// </summary>
        [BuildElementCollection("namespaces", "namespace")]
        public XmlNamespaceCollection Namespaces {
            get { return _namespaces; }
            set { _namespaces = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the XML peek task.
        /// </summary>
        protected override void ExecuteTask() {
            Log(Level.Verbose, "Peeking at '{0}' with XPath expression '{1}'.", 
                XmlFile.FullName,  XPath);

            // ensure the specified xml file exists
            if (!XmlFile.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                                       ResourceUtils.GetString("NA1154"), XmlFile.FullName), Location);
            }

            try {
                XmlDocument document = LoadDocument(XmlFile.FullName);
                Properties[Property] = GetNodeContents(XPath, document, NodeIndex);
            } catch (BuildException ex) {
                throw ex; // Just re-throw the build exceptions.
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1153"), XmlFile.FullName), 
                    Location, ex);
            }
        }
        
        #endregion Override implementation of Task
        
        #region private Instance Methods

        /// <summary>
        /// Loads an XML document from a file on disk.
        /// </summary>
        /// <param name="fileName">The file name of the file to load the XML document from.</param>
        /// <returns>
        /// A <see cref="XmlDocument">document</see> containing
        /// the document object representing the file.
        /// </returns>
        private XmlDocument LoadDocument(string fileName) {
            XmlDocument document = null;

            try {
                document = new XmlDocument();
                document.Load(fileName);
                return document;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1158"), fileName), Location,
                    ex);
            }
        }

        /// <summary>
        /// Gets the contents of the node specified by the XPath expression.
        /// </summary>
        /// <param name="xpath">The XPath expression used to determine which nodes to choose from.</param>
        /// <param name="document">The XML document to select the nodes from.</param>
        /// <param name="nodeIndex">The node index in the case where multiple nodes satisfy the expression.</param>
        /// <returns>
        /// The contents of the node specified by the XPath expression.
        /// </returns>
        private string GetNodeContents(string xpath, XmlDocument document, int nodeIndex ) {
            string contents = null;
            Object result = null;
            int numNodes = 0;

            try {
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(document.NameTable);
                foreach (XmlNamespace xmlNamespace in Namespaces) {
                    if (xmlNamespace.IfDefined && !xmlNamespace.UnlessDefined) {
                        nsMgr.AddNamespace(xmlNamespace.Prefix, xmlNamespace.Uri);
                    }
                }
                XPathNavigator nav = document.CreateNavigator();
                XPathExpression expr = nav.Compile(xpath);
                expr.SetContext(nsMgr);
                result = nav.Evaluate(expr);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1155"), xpath), 
                    Location, ex);
            }

            if (result == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1156"), xpath),
                    Location);
            }

            // When using XPathNavigator.Evaluate(),
            // the result of the expression can be one of Boolean, number, 
            // string, or node set). This maps to Boolean, Double, String, 
            // or XPathNodeIterator objects respectively.
            // So therefore if the result is not null, then there is at least
            // 1 node that matches.
            numNodes = 1;

            // If the result is a node set, then there could be multiple nodes.
            XPathNodeIterator xpathNodesIterator = result as XPathNodeIterator;
            if (xpathNodesIterator != null) {
                numNodes = xpathNodesIterator.Count;
            }

            Log(Level.Verbose, "Found '{0}' node{1} with the XPath expression '{2}'.",
                numNodes, numNodes > 1 ? "s" : "", xpath);
          
            if (xpathNodesIterator != null) {
                if (nodeIndex >= numNodes){
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1157"), nodeIndex), Location);
                }

                while (xpathNodesIterator.MoveNext()) {
                    // CurrentPosition is 1-based.
                    if (xpathNodesIterator.CurrentPosition == (nodeIndex + 1)) {
                        // Get the entire node of the current xpathNodesIterator and return innerxml.
                        XmlNode currentNode = ((IHasXmlNode)xpathNodesIterator.Current).GetNode();
                        contents = currentNode.InnerXml;
                    }
                }
            } else {
                if (result is IFormattable) {
                    IFormattable formattable = (IFormattable) result;
                    contents = formattable.ToString(null, CultureInfo.InvariantCulture);
                } else {
                    contents = result.ToString();
                }
            }

            return contents;
        }

        #endregion private Instance Methods
    }
}
