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

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public abstract class AssemblyReferenceBase : FileReferenceBase {
        #region Protected Instance Constructors

        protected AssemblyReferenceBase(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(xmlDefinition, referencesResolver, parent, gacCache) {
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
            get {
                if (IsPrivateSpecified) {
                    return IsPrivate;
                } else {
                    return !IsSystem && !GacCache.IsAssemblyInGac(
                        ResolveAssemblyReference());
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this reference represents a system 
        /// assembly.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this reference represents a system 
        /// assembly; otherwise, <see langword="false" />.
        /// </value>
        protected override bool IsSystem {
            get { 
                string assemblyFile = ResolveAssemblyReference();
                return string.Compare(Path.GetDirectoryName(assemblyFile), 
                    SolutionTask.Project.TargetFramework.FrameworkAssemblyDirectory.FullName, 
                    true, CultureInfo.InvariantCulture) == 0;
            }
        }

        /// <summary>
        /// Gets the path of the reference, without taking the "copy local"
        /// setting into consideration.
        /// </summary>
        /// <param name="config">The project configuration.</param>
        /// <returns>
        /// The output path of the reference.
        /// </returns>
        public override string GetPrimaryOutputFile(ConfigurationBase config) {
            return ResolveAssemblyReference();
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
            return base.GetAssemblyOutputFiles(ResolveAssemblyReference());
        }

        /// <summary>
        /// Gets the complete set of assemblies that need to be referenced when
        /// a project references this component.
        /// </summary>
        /// <param name="config">The project configuration.</param>
        /// <returns>
        /// The complete set of assemblies that need to be referenced when a 
        /// project references this component.
        /// </returns>
        public override StringCollection GetAssemblyReferences(ConfigurationBase config) {
            // if we're dealing with an assembly reference, then we only 
            // need to reference that assembly itself as VS.NET forces users
            // to add all dependent assemblies to the project itself

            // ensure referenced assembly actually exists
            string assemblyFile = ResolveAssemblyReference();
            if (!File.Exists(assemblyFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Couldn't find assembly \"{0}\", referenced by project"
                    + " \"{1}\".", assemblyFile, Parent.Name), 
                    Location.UnknownLocation);
            }

            // add referenced assembly to list of reference assemblies
            StringCollection assemblyReferences = new StringCollection();
            assemblyReferences.Add(assemblyFile);

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
            return GetTimestamp(ResolveAssemblyReference());
        }

        #endregion Override implementation of ReferenceBase

        #region Public Instance Methods

        public ProjectReferenceBase CreateProjectReference(ProjectBase project) {
            return project.CreateProjectReference(project, IsPrivateSpecified, 
                IsPrivate);
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        protected abstract string ResolveAssemblyReference();

        /// <summary>
        /// Searches for the given file in all paths in <paramref name="folderList" />.
        /// </summary>
        /// <param name="folderList">The folders to search.</param>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>
        /// The path of the assembly if <paramref name="fileName" /> was found
        /// in <paramref name="folderList" />; otherwise, <see langword="null" />.
        /// </returns>
        protected string ResolveFromFolderList(StringCollection folderList, string fileName) {
            foreach (string path in folderList) {
                try {
                    string assemblyFile = Path.Combine(path, fileName);
                    if (File.Exists(assemblyFile)) {
                        return assemblyFile;
                    }
                } catch (Exception ex) {
                    Log(Level.Verbose, "Error resolve reference to \"{0}\""
                        + " in directory \"{1}\".", fileName, path);
                    Log(Level.Debug, ex.ToString());
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves an assembly reference in the framework assembly directory
        /// of the target framework.
        /// </summary>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>
        /// The full path of the assembly file if the assembly could be located 
        /// in the framework assembly directory; otherwise, <see langword="null" />.
        /// </returns>
        protected string ResolveFromFramework(string fileName) {
            string systemAssembly = Path.Combine(SolutionTask.Project.TargetFramework.
                FrameworkAssemblyDirectory.FullName, fileName);
            if (File.Exists(systemAssembly)) {
                return systemAssembly;
            }
            return null;
        }

        /// <summary>
        /// Resolves an assembly reference using a path relative to the project 
        /// directory.
        /// </summary>
        /// <returns>
        /// The full path of the assembly, or <see langword="null" /> if 
        /// <paramref name="relativePath" /> is <see langword="null" /> or an
        /// empty <see cref="string" />.
        /// </returns>
        protected string ResolveFromRelativePath(string relativePath) {
            if (!StringUtils.IsNullOrEmpty(relativePath)) {
                return Path.GetFullPath(Path.Combine(Parent.ProjectDirectory.FullName, 
                    relativePath));
            }
            return null;
        }

        protected virtual string ResolveFromAssemblyFolders(XmlElement referenceElement, string fileName) {
            return ResolveFromFolderList(SolutionTask.AssemblyFolderList, 
                fileName);
        }

        #endregion Protected Instance Methods
    }
}
