// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Executes embedded tasks in the order in which they are defined.
    /// </summary>
    public class TaskContainer : Task {
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
        /// <see langword="true" />, as a <see cref="TaskContainer" /> is
        /// responsable for creating tasks from the nested build elements.
        /// </value>
        protected override bool CustomXmlProcessing {
            get { return true;}
        }

        #endregion Override implementation of Element

        #region Override implementation of Task

        /// <summary>
        /// Automatically exclude build elements that are defined on the task 
        /// from things that get executed, as they are evaluated normally during
        /// XML task initialization.
        /// </summary>
        protected override void Initialize() {
            base.Initialize();

            // Exclude any BuildElements (like FileSets, etc.) from our execution elements.
            // These build elements will be handled during the xml init of the task container (Element xmlinit code)
            _subXMLElements = new StringCollection();
            foreach (MemberInfo memInfo in this.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                if(memInfo.DeclaringType.Equals(typeof(object))) {
                    continue;
                }

                BuildElementAttribute buildElemAttr = (BuildElementAttribute) Attribute.GetCustomAttribute(memInfo, typeof(BuildElementAttribute), true);
                if (buildElemAttr != null) {
                    _subXMLElements.Add(buildElemAttr.Name);
                }
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            ExecuteChildTasks();
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Creates and executes the embedded (child XML nodes) elements.
        /// </summary>
        /// <remarks>
        /// Skips any element defined by the host <see cref="Task" /> that has 
        /// a <see cref="BuildElementAttribute" /> defined.
        /// </remarks>
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
            if (_subXMLElements == null) {
                _subXMLElements = new StringCollection();
            }

            if (!_subXMLElements.Contains(name)) {
                _subXMLElements.Add(name);
            }
        }

        #endregion Protected Instance Methods
    }
}
