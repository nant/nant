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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.Core {
    /// <summary>
    /// Executes embedded tasks. First inherit from TaskContainer, then call ExecuteChildTasks during Exec.
    /// </summary>
    /// <remarks>
    ///    <para>
    ///    All build elements (like a <see cref="FileSet" />) are automatically 
    ///    excluded from things that get executed. They are evaluated normally 
    ///    during XML task initialization.
    ///    </para>
    ///    <para>
    ///    For an example, see <see cref="IfTask" /> or <see cref="LoopTask" />.
    ///    </para>
    /// </remarks>
    public class TaskContainer : Task {
        #region Private Instance Fields

        private StringCollection _subXMLElements = null;

        #endregion Private Instance Fields

        #region Override implementation of Task

        protected override void InitializeTask(XmlNode taskNode) {
            base.InitializeTask(taskNode);

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

                Task task = CreateChildTask(childNode);
                // for now, we should assume null tasks are because of incomplete metadata about the XML.
                if(task != null) {
                    task.Parent = this;
                    task.Execute();
                }
            }
        }

        protected virtual Task CreateChildTask(XmlNode node) {
            return Project.CreateTask(node);
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
