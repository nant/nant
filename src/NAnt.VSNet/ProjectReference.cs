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

        public override DirectoryInfo GetBaseDirectory(ConfigurationSettings config) {
            return Project.GetConfiguration(config.Name).OutputDir;
        }

        public override string GetOutputFile(ConfigurationBase config) {
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
        public override StringCollection GetAssemblyReferences(ConfigurationBase config) {
            // if we're dealing with a project reference, then we need to
            // reference all assembly references of that project
            StringCollection assemblyReferences = Project.GetAssemblyReferences(
                config.Name);

            // and the project output file itself
            string projectOutputFile = Project.GetConfiguration(
                config.Name).OutputPath;
            if (!File.Exists(projectOutputFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Output file '{0}' of project '{1}' does not exist.",
                    projectOutputFile, Project.Name), Location.UnknownLocation);
            }
            assemblyReferences.Add(projectOutputFile);
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
