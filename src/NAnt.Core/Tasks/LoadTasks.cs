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
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Loads tasks form a given assembly or all assemblies in given directories.
    /// </summary>
    /// <remarks></remarks>
     /// <example>
    ///   <para>
    ///   Load tasks from a single assembly 
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks assembly="C:foo\NAnt.Contrib.Tasks.dll" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Scan a single directory for task assemblies 
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks path="C:\foo" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Use a fileset containing both directories and assembly paths
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks>
    ///    <fileset>
    ///        <includes name="C:\cvs\NAntContrib\build"/>
    ///        <includes name="C:\cvs\NAntContrib\build\NAnt.Contrib.Tasks.dll"/>
    ///    </fileset>   
    ///</loadtasks>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("loadtasks")]
    public class LoadTasksTask : Task {
        #region Private Instance Fields

        string _assembly = null;
        string _path = null;
        FileSet _fileset = new FileSet();

        #endregion Private Instance Fields
        
        #region Public Instance Properties

        /// <summary>An assembly to load tasks from.</summary>
        [TaskAttribute("assembly")]
        public string AssemblyPath      { get { return _assembly; } set {_assembly = value; } }

        /// <summary>A directory to scan for task assemblies.</summary>
        [TaskAttribute("path")]
        public string Path              { get { return _path; } set {_path = value; } }
        
         /// <summary>Filesets are used to select which directories or individual assemblies to scan.</summary>
        [FileSet("fileset")]
        public FileSet TaskFileSet      { get { return _fileset; } }
        
        #endregion Public Instance Properties

        #region Override implemenation of Task
        
        /// <summary>
        /// Executes the Load Tasks task.
        /// </summary>
        /// <exception cref="BuildException">Specified Assembly does not exist or specified directory does not exist.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        protected override void ExecuteTask() {
            ValidateAttributes();
            // single file case
            if (AssemblyPath != null) {
                if (!File.Exists(Project.GetFullPath(AssemblyPath))) {
                    string msg = String.Format(CultureInfo.InvariantCulture,"assembly {0} does not exist. Can't scan for tasks", AssemblyPath);
                    throw new BuildException(msg, Location);
                }  
                TaskFileSet.FileNames.Add(Project.GetFullPath(AssemblyPath));
            } else if (Path != null) {
                if (!Directory.Exists(Project.GetFullPath(Path))) {
                    string msg = String.Format(CultureInfo.InvariantCulture,"Path {0} does not exist. Can't scan for tasks", Path);
                    throw new BuildException(msg, Location);
                }
                TaskFileSet.DirectoryNames.Add(Project.GetFullPath(Path));
            }
            // process the fileset
            foreach (string assemblyPath in TaskFileSet.FileNames) {
                Log(Level.Info, LogPrefix + "Loading tasks from assembly {0}.", assemblyPath);
                TaskFactory.AddTasks(Assembly.LoadFrom(assemblyPath));
            }
            // now the filenames
            foreach (string scanPath in TaskFileSet.DirectoryNames) {
                Log(Level.Info, LogPrefix + "Scanning directory {0} for task assemblies.", scanPath);
                TaskFactory.ScanDir(scanPath);
            }
        }

        #endregion Override implemenation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Validates the attributes.
        /// </summary>
        /// <exception cref="BuildException">Both <see cref="AssemblyPath" /> and <see cref="Path" /> are set.</exception>
        protected void ValidateAttributes(){ 
            //verify that our params are correct
            if (AssemblyPath != null  && Path != null) {
                string msg = "Both asssembly and path attributes are set. Use one or the other.";
                throw new BuildException(msg, Location);
            }
        }

        #endregion Protected Instance Methods
    }
}
