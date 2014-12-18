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

using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Util;

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
        /// Resolves an assembly reference.
        /// </summary>
        /// <returns>
        /// The full path to the resolved assembly, or <see langword="null" />
        /// if the assembly reference could not be resolved.
        /// </returns>
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

        #region Private Instance Properties

        /// <summary>
        /// Gets the Visual Studio .NET AssemblyFolders registry key matching
        /// the current target framework.
        /// </summary>
        /// <value>
        /// The Visual Studio .NET AssemblyFolders registry key matching the 
        /// current target framework.
        /// </value>
        /// <exception cref="BuildException">The current target framework is not supported.</exception>
        /// <remarks>
        /// We use the target framework instead of the product version of the 
        /// containing project file to determine what registry key to scan, as
        /// we don't want to use assemblies meant for uplevel framework versions.
        /// </remarks>
        private string AssemblyFoldersKey {
            get {
                string visualStudioVersion = Parent.SolutionTask.Project.
                    TargetFramework.VisualStudioVersion.ToString();
                return string.Format(CultureInfo.InvariantCulture, 
                    @"SOFTWARE\Microsoft\VisualStudio\{0}\AssemblyFolders",
                    visualStudioVersion);
            }
        }

        #endregion Private Instance Properties

        #region Private Instance Methods

        private string GetComponentAssemblyFolder(XmlElement referenceElement) {
            string componentAssemblyFolder = null;

            if (referenceElement.Attributes["AssemblyFolderKey"] != null) {
                string assemblyFolderKey = referenceElement.Attributes["AssemblyFolderKey"].Value;

                RegistryKey registryHive = null;
                
                string[] assemblyFolderKeyParts = assemblyFolderKey.Split('\\');
                if (assemblyFolderKeyParts.Length < 2 || assemblyFolderKeyParts.Length > 3) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Invalid AssemblyFolderKey \"{0}\" for assembly"
                        + " reference \"{1}\", referenced by project"
                        + " \"{2}\".", assemblyFolderKey, Name, Parent.Name),
                        Location.UnknownLocation);
                }
                
                switch (assemblyFolderKeyParts[0]) {
                    case "hklm":
                        registryHive = Registry.LocalMachine;
                        break;
                    case "hkcu":
                        registryHive = Registry.CurrentUser;
                        break;
                    default:
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Invalid AssemblyFolderKey \"{0}\" for assembly"
                            + " reference \"{1}\", referenced by project"
                            + " \"{2}\".", assemblyFolderKey, Name, Parent.Name),
                            Location.UnknownLocation);
                }

                RegistryKey repositoryKey = null;

                // if AssemblyFolderKey has three parts, then the second
                // parts specifies the registry key to search
                if (assemblyFolderKeyParts.Length == 3) {
                    switch (assemblyFolderKeyParts[1]) {
                        case "dn":
                            repositoryKey = registryHive.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders");
                            break;
                        default:
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Invalid AssemblyFolderKey \"{0}\" for assembly"
                                + " reference \"{1}\", referenced by project"
                                + " \"{2}\".", assemblyFolderKey, Name, Parent.Name),
                                Location.UnknownLocation);
                    }
                } else {
                    repositoryKey = registryHive.OpenSubKey(AssemblyFoldersKey);
                }

                if (repositoryKey != null) {
                    RegistryKey componentKey = repositoryKey.OpenSubKey(
                        assemblyFolderKeyParts[assemblyFolderKeyParts.Length - 1]);
                    if (componentKey != null) {
                        string folder = componentKey.GetValue(string.Empty) as string;
                        if (folder != null) {
                            componentAssemblyFolder = folder;
                        } else {
                            Log(Level.Debug, "Default value for AssemblyFolder"
                                + " \"{0}\" does not exist or is not a string"
                                + " value.", assemblyFolderKey);
                        }
                    } else {
                        Log(Level.Debug, "Component key for AssemblyFolder \"{0}\""
                            + " does not exist.", assemblyFolderKey);
                    }
                } else {
                    Log(Level.Debug, "Repository for AssemblyFolder \"{0}\" does"
                        + " not exist.", assemblyFolderKey);
                }
            }

            return componentAssemblyFolder;
        }

        protected override string ResolveFromAssemblyFolders(XmlElement referenceElement, string fileName) {
            string resolvedAssemblyFile = null;

            string componentAssemblyFolder = GetComponentAssemblyFolder(
                referenceElement);
            if (componentAssemblyFolder != null) {
                StringCollection folderList = new StringCollection();
                folderList.Add(componentAssemblyFolder);
                resolvedAssemblyFile = ResolveFromFolderList(folderList, 
                    fileName);
            }

            if (resolvedAssemblyFile == null) {
                resolvedAssemblyFile = base.ResolveFromAssemblyFolders(
                    referenceElement, fileName);
            }

            return resolvedAssemblyFile;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly string _assemblyFile;
        private readonly bool _isPrivateSpecified;
        private readonly bool _isPrivate;
        private readonly string _name = string.Empty;

        #endregion Private Instance Fields
    }
}
