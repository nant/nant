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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XPath;

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
        private bool _useSourcePath;

        // framework configuration settings
        private bool _supportsAssemblyReferences;
        private bool _supportsExternalFileReferences;

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
        /// Use each source file's directory as the current directory for 
        /// resolving relative file paths. The default is <see langword="false" />.
        /// Only supported when targeting .NET 2.0 (or higher).
        /// </summary>
        [TaskAttribute("usesourcepath", Required=false)]
        public bool UseSourcePath {
            get { return _useSourcePath; }
            set { _useSourcePath = value; }
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

        /// <summary>
        /// Indicates whether external file references are supported by the 
        /// <c>resgen</c> tool for the current target framework. The default 
        /// is <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsexternalfilereferences")]
        public bool SupportsExternalFileReferences {
            get { return _supportsExternalFileReferences; }
            set { _supportsExternalFileReferences = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private bool RequiresAssemblyReferences {
            get {
                if (Resources.FileNames.Count > 0) {
                    foreach (string resourceFile in Resources.FileNames) {
                        if (ReferencesThirdPartyAssemblies(resourceFile)) {
                            return true;
                        }
                    }
                } else if (InputFile != null) {
                    return ReferencesThirdPartyAssemblies(InputFile.FullName);
                }
                return false;
            }
        }

        #endregion Private Instance Properties

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
                // use a newly created temporary directory as working directory
                BaseDirectory = FileUtils.GetTempDirectory();

                // avoid copying the assembly references (and resgen) to a
                // temporary directory if not necessary
                if (Assemblies.FileNames.Count == 0 || !RequiresAssemblyReferences) {
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
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Assemblies.BaseDirectory == null) {
                Assemblies.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (Resources.BaseDirectory == null) {
                Resources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            _arguments = "";
            if (Resources.FileNames.Count > 0) {
                if (OutputFile != null) {
                    throw new BuildException("'output' attribute is incompatible with fileset use.", Location);
                }
                foreach (string filename in Resources.FileNames) {
                    FileInfo outputFile = GetOutputFile(new FileInfo(filename), 
                        Resources.Prefix);

                    if (NeedsCompiling(new FileInfo(filename), outputFile)) {
                        if (StringUtils.IsNullOrEmpty(_arguments)) {
                            AppendArgument ("/compile");

                            if (UseSourcePath) {
                                if (SupportsExternalFileReferences) {
                                    AppendArgument(" /usesourcepath");
                                } else {
                                    Log(Level.Warning, "The resource compiler for {0}"
                                        + " does not support external file references.", 
                                        Project.TargetFramework.Description);
                                }
                            }
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

                    if (UseSourcePath) {
                        if (SupportsExternalFileReferences) {
                            AppendArgument("/usesourcepath");
                        } else {
                            Log(Level.Warning, "The resource compiler for {0}"
                                + " does not support external file references.", 
                                Project.TargetFramework.Description);
                        }
                    }

                    AppendArgument(string.Format(CultureInfo.InvariantCulture, 
                        " \"{0}\" \"{1}\"", InputFile.FullName, outputFile.FullName));
                }
            }

            if (!StringUtils.IsNullOrEmpty(_arguments)) {
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
                Log(Level.Verbose, "Output file '{0}' does not exist, recompiling.",
                    outputFile.FullName);
                return true;
            }

            // check if input file was updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(inputFile.FullName, outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if reference assemblies were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(Assemblies.FileNames, outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if we're dealing with a resx file
            if (string.Compare(inputFile.Extension, ".resx", true, CultureInfo.InvariantCulture) == 0) {
                StringCollection externalFileReferences = GetExternalFileReferences(inputFile);
                if (externalFileReferences != null) {
                    fileName = FileSet.FindMoreRecentLastWriteTime(externalFileReferences, outputFile.LastWriteTime);
                    if (fileName != null) {
                        Log(Level.Verbose, "'{0}' has been updated, recompiling.", fileName);
                        return true;
                    }
                }
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
            
            // if output is empty just change the extension 
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

        /// <summary>
        /// Determines whether the specified resource file references third
        /// party assemblies by checking whether a &lt;data&gt; element exists
        /// with a &quot;type&quot; attribute that does not start with 
        /// &quot;System.&quot;.
        /// </summary>
        /// <param name="resourceFile">The resource file to check.</param>
        /// <returns>
        /// <see langword="true" /> if the resource file references third party
        /// assemblies, or an error occurred; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// This check will only be accurate for 1.0 resource file, but the
        /// 2.0 resx files can only be compiled with a resgen tool that supported
        /// assembly references, so this method will not be used anyway.
        /// </remarks>
        private bool ReferencesThirdPartyAssemblies(string resourceFile) {
            try {
                if (!File.Exists(resourceFile)) {
                    return false;
                }

                // only resx files require assembly references
                if (string.Compare(Path.GetExtension(resourceFile), ".resx", true, CultureInfo.InvariantCulture) != 0) {
                    return false;
                }

                using (StreamReader sr = new StreamReader(resourceFile, true)) {
                    XPathDocument xpathDoc = new XPathDocument(new XmlTextReader(sr));

                    // determine the number of <data> elements that have a "type"
                    // attribute with a value that does not start with "System."
                    // and is not fully qualified
                    int count = xpathDoc.CreateNavigator().Select("/root/data[@type and not(starts-with(@type, 'System.') and contains(@type,'PublicKeyToken='))]").Count;

                    // if there are no <data> elements of a third party type, we 
                    // assume that the resource file does not reference types from
                    // third party assemblies
                    return count > 0;
                }
            } catch (Exception) {
                // have the resgen tool deal with issues (eg. invalid xml)
                return true;
            }
        }

        /// <summary>
        /// Returns a list of external file references for the specified file.
        /// </summary>
        /// <param name="resxFile">The resx file for which a list of external file references should be returned.</param>
        /// <returns>
        /// A list of external file references for the specified file, or
        /// <see langword="null" /> if <paramref name="resxFile" /> does not 
        /// exist or does not support external file references.
        /// </returns>
        private StringCollection GetExternalFileReferences(FileInfo resxFile) {
            if (!resxFile.Exists) {
                return null;
            }

            using (StreamReader sr = new StreamReader(resxFile.FullName, true)) {
                XPathDocument xpathDoc = new XPathDocument(new XmlTextReader(sr));

                XPathNavigator xpathNavigator = xpathDoc.CreateNavigator();

                // check resheader version
                xpathNavigator.Select("/root/resheader[@name = 'version']/value");
                XPathNodeIterator nodeIterator = xpathNavigator.Select("/root/resheader[@name = 'version']/value");
                if (nodeIterator.MoveNext()) {
                    string version = nodeIterator.Current.Value;
                    // 1.0 resx files do not support external file references
                    if (version == "1.0.0.0") {
                        return null;
                    }
                }

                StringCollection externalFiles = new StringCollection();
                string baseExternalFileDirectory = UseSourcePath ? resxFile.DirectoryName
                    : Project.BaseDirectory;

                // determine the number of <data> elements that have a "type"
                // attribute with a value that does not start with "System."
                XPathNodeIterator xfileIterator = xpathNavigator.Select("/root/data[@type = 'System.Resources.ResXFileRef, System.Windows.Forms']/value");
                while (xfileIterator.MoveNext()) {
                    string[] parts = xfileIterator.Current.Value.Split(';');
                    if (parts.Length <= 1) {
                        continue;
                    }
                    externalFiles.Add(Path.Combine(baseExternalFileDirectory, parts[0]));
                }

                return externalFiles;
            }
        }

        #endregion Private Instance Methods
    }
}
