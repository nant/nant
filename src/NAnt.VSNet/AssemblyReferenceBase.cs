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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;
using NAnt.Core;
using NAnt.Core.Util;

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
                    // only copy local if assembly reference could be resolved,
                    // if not a system assembly and is not in the GAC
                    string assemblyFile = ResolveAssemblyReference();
                    return assemblyFile != null && !IsSystem && 
                        !GacCache.IsAssemblyInGac(assemblyFile);
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
                // if the assembly cannot be resolved, we consider it not to
                // be a system assembly
                string assemblyFile = ResolveAssemblyReference();
                if (assemblyFile == null) {
                    return false;
                }
                // check if assembly is stored in the framework assembly 
                // directory
                return string.Compare(Path.GetDirectoryName(assemblyFile), 
                    SolutionTask.Project.TargetFramework.FrameworkAssemblyDirectory.FullName, 
                    true, CultureInfo.InvariantCulture) == 0;
            }
        }

        /// <summary>
        /// Gets the path of the reference, without taking the "copy local"
        /// setting into consideration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The output path of the reference.
        /// </returns>
        public override string GetPrimaryOutputFile(Configuration solutionConfiguration) {
            return ResolveAssemblyReference();
        }

        /// <summary>
        /// Gets the complete set of output files for the referenced project.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <param name="outputFiles">The set of output files to be updated.</param>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public override void GetOutputFiles(Configuration solutionConfiguration, Hashtable outputFiles) {
            string assemblyFile = ResolveAssemblyReference();
            if (assemblyFile != null) {
                base.GetAssemblyOutputFiles(assemblyFile, outputFiles);
            }
        }

        /// <summary>
        /// Gets the complete set of assemblies that need to be referenced when
        /// a project references this component.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The complete set of assemblies that need to be referenced when a 
        /// project references this component.
        /// </returns>
        public override StringCollection GetAssemblyReferences(Configuration solutionConfiguration) {
            // if we're dealing with an assembly reference, then we only 
            // need to reference that assembly itself as VS.NET forces users
            // to add all dependent assemblies to the project itself

            StringCollection assemblyReferences = new StringCollection();

            // attempt to resolve assembly reference
            string assemblyFile = ResolveAssemblyReference();
            if (assemblyFile == null) {
                Log(Level.Warning, "Assembly \"{0}\", referenced"
                    + " by project \"{1}\", could not be resolved.", Name, 
                    Parent.Name);
                return assemblyReferences;
            }

            // ensure assembly actually exists
            if (!File.Exists(assemblyFile)) {
                Log(Level.Warning, "Assembly \"{0}\", referenced"
                    + " by project \"{1}\", does not exist.", assemblyFile, 
                    Parent.Name);
                return assemblyReferences;
            }

            // add referenced assembly to list of reference assemblies
            assemblyReferences.Add(assemblyFile);

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
            string assemblyFile = ResolveAssemblyReference();
            if (assemblyFile == null) {
                return DateTime.MaxValue;
            }
            return GetFileTimestamp(assemblyFile);
        }

        #endregion Override implementation of ReferenceBase

        #region Public Instance Methods

        public ProjectReferenceBase CreateProjectReference(ProjectBase project) {
            return project.CreateProjectReference(project, IsPrivateSpecified, 
                IsPrivate);
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Resolves an assembly reference.
        /// </summary>
        /// <returns>
        /// The full path to the resolved assembly, or <see langword="null" />
        /// if the assembly reference could not be resolved.
        /// </returns>
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
            Log(Level.Debug, "Attempting to resolve \"{0}\" in AssemblyFolders...",
                fileName);

            foreach (string path in folderList) {
                Log(Level.Debug, "Checking \"{0}\"...", path);
                try {
                    string assemblyFile = FileUtils.CombinePaths(path, fileName);
                    if (File.Exists(assemblyFile)) {
                        Log(Level.Debug, "Assembly found in \"{0}\".", path);
                        return assemblyFile;
                    } else {
                        Log(Level.Debug, "Assembly not found in \"{0}\".", path);
                    }
                } catch (Exception ex) {
                    Log(Level.Verbose, "Error resolving reference to \"{0}\""
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

            //string systemAssembly = FileUtils.CombinePaths(SolutionTask.Project.TargetFramework.
            //    FrameworkAssemblyDirectory.FullName, fileName);
            string systemAssembly = SolutionTask.Project.TargetFramework.ResolveAssembly(fileName);

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
            if (!String.IsNullOrEmpty(relativePath)) {
                // TODO: VS.NET seems to be able to handle a project dir / hint 
                // path combination that is more than 260 characters long
                //
                // eg. ../Assemblies/..Assemblies/../......

                string combinedPath = FileUtils.CombinePaths(Parent.ProjectDirectory.FullName, 
                    relativePath);

                try {
                    return FileUtils.GetFullPath(combinedPath);
                } catch (PathTooLongException ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Assembly \"{0}\", referenced by project \"{1}\", could not be"
                        + " resolved using path \"{2}\".", Name, Parent.Name, combinedPath), 
                        Location.UnknownLocation, ex);
                }
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
