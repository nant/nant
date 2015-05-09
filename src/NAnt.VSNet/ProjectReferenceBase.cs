// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Matthew Mastracci (matt@aclaro.com)
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public abstract class ProjectReferenceBase : ReferenceBase {
        #region Protected Instance Constructors

        protected ProjectReferenceBase(ReferencesResolver referencesResolver, ProjectBase parent) : base(referencesResolver, parent) {
        }

        #endregion Protected Instance Constructors

        #region Protected Instance Properties

        protected abstract bool IsPrivate {
            get;
        }

        protected abstract bool IsPrivateSpecified {
            get;
        }

        #endregion Protected Instance Properties

        #region Override implementation of ReferenceBase

        /// <summary>
        /// Gets a value indicating whether the output file(s) of this reference 
        /// should be copied locally.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output file(s) of this reference 
        /// should be copied locally; otherwise, <see langword="false" />.
        /// </value>
        public override bool CopyLocal {
            get { return IsPrivateSpecified ? IsPrivate : true; }
        }

        public override string Name {
            get { return Project.Name; }
        }

        /// <summary>
        /// Gets a value indicating whether this reference represents a system 
        /// assembly.
        /// </summary>
        /// <value>
        /// <see langword="false" /> as a project by itself can never be a
        /// system assembly.
        /// </value>
        protected override bool IsSystem {
            get { return false; }
        }

        /// <summary>
        /// Gets the output path of the reference, without taking the "copy local"
        /// setting into consideration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The output path of the reference.
        /// </returns>
        public override string GetPrimaryOutputFile(Configuration solutionConfiguration) {
            return Project.GetOutputPath(solutionConfiguration);
        }

        /// <summary>
        /// Gets the complete set of output files for the referenced project.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <param name="outputFiles">The set of output files to be updated.</param>
        /// <returns>
        /// The complete set of output files for the referenced project.
        /// </returns>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public override void GetOutputFiles(Configuration solutionConfiguration, Hashtable outputFiles) {
            Project.GetOutputFiles(solutionConfiguration, outputFiles);
        }

        /// <summary>
        /// Gets the complete set of assemblies that need to be referenced when
        /// a project references this project.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The complete set of assemblies that need to be referenced when a 
        /// project references this project.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Apparently, there's some hack in VB.NET that allows a type to be used
        /// that derives from a type in an assembly that is not referenced by the
        /// project.
        /// </para>
        /// <para>
        /// When building from the command line (using vbc), the following error
        /// is reported "error BC30007: Reference required to assembly 'X' 
        /// containing the base class 'X'. Add one to your project".
        /// </para>
        /// <para>
        /// Somehow VB.NET can workaround this issue, without actually adding a
        /// reference to that assembly. I verified this with both VS.NET 2003 and
        /// VS.NET 2005.
        /// </para>
        /// <para>
        /// For now, we have no other option than to return all assembly 
        /// references of the referenced project if the parent is a VB.NET 
        /// project.
        /// </para>
        /// </remarks>
        public override StringCollection GetAssemblyReferences(Configuration solutionConfiguration) {
            StringCollection assemblyReferences = null;

            // check if parent is a VB.NET project
            if (Parent is VBProject) {
                assemblyReferences = Project.GetAssemblyReferences(solutionConfiguration);
            } else {
                assemblyReferences = new StringCollection();
            }

            ConfigurationBase projectConfig = Project.GetConfiguration(
                solutionConfiguration);

            // check if project is actual configured to be built
            if (projectConfig != null) {
                string projectOutputFile = projectConfig.BuildPath;

                // check if project has output file
                if (projectOutputFile != null) {
                    if (File.Exists(projectOutputFile)) {
                        // add primary output to list of reference assemblies
                        assemblyReferences.Add(projectOutputFile);
                    }
                }
            }

            // return assembly references
            return assemblyReferences;
        }

        /// <summary>
        /// Gets the timestamp of the reference.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The timestamp of the reference.
        /// </returns>
        public override DateTime GetTimestamp(Configuration solutionConfiguration) {
            string projectOutputFile = Project.GetOutputPath(solutionConfiguration);
            if (projectOutputFile != null) {
                return GetFileTimestamp(projectOutputFile);
            } else {
                // if project has no output file, then we assume that it creates
                // a file through another way (eg. by launching an application 
                // that creates an assembly)
                return DateTime.MaxValue;
            }
        }

        #endregion Override implementation of ReferenceBase

        #region Public Instance Properties

        public abstract ProjectBase Project {
            get;
        }

        #endregion Public Instance Properties

        #region Protected Instance Methods

        protected ProjectBase LoadProject(SolutionBase solution, TempFileCollection tfc, GacCache gacCache, DirectoryInfo outputDir, string projectFile) {
            if (ProjectStack.Contains(projectFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Circular reference to \"{0}\" detected in project \"{1}\".", 
                    Path.GetFileNameWithoutExtension(projectFile), Parent.Name), 
                    Location.UnknownLocation);
            }

            try {
                ProjectStack.Push(projectFile);

                Log(Level.Verbose, "Loading referenced project '{0}'.", projectFile);
                return SolutionTask.ProjectFactory.LoadProject(solution, 
                    SolutionTask, tfc,  gacCache, ReferencesResolver, outputDir,
                    projectFile);
            } finally {
                ProjectStack.Pop();
            }
        }

        #endregion Protected Instance Methods

        #region Private Static Fields

        private static readonly Stack ProjectStack = new Stack();

        #endregion Private Static Fields
    }
}
