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

using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Compiles VS.NET solutions (or sets of projects), automatically determining project dependencies from inter-project references.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This task will analyze each of the given .csproj or .vbproj files and
    /// build them in the proper order.  It supports reading solution files, as well
    /// as enterprise template projects.
    /// </para>
    /// <para>
    /// This task also supports the model of referencing projects by their
    /// output filenames, rather than referencing them inside the solution.  It will
    /// automatically detect the existance of a file reference and convert it to a 
    /// project reference.  For example, if project A references the file in the
    /// release output directory of project B, the solution task will automatically convert
    /// this to a project dependency on project B and will reference the appropriate configuration output
    /// directory at the final build time (ie: reference the debug version of B if the solution is built as debug).
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Compiles all of the projects in <c>test.sln</c>, in relase mode, in the proper order.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release" solutionfile="test.sln">        
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles all of the projects in <c>projects.txt</c>, in the proper order.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release">        
    ///        <projects>
    ///            <includesList name="projects.txt" />
    ///        </projects>
    ///    </solution>
    ///     ]]>
    ///   </code>
    ///   <para>Compiles projects A, B and C, using the output of project X as a reference.</para>
    ///   <code>
    ///     <![CDATA[
    ///    <solution configuration="release">        
    ///        <projects>
    ///            <includes name="A\A.csproj" />
    ///            <includes name="B\b.vbproj" />
    ///            <includes name="C\c.csproj" />
    ///        </projects>
    ///     <referenceprojects>
    ///         <includes name="X\x.csproj" />
    ///     </referenceprojects>
    ///    </solution>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("solution")]
    public class SolutionTask : Task {
        public SolutionTask() {
            _strConfiguration = "";
            _fsProjects = new FileSet();
            _fsReferenceProjects = new FileSet();
        }

        protected override void ExecuteTask() {
            Log(Level.Info, LogPrefix + "Starting solution build.");
        
            Solution sln;
            if (Verbose) {
                Log(Level.Info, LogPrefix + "Included projects:" );
                foreach (string strProject in _fsProjects.FileNames) {
                    Log(Level.Info, LogPrefix + " - " + strProject);
                }

                Log(Level.Info, LogPrefix + "Reference projects:");
                foreach (string strProject in _fsReferenceProjects.FileNames) {
                    Log(Level.Info, LogPrefix + " - " + strProject);
                }
            }
            
            if (_strSolutionFile == null) {
                sln = new Solution(new ArrayList(_fsProjects.FileNames), new ArrayList(_fsReferenceProjects.FileNames), this);
            } else {
                sln = new Solution(_strSolutionFile, new ArrayList(_fsProjects.FileNames), new ArrayList(_fsReferenceProjects.FileNames), this);
            }
            if (!sln.Compile(_strConfiguration, new ArrayList(), null, Verbose, false)) {
                throw new BuildException("Project build failed");
            }
        }

        /// <summary>
        /// The names of the projects to build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional.
        /// </para>
        /// </remarks>
        [FileSet("projects", Required=false)]
        public FileSet Projects {
            get { return _fsProjects; }
            set { _fsProjects = value; }
        }

        /// <summary>
        /// The names of the projects to scan, but not build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional.  These projects are used to resolve project references.  These projects are
        /// generally external to the solution being built.  References to these project's output files
        /// are converted to use the appropriate solution configuration at build time.
        /// 
        /// </para>
        /// </remarks>
        [FileSet("referenceprojects", Required=false)]
        public FileSet ReferenceProjects {
            get { return _fsReferenceProjects; }
            set { _fsReferenceProjects = value; }
        }

        /// <summary>
        /// The name of the VS.NET solution file to build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional, can use <![CDATA[<projects>]]> list instead.
        /// </para>
        /// </remarks>
        [TaskAttribute("solutionfile", Required=false)]
        public string SolutionFile {
            set { _strSolutionFile = value; }
        }

        /// <summary>
        /// The name of the solution configuration to build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Generally <c>release</c> or <c>debug</c>.  Not case-sensitive.
        /// </para>
        /// </remarks>
        [TaskAttribute("configuration")]
        public string Configuration {
            set { _strConfiguration = value; }
        }

        string _strSolutionFile, _strConfiguration;
        FileSet _fsProjects, _fsReferenceProjects;
    }
}
