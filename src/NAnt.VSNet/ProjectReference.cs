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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public class ProjectReference : ReferenceBase {
        #region Public Instance Constructors

        public ProjectReference(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, SolutionBase solution, ProjectSettings projectSettings, GacCache gacCache, DirectoryInfo outputDir) : base(referencesResolver, parent) {
            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }

            if (gacCache == null) {
                throw new ArgumentNullException("gacCache");
            }

            if (projectSettings == null) {
                throw new ArgumentNullException("projectSettings");
            }

            if (solution == null) {
                throw new BuildException("Project reference found, but no solution"
                    + " was specified.", Location.UnknownLocation);
            }

            XmlAttribute privateAttribute = xmlDefinition.Attributes["Private"];
            if (privateAttribute != null) {
                _isPrivateSpecified = true;
                _isPrivate = bool.Parse(privateAttribute.Value);
            }

            // determine path of project file
            string projectFile = solution.GetProjectFileFromGuid(
                xmlDefinition.GetAttribute("Project"));

            // load referenced project
            Log(Level.Verbose, "Loading referenced project '{0}'.", projectFile);
            _project = ProjectFactory.LoadProject(solution, 
                SolutionTask, projectSettings.TemporaryFiles, gacCache, 
                ReferencesResolver, outputDir, projectFile);
        }

        public ProjectReference(ProjectBase project, ProjectBase parent, bool isPrivateSpecified, bool isPrivate) : base(project.ReferencesResolver, parent) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            _project = project;
            _isPrivateSpecified = isPrivateSpecified;
            _isPrivate = isPrivate;
        }

        #endregion Public Instance Constructors

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

        protected override bool IsPrivate {
            get { return _isPrivate; }
        }

        protected override bool IsPrivateSpecified {
            get { return _isPrivateSpecified; }
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
        /// <param name="config">The project configuration.</param>
        /// <returns>
        /// The output path of the reference.
        /// </returns>
        public override string GetPrimaryOutputFile(ConfigurationBase config) {
            return Project.GetOutputPath(config.Name);
        }

        /// <summary>
        /// Gets the complete set of output files for the referenced project.
        /// </summary>
        /// <param name="config">The project configuration.</param>
        /// <returns>
        /// The complete set of output files for the referenced project.
        /// </returns>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public override Hashtable GetOutputFiles(ConfigurationBase config) {
            return Project.GetOutputFiles(config.Name);
        }

        /// <summary>
        /// Gets the complete set of assemblies that need to be referenced when
        /// a project references this project.
        /// </summary>
        /// <param name="config">The project configuration.</param>
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
        public override StringCollection GetAssemblyReferences(ConfigurationBase config) {
            StringCollection assemblyReferences = null;

            // check if parent is a VB.NET project
            if (typeof(VBProject).IsAssignableFrom(Parent.GetType())) {
                assemblyReferences = Project.GetAssemblyReferences(config.Name);
            } else {
                assemblyReferences = new StringCollection();
            }

            string projectOutputFile = Project.GetConfiguration(
                config.Name).OutputPath;
            if (!File.Exists(projectOutputFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Output file '{0}' of project '{1}' does not exist.",
                    projectOutputFile, Project.Name), Location.UnknownLocation);
            }

            // add primary output to list of reference assemblies
            assemblyReferences.Add(projectOutputFile);

            // return assembly references
            return assemblyReferences;
        }

        /// <summary>
        /// Gets the timestamp of the reference.
        /// </summary>
        /// <param name="config">The build configuration of the reference.</param>
        /// <returns>
        /// The timestamp of the reference.
        /// </returns>
        public override DateTime GetTimestamp(ConfigurationBase config) {
            return GetTimestamp(Project.GetOutputPath(config.Name));
        }

        #endregion Override implementation of ReferenceBase

        #region Public Instance Properties

        public ProjectBase Project {
            get { return _project; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private readonly ProjectBase _project;
        private readonly bool _isPrivateSpecified;
        private readonly bool _isPrivate;

        #endregion Private Instance Fields
    }
}
