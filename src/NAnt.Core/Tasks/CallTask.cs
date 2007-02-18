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
// Gerry Shaw (gerry_shaw@yahoo.com)

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Calls a NAnt target in the current project.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When the <see cref="CallTask" /> is used to execute a target, both that 
    ///   target and all its dependent targets will be re-executed.
    ///   </para>
    ///   <para>
    ///   To avoid dependent targets from being executed more than once, two 
    ///   options are available:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///         <description>
    ///         Add an &quot;unless&quot; attribute with value &quot;${<see href="../functions/target.has-executed.html">target::has-executed</see>('<c>&lt;target name&gt;</c>')}&quot;
    ///         to the dependent targets.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Set the <see cref="CascadeDependencies" /> attribute on the 
    ///         <see cref="CallTask" /> to <see langword="false " /> (<c>recommended</c>).
    ///         </description>
    ///     </item>
    ///   </list>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Call the target &quot;build&quot;.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <call target="build" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   This shows how a project could 'compile' a debug and release build 
    ///   using a common compile target.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <project default="build">
    ///     <property name="debug" value="false" />
    ///     <target name="init">
    ///         <echo message="initializing" />
    ///     </target>
    ///     <target name="compile" depends="init">
    ///         <echo message="compiling with debug = ${debug}" />
    ///     </target>
    ///     <target name="build">
    ///         <property name="debug" value="false" />
    ///         <call target="compile" />
    ///         <property name="debug" value="true" />
    ///         <call target="compile" />
    ///     </target>
    /// </project>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   The <see cref="CascadeDependencies" /> parameter of the 
    ///   <see cref="CallTask" /> defaults to <see langword="true" />, 
    ///   causing the &quot;init&quot; target to be executed for both
    ///   the &quot;debug&quot; and &quot;release&quot; build.
    ///   </para>
    ///   <para>
    ///   This results in the following build log:
    ///   </para>
    ///   <code>
    /// build:
    ///   
    /// init:
    ///
    ///     [echo] initializing
    ///     
    /// compile:
    /// 
    ///     [echo] compiling with debug = false
    ///     
    /// init:
    /// 
    ///     [echo] initializing
    ///     
    /// compile:
    /// 
    ///     [echo] compiling with debug = true
    ///     
    /// BUILD SUCCEEDED
    ///   </code>
    ///   <para>
    ///   If the &quot;init&quot; should only be executed once, set the
    ///   <see cref="CascadeDependencies" /> attribute of the <see cref="CallTask" />
    ///   to <see langword="false" />.
    ///   </para>
    ///   <para>
    ///   The build log would then look like this:
    ///   </para>
    ///   <code>
    /// build:
    ///   
    /// init:
    ///
    ///     [echo] initializing
    ///     
    /// compile:
    /// 
    ///     [echo] compiling with debug = false
    ///     
    /// compile:
    /// 
    ///     [echo] compiling with debug = true
    ///     
    /// BUILD SUCCEEDED
    ///   </code>
    /// </example>
    [TaskName("call")]
    public class CallTask : Task {
        #region Private Instance Fields

        private string _target;
        private bool _force;
        private bool _cascade = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// NAnt target to call.
        /// </summary>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string TargetName {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        // TO-DO : remove this property after NAnt 0.8.6 or so.

        /// <summary>
        /// Force an execute even if the target has already been executed. The 
        /// default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("force")]
        [System.Obsolete("Use the \"cascase\" attribute to control whether dependencies should be re-executed.", false)]
        public bool ForceExecute {
            get { return _force; }
            set { _force = value; }
        }
        
        /// <summary>
        /// Execute the specified targets dependencies -- even if they have been 
        /// previously executed. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("cascade")] 
        public bool CascadeDependencies {
            get { return _cascade; }
            set { _cascade = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the specified target.
        /// </summary>
        protected override void ExecuteTask() {
            Target owningTarget = Parent as Target;

            if (owningTarget != null) {
                // topologically sorted list of targets that will be executed
                TargetCollection targets = Project.TopologicalTargetSort(TargetName, Project.Targets);

                // check if owning target is part of list of targets that will
                // be executed again
                if (targets.Find(owningTarget.Name) != null) {
                    // check if owning target is actually a dependency of the 
                    // target that should be executed
                    if (targets.IndexOf(targets.Find(owningTarget.Name)) < targets.IndexOf(targets.Find(TargetName))) {
                        throw new BuildException("Circular dependency: " + targets.ToString(" <- ") + " <- " + owningTarget.Name);
                    }
                }
            }

            Project.Execute(TargetName, CascadeDependencies);
        }

        /// <summary>
        /// Makes sure the <see cref="CallTask" /> is not calling its own 
        /// parent.
        /// </summary>
        protected override void Initialize() {
            Target target = Project.Targets.Find(TargetName);
            if (target != null) {
                Target owningTarget = Parent as Target;

                if (target == owningTarget) {
                    throw new BuildException("Call task cannot call its own parent.", 
                        Location);
                }
            }
        }

        #endregion Override implementation of Task
    }
}
