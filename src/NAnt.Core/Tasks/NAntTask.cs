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

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Runs NAnt on a supplied build file. This can be used to build subprojects.
    /// </summary>
    /// <example>
    ///   <para>Build a project located in a different directory if the <c>debug</c> property is not <c>true</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <nant buildfile="${src.dir}/Extras/BuildServer/BuildServer.build" unless="${debug}" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nant")]
    public class NAntTask : Task {
        #region Private Instance Fields

        private string _buildFileName = null;
        private string _target = null;
        private bool _inheritAll = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The build file to build. If not specified, use the current build file.
        /// </summary>
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

        /// <summary>
        /// The target to execute. To specify more than one target seperate 
        /// targets with a space. Targets are executed in order if possible. 
        /// Defaults to use target specified in the project's default attribute.
        /// </summary>
        [TaskAttribute("target")]
        public string DefaultTarget {
            get { return _target; }
            set { _target = value; }
        }

        /// <summary>
        /// Specifies whether current property values should be inherited by 
        /// the executed project. Default is <c>false</c>.
        /// </summary>
        [TaskAttribute("inheritall"), BooleanValidator()]
        public bool InheritAll {
            get { return _inheritAll; }
            set { _inheritAll = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            Log(Level.Info, LogPrefix + "{0} {1}", BuildFileName, DefaultTarget);
            Log(Level.Info, string.Empty);

            // create new project with same threshold as current project and increased indentation level
            Project project = new Project(Project.GetFullPath(BuildFileName), Project.Threshold, Project.IndentationLevel + 1);

            // add listeners of current project to new project
            project.AttachBuildListeners(Project.BuildListeners);

            // have the new project inherit the default framework from the current project
            if (Project.DefaultFramework != null && project.FrameworkInfoDictionary.Contains(Project.DefaultFramework.Name)) {
                project.DefaultFramework = project.FrameworkInfoDictionary[Project.DefaultFramework.Name];
            }

            // have the new project inherit the current framework from the current project 
            if (Project.CurrentFramework != null && project.FrameworkInfoDictionary.Contains(Project.CurrentFramework.Name)) {
                project.CurrentFramework = project.FrameworkInfoDictionary[Project.CurrentFramework.Name];
            }

            if (InheritAll) {
                StringCollection excludes = new StringCollection();
                excludes.Add(Project.NAntPropertyFileName);
                excludes.Add(Project.NAntPropertyLocation);
                excludes.Add(Project.NAntPropertyOnSuccess);
                excludes.Add(Project.NAntPropertyOnFailure);
                excludes.Add(Project.NAntPropertyProjectBaseDir);
                excludes.Add(Project.NAntPropertyProjectBuildFile);
                excludes.Add(Project.NAntPropertyProjectDefault);
                excludes.Add(Project.NAntPropertyProjectName);
                excludes.Add(Project.NAntPropertyVersion);
                project.Properties.Inherit(Properties, excludes);
            }
            // pass datatypes thru to the child project
            project.DataTypeReferences.Inherit( Project.DataTypeReferences );
            
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
