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
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Runs NAnt on a supplied build file, or a set of build files.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Build a project located in a different directory if the <c>debug</c> 
    ///   property is not <see langword="true" />.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nant buildfile="${src.dir}/Extras/BuildServer/BuildServer.build" unless="${debug}" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Build a project while adding a set of properties to that project.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nant buildfile="${src.dir}/Extras/BuildServer/BuildServer.build">
    ///     <properties>
    ///         <property name="build.dir" value="c:/buildserver" />
    ///         <property name="build.debug" value="false" />
    ///         <property name="lib.dir" value="c:/shared/lib" readonly="true" />
    ///     </properties>
    /// </nant>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Build all projects named <c>default.build</c> located anywhere under 
    ///   the project base directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <nant>
    ///     <buildfiles>
    ///         <includes name="**/default.build" />
    ///     </buildfiles>
    /// </nant>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nant")]
    public class NAntTask : Task {
        #region Private Instance Fields

        private FileInfo _buildFile;
        private FileSet _buildFiles = new FileSet();
        private string _target;
        private bool _inheritAll = true;
        private ArrayList _overrideProperties = new ArrayList();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The build file to build.
        /// </summary>
        [TaskAttribute("buildfile")]
        public FileInfo BuildFile {
            get { return _buildFile; }
            set { _buildFile = value; }
        }

        /// <summary>
        /// The target to execute. To specify more than one target seperate 
        /// targets with a space. Targets are executed in order if possible. 
        /// The default is to use target specified in the project's default 
        /// attribute.
        /// </summary>
        [TaskAttribute("target")]
        public string DefaultTarget {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Used to specify a set of build files to process.
        /// </summary>
        [BuildElement("buildfiles")]
        public virtual FileSet BuildFiles {
            get { return _buildFiles; }
            set { _buildFiles = value; }
        }

        /// <summary>
        /// Specifies whether current property values should be inherited by 
        /// the executed project. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("inheritall"), BooleanValidator()]
        public bool InheritAll {
            get { return _inheritAll; }
            set { _inheritAll = value; }
        }

        /// <summary>
        /// Specifies a collection of properties that should be created in the
        /// executed project.  Note, existing properties with identical names 
        /// that are not read-only will be overwritten.
        /// </summary>
        [BuildElementCollection("properties", "property", ElementType=typeof(PropertyTask))]
        public ArrayList OverrideProperties {
            get { return _overrideProperties; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Validates the <see cref="NAntTask" /> element.
        /// </summary>
        /// <param name="taskNode">The XML node of the task to use for initialization.</param>
        protected override void InitializeTask(XmlNode taskNode) {
            if (BuildFile != null && BuildFiles != null && BuildFiles.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "The 'buildfile' attribute and the <buildfiles> element" 
                    + " cannot be combined."), Location);
            }
        }

        protected override void ExecuteTask() {
            // run the build file specified in an attribute
            if (BuildFile != null) {
                RunBuild(BuildFile);
            } else {
                // run all build files specified in the fileset
                foreach (string buildFile in BuildFiles.FileNames) {
                    RunBuild(new FileInfo(buildFile));
                }
            }
        }

        private void RunBuild(FileInfo buildFile) {
            Log(Level.Info, LogPrefix + "{0} {1}", buildFile.FullName, DefaultTarget);
            Log(Level.Info, string.Empty);

            // create new project with same threshold as current project and 
            // increased indentation level, and initialize it using the same
            // configuration node
            Project project = new Project(buildFile.FullName, Project.Threshold, 
                Project.IndentationLevel + 1, Project.ConfigurationNode);

            // add listeners of current project to new project
            project.AttachBuildListeners(Project.BuildListeners);

            // have the new project inherit the runtime framework from the 
            // current project
            if (Project.RuntimeFramework != null && project.Frameworks.Contains(Project.RuntimeFramework.Name)) {
                project.RuntimeFramework = project.Frameworks[Project.RuntimeFramework.Name];
            }

            // have the new project inherit the current framework from the 
            // current project 
            if (Project.TargetFramework != null && project.Frameworks.Contains(Project.TargetFramework.Name)) {
                project.TargetFramework = project.Frameworks[Project.TargetFramework.Name];
            }

            // have the new project inherit properties from the current project
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

            // add/overwrite properties
            foreach (PropertyTask property in OverrideProperties) {
                property.Project = project;
                property.Execute();
            }

            // pass datatypes thru to the child project
            project.DataTypeReferences.Inherit(Project.DataTypeReferences);
            
            // handle multiple targets
            if (DefaultTarget != null) {
                foreach (string t in DefaultTarget.Split(' ')) {
                    string target = t.Trim();
                    if (target.Length > 0) {
                        project.BuildTargets.Add(target);
                    }
                }
            }

            // store original current directory
            string oldCurrentDirectory = Directory.GetCurrentDirectory();

            try {
                // change current directory to directory of the build file that
                // will be run
                Directory.SetCurrentDirectory(buildFile.DirectoryName);
                // run the given build
                if (!project.Run()) {
                    throw new BuildException("Nested build failed.  Refer to build log for exact reason.");
                }
            } finally {
                // restore current directory to original value
                Directory.SetCurrentDirectory(oldCurrentDirectory);
            }
        }

        #endregion Override implementation of Task
    }
}
