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
using NAnt.Core.Util;

namespace NAnt.Core {
    public sealed class Target : Element {
        #region Private Instance Fields

        private string _name = null;
        private string _description = null;
        private string _ifCondition = null;
        private string _unlessCondition = null;
        private StringCollection _dependencies = new StringCollection();

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Target" /> class.
        /// </summary>
        public Target() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the target.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   Hides <see cref="Element.Name"/> to have <see cref="Target" /> 
        ///   return the name of target, not the name of XML element - which 
        ///   would always be <c>target</c>.
        ///   </para>
        ///   <para>
        ///   Note: Properties are not allowed in the name.
        ///   </para>
        /// </remarks>
        [TaskAttribute("name", Required=true, ExpandProperties=false)]
        [StringValidator(AllowEmpty=false)]
        public new string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the target will be executed; 
        /// otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if", ExpandProperties=false)]
        public string IfCondition {
            get { return _ifCondition; }
            set { _ifCondition = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets a value indicating whether the target should be executed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the target should be executed; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public bool IfDefined {
            get {
                // expand properties in condition
                string expandedCondition = Project.Properties.ExpandProperties(IfCondition, Location);

                // if a condition is supplied, it should evaluate to a bool
                if (!StringUtils.IsNullOrEmpty(expandedCondition)) {
                    try {
                        return Convert.ToBoolean(expandedCondition, CultureInfo.InvariantCulture);
                    } catch (FormatException) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot resolve expanded value '{0}' of 'if' attribute" +
                            " to a Boolean value.", expandedCondition), Location);
                    }
                }

                // no condition is supplied
                return true;
            }
        }

        /// <summary>
        /// Opposite of if. If <see langword="false" /> then the target will be 
        /// executed; otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless", ExpandProperties=false)]
        public string UnlessCondition {
            get { return _unlessCondition; }
            set { _unlessCondition = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets a value indicating whether the target should NOT be executed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the target should NOT be executed;
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool UnlessDefined {
            get {
                // expand properties in condition
                string expandedCondition = Project.Properties.ExpandProperties(UnlessCondition, Location);

                // if a condition is supplied, it should evaluate to a bool
                if (!StringUtils.IsNullOrEmpty(expandedCondition)) {
                    try {
                        return Convert.ToBoolean(expandedCondition, CultureInfo.InvariantCulture);
                    } catch (FormatException) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot resolve expanded value '{0}' of 'unless'" +
                            " attribute to a Boolean value.", expandedCondition), 
                            Location);
                    }
                }

                // no condition is supplied
                return false;
            }
        }

        /// <summary>
        /// The description of the target.
        /// </summary>
        [TaskAttribute("description")]
        public string Description {
            set { _description = value;}
            get { return _description; }
        }

        /// <summary>
        /// Space separated list of targets that this target depends on.
        /// </summary>
        [TaskAttribute("depends")]
        public string DependsListString {
            set {
                foreach (string str in value.Split(new char[] {' ', ','})) {
                    string dependency = str.Trim();
                    if (dependency.Length > 0) {
                        Dependencies.Add(dependency);
                    }
                }
            }
        }

        /// <summary>
        /// A collection of target names that must be executed before this 
        /// target.
        /// </summary>
        public StringCollection Dependencies {
            get { return _dependencies; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Executes dependent targets first, then the target.
        /// </summary>
        public void Execute() {
            if (IfDefined && !UnlessDefined) {
                try {
                    Project.OnTargetStarted(this, new BuildEventArgs(this));
                
                    // select all the task nodes and execute them
                    foreach (XmlNode childNode in XmlNode) {
                        if (!(childNode.NodeType == XmlNodeType.Element)|| !childNode.NamespaceURI.Equals(Project.Document.DocumentElement.NamespaceURI)) {
                            continue;
                        }
                        
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

        #endregion Public Instance Methods
    }
}
