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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class ManagedAssemblyReference : AssemblyReferenceBase {
        public ManagedAssemblyReference(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(xmlDefinition, referencesResolver, parent, gacCache) {
            XmlAttribute privateAttribute = xmlDefinition.Attributes["Private"];
            if (privateAttribute != null) {
                _isPrivateSpecified = true;
                _isPrivate = bool.Parse(privateAttribute.Value);
            }

            // determine name of reference
            XmlAttribute assemblyNameAttribute = XmlDefinition.Attributes["AssemblyName"];
            if (assemblyNameAttribute != null) {
                _name = assemblyNameAttribute.Value;
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

            string assemblyFileName = Name + ".dll";

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

            // ResolveFromRelativePath will return a path regardless of 
            // whether the file actually exists
            //
            // the file might actually be created as result of building
            // a project
            resolvedAssemblyFile = ResolveFromRelativePath(
                referenceElement.GetAttribute("HintPath"));
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                "Assembly \"{0}\", referenced by project \"{1}\", could not be"
                + " resolved.", Name, Parent.Name), Location.UnknownLocation);
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

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _assemblyFile;
        private readonly bool _isPrivateSpecified;
        private readonly bool _isPrivate;
        private readonly string _name = string.Empty;

        #endregion Private Instance Fields
    }
}
