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

using SourceForge.NAnt.Attributes;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;


namespace SourceForge.NAnt.Tasks {

    /// <summary>Tell Nant to load a tasks form a given assembly or all assemblies in given directories.</summary>
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

        string _assembly = null;
        string _path = null;
        string _toDirectory = null;
        bool _overwrite = false;
    
        FileSet _fileset = new FileSet();        
        
        #region Public Attribute Properties
        /// <summary>The file to copy.</summary>
        [TaskAttribute("assembly")]
        public string AssemblyPath        { get { return _assembly; } set {_assembly = value; } }

        /// <summary>The file to copy to.</summary>
        [TaskAttribute("path")]
        public string Path            { get { return _path; } set {_path = value; } }
        
         /// <summary>Filesets are used to select which directories or individual assemblies to scan.</summary>
        [FileSet("fileset")]
        public FileSet TaskFileSet      { get { return _fileset; } }
        
        #endregion
        /// <summary>
        ///  Validate eAttributes
        /// </summary>        
        protected  void ValidateAttributes(){ 
            //verify that our params are correct
            if ( AssemblyPath != null  && (Path != null) ){
                string msg = "Both asssembly  and path attributes are set. Use one or the other.";
                throw new BuildException( msg, Location );
            }
        }
        
        /// <summary>
        /// Executes the Load Tasks task.
        /// </summary>
        /// <exception cref="BuildException">A file that has to be copied does not exist or could not be copied.</exception>
        protected override void ExecuteTask() {
            
            ValidateAttributes();
            // single file case
            if ( AssemblyPath != null ) {
                if ( ! File.Exists(   Project.GetFullPath( AssemblyPath )) ) {
                    string msg = String.Format(CultureInfo.InvariantCulture,"assembly {0} does not exist. Can't scan for tasks", AssemblyPath );
                    throw new BuildException( msg, Location );
                }  
                TaskFileSet.FileNames.Add(  AssemblyPath );
            }
            else if (Path != null){
                if ( ! Directory.Exists( Project.GetFullPath( Path )) ){
                    string msg = String.Format(CultureInfo.InvariantCulture,"path {0} does not exist. Can't scan for tasks", Path );
                    throw new BuildException( msg, Location );
                }
                TaskFileSet.DirectoryNames.Add( Path );
            }
            // process the fileset
            foreach ( string assemblyPath in TaskFileSet.FileNames){
                Log.WriteLine( LogPrefix + "Loading Tasks from Assembly {0}", assemblyPath );
                TaskFactory.AddTasks( Assembly.LoadFrom(assemblyPath) );
            }
            // now the filenames
            foreach ( string scanPath in TaskFileSet.DirectoryNames ){
                Log.WriteLine( LogPrefix + "Scanning directory {0} for task assemblies", scanPath );
                TaskFactory.ScanDir( scanPath );
            }            
        }
    }
}