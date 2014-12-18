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
using System.Globalization;
using System.IO;
using System.Xml;
using NAnt.Core;
using NAnt.Core.Util;
using NAnt.Win32.Tasks;

namespace NAnt.VSNet {
    public class ManagedWrapperReference : WrapperReferenceBase {
        #region Public Instance Constructors

        public ManagedWrapperReference(XmlElement xmlDefinition, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache, ProjectSettings projectSettings) : base(xmlDefinition, referencesResolver, parent, gacCache) {
            if (projectSettings == null) {
                throw new ArgumentNullException("projectSettings");
            }

            _projectSettings = projectSettings;

            // determine name of wrapper reference
            XmlAttribute wrapperNameAttribute = XmlDefinition.Attributes["Name"];
            if (wrapperNameAttribute != null) {
                _name = wrapperNameAttribute.Value;
            }

            // determine wrapper tool
            XmlAttribute toolAttribute = XmlDefinition.Attributes["WrapperTool"];
            if (toolAttribute == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Wrapper tool for reference \"{0}\" in project \"{1}\" could"
                    + " not be determined.", Name, Parent.Name), 
                    Location.UnknownLocation);
            }
            _wrapperTool = toolAttribute.Value;

            // determine if there's a primary interop assembly for the typelib
            _primaryInteropAssembly = GetPrimaryInteropAssembly();

