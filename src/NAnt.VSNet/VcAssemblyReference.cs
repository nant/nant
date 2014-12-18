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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.VSNet {
    public class VcAssemblyReference : AssemblyReferenceBase {
        public VcAssemblyReference(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(xmlDefinition, referencesResolver, parent, gacCache) {
            XmlAttribute privateAttribute = xmlDefinition.Attributes["CopyLocal"];
            if (privateAttribute != null) {
                _isPrivateSpecified = true;
                _isPrivate = bool.Parse(privateAttribute.Value);
            }

            // determine name of reference by taking filename part of relative
            // path, without extension
            XmlAttribute relativePathAttribute = XmlDefinition.Attributes["RelativePath"];
            if (relativePathAttribute != null) {
                _name = Path.GetFileNameWithoutExtension(relativePathAttribute.Value);
            }

            _assemblyFile = ResolveAssemblyReference();
        }

        #region Override implementation of AssemblyReferenceBase

        protected override bool IsPrivate {
            get { return _isPrivate; }
        }

        protected override bool IsPrivateSpecified {
            get { return _isPrivateSpecified; }
        }

        /// <summary>
        /// Resolves an assembly reference.
        /// </summary>
        /// <returns>
        /// The full path to the resolved assembly, or <see langword="null" />
        /// if the assembly reference could not be resolved.
        /// </returns>
        protected override string ResolveAssemblyReference() {
            // check if assembly reference was resolved before
            if (_assemblyFile != null) {
                // if assembly file actually exists, there's no need to resolve
                // the assembly reference again
                if (File.Exists(_assemblyFile)) {
                    return _assemblyFile;
                }
            }

            XmlElement referenceElement = XmlDefinition;
            string assemblyFileName = null;

            string relativePath = referenceElement.GetAttribute("RelativePath");
            if (relativePath == null) {
                throw new BuildException("For Visual C++ projects only assembly"
                    + " references using relative paths are supported.", 
                    Location.UnknownLocation);
            } else {
                // expand macro's in RelativePath
                assemblyFileName = _rxMacro.Replace(relativePath, 
                    new MatchEvaluator(EvaluateMacro));

                // TODO: support locating assemblies in VCConfiguration.ReferencesPath,
                // but for now just remove it from reference filename and
                // search all assembly folders
                assemblyFileName = assemblyFileName.Replace("{ReferencesPath}\\",
                    string.Empty);
            }

            // 1. The RelativePath might be fully qualified (after macro expansion)
            if (Path.IsPathRooted(assemblyFileName)) {
                // consider assembly resolve although we're not sure whether 
                // the file actually exists
                //
                // the file might actually be created as result of building
                // a project
                return assemblyFileName;
            }

            // 2. The project directory
            // NOT SURE IF THIS IS CORRECT

            // 3. The ReferencePath
            // NOT SURE WE SHOULD DO THIS ONE

            // 4. The .NET Framework directory
            string resolvedAssemblyFile = ResolveFromFramework(assemblyFileName);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // 5. AssemblyFolders
            resolvedAssemblyFile = ResolveFromAssemblyFolders(referenceElement,
                Path.GetFileName(assemblyFileName));
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // assembly reference could not be resolved
            return null;
        }

        #endregion Override implementation of AssemblyReferenceBase

        #region Override implementation of ReferenceBase

        /// <summary>
        /// Gets the name of the referenced assembly.
        /// </summary>
        /// <value>
        /// The name of the referenced assembly, or <see langword="null" /> if
        /// the name could not be determined.
        /// </value>
        public override string Name {
            get { return _name; }
        }

        #endregion Override implementation of ReferenceBase

        #region Private Instance Methods

        /// <summary>
        /// Is called each time a regular expression match is found during a 
        /// <see cref="M:Regex.Replace(string, MatchEvaluator)" /> operation.
        /// </summary>
        /// <param name="m">The <see cref="Match" /> resulting from a single regular expression match during a <see cref="M:Regex.Replace(string, MatchEvaluator)" />.</param>
        /// <returns>
        /// The expanded <see cref="Match" />.
        /// </returns>
        /// <exception cref="BuildException">The macro is not supported.</exception>
        /// <exception cref="NotImplementedException">Expansion of a given macro is not yet implemented.</exception>
        private string EvaluateMacro(Match m) {
            string macro = m.Groups[1].Value;

            // expand using solution level macro's
            string expandedMacro = SolutionTask.ExpandMacro(macro);
            if (expandedMacro != null) {
                return expandedMacro;
            }

            // expand using project level macro's
            expandedMacro = Parent.ExpandMacro(macro);
            if (expandedMacro != null) {
                return expandedMacro;
            }

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Macro \"{0}\", used by assembly reference \"{1}\" in project"
                + " \"{2}\" is not supported in assembly references.", macro, 
                Name, Parent.Name), Location.UnknownLocation);
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _assemblyFile;
        private readonly bool _isPrivateSpecified;
        private readonly bool _isPrivate;
        private readonly string _name = string.Empty;
        private readonly Regex _rxMacro = new Regex(@"\$\((\w+)\)");

        #endregion Private Instance Fields
    }
}
