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
    /// Loads tasks form a given assembly or all assemblies in a given directory
    /// or <see cref="FileSet" />.
    /// </summary>
    /// <remarks></remarks>
     /// <example>
    ///   <para>
    ///   Load tasks from a single assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks assembly="c:foo\NAnt.Contrib.Tasks.dll" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Scan a single directory for task assemblies.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks path="c:\foo" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Use a <see cref="TaskFileSet" /> containing both a directory and an 
    ///   assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadtasks>
    ///    <fileset>
    ///        <include name="C:\cvs\NAntContrib\build" />
    ///        <include name="C:\cvs\NAntContrib\build\NAnt.Contrib.Tasks.dll" />
    ///    </fileset>
    ///</loadtasks>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("loadtasks")]
    public class LoadTasksTask : Task {
        #region Private Instance Fields

        private FileInfo _assembly;
        private DirectoryInfo _path;
        private FileSet _fileset = new FileSet();

        #endregion Private Instance Fields
        
        #region Public Instance Properties

        /// <summary>
        /// An assembly to load tasks from.
        /// </summary>
        [TaskAttribute("assembly")]
        public FileInfo AssemblyPath {
            get { return _assembly; }
            set { _assembly = value; }
        }

        /// <summary>
        /// A directory to scan for task assemblies.
        /// </summary>
        [TaskAttribute("path")]
        public DirectoryInfo Path {
            get { return _path; }
            set { _path = value; }
        }
        
         /// <summary>
         /// Used to select which directories or individual assemblies to scan.
         /// </summary>
        [BuildElement("fileset")]
        public FileSet TaskFileSet {
            get { return _fileset; }
            set { _fileset = value; }
        }
        
        #endregion Public Instance Properties

        #region Override implemenation of Task
        
        /// <summary>
        /// Executes the Load Tasks task.
        /// </summary>
        /// <exception cref="BuildException">Specified assembly or path does not exist.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (TaskFileSet.BaseDirectory == null) {
                TaskFileSet.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (AssemblyPath != null) { // single file case
                if (!AssemblyPath.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Assembly '{0}' does not exist. Can't scan for extensions.", 
                        AssemblyPath.FullName), Location);
                }  
                TaskFileSet.FileNames.Add(AssemblyPath.FullName);
            } else if (Path != null) {
                if (!Path.Exists) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Path '{0}' does not exist. Can't scan for extensions.", 
                        Path), Location);
                }
                TaskFileSet.DirectoryNames.Add(Path.FullName);
            }
            // process the fileset
            foreach (string assemblyPath in TaskFileSet.FileNames) {
                Log(Level.Info, LogPrefix + "Loading tasks from assembly '{0}'.", 
                    assemblyPath);
                TypeFactory.ScanAssembly(assemblyPath);
            }
            // now the filenames
            foreach (string scanPath in TaskFileSet.DirectoryNames) {
                Log(Level.Info, LogPrefix + "Scanning directory '{0}' for extension assemblies.", scanPath);
                TypeFactory.ScanDir(scanPath);
            }
        }

        /// <summary>
        /// Validates the attributes.
        /// </summary>
        /// <exception cref="BuildException">Both <see cref="AssemblyPath" /> and <see cref="Path" /> are set.</exception>
        protected override void InitializeTask(XmlNode taskNode) {
            //verify that our params are correct
            if (AssemblyPath != null && Path != null) {
                throw new BuildException("Both asssembly and path attributes are set." 
                    + " Use one or the other.", Location);
            }
        }

        #endregion Override implemenation of Task
    }
}
