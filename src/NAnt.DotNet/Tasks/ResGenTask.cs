//// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Joe Jones (joejo@microsoft.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Klemen Zagar (klemen@zagar.ws)
// Ian MacLean (ian_maclean@another.com)
// Gert Driesen (drieseng@ardatis.com)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Converts files from one resource format to another.
    /// </summary>
    /// <remarks>
    /// <note>
    /// If no <see cref="ToDirectory" /> is specified, the resource file will 
    /// be created next to the input file.
    /// </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Convert a resource file from the <c>.resx</c> to the <c>.resources</c> 
    ///   format.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <resgen input="translations.resx" output="translations.resources" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Convert a set of <c>.resx</c> files to the <c>.resources</c> format.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <resgen todir=".">
    ///     <resources>
    ///         <include name="*.resx" />
    ///     </resources>
    /// </resgen>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("resgen")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class ResGenTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _arguments;
        private AssemblyFileSet _assemblies = new AssemblyFileSet();
        private FileInfo _inputFile; 
        private FileInfo _outputFile;
        private string _programFileName;
        private ResourceFileSet _resources = new ResourceFileSet();
        private string _targetExt = "resources";
        private DirectoryInfo _toDir;
        private DirectoryInfo _workingDirectory;

        // framework configuration settings
        private bool _supportsAssemblyReferences;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Input file to process.
        /// </summary>
        /// <value>
        /// The full path to the input file.
        /// </value>
        [TaskAttribute("input", Required=false)]
        public FileInfo InputFile {
            get { return _inputFile; }
            set { _inputFile = value; }
        }

        /// <summary>
        /// The resource file to output.
        /// </summary>
        [TaskAttribute("output", Required=false)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The target type. The default is <c>resources</c>.
        /// </summary>
        [TaskAttribute("target", Required=false)]
        public string TargetExt {
            get { return _targetExt; }
            set { _targetExt = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The directory to which outputs will be stored.
        /// </summary>
        [TaskAttribute("todir", Required=false)]
        public DirectoryInfo ToDirectory {
            get { return _toDir; }
            set { _toDir = value; }
        }
       
        /// <summary>
        /// Takes a list of <c>.resx</c> or <c>.txt</c> files to convert to <c>.resources</c> files.      
        /// </summary>
        [BuildElement("resources")]
        public ResourceFileSet Resources {
            get { return _resources; }
            set { _resources = value; }
        }

        /// <summary>
        /// Reference metadata from the specified assembly files.
        /// </summary>
        [BuildElement("assemblies")]
        public AssemblyFileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// Indicates whether assembly references are supported by the 
        /// <c>resgen</c> tool for the current target framework. The default 
        /// is <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsassemblyreferences")]
        public bool SupportsAssemblyReferences {
            get { return _supportsAssemblyReferences; }
            set { _supportsAssemblyReferences = value; }
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
            get { return _arguments; } 
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
            if (!SupportsAssemblyReferences) {
                // create instance of Copy task
                CopyTask ct = new CopyTask();

                // inherit project from current task
                ct.Project = Project;

                // inherit namespace manager from current task
                ct.NamespaceManager = NamespaceManager;

                // parent is current task
                ct.Parent = this;

                // only output warning messages or higher
                ct.Threshold = Level.Warning;

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
                foreach (string file in Assemblies.FileNames) {
                    ct.CopyFileSet.Includes.Add(file);
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
            } else {
                foreach (string assembly in Assemblies.FileNames) {
                    AppendArgument(string.Format(CultureInfo.InvariantCulture,
                        " /r:\"{0}\"", assembly));
                }
            }

            // further delegate preparation to base class
            base.PrepareProcess(process);
        }

        /// <summary>
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            _arguments = "";
            if (Resources.FileNames.Count > 0 ) {
                if (OutputFile != null) {
                    throw new BuildException("'output' attribute is incompatible with fileset use.", Location);
                }
                foreach (string filename in Resources.FileNames ) {
                    FileInfo outputFile = GetOutputFile(new FileInfo(filename), 
                        Resources.Prefix);

                    if (NeedsCompiling(new FileInfo(filename), outputFile)) {
                        if (StringUtils.IsNullOrEmpty(_arguments)) {
                            AppendArgument ("/compile");
                        }

                        // ensure output directory exists
                        if (!outputFile.Directory.Exists) {
                            outputFile.Directory.Create();
                        }

                        AppendArgument(string.Format(CultureInfo.InvariantCulture, 
                            " \"{0},{1}\"", filename, outputFile.FullName));
                    }
                }
            } else {
                // Single file situation
                if (InputFile == null) {
                    throw new BuildException("Resource generator needs either an input attribute, or a non-empty fileset.", Location);
                }

                FileInfo outputFile = GetOutputFile(InputFile, null);

                if (NeedsCompiling(InputFile, outputFile)) {
                    // ensure output directory exists
                    if (!outputFile.Directory.Exists) {
                        outputFile.Directory.Create();
                    }

                    AppendArgument(string.Format(CultureInfo.InvariantCulture, 
                        " \"{0}\" \"{1}\"", InputFile.FullName, outputFile.FullName));
                }
            }

            if (!StringUtils.IsNullOrEmpty(_arguments)) {
                // determine working directory
                BaseDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), 
                    Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)));

                try {
                    // check if working directory exists
                    if (!BaseDirectory.Exists) {
                        // create working directory
                        BaseDirectory.Create();
                        // refresh filesystem info
                        BaseDirectory.Refresh();
                    }

                    // call base class to do the work
                    base.ExecuteTask();
                } finally {
                    if (BaseDirectory.Exists) {
                        DeleteTask deleteTask = new DeleteTask();
                        deleteTask.Project = Project;
                        deleteTask.Parent = this;
                        deleteTask.InitializeTaskConfiguration();
                        deleteTask.Directory = BaseDirectory;
                        deleteTask.Threshold = Level.None; // no output in build log
                        deleteTask.Execute();
                    }
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Public Instance Methods

        /// <summary>
        /// Cleans up generated files.
        /// </summary>
        public void RemoveOutputs() {
            foreach (string filename in Resources.FileNames) {
                FileInfo outputFile = GetOutputFile(new FileInfo(filename), 
                    Resources.Prefix);
                if (filename != outputFile.FullName) {
                    outputFile.Delete();
                }
            }
            if (InputFile != null) {
                FileInfo outputFile = GetOutputFile(InputFile, null);
                
                if (InputFile.FullName != outputFile.FullName) {
                    outputFile.Delete();
                }
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the specified input file needs to be compiled.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="outputFile">The output file.</param>
        /// <returns>
        /// <see langword="true" /> if the input file need to be compiled; 
        /// otherwise <see langword="false" />.
        /// </returns>
        protected virtual bool NeedsCompiling(FileInfo inputFile, FileInfo outputFile) {
            if (!outputFile.Exists) {
                Log(Level.Verbose, LogPrefix + "Output file '{0}' does not exist, recompiling.",
                    outputFile.FullName);
                return true;
            }

            // check if input file was updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(inputFile.FullName, outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if reference assemblies were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(Assemblies.FileNames, outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        /// <summary>
        /// Adds a command line argument to the command line for the external
        /// program that is used to convert the resource files.
        /// </summary>
        /// <param name="s">The argument that should be added to the command line.</param>
        protected void AppendArgument(string s) {
            _arguments += s;
        }
        
        #endregion Protected Instance Methods

        #region Private Instance Methods
       
        /// <summary>
        /// Determines the full path and extension for the output file.
        /// </summary>
        /// <param name="file">The output file for which the full path and extension should be determined.</param>
        /// <param name="prefix">prefix to prepend to the output .resources file</param>
        /// <returns>
        /// The full path (with extensions) for the specified file.
        /// </returns>
        private FileInfo GetOutputFile(FileInfo file, string prefix) {
            FileInfo outputFile;
            
            // If output is empty just change the extension 
            if (OutputFile == null) {
                if (ToDirectory == null) {
                    outputFile = file;
                } else {
                    outputFile = new FileInfo(Path.Combine(ToDirectory.FullName, file.Name));
                }
                if (!StringUtils.IsNullOrEmpty(prefix)) {
                    outputFile = new FileInfo(outputFile.FullName.Replace(
                        file.Name, prefix + "." + file.Name));
                }
                outputFile = new FileInfo(Path.ChangeExtension(outputFile.FullName, TargetExt));
            } else {
                outputFile = OutputFile;
            }
            return outputFile;
        }
        
        #endregion Private Instance Methods
    }
}
