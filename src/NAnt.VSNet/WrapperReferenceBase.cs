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
using System.Runtime.InteropServices;
#if NET_2_0
using System.Runtime.InteropServices.ComTypes;
#endif
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Util;
using NAnt.Core.Types;

using NAnt.Win32.Tasks;

namespace NAnt.VSNet {
    public abstract class WrapperReferenceBase : FileReferenceBase {
        #region Protected Instance Constructors

        protected WrapperReferenceBase(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache) : base(xmlDefinition, referencesResolver, parent, gacCache) {
        }

        #endregion Protected Instance Constructors

        #region Override implementation of ReferenceBase

        /// <summary>
        /// Gets a value indicating whether the output file(s) of this reference 
        /// should be copied locally.
        /// </summary>
        /// <value>
        /// <see langword="false" /> if the reference wraps a Primary Interop 
        /// Assembly; otherwise, <see langword="true" />.
        /// </value>
        public override bool CopyLocal {
            get { return (WrapperTool != "primary"); }
        }

        /// <summary>
        /// Gets a value indicating whether this reference represents a system 
        /// assembly.
        /// </summary>
        /// <value>
        /// <see langword="false" /> as none of the system assemblies are wrappers
        /// or Primary Interop Assemblies anyway.
        /// </value>
        protected override bool IsSystem {
            get { return false; }
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
            return WrapperAssembly;
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
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = Parent.BuildConfigurations[solutionConfiguration];

            base.GetAssemblyOutputFiles(CreateWrapper(config), outputFiles);
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
            // obtain project configuration (corresponding with solution configuration)
            ConfigurationBase config = Parent.BuildConfigurations[solutionConfiguration];

            // ensure wrapper is actually created
            string assemblyFile = CreateWrapper(config);
            if (!File.Exists(assemblyFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Couldn't find assembly \"{0}\", referenced by project \"{1}\".", 
                    assemblyFile, Parent.Name), Location.UnknownLocation);
            }

            // add referenced assembly to list of reference assemblies
            StringCollection assemblyReferences = new StringCollection();
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
            return GetFileTimestamp(WrapperAssembly);
        }

        #endregion Override implementation of ReferenceBase

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the tool that should be used to create the 
        /// <see cref="WrapperAssembly" />.
        /// </summary>
        /// <value>
        /// The name of the tool that should be used to create the 
        /// <see cref="WrapperAssembly" />.
        /// </value>
        public abstract string WrapperTool {
            get;
        }

        /// <summary>
        /// Gets the path of the wrapper assembly.
        /// </summary>
        /// <value>
        /// The path of the wrapper assembly.
        /// </value>
        /// <remarks>
        /// The wrapper assembly is stored in the object directory of the 
        /// project.
        /// </remarks>
        public abstract string WrapperAssembly {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the wrapper assembly has already been
        /// created.
        /// </summary>
        public bool IsCreated {
            get { return _isCreated; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the path of the Primary Interop Assembly.
        /// </summary>
        /// <value>
        /// The path of the Primary Interop Assembly, or <see langword="null" />
        /// if not available.
        /// </value>
        protected abstract string PrimaryInteropAssembly {
            get;
        }

        /// <summary>
        /// Gets the hex version of the type library as defined in the definition
        /// of the reference.
        /// </summary>
        /// <value>
        /// The hex version of the type library.
        /// </value>
        protected abstract string TypeLibVersion {
            get;
        }

        /// <summary>
        /// Gets the GUID of the type library as defined in the definition
        /// of the reference.
        /// </summary>
        /// <value>
        /// The GUID of the type library.
        /// </value>
        protected abstract string TypeLibGuid {
            get;
        }

        /// <summary>
        /// Gets the locale of the type library in hex notation.
        /// </summary>
        /// <value>
        /// The locale of the type library.
        /// </value>
        protected abstract string TypeLibLocale {
            get;
        }

        /// <summary>
        /// Gets the name of the type library.
        /// </summary>
        /// <value>
        /// The name of the type library.
        /// </value>
        protected virtual string TypeLibraryName {
            get {
                return GetTypeLibraryName(GetTypeLibrary());
            }
        }

        #endregion Protected Instance Properties

        #region Protected Instance Methods

        protected abstract void ImportTypeLibrary();
        protected abstract void ImportActiveXLibrary();

        protected string ResolveWrapperAssembly() {
            string wrapperAssembly = null;

            switch (WrapperTool) {
                case "primary":
                    wrapperAssembly = PrimaryInteropAssembly;
                    if (wrapperAssembly == null) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Couldn't find Primary Interop Assembly \"{0}\","
                            + " referenced by project \"{1}\".", Name, Parent.Name), 
                            Location.UnknownLocation);
                    }

                    return wrapperAssembly;
                case "aximp":
                    wrapperAssembly = "AxInterop." + TypeLibraryName + ".dll";
                    break;
                default:
                    wrapperAssembly = "Interop." + TypeLibraryName + ".dll";
                    break;
            }

            // resolve to full path
            return FileUtils.CombinePaths(Parent.ObjectDir.FullName,
                wrapperAssembly);
        }

