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

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>
    /// Calls a NAnt target in the current project.
    /// </summary>
    /// <example>
    ///   <para>Call the target &quot;build&quot;.</para>
    ///   <code><![CDATA[<call target="build"/>]]></code>
    ///   <para>This shows how a project could 'compile' a debug and release build using a common compile target.</para>
    ///   <code>
    ///     <![CDATA[
    /// <project default="build">
    ///     <target name="compile">
    ///         <echo message="compiling with debug = ${debug}"/>
    ///     </target>
    ///     <target name="build">
    ///         <property name="debug" value="false"/>
    ///         <call target="compile"/>
    ///         <property name="debug" value="true"/>
    ///         <call target="compile" force="true"/> <!-- notice the force attribute -->
    ///     </target>
    /// </project>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("call")]
    public class CallTask : Task {

        string _target = null;
        bool _force = false;

        // Attribute properties
        /// <summary>NAnt target to call.</summary>
        [TaskAttribute("target", Required=true)]
        public string TargetName { get { return _target; } set { _target = value; }}

        /// <summary>Force a Execute even if the target has already been executed</summary>
        [TaskAttribute("force")]
        public bool ForceExecute { set { _force= value; }}

        protected override void ExecuteTask() {
            if(_force) {
                Target t = Project.Targets.Find(_target);
                if(t == null) {
                    // if we can't find it, then neither should Project.Execute.
                    // Let them do the error handling and exception generation.
                    Project.Execute(_target);
                }

                //Execute a copy.
                t.Copy().Execute();

            }
            else {
                Project.Execute(TargetName);
            }
        }
    }
}
