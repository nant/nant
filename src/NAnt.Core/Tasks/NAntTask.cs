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

using System.Collections.Specialized;
using System.Globalization;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Runs NAnt on a supplied build file. This can be used to build subprojects.
    /// </summary>
    /// <example>
    ///   <para>Build the BuildServer project located in a different directory but only if the <c>debug</c> property is not true.</para>
    ///   <code><![CDATA[<nant unless="${debug}" buildfile="${src.dir}/Extras/BuildServer/BuildServer.build"/>]]></code>
    /// </example>
    [TaskName("nant")]
    public class NAntTask : Task {
        #region Private Instance Fields

        string _buildFileName = null;
        string _target = null;
        bool _inheritAll = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>The build file to build. If not specified, use the current build file.</summary>
        [TaskAttribute("buildfile")]
        public string BuildFileName {
            get { 
                if (_buildFileName != null) {
                    return _buildFileName;
                }
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

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            Log(Level.Verbose, LogPrefix + "{0} {1}", BuildFileName, DefaultTarget);

            // create new prjoect with same threshold as current project and increased indentation level
            Project project = new Project(Project.GetFullPath(BuildFileName), Project.Threshold, Project.IndentationLevel + 1);

            // add listeners of current project to new project
            project.AttachBuildListeners(Project.BuildListeners);

            // have the new project inherit the framework from the current project 
            if (Project.CurrentFramework != null && project.FrameworkInfoDictionary.Contains(Project.CurrentFramework.Name)) {
                project.CurrentFramework = project.FrameworkInfoDictionary[Project.CurrentFramework.Name];
            }

            if (InheritAll) {
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
        }

        #endregion Override implementation of Task
    }
}
