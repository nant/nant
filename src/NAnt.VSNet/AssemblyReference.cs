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
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class AssemblyReference : FileReferenceBase {
        #region Public Instance Constructors

        public AssemblyReference(XmlElement definition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(definition, referencesResolver, parent, gacCache) {
            // determine name of reference
            XmlAttribute assemblyNameAttribute = XmlDefinition.Attributes["AssemblyName"];
            if (assemblyNameAttribute != null) {
                _name = assemblyNameAttribute.Value;
            }

            _assemblyFile = ResolveAssemblyReference();
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
        /// Gets the name of the referenced assembly.
        /// </summary>
        /// <value>
        /// The name of the referenced assembly, or <see langword="null" /> if
        /// the name could not be determined.
        /// </value>
        public override string Name {
            get { return _name; }
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

        public override DirectoryInfo GetBaseDirectory(ConfigurationSettings config) {
            return new DirectoryInfo(Path.GetDirectoryName(
                ResolveAssemblyReference()));
        }

        public override string GetOutputFile(ConfigurationBase config) {
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
            // need to reference that assembly itself as VS.NET forced users
            // to add all dependent assemblies to the project itself

            // ensure referenced assembly actually exists
            string assemblyFile = ResolveAssemblyReference();
            if (!File.Exists(assemblyFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Couldn't find referenced assembly '{0}'.", assemblyFile), 
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

        #region Private Instance Methods

        /// <summary>
        /// <para>
        /// Resolves an assembly reference.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Visual Studio .NET uses the following search mechanism :
        /// </para>
        /// <list type="number">
        ///     <item>
        ///         <term>
        ///             The project directory.
        ///         </term>
        ///     </item>
        ///     <item>
        ///         <term>
        ///             The directories specified in the "ReferencePath" property, 
        ///             which is stored in the .USER file.
        ///         </term>
        ///     </item>
        ///     <item>
        ///         <term>
        ///             The .NET Framework directory (see KB306149) 
        ///         </term>
        ///     </item>
        ///     <item>
        ///         <term>
        ///             <para>
        ///                 The directories specified under the following registry 
        ///                 keys:
        ///             </para>
        ///             <list type="bullet">
        ///                 <item>
        ///                     <term>
        ///                         HKLM\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders
        ///                     </term>
        ///                 </item>
        ///                 <item>
        ///                     <term>
        ///                         HKCU\SOFTWARE\Microsoft\.NETFramework\AssemblyFolders
        ///                     </term>
        ///                 </item>
        ///                 <item>
        ///                     <term>
        ///                         HKLM\SOFTWARE\Microsoft\VisualStudio\&lt;major version&gt;.&lt;minor version&gt;\AssemblyFolders
        ///                     </term>
        ///                 </item>
        ///                 <item>
        ///                     <term>
        ///                         HKCU\SOFTWARE\Microsoft\VisualStudio\&lt;major version&gt;.&lt;minor version&gt;\AssemblyFolders
        ///                     </term>
        ///                 </item>
        ///             </list>
        ///             <para>
        ///                 Future versions of Visual Studio .NET will also check 
        ///                 in:
        ///             </para>
        ///             <list type="bullet">
        ///                 <item>
        ///                     <term>
        ///                         HKLM\SOFTWARE\Microsoft\.NETFramework\AssemblyFoldersEx
        ///                     </term>
        ///                 </item>
        ///                 <item>
        ///                     <term>
        ///                         HKCU\SOFTWARE\Microsoft\.NETFramework\AssemblyFoldersEx
        ///                     </term>
        ///                 </item>
        ///             </list>
        ///         </term>
        ///     </item>
        ///     <item>
        ///         <term>
        ///             The HintPath.
        ///         </term>
        ///     </item>
        /// </list>
        /// </remarks>
        private string ResolveAssemblyReference() {
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

            // check if we're dealing with a Visual C++ assembly reference
            if (referenceElement.Name == "AssemblyReference") {
                assemblyFileName = referenceElement.GetAttribute("RelativePath");

                if (assemblyFileName == null) {
                    throw new BuildException("For Visual C++ projects only assembly"
                        + " references using relative paths are supported.", 
                        Location.UnknownLocation);
                } else {
                    // expand macro's in RelativePath
                    assemblyFileName = _rxMacro.Replace(assemblyFileName, 
                        new MatchEvaluator(EvaluateMacro));

                    // TODO: support locating assemblies in VCConfiguration.ReferencesPath,
                    // but for now just remove it from reference filename
                    assemblyFileName = assemblyFileName.Replace("{ReferencesPath}\\", "");
                }
            } else {
                assemblyFileName = Name + ".dll";
            }

            // 1. The project directory
            // NOT SURE IF THIS IS CORRECT

            // 2. The ReferencePath
            // NOT SURE WE SHOULD DO THIS ONE

            // 3. The .NET Framework directory
            string resolvedAssemblyFile = ResolveFromFramework(assemblyFileName);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // 4. AssemblyFolders
            resolvedAssemblyFile = ResolveFromAssemblyFolders(referenceElement, 
                assemblyFileName);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // 5. The HintPath / Relative Path
            if (referenceElement.Name != "AssemblyReference") {
                // for now we do not support relative paths assembly references
                // in Visual C++ projects

                // ResolveFromRelativePath will return a path regardless of 
                // whether the file actually exists
                //
                // the file might actually be created as result of building
                // a project
                resolvedAssemblyFile = ResolveFromRelativePath(referenceElement, 
                    referenceElement.GetAttribute("HintPath"));
                if (resolvedAssemblyFile != null) {
                    return resolvedAssemblyFile;
                }


                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Reference to assembly '{0}' could not be resolved.",
                    referenceElement.Attributes["AssemblyName"].Value), 
                    Location.UnknownLocation);
            } else {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Reference to assembly '{0}' could not be resolved.",
                    referenceElement.Attributes["RelativePath"].Value), 
                    Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Searches for the given file in all paths in <paramref name="folderList" />.
        /// </summary>
        /// <param name="folderList">The folders to search.</param>
        /// <param name="fileName">The file to search for.</param>
        /// <returns>
        /// The path of the assembly if <paramref name="fileName" /> was found
        /// in <paramref name="folderList" />; otherwise, <see langword="null" />.
        /// </returns>
        private string ResolveFromFolderList(StringCollection folderList, string fileName) {
            foreach (string path in folderList) {
                string assemblyFile = Path.Combine(path, fileName);
                if (File.Exists(assemblyFile)) {
                    return assemblyFile;
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
        private string ResolveFromFramework(string fileName) {
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
        private string ResolveFromRelativePath(XmlElement referenceElement, string relativePath) {
            if (!StringUtils.IsNullOrEmpty(relativePath)) {
                return Path.GetFullPath(Path.Combine(Parent.ProjectDirectory.FullName, 
                    relativePath));
            }
            return null;
        }

        private string ResolveFromAssemblyFolders(XmlElement referenceElement, string fileName) {
            string resolvedAssemblyFile = null;

            if (referenceElement.Attributes["AssemblyFolderKey"] != null) {
                string assemblyFolderKey = referenceElement.Attributes["AssemblyFolderKey"].Value;

                try {
                    RegistryKey registryHive = null;

                    switch (assemblyFolderKey.Substring(0,4)) {
                        case "hklm":
                            registryHive = Registry.LocalMachine;
                            break;
                        case "hkcu":
                            registryHive = Registry.CurrentUser;
                            break;
                    }

                    if (registryHive != null) {
                        foreach (string assemblyFolderRootKey in SolutionTask.AssemblyFolderRootKeys) {
                            RegistryKey assemblyFolderRegistryRoot = registryHive.OpenSubKey(assemblyFolderRootKey);
                            if (assemblyFolderRegistryRoot != null) {
                                RegistryKey assemblyFolderRegistryKey = assemblyFolderRegistryRoot.OpenSubKey(assemblyFolderKey.Substring(5));
                                if (assemblyFolderRegistryKey != null) {
                                    string assemblyFolder = assemblyFolderRegistryKey.GetValue(string.Empty) as string;
                                    if (assemblyFolder != null) {
                                        resolvedAssemblyFile = Path.Combine(
                                            assemblyFolder, fileName);
                                        if (File.Exists(resolvedAssemblyFile)) {
                                            return resolvedAssemblyFile;
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log(Level.Verbose, "Error resolve reference to '{0}' using"
                        + " AssemblyFolderKey '{1}'.", fileName,
                        assemblyFolderKey);
                    Log(Level.Debug, ex.ToString());
                }
            }

            resolvedAssemblyFile = ResolveFromFolderList(SolutionTask.
                AssemblyFolders.DirectoryNames, fileName);
            if (resolvedAssemblyFile == null) {
                resolvedAssemblyFile = ResolveFromFolderList(SolutionTask.
                    DefaultAssemblyFolders.DirectoryNames, fileName);
            }
            return resolvedAssemblyFile;
        }

        /// <summary>
        /// Is called each time a regular expression match is found during a 
        /// <see cref="Regex.Replace(string, MatchEvaluator)" /> operation.
        /// </summary>
        /// <param name="m">The <see cref="Match" /> resulting from a single regular expression match during a <see cref="Regex.Replace(string, MatchEvaluator)" />.</param>
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
                "Macro \"{0}\" is not supported in assembly references.", macro), 
                Location.UnknownLocation);
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _assemblyFile;
        private readonly string _name = string.Empty;
        private readonly Regex _rxMacro = new Regex(@"\$\((\w+)\)");

        #endregion Private Instance Fields
    }
}
