// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Ian MacLean (ian@maclean.ms)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Registers an assembly, or set of assemblies for use from COM clients.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Refer to the <see href="ms-help://MS.VSCC/MS.MSDNVS/cptools/html/cpgrfassemblyregistrationtoolregasmexe.htm">Regasm</see> 
    ///   documentation for more information on the regasm tool.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Register types in a single assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regasm assembly="myAssembly.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Register types of an assembly and generate a type library containing
    ///   definitions of accessible types defined within the assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regasm assembly="myAssembly.dll" typelib="myAssembly.tlb" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Register types of set of assemblies at once, while specifying a set
    ///   of reference assemblies.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regasm codebase="true">
    ///     <assemblies>
    ///         <include name="OutlookAddin.dll" />
    ///         <include name="OfficeCoreAddin.dll" />
    ///     </assemblies>
    ///     <references>
    ///         <include name="CommonTypes.dll" />
    ///     </references>
    /// </regasm>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("regasm")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class RegAsmTask : ExternalProgramBase {
        #region Private Instance Fields

        private StringBuilder _arguments = new StringBuilder();
        private string _programFileName;
        private DirectoryInfo _workingDirectory;
        private FileInfo _assemblyFile;
        private FileInfo _regfile;
        private FileInfo _typelib;
        private bool _codebase;
        private bool _unregister;
        private bool _registered;
        private AssemblyFileSet _assemblies = new AssemblyFileSet();
        private AssemblyFileSet _references = new AssemblyFileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the file to register. This is provided as an alternate 
        /// to using the task's <see cref="Assemblies" />.
        /// </summary>
        [TaskAttribute("assembly")]
        public FileInfo AssemblyFile {
            get { return _assemblyFile; }
            set { _assemblyFile = value; }
        }

        /// <summary>
        /// Registry file to export to instead of entering the types directly 
        /// into the registry.
        /// </summary>
        [TaskAttribute("regfile")]
        public FileInfo RegistryFile {
            get { return _regfile; }
            set { _regfile = value; }
        }

        /// <summary>
        /// Set the code base registry setting.
        /// </summary>
        [TaskAttribute("codebase")]
        [BooleanValidator()]
        public bool CodeBase {
            get { return _codebase; }
            set { _codebase = value; }
        }

        /// <summary>
        /// Only refer to already registered type libraries.
        /// </summary>
        [TaskAttribute("registered")]
        [BooleanValidator()]
        public bool Registered {
            get { return _registered; }
            set { _registered = value; }
        }
        
        /// <summary>
        /// Export the assemblies to the specified type library and register it.
        /// </summary>
        [TaskAttribute("typelib")]
        public FileInfo TypeLib {
            get { return _typelib; }
            set { _typelib = value; }
        }
        
        /// <summary>
        /// Unregister the assembly. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unregister")]
        [BooleanValidator()]
        public bool Unregister {
            get { return _unregister; }
            set { _unregister = value; }
        }
               
        /// <summary>
        /// The set of assemblies to register, or unregister.
        /// </summary>
        [BuildElement("assemblies")]
        public AssemblyFileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// The set of assembly references.
        /// </summary>
        [BuildElement("references")]
        public AssemblyFileSet References {
            get { return _references; }
            set { _references = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the working directory for the application.
        /// </summary>
        /// <value>
        /// The working directory for the application.
        /// </value>
        public override DirectoryInfo BaseDirectory {
            get { 
                if (_workingDirectory == null) {
                    return base.BaseDirectory; 
                }
                return _workingDirectory;
            }
            set {
                _workingDirectory = value;
            }
        }

        /// <summary>
        /// Gets the command line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command line arguments for the external program.
        /// </value>
        public override string ProgramArguments { 
            get { return _arguments.ToString(); } 
        }

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>
        /// The filename of the external program.
        /// </value>
        /// <remarks>
        /// Override in derived classes to explicitly set the location of the 
        /// external tool.
        /// </remarks>
        public override string ProgramFileName { 
            get { 
                if (_programFileName == null) {
                    _programFileName = base.ProgramFileName;
                }
                return _programFileName;
            }
        }

        /// <summary>
        /// Updates the <see cref="ProcessStartInfo" /> of the specified 
        /// <see cref="Process"/>.
        /// </summary>
        /// <param name="process">The <see cref="Process" /> of which the <see cref="ProcessStartInfo" /> should be updated.</param>
        protected override void PrepareProcess(Process process) {
            // avoid copying the assembly references (and regasm) to a
            // temporary directory if not necessary
            if (References.FileNames.Count == 0) {
                // further delegate preparation to base class
                base.PrepareProcess(process);

                // no further processing required
                return;
            }

            // create instance of Copy task
            CopyTask ct = new CopyTask();

            // inherit project from current task
            ct.Project = Project;

            // inherit namespace manager from current task
            ct.NamespaceManager = NamespaceManager;

            // parent is current task
            ct.Parent = this;

            // inherit verbose setting from resgen task
            ct.Verbose = Verbose;

            // only output warning messages or higher, unless we're running
            // in verbose mode
            if (!ct.Verbose) {
                ct.Threshold = Level.Warning;
            }

            // make sure framework specific information is set
            ct.InitializeTaskConfiguration();

            // set parent of child elements
            ct.CopyFileSet.Parent = ct;

            // inherit project from solution task for child elements
            ct.CopyFileSet.Project = ct.Project;

            // inherit namespace manager from solution task
            ct.CopyFileSet.NamespaceManager = ct.NamespaceManager;

            // set base directory of fileset
            ct.CopyFileSet.BaseDirectory = Assemblies.BaseDirectory;

            // copy all files to base directory itself
            ct.Flatten = true;

            // copy referenced assemblies
            foreach (string file in References.FileNames) {
                ct.CopyFileSet.Includes.Add(file);
            }

            // copy assemblies to register
            foreach (string file in Assemblies.FileNames) {
                ct.CopyFileSet.Includes.Add(file);
            }

            if (AssemblyFile != null) {
                ct.CopyFileSet.Includes.Add(AssemblyFile.FullName);
            }

            // copy command line tool to working directory
            ct.CopyFileSet.Includes.Add(base.ProgramFileName);

            // set destination directory
            ct.ToDirectory = BaseDirectory;

            // increment indentation level
            ct.Project.Indent();
            try {
                // execute task
                ct.Execute();
            } finally {
                // restore indentation level
                ct.Project.Unindent();
            }

            // change program to execute the tool in working directory as
            // that will allow this tool to resolve assembly references
            // using assemblies stored in the same directory
            _programFileName = Path.Combine(BaseDirectory.FullName, 
                Path.GetFileName(base.ProgramFileName));

            // further delegate preparation to base class
            base.PrepareProcess(process);
        }


        #endregion Override implementation of ExternalProgramBase

        #region Override implementation of Task

        /// <summary>
        /// Registers or unregisters a single assembly, or a group of assemblies.
        /// </summary>
        protected override void ExecuteTask() {
            if (AssemblyFile == null && Assemblies.FileNames.Count == 0) {
                return;
            }

            // when reference assembly are specified, we copy all references 
            // and all assemblies to a temp directory and run regasm from there
            if (References.FileNames.Count != 0) {
                // use a newly created temporary directory as working directory
                BaseDirectory = FileUtils.GetTempDirectory();
            }

            if (Unregister) {
               _arguments.Append(" /unregister");
            }
            if (TypeLib != null) {
                _arguments.AppendFormat(CultureInfo.InvariantCulture,
                    " /tlb:\"{0}\"", TypeLib.FullName);
            }
            if (CodeBase) {
                _arguments.Append(" /codebase");
            }
            if (RegistryFile != null) {
                _arguments.AppendFormat(CultureInfo.InvariantCulture,
                    " /regfile:\"{0}\"", RegistryFile.FullName);
            }
            if (Registered) {
                _arguments.Append(" /registered");
            }
            if (Verbose) {
                _arguments.Append(" /verbose");
            } else {
                _arguments.Append(" /silent");
            }
            _arguments.Append(" /nologo");

            if (AssemblyFile != null) {
                Log(Level.Info, "{0} '{1}' for COM Interop", 
                    Unregister ? "Unregistering" : "Registering", 
                    AssemblyFile.FullName);
                _arguments.AppendFormat(" \"{0}\"", GetAssemblyPath(
                    AssemblyFile.FullName));
            } else {
                // display build log message
                Log(Level.Info, "{0} {1} files for COM interop", 
                    Unregister ? "UnRegistering" : "Registering", 
                    Assemblies.FileNames.Count);

                // add files to command line
                foreach (string path in Assemblies.FileNames) {
                    Log(Level.Verbose, "{0} '{1}' for COM Interop", 
                        Unregister ? "UnRegistering" : "Registering", 
                        path);

                    _arguments.AppendFormat(" \"{0}\"", GetAssemblyPath(path));
                }
            }

            try {
                // call base class to do the work
                base.ExecuteTask();
            } finally {
                // we only need to remove temporary directory if it was
                // actually created
                if (_workingDirectory != null) {
                    // delete temporary directory and all files in it
                    DeleteTask deleteTask = new DeleteTask();
                    deleteTask.Project = Project;
                    deleteTask.Parent = this;
                    deleteTask.InitializeTaskConfiguration();
                    deleteTask.Directory = _workingDirectory;
                    deleteTask.Threshold = Level.None; // no output in build log
                    deleteTask.Execute();
                }
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        private string GetAssemblyPath(string assembly) {
            if (_workingDirectory == null) {
                return assembly;
            }

            return Path.Combine(_workingDirectory.FullName, 
                Path.GetFileName(assembly));
        }

        #endregion Private Instance Methods
    }
}
