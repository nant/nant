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
using System.Xml;
using System.Globalization;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {

    public class Target : Element, ICloneable {

        string _name = null;
        string _desc = null;
        bool _hasExecuted = false;
        bool _ifDefined = true;
        bool _unlessDefined = false;
        StringCollection _dependencies = new StringCollection();

        /// <summary> Public Constructor </summary>
        public Target() {
             }

        //Copy ctor
        protected Target(Target t) : base((Element)t) {
            this._name = t._name;
            this._desc = t._desc;
            this._dependencies = t._dependencies;
            this._ifDefined = t._ifDefined;
            this._unlessDefined = t._unlessDefined;
            this._hasExecuted = false;
        }

        /// <summary>The name of the target.</summary>
        /// <remarks>
        ///   <para>Hides <see cref="Element.Name"/> to have <c>Target</c> return the name of target, not the name of Xml element - which would always be <c>target</c>.</para>
        ///   <para>Note: Properties are not allowed in the name.</para>
        /// </remarks>
        ///
        [TaskAttribute("name", Required=true, ExpandProperties=false)]
        public new string Name {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>If true then the target will be executed; otherwise skipped. Default is "true".</summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>Opposite of if.  If false then the target will be executed; otherwise skipped. Default is "false".</summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        /// <summary> The Target Description </summary>
        [TaskAttribute("description")]
        public string Desc {
            set { _desc = value;}
            get { return _desc; }
        }

        /// <summary> The space sep list of targets that this target depends on.</summary>
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


        /// <summary>Indicates if the target has been executed.</summary>
        /// <remarks>
        ///   <para>Targets that have been executed will not execute a second time.</para>
        /// </remarks>
        public bool HasExecuted {
            get { return _hasExecuted; }
        }

        /// <summary>A collection of target names that must be executed before this target.</summary>
        public StringCollection Dependencies {
            get { return _dependencies; }
        }

        /// <summary>
        /// The xml used to initialize this Target.
        /// </summary>
        protected XmlNode TargetNode { get {return XmlNode;} }

        /// <summary>Executes dependent targets first, then the target.</summary>
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
                    Project.OnTargetStarted(this, new BuildEventArgs(_name));

                    //these two lines should be removed and replaced with implementing
                    //OnTargetStarted in the ConsoleLogger
                    Log.WriteLine();
                    Log.WriteLine("{0}:", Name);

                    // select all the task nodes and execute them
                    foreach (XmlNode taskNode in XmlNode) {
                        if(taskNode.Name.StartsWith("#")) continue;

                        Task task = Project.CreateTask(taskNode, this);
                        if (task != null) {
                            task.Execute();
                        }
                    }
                } finally {
                    Project.OnTargetFinished(this, new BuildEventArgs(_name));
                }
            }
        }

        /// <summary>
        /// Creates a deep copy by calling Copy().
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone() {
            return (object) Copy();
        }

        /// <summary>
        /// Creates a new (deep) copy.
        /// </summary>
        /// <returns>A copy with the _hasExecuted set to false. This allows the new Target to be Executed.</returns>
        public Target Copy() {
            return new Target(this);
        }
    }
}
