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

namespace NAnt.Core {
    /// <summary>
    /// Executes embedded tasks. First inherit from TaskContainer, then call ExecuteChildTasks during Exec.
    /// <note>
    ///     <para>
    ///         All BuildElements (like a FileSet or OptionSet) are automatically excluded from things that get executed. 
    ///         They are evaluted normally during xml task initialization.
    ///     </para>
    ///     <para>
    ///         For an example, see <if/> or <foreach/>
    ///     </para>
    /// </note>
    /// </summary>
    public class TaskContainer : Task {
        #region Private Instance Fields

        private StringCollection _subXMLElements = null;

        #endregion Private Instance Fields

        #region Override implementation of Task

        protected override void InitializeTask(XmlNode taskNode) {
            base.InitializeTask(taskNode);

            //Exclude any BuildElements (like FileSets, etc.) from our execution elements.
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
        /// Creates and executes the embedded (child xml nodes) elements.
        /// </summary>
        /// <remarks>
        /// Skips any element defined by the host <see cref="Task" /> that has 
        /// a <see cref="BuildElementAttribute" /> defined.
        /// </remarks>
        protected virtual void ExecuteChildTasks() {
            foreach (XmlNode childNode in XmlNode) {
                if(childNode.Name.StartsWith("#") && 
                    childNode.NamespaceURI.Equals(Project.Document.DocumentElement.NamespaceURI)) {
                    continue;
                }

                if (IsPrivateXmlElement(childNode)) {
                    continue;
                }

                Task task = CreateChildTask(childNode);
                // for now, we should assume null tasks are because of incomplete metadata about the xml.
                if(task != null) {
                    task.Parent = this;
                    task.Execute();
                }
            }
        }

        protected virtual Task CreateChildTask(XmlNode node) {
            try {
                return Project.CreateTask(node);
            } catch (BuildException ex) {
                Log(Level.Error, "{0} Failed to created task for '{1}' xml element for reason: \n {2}", LogPrefix, node.Name , ex.ToString());
            }
            return null;
        }
        
        protected virtual bool IsPrivateXmlElement(XmlNode node) {
            return (_subXMLElements != null && _subXMLElements.Contains(node.Name));
        }

        protected virtual void AddPrivateXmlElementName(string name) {
            if(_subXMLElements == null) {
                _subXMLElements = new StringCollection();
            }

            if(!_subXMLElements.Contains(name)) {
                _subXMLElements.Add(name);
            }
        }

        #endregion Protected Instance Methods
    }
}
