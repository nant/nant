// NAnt - A .NET build tool
// Copyright (C) 2001-2012 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Collections.Specialized;
using System.Globalization;
using System.Xml;
using NAnt.Core.Util;

namespace NAnt.Core
{
    /// <summary>
    /// Executes embedded tasks/elements in the order in which they are defined.
    /// </summary>
    public class ElementContainer : Element
    {
        #region Private Instance Fields

        private StringCollection _subXMLElements;

        #endregion Private Instance Fields

        #region Override implementation of Element

        /// <summary>
        /// Gets a value indicating whether the element is performing additional
        /// processing using the <see cref="XmlNode" /> that was use to 
        /// initialize the element.
        /// </summary>
        /// <value>
        /// <see langword="true" />, as a <see cref="ElementContainer" /> is
        /// responsable for creating tasks from the nested build elements.
        /// </value>
        protected override bool CustomXmlProcessing {
            get { return true;}
        }

        #endregion Override implementation of Element

        #region Public Instance Methods

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public virtual void Execute() {
            ExecuteChildTasks();
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Creates and executes the embedded (child XML nodes) elements.
        /// </summary>
        protected virtual void ExecuteChildTasks() {
            foreach (XmlNode childNode in XmlNode) {
                //we only care about xmlnodes (elements) that are of the right namespace.
                if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                    continue;
                }
                
                // ignore any private xml elements (by def. this includes any property with a BuildElementAttribute (name).
                if (IsPrivateXmlElement(childNode)) {
                    continue;
                }

                if (TypeFactory.TaskBuilders.Contains(childNode.Name)) {
                    // create task instance
                    Task task = CreateChildTask(childNode);
                    // for now, we should assume null tasks are because of 
                    // incomplete metadata about the XML
                    if (task != null) {
                        task.Parent = this;
                        // execute task
                        task.Execute();
                    }
                } else if (TypeFactory.DataTypeBuilders.Contains(childNode.Name)) {
                    // we are an datatype declaration
                    DataTypeBase dataType = CreateChildDataTypeBase(childNode);

                    Log(Level.Debug, "Adding a {0} reference with id '{1}'.", childNode.Name, dataType.ID);
                    if (!Project.DataTypeReferences.Contains(dataType.ID)) {
                        Project.DataTypeReferences.Add(dataType.ID, dataType);
                    } else {
                        Project.DataTypeReferences[dataType.ID] = dataType; // overwrite with the new reference.
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1071"), childNode.Name), 
                        Project.GetLocation(childNode));
                }
            }
        }

        /// <summary>
        /// Creates the child task specified by the passed XmlNode.
        /// </summary>
        /// <param name="node">The node specifiing the task.</param>
        /// <returns>The created task instance.</returns>
        protected virtual Task CreateChildTask(XmlNode node) {
            return Project.CreateTask(node);
        }

        protected virtual DataTypeBase CreateChildDataTypeBase(XmlNode node) {
            return Project.CreateDataTypeBase(node);
        }
        
        protected virtual bool IsPrivateXmlElement(XmlNode node) {
            return (_subXMLElements != null && _subXMLElements.Contains(node.Name));
        }

        protected virtual void AddPrivateXmlElementName(string name) {
            if (_subXMLElements == null)
                _subXMLElements = new StringCollection();

            if (!_subXMLElements.Contains(name))
                _subXMLElements.Add(name);
        }

        #endregion Protected Instance Methods
    }
}