        protected string GetPrimaryInteropAssembly() {
            string typeLibVersionKey = string.Format(CultureInfo.InvariantCulture, 
                @"TYPELIB\{0}\{1}", TypeLibGuid, TypeLibVersion);

            string assemblyFile = null;

            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(typeLibVersionKey)) {
                if (registryKey != null && registryKey.GetValue("PrimaryInteropAssemblyName") != null) {
                    string primaryInteropAssemblyName = (string) registryKey.GetValue("PrimaryInteropAssemblyName");

                    try {
                        // get filename of primary interop assembly
                        assemblyFile = ReferencesResolver.GetAssemblyFileName(
                            primaryInteropAssemblyName);
                    } catch (Exception ex) {
                        // only have build fail if we're actually dealing with a
                        // reference to a primary interop assembly
                        //
                        // certain tools (such as Office) register the name of
                        // the primary interop assembly of the typelib, but the 
                        // actual primary interop assembly is not always installed
                        if (WrapperTool == "primary") {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Primary Interop Assembly \"{0}\", referenced by project"
                                + " \"{1}\", could not be loaded.", primaryInteropAssemblyName,
                                Parent.Name), Location.UnknownLocation, ex);
                        }
                    }
                }
            }

            return assemblyFile;
        }

        protected string GetTypeLibrary() {
            string typeLibKey = string.Format(CultureInfo.InvariantCulture, 
                @"TYPELIB\{0}\{1}\{2}\win32", TypeLibGuid, TypeLibVersion,
                TypeLibLocale);

            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(typeLibKey)) {
                // TODO: if there's no direct match, then use a type library 
                // with the same major version, and the highest minor version

                // TODO: check if the library identifier matches the one of the
                // reference

                if (registryKey == null) {
                    throw CreateTypeLibraryNotRegisteredException();
                }

                string typeLibValue = (string) registryKey.GetValue(null);
                if (String.IsNullOrEmpty(typeLibValue)) {
                    throw CreateInvalidTypeLibraryRegistrationException();
                }

                // extract path to type library from reg value
                string typeLibPath = TlbImpTask.ExtractTypeLibPath(typeLibValue);
                // check if the typelib actually exists
                if (!File.Exists(typeLibPath)) {
                    throw CreateTypeLibraryPathDoesNotExistException(typeLibPath);
                }
                return typeLibValue;
            }
        }

        protected string GetTypeLibraryName(string typeLibraryPath) {
            Object typeLib;
            try {
                LoadTypeLibEx(typeLibraryPath, 0, out typeLib);
            } catch (COMException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Type library \"{0}\" could not be loaded.", typeLibraryPath),
                    Location.UnknownLocation, ex);
            }

            if (typeLib == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Type library \"{0}\" could not be loaded.", typeLibraryPath),
                    Location.UnknownLocation);
            }

#if NET_2_0
            return Marshal.GetTypeLibName((ITypeLib) typeLib);
#else
            return Marshal.GetTypeLibName((UCOMITypeLib) typeLib);
