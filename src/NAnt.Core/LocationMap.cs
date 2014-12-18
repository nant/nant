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
//
// Ian MacLean (imaclean@gmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Collections;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace NAnt.Core {
    /// <summary>
    /// Maps XML nodes to the text positions from their original source.
    /// </summary>
    [Serializable()]
    internal class LocationMap {
        #region Private Instance Fields

        // The LocationMap uses a hash table to map filenames to resolve specific maps.
        private Hashtable _fileMap = new Hashtable();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationMap" /> class.
        /// </summary>
        public LocationMap() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods
        
        /// <summary>
        /// Determines if a file has been loaded by the current project. 
        /// </summary>
        /// <param name="fileOrUri">The file to check.</param>
        /// <returns>
        /// <see langword="true" /> if the specified file has already been loaded
        /// by the current project; otherwise, <see langword="false" />.
        /// </returns>
        public bool FileIsMapped(string fileOrUri){
            Uri uri = new Uri(fileOrUri);
            return _fileMap.ContainsKey(uri.AbsoluteUri);
        }

        /// <summary>
        /// Adds an <see cref="XmlDocument" /> to the map.
        /// </summary>
        /// <remarks>
        /// An <see cref="XmlDocument" /> can only be added to the map once.
        /// </remarks>
        public void Add(XmlDocument doc) {
            // check for non-backed documents
            if (String.IsNullOrEmpty(doc.BaseURI)) {
                return;
            }

            // convert URI to absolute URI
            Uri uri = new Uri(doc.BaseURI);
            string fileName = uri.AbsoluteUri;

            // prevent duplicate mapping
            if (FileIsMapped(fileName)) {
                // do not re-map the file a 2nd time
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "XML document '{0}' has already been mapped.", fileName));
           }

            Hashtable map = new Hashtable();

            string parentXPath = "/"; // default to root
            string previousXPath = "";
            int previousDepth = 0;

            // Load text reader.
            XmlTextReader reader = new XmlTextReader(fileName);
            try {
                map.Add((object) "/", (object) new TextPosition(1, 1));

                ArrayList indexAtDepth = new ArrayList();

                // loop thru all nodes in the document
                while (reader.Read()) {
                    // Ignore nodes we aren't interested in
                    if ((reader.NodeType != XmlNodeType.Whitespace) &&
                        (reader.NodeType != XmlNodeType.EndElement) &&
                        (reader.NodeType != XmlNodeType.ProcessingInstruction) &&
                        (reader.NodeType != XmlNodeType.XmlDeclaration) &&
                        (reader.NodeType != XmlNodeType.DocumentType)) {

                        int level = reader.Depth;
                        string currentXPath = "";

                        // If we are higher than before
                        if (reader.Depth < previousDepth) {
                            // Clear vars for new depth
                            string[] list = parentXPath.Split('/');
                            string newXPath = ""; // once appended to / will be root node

                            for (int j = 1; j < level+1; j++) {
                                newXPath += "/" + list[j];
                            }

                            // higher than before so trim xpath\
                            parentXPath = newXPath; // one up from before

                            // clear indexes for depth greater than ours
                            indexAtDepth.RemoveRange(level+1, indexAtDepth.Count - (level+1));
                        } else if (reader.Depth > previousDepth) {
                            // we are lower
                            parentXPath = previousXPath;
                        }

                        // End depth setup
                        // Setup up index array
                        // add any needed extra items ( usually only 1 )
                        // would have used array but not sure what maximum depth will be beforehand
                        for (int index = indexAtDepth.Count; index < level+1; index++) {
                            indexAtDepth.Add(0);
                        }

                        // Set child index
                        if ((int) indexAtDepth[level] == 0) {
                            // first time thru
                            indexAtDepth[level] = 1;
                        } else {
                            indexAtDepth[level] = (int) indexAtDepth[level] + 1; // lower so append to xpath
                        }

                        // Do actual XPath generation
                        if (parentXPath.EndsWith("/")) {
                            currentXPath = parentXPath;
                        } else {
                            currentXPath = parentXPath + "/"; // add seperator
                        }

                        // Set the final XPath
                        currentXPath += "child::node()[" + indexAtDepth[level] + "]";

                        // Add to our hash structures
                        map.Add((object) currentXPath, (object) new TextPosition(reader.LineNumber, reader.LinePosition));

                        // setup up loop vars for next iteration
                        previousXPath = currentXPath;
                        previousDepth = reader.Depth;
                    }
                }
            } finally {
                reader.Close();
            }

            // add map at the end to prevent adding maps that had errors
            _fileMap.Add(fileName, map);
        }

        /// <summary>
        /// Returns the <see cref="Location"/> in the XML file for the given node.
        /// </summary>
        /// <remarks>
        /// The <paramref name="node" /> must be from an <see cref="XmlDocument" /> 
        /// that has been added to the map.
        /// </remarks>
        public Location GetLocation(XmlNode node) {
            // check for non-backed documents
            if (String.IsNullOrEmpty(node.BaseURI))
                return Location.UnknownLocation; // return empty location because we have a fileless node.

            // convert URI to absolute URI
            Uri uri = new Uri(node.BaseURI);
            string fileName = uri.AbsoluteUri;

            if (!FileIsMapped(fileName)) {
                throw new ArgumentException("Xml node has not been mapped.");
            }

            // find xpath for node
            Hashtable map = (Hashtable) _fileMap[fileName];
            string xpath = GetXPathFromNode(node);
            if (!map.ContainsKey(xpath)) {
                throw new ArgumentException("Xml node has not been mapped.");
            }

            TextPosition pos = (TextPosition) map[xpath];
            Location location = new Location(fileName, pos.Line, pos.Column);
            return location;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetXPathFromNode(XmlNode node) {
            // IM TODO review this algorithm - tidy up
            XPathNavigator nav = node.CreateNavigator();

            string xpath = "";
            int index = 0;

            while (nav.NodeType.ToString(CultureInfo.InvariantCulture) != "Root") {
                // loop thru children until we find ourselves
                XPathNavigator navParent = nav.Clone();
                navParent.MoveToParent();
                int parentIndex = 0;
                navParent.MoveToFirstChild();
                if (navParent.IsSamePosition(nav)) {
                    index = parentIndex;
                }
                while (navParent.MoveToNext()) {
                    parentIndex++;
                    if (navParent.IsSamePosition(nav)) {
                        index = parentIndex;
                    }
                }

                nav.MoveToParent(); // do loop condition here
                index++; // Convert to 1 based index

                string thisNode = "child::node()[" + index.ToString(CultureInfo.InvariantCulture) + "]";

                if (xpath.Length == 0) {
                    xpath = thisNode;
                } else {
                    // build xpath string
                    xpath = thisNode + "/" + xpath;
                }
            }

            // prepend slash to ...
            xpath = "/" + xpath;

            return xpath;
        }

        #endregion Private Instance Methods

        /// <summary>
        /// Represents a position in the build file.
        /// </summary>
        [Serializable()]
        private struct TextPosition {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TextPosition" />
            /// with the speified line and column.
            /// </summary>
            /// <param name="line">The line coordinate of the position.</param>
            /// <param name="column">The column coordinate of the position.</param>
            public TextPosition(int line, int column) {
                Line = line;
                Column = column;
            }

            #endregion Public Instance Constructors

            #region Public Instance Fields

            /// <summary>
            /// The line coordinate of the position.
            /// </summary>
            public int Line;

            /// <summary>
            /// The column coordinate of the position.
            /// </summary>
            public int Column;

            #endregion Public Instance Fields
        }
    }
}
