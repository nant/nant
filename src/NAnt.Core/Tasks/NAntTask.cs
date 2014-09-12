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

using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Runs NAnt on a supplied build file, or a set of build files.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   By default, all the properties of the current project will be available
    ///   in the new project. Alternatively, you can set <see cref="InheritAll" />
    ///   to <see langword="false" /> to not copy any properties to the new 
    ///   project.
    ///   </para>
    ///   <para>
    ///   You can also set properties in the new project from the old project by 
    ///   using nested property tags. These properties are always passed to the 
    ///   new project regardless of the setting of <see cref="InheritAll" />.
    ///   This allows you to parameterize your subprojects.
    ///   </para>
    ///   <para>
    ///   References to data types can also be passed to the new project, but by
    ///   default they are not. If you set the <see cref="InheritRefs" /> to 
    ///   <see langword="true" />, all references will be copied.
    ///   </para>
    /// </remarks>
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
    ///         <include name="**/default.build" />
    ///         <!-- avoid recursive execution of current build file -->
    ///         <exclude name="${project::get-buildfile-path()}" />
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
        private bool _inheritRefs;
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
        /// the executed project. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("inheritall")]
        [BooleanValidator()]
        public bool InheritAll {
            get { return _inheritAll; }
            set { _inheritAll = value; }
        }

        /// <summary>
        /// Specifies whether all references will be copied to the new project. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("inheritrefs")]
        [BooleanValidator()]
        public bool InheritRefs {
            get { return _inheritRefs; }
            set { _inheritRefs = value; }
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
        protected override void Initialize() {
            if (BuildFile != null && BuildFiles != null && BuildFiles.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1141")), Location);
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            // run the build file specified in an attribute
            if (BuildFile != null) {
                RunBuild(BuildFile);
            } else {
                if (BuildFiles.FileNames.Count == 0) {
                    Log(Level.Warning, "No matching build files found to run.");
                    return;
                }

                // run all build files specified in the fileset
                foreach (string buildFile in BuildFiles.FileNames) {
                    RunBuild(new FileInfo(buildFile));
                }
            }
        }

        private void RunBuild(FileInfo buildFile) {
            Log(Level.Info, "{0} {1}", buildFile.FullName, DefaultTarget);

            // create new project with same threshold as current project and 
            // increased indentation level, and initialize it using the same
            // configuration node
            Project project = new Project(buildFile.FullName, Project);

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
                // expand properties in context of current project for non-dynamic
                // properties
                if (!property.Dynamic) {
                    property.Value = Project.ExpandProperties(property.Value, Location);
                }
                property.Project = project;
                property.Execute();
            }

            if (InheritRefs) {
                // pass datatypes thru to the child project
                project.DataTypeReferences.Inherit(Project.DataTypeReferences);
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

            // run the given build
            if (!project.Run()) {
                throw new BuildException("Nested build failed.  Refer to build log for exact reason.");
            }
        }

        #endregion Override implementation of Task
    }
}