#endif
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private string CreateWrapper(ConfigurationBase config) {
            // if wrapper assembly was created during the current build, then
            // there's no need to create it again
            if (IsCreated) {
                return WrapperAssembly;
            }

            // synchronize build and output directory
            Sync(config);

            switch (WrapperTool) {
                case "primary":
                    // nothing to do for Primary Interop Assembly
                    break;
                case "tlbimp":
                    if (PrimaryInteropAssembly != null) {
                        // if tlbimp is defined as import tool, but a primary
                        // interop assembly is available, then output a 
                        // warning
                        Log(Level.Warning, "The component \"{0}\", referenced by"
                            + " project \"{1}\" has an updated custom wrapper"
                            + " available.", Name, Parent.Name);
                    }

                    ImportTypeLibrary();
                    break;
                case "aximp":
                    ImportActiveXLibrary();
                    break;
                default:
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Wrapper tool \"{0}\" for reference \"{1}\" in project"
                        + " \"{2}\" is not supported.", WrapperTool, Name, 
                        Parent.Name), Location.UnknownLocation);
            }

            // mark wrapper as completed
            _isCreated = true;

            return WrapperAssembly;
        }

        /// <summary>
        /// Removes wrapper assembly from build directory, if wrapper assembly 
        /// no longer exists in output directory or is not in sync with build 
        /// directory, to force rebuild.
        /// </summary>
        /// <param name="config">The project configuration.</param>
        private void Sync(ConfigurationBase config) {
            if (!CopyLocal || !File.Exists(WrapperAssembly)) {
                // nothing to synchronize
                return;
            }

            // determine path where wrapper assembly should be deployed to
            string outputFile = FileUtils.CombinePaths(config.OutputDir.FullName,
                Path.GetFileName(WrapperAssembly));

            // determine last modification date/time of built wrapper assembly
            DateTime wrapperModTime = File.GetLastWriteTime(WrapperAssembly);

            // rebuild wrapper assembly if output assembly is more recent, 
            // or have been removed (by the user) to force a rebuild
            if (FileSet.FindMoreRecentLastWriteTime(outputFile, wrapperModTime) != null) {
                // remove wrapper assembly to ensure a rebuild is performed
                DeleteTask deleteTask = new DeleteTask();
                deleteTask.Project = SolutionTask.Project;
                deleteTask.Parent = SolutionTask;
                deleteTask.InitializeTaskConfiguration();
                deleteTask.File = new FileInfo(WrapperAssembly);
                deleteTask.Threshold = Level.None; // no output in build log
                deleteTask.Execute();
            }
        }

        private BuildException CreateTypeLibraryNotRegisteredException() {
            string msg = null;

            if (String.IsNullOrEmpty(Name)) {
                msg = string.Format(CultureInfo.InvariantCulture, "Couldn't" +
                    " find type library \"{0}\" with version {1}, referenced" +
                    " by project \"{2}\".", TypeLibGuid, TypeLibVersion, 
                    Parent.Name); 
            } else {
                msg = string.Format(CultureInfo.InvariantCulture, "Couldn't" +
                    " find type library \"{0}\" ({1} with version {2}), referenced" +
                    " by project \"{3}\".", Name, TypeLibGuid, TypeLibVersion, 
                    Parent.Name); 
            }

            return new BuildException(msg, Location.UnknownLocation);
        }

        private BuildException CreateInvalidTypeLibraryRegistrationException() {
            string msg = null;

            if (String.IsNullOrEmpty(Name)) {
                msg = string.Format(CultureInfo.InvariantCulture, "Couldn't" +
                    " find path of type library \"{0}\" with version {1}, referenced" +
                    " by project \"{2}\". Ensure the type library is registered" +
                    "correctly.", TypeLibGuid, TypeLibVersion, Parent.Name); 
            } else {
                msg = string.Format(CultureInfo.InvariantCulture, "Couldn't" +
                    " find path of type library \"{0}\" ({1} with version {2})," +
                    " referenced by project \"{3}\". Ensure the type library is" +
                    " registered correctly.", Name, TypeLibGuid, TypeLibVersion, 
                    Parent.Name); 
            }
            return new BuildException(msg, Location.UnknownLocation);
        }

        private BuildException CreateTypeLibraryPathDoesNotExistException(string typeLibraryPath) {
            string msg = null;

            if (String.IsNullOrEmpty(Name)) {
                msg = string.Format(CultureInfo.InvariantCulture, "Type library" +
                    " \"{0}\" with version {1}, referenced by project \"{2}\"," +
                    " no longer exists at registered path \"{3}\".", TypeLibGuid, 
                    TypeLibVersion, Parent.Name, typeLibraryPath); 
            } else {
                msg = string.Format(CultureInfo.InvariantCulture, "Type library" +
                    " \"{0}\" ({1} with version {2}), referenced by project \"{3}\"," +
                    " no longer exists at registered path \"{4}\".", Name, 
                    TypeLibGuid, TypeLibVersion, Parent.Name, typeLibraryPath); 
            }
            return new BuildException(msg, Location.UnknownLocation);
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        [DllImport( "oleaut32.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
        private static extern void LoadTypeLibEx(string strTypeLibName, int regKind, 
            [MarshalAs(UnmanagedType.Interface)] out Object typeLib);

        #endregion Private Static Methods

        #region Private Instance Fields

        private bool _isCreated;

        #endregion Private Instance Fields
    }
}