            // determine filename of wrapper assembly
            _wrapperAssembly = ResolveWrapperAssembly();
        }

        #endregion Public Instance Constructors

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

        #region Override implementation of WrapperReferenceBase

        /// <summary>
        /// Gets the name of the tool that should be used to create the 
        /// <see cref="WrapperAssembly" />.
        /// </summary>
        /// <value>
        /// The name of the tool that should be used to create the 
        /// <see cref="WrapperAssembly" />.
        /// </value>
        public override string WrapperTool {
            get { return _wrapperTool; }
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
        public override string WrapperAssembly {
            get { return _wrapperAssembly; }
        }

        /// <summary>
        /// Gets the path of the Primary Interop Assembly.
        /// </summary>
        /// <value>
        /// The path of the Primary Interop Assembly, or <see langword="null" />
        /// if not available.
        /// </value>
        protected override string PrimaryInteropAssembly {
            get { return _primaryInteropAssembly; }
        }

        /// <summary>
        /// Gets the hex version of the type library as defined in the definition
        /// of the reference.
        /// </summary>
        /// <value>
        /// The hex version of the type library.
        /// </value>
        /// <exception cref="BuildException">
        ///   <para>
        ///   The definition of the reference does not contain a &quot;VersionMajor&quot; attribute.
        ///   </para>
        ///   <para>-or</para>
        ///   <para>
        ///   The definition of the reference does not contain a &quot;VersionMinor&quot; attribute.
        ///   </para>
        ///   </exception>
        protected override string TypeLibVersion {
            get {
                XmlAttribute majorAttribute = XmlDefinition.Attributes["VersionMajor"];
                if (majorAttribute == null) {
                    throw new BuildException("The definition of the reference"
                        + " does not contain a \"VersionMajor\" attribute.");
                }

                XmlAttribute minorAttribute = XmlDefinition.Attributes["VersionMinor"];
                if (minorAttribute == null) {
                    throw new BuildException("The definition of the reference"
                        + " does not contain a \"VersionMinor\" attribute.");
                }

                string majorVersion = (int.Parse(majorAttribute.Value, 
                    CultureInfo.InvariantCulture)).ToString("x", 
                    CultureInfo.InvariantCulture);
                string minorVersion = (int.Parse(minorAttribute.Value, 
                    CultureInfo.InvariantCulture)).ToString("x", 
                    CultureInfo.InvariantCulture);
                return majorVersion + "." + minorVersion;
            }
        }

        /// <summary>
        /// Gets the GUID of the type library as defined in the definition
        /// of the reference.
        /// </summary>
        /// <value>
        /// The GUID of the type library.
        /// </value>
        protected override string TypeLibGuid {
            get {
                XmlAttribute guidAttribute = XmlDefinition.Attributes["Guid"];
                if (guidAttribute == null) {
                    throw new BuildException("The definition of the reference"
                        + " does not contain a \"Guid\" attribute.");
                }
                return guidAttribute.Value;
            }
        }

        /// <summary>
        /// Gets the locale of the type library in hex notation.
        /// </summary>
        /// <value>
        /// The locale of the type library.
        /// </value>
        protected override string TypeLibLocale {
            get {
                XmlAttribute localeAttribute = XmlDefinition.Attributes["Lcid"];
                if (localeAttribute == null) {
                    throw new BuildException("The definition of the reference"
                        + " does not contain a \"Lcid\" attribute.");
                }
                return int.Parse(localeAttribute.Value).ToString("x");
            }
        }

        #endregion Override implementation of WrapperReferenceBase

        #region Private Instance Properties

        private ProjectSettings ProjectSettings {
            get { return _projectSettings; }
        }

        #endregion Private Instance Properties

        #region Private Instance Methods

        protected override void ImportTypeLibrary() {
            TlbImpTask tlbImp = new TlbImpTask();

            // parent is solution task
            tlbImp.Parent = SolutionTask;

            // inherit project from solution task
            tlbImp.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            tlbImp.NamespaceManager = SolutionTask.NamespaceManager;

            // inherit verbose setting from solution task
            tlbImp.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            tlbImp.InitializeTaskConfiguration();

            tlbImp.TypeLib = new FileInfo(GetTypeLibrary());
            tlbImp.OutputFile = new FileInfo(WrapperAssembly);
            tlbImp.Namespace = TypeLibraryName;

            // according to "COM Programming with Microsoft .NET" (page 59)
            // the /sysarray option should always be set in order to 
            // generate wrappers that match those generated by VS.NET
            tlbImp.SysArray = true;

            // transform [out, retval] parameters of methods on dispatch-only 
            // interfaces (dispinterfaces) into return values.
            //
            // this only affects .NET 1.1 or higher, as tlbimp does not expose
            // this on .NET 1.0.
            tlbImp.Transform = "dispret";

            // use other imported type libraries to resolve references
            //
            // there's one serious limitation in the current implementation:
            //
            // if type library A references type library B, then we should 
            // first import type library B and use a reference to that 
            // imported type library when we import type library A.
            // 
            // however, we have no way to find out in which order the type
            // libraries should be imported. So only if type library B is 
            // first listed in the project file, it will work fine.
            //
            // we should find a way to analyse a type library to determine
            // dependencies on other type libraries
            // 
            // according to JR (jrv72@users.sourceforge.net) a possible
            // solution could be to "use TypeLibConverter.ConvertTypeLibToAssembly. 
            // This has a callback of type ITypeLibImporterNotifySink, which I 
            // speculate allows one to recognize when one type library 
            // depends on another. I believe what you have to do is start 
            // with an arbitrary type library, and if that type library calls 
            // back on the ResolveRef() method, and if that type library is 
            // one you were planning to add later, you compile it 
            // immediately and pass the assembly back out of ResolveRef. I 
            // haven't tested this yet, but it's my best understanding of 
            // how it all works.
            foreach (ReferenceBase reference in Parent.References) {
                // we're only interested in imported type libraries
                WrapperReferenceBase wrapper = reference as WrapperReferenceBase;

                // avoid stack overflow causes by mutual dependencies
                if (wrapper == null || !wrapper.IsCreated || wrapper.WrapperTool != "tlbimp") {
                    continue;
                }

                tlbImp.References.Includes.Add(wrapper.WrapperAssembly);
            }

            if (ProjectSettings.AssemblyOriginatorKeyFile != null) {
                tlbImp.KeyFile = new FileInfo(FileUtils.CombinePaths(Parent.ProjectDirectory.FullName, 
                    ProjectSettings.AssemblyOriginatorKeyFile));
            }

            if (ProjectSettings.AssemblyKeyContainerName != null) {
                tlbImp.KeyContainer = ProjectSettings.AssemblyKeyContainerName;
            }

            // increment indentation level
            tlbImp.Project.Indent();
            try {
                // execute task
                tlbImp.Execute();
            } finally {
                // restore indentation level
                tlbImp.Project.Unindent();
            }
        }

        protected override void ImportActiveXLibrary() {
            AxImpTask axImp = new AxImpTask();

            // parent is solution task
            axImp.Parent = SolutionTask;

            // inherit project from solution task
            axImp.Project = SolutionTask.Project;

            // inherit namespace manager from solution task
            axImp.NamespaceManager = SolutionTask.NamespaceManager;

            // inherit verbose setting from solution task
            axImp.Verbose = SolutionTask.Verbose;

            // make sure framework specific information is set
            axImp.InitializeTaskConfiguration();

            axImp.OcxFile = new FileInfo(GetTypeLibrary());
            axImp.OutputFile = new FileInfo(WrapperAssembly);

            if (ProjectSettings.AssemblyOriginatorKeyFile != null) {
                axImp.KeyFile = new FileInfo(FileUtils.CombinePaths(Parent.ProjectDirectory.FullName, 
                    ProjectSettings.AssemblyOriginatorKeyFile));
            }

            if (ProjectSettings.AssemblyKeyContainerName != null) {
                axImp.KeyContainer = ProjectSettings.AssemblyKeyContainerName;
            }

            string rcw = PrimaryInteropAssembly;
            if (rcw == null) {
                // if no primary interop assembly is provided for ActiveX control,
                // then use the imported type library (if available)
                rcw = FileUtils.CombinePaths(Parent.ObjectDir.FullName, 
                    "Interop." + TypeLibraryName + ".dll");
            }
            if (File.Exists(rcw)) {
                axImp.RcwFile = new FileInfo(rcw);
            }

            // increment indentation level
            axImp.Project.Indent();
            try {
                // execute task
                axImp.Execute();
            } finally {
                // restore indentation level
                axImp.Project.Unindent();
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _name;
        private readonly string _wrapperTool;
        private readonly string _wrapperAssembly;
        private readonly ProjectSettings _projectSettings;
        private string _primaryInteropAssembly;
        
        #endregion Private Instance Fields
    }
}
