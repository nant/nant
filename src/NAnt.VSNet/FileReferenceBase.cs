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
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public abstract class FileReferenceBase : ReferenceBase {
        #region Protected Instance Constructors

        protected FileReferenceBase(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(referencesResolver, parent) {
            if (xmlDefinition == null) {
                throw new ArgumentNullException("xmlDefinition");
            }
            if (gacCache == null) {
                throw new ArgumentNullException("gacCache");
            }

            _xmlDefinition = xmlDefinition;
            _gacCache = gacCache;
        }

        #endregion Protected Instance Constructors

        #region Protected Instance Properties

        protected XmlElement XmlDefinition {
            get { return _xmlDefinition; }
        }

        protected GacCache GacCache {
            get { return _gacCache; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of ReferenceBase

        /// <summary>
        /// Gets a value indicating whether the reference is managed for the
        /// specified configuration.
        /// </summary>
        /// <param name="config">The build configuration of the reference.</param>
        /// <returns>
        /// <see langword="true" />.
        /// </returns>
        public override bool IsManaged(Configuration config) {
            return true;
        }

        #endregion Override implementation of ReferenceBase

        #region Protected Instance Methods

        /// <summary>
        /// Gets the complete set of output files for the specified assembly 
        /// and adds them to <paremref name="outputFiles"/> collection.
        /// </summary>
        /// <param name="assemblyFile">The path of the assembly to get the output files for.</param>
        /// <param name="outputFiles">The set of output files to be updated.</param>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        protected void GetAssemblyOutputFiles(string assemblyFile, Hashtable outputFiles) {
            if (!File.Exists(assemblyFile)) {
                // no need to output warning if set of output files cannot be
                // generated
                return;
            }

            if (!outputFiles.ContainsKey(assemblyFile)) {
                string[] referencedModules = GetAllReferencedModules(assemblyFile);

                // get a list of the references in the output directory
                foreach (string referenceFile in referencedModules) {
                    // skip module if module is not the assembly referenced by 
                    // the project and is installed in GAC
                    if (string.Compare(referenceFile, assemblyFile, true, CultureInfo.InvariantCulture) != 0) {
                        // skip referenced module if the assembly referenced by
                        // the project is a system reference or the module itself
                        // is installed in the GAC
                        if (IsSystem || GacCache.IsAssemblyInGac(referenceFile)) {
                            continue;
                        }
                    }

                    // get list of files related to referenceFile, this will include
                    // referenceFile itself
                    GetRelatedFiles(referenceFile, outputFiles);
                }
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private string[] GetAllReferencedModules(string module) {
            string fullPathToModule = FileUtils.GetFullPath(module);
            string moduleDirectory = Path.GetDirectoryName(fullPathToModule);

            Hashtable allReferences = new Hashtable();
            Hashtable unresolvedReferences = new Hashtable();

            try {
                allReferences.Add(fullPathToModule, null);
                unresolvedReferences.Add(fullPathToModule, null);

                while (unresolvedReferences.Count > 0) {
                    IDictionaryEnumerator unresolvedEnumerator = unresolvedReferences.GetEnumerator();
                    unresolvedEnumerator.MoveNext();

                    string referenceToResolve = (string) unresolvedEnumerator.Key;

                    unresolvedReferences.Remove(referenceToResolve);

                    ReferencesResolver.AppendReferencedModulesLocatedInGivenDirectory(
                        moduleDirectory, referenceToResolve, ref allReferences, 
                        ref unresolvedReferences);
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error resolving module references of '{0}'.", fullPathToModule),
                    Location.UnknownLocation, ex);
            }

            string[] result = new string[allReferences.Keys.Count];
            allReferences.Keys.CopyTo(result, 0);
            return result;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly XmlElement _xmlDefinition;
        private readonly GacCache _gacCache;

        #endregion Private Instance Fields
    }
}
