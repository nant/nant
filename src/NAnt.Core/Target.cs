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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian_maclean@another.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Xml;

using NAnt.Core.Attributes;

namespace NAnt.Core {
    public sealed class Target : Element, ICloneable {
        #region Private Instance Fields

        private string _name = null;
        private string _desc = null;
        private bool _hasExecuted = false;
        private bool _ifDefined = true;
        private bool _unlessDefined = false;
        private StringCollection _dependencies = new StringCollection();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary> Public Constructor </summary>
        public Target() {
        }

        #endregion Public Instance Constructors

        #region Private Instance Constructors

        private Target(Target t) : base((Element)t) {
            this._name = t._name;
            this._desc = t._desc;
            this._dependencies = t._dependencies;
            this._ifDefined = t._ifDefined;
            this._unlessDefined = t._unlessDefined;
            this._hasExecuted = false;
        }

        #endregion Private Instance Constructors

        /// <summary>
        /// The name of the target.
        /// </summary>
        /// <remarks>
        ///   <para>Hides <see cref="Element.Name"/> to have <see cref="Target" /> return the name of target, not the name of Xml element - which would always be <c>target</c>.</para>
        ///   <para>Note: Properties are not allowed in the name.</para>
        /// </remarks>
        ///
        [TaskAttribute("name", Required=true, ExpandProperties=false)]
        public new string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// If <c>true</c> then the target will be executed; otherwise skipped. 
        /// Default is <c>true</c>.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of if. If <c>false</c> then the target will be executed; 
        /// otherwise skipped. Default is <c>false</c>.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        /// <summary>
        /// The description of the target.
        /// </summary>
        [TaskAttribute("description")]
        public string Desc {
            set { _desc = value;}
            get { return _desc; }
        }

        /// <summary>
        /// Space separated list of targets that this target depends on.
        /// </summary>
        [TaskAttribute("depends")]
        public string DependsListString {
            set {
                foreach (string str in value.Split(new char[] {' ', ','})) { // TODO: change this to just ' '
                    string dependency = str.Trim();
                    if (dependency.Length > 0) {
                        Dependencies.Add(dependency);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if the target has been executed.
        /// </summary>
        /// <remarks>
        /// Targets that have been executed will not execute a second time.
        /// </remarks>
        public bool HasExecuted {
            get { return _hasExecuted; }
        }

        /// <summary>
        /// A collection of target names that must be executed before this 
        /// target.
        /// </summary>
        public StringCollection Dependencies {
            get { return _dependencies; }
        }

        /// <summary>
        /// Executes dependent targets first, then the target.
        /// </summary>
        public void Execute() {
            if (!HasExecuted && IfDefined && !UnlessDefined) {
                // set right at the start or a <call> task could start an infinite loop
                _hasExecuted = true;
                foreach (string targetName in Dependencies) {
                    Target target = Project.Targets.Find(targetName);
                    if (target == null) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Unknown dependent target '{0}' of target '{1}'", targetName, Name), Location);
                    }
                    target.Execute();
                }

                try {
                    Project.OnTargetStarted(this, new BuildEventArgs(this));
                
                    // select all the task nodes and execute them
                    foreach (XmlNode childNode in XmlNode) {
                        if(childNode.Name.StartsWith("#")) continue;
                        
                           if (TypeFactory.TaskBuilders.Contains(childNode.Name)) {
                                Task task = Project.CreateTask(childNode, this);
                                if (task != null) {
                                    task.Execute();
                                }
                           } else if (TypeFactory.DataTypeBuilders.Contains(childNode.Name)) {
                                DataTypeBase dataType = Project.CreateDataTypeBase(childNode);
                                Project.Log(Level.Verbose, "Adding a {0} reference with id '{1}'.", childNode.Name, dataType.ID);
                                Project.DataTypeReferences.Add(dataType.ID, dataType);                     
                           } else {
                                string message = string.Format(CultureInfo.InvariantCulture,"invalid element <{0}>. Unknown task or datatype.", childNode.Name ); 
                                throw new BuildException(message, Project.LocationMap.GetLocation(childNode) );
                           }

                    }
                } finally {
                    Project.OnTargetFinished(this, new BuildEventArgs(this));
                }
            }
        }

        #region Implementation of ICloneable

        /// <summary>
        /// Creates a deep copy of the <see cref="Target" />.
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone() {
            return Clone();
        }

        /// <summary>
        /// Creates a deep copy of the <see cref="Target" />.
        /// </summary>
        /// <returns>
        /// A copy of the <see cref="Target" /> with <see cref="HasExecuted" /> 
        /// set to <c>false</c>. This allows the new <see cref="Target" /> to be 
        /// executed.
        /// </returns>
        public Target Clone() {
            return new Target(this);
        }

        #endregion Implementation of ICloneable
    }
}
