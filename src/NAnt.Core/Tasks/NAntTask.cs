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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Runs NAnt on a supplied build file. This can be used to build subprojects.</summary>
    /// <example>
    ///   <para>Build the BuildServer project located in a different directory but only if the <c>debug</c> property is not true.</para>
    ///   <code><![CDATA[<nant unless="${debug}" buildfile="${src.dir}/Extras/BuildServer/BuildServer.build"/>]]></code>
    /// </example>
    [TaskName("nant")]
    public class NAntTask : Task {

        string _buildFileName = null;
        string _target = null;
        bool _inheritAll = true;
        bool _newAppDomain = false;

        // TODO: Support for handling properties.  Should it inherit the parent 
        // project's properties.  How to set properties for the new project?  
        // The same Ant task handles these issues.

        /// <summary>The build file to build. If not specified, use the current build file.</summary>
        [TaskAttribute("appdomain")]
        public bool NewAppDomain {
            set { _newAppDomain = value; }
        }

        /// <summary>The build file to build. If not specified, use the current build file.</summary>
        [TaskAttribute("buildfile")]
        public string BuildFileName {
            get { 
                if(_buildFileName != null)
                    return _buildFileName;
                return Project.BuildFileLocalName; 
            }
            set { _buildFileName = value; }
        }

        /// <summary>The target to execute.  To specify more than one target seperate targets with a space.  Targets are executed in order if possible.  Default to use target specified in the project's default attribute.</summary>
        [TaskAttribute("target")]
        public string DefaultTarget {
            get { return _target; }
            set { _target = value; }
        }

        /// <summary>Specifies whether current property values should be inherited by the executed project. Default is false.</summary>
        [TaskAttribute("inheritall"), BooleanValidator()]
        public bool InheritAll {
            get { return _inheritAll; }
            set { _inheritAll = value; }
        }

        protected override void ExecuteTask() {
            try {
                Log.WriteLine(LogPrefix + "{0} {1}", BuildFileName, DefaultTarget);
                Log.Indent();
                Project project = new Project(Project.GetFullPath(BuildFileName), Verbose);
                //project.Verbose = Verbose;

                if ( _inheritAll ) {
                    StringCollection excludes = new StringCollection();
                    excludes.Add(Project.NANT_PROPERTY_FILENAME);
                    excludes.Add(Project.NANT_PROPERTY_LOCATION);
                    excludes.Add(Project.NANT_PROPERTY_ONSUCCESS);
                    excludes.Add(Project.NANT_PROPERTY_ONFAILURE);
                    excludes.Add(Project.NANT_PROPERTY_PROJECT_BASEDIR);
                    excludes.Add(Project.NANT_PROPERTY_PROJECT_BUILDFILE);
                    excludes.Add(Project.NANT_PROPERTY_PROJECT_DEFAULT);
                    excludes.Add(Project.NANT_PROPERTY_PROJECT_NAME);
                    excludes.Add(Project.NANT_PROPERTY_VERSION);
                    project.Properties.Inherit(Properties, excludes);
                }

                // handle multiple targets
                if (DefaultTarget != null) {
                    foreach (string t in DefaultTarget.Split(' ')) {
                        string target = t.Trim();
                        if (target.Length > 0) {
                            project.BuildTargets.Add(target);
                        }
                    }
                }
                if (!project.Run()) {
                    throw new BuildException("Nested build failed.  Refer to build log for exact reason.");
                }
            } finally {
                Log.Unindent();
            }
        }
    }
}
