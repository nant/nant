// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Matthew Mastracci (mmastrac@canada.com)
// Sascha Andres (sa@programmers-world.com)
// Gert Driesen (drieseng@users.sourceforge.net)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Generates a <c>.licence</c> file from a <c>.licx</c> file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If no output file is specified, the default filename is the name of the
    /// target file with the extension <c>.licenses</c> appended.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Generate the file <c>component.exe.licenses</c> file from <c>component.licx</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <license input="component.licx" licensetarget="component.exe" />
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [TaskName("license")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class LicenseTask : ExternalProgramBase {
        #region Private Instance Fields

        private AssemblyFileSet _assemblies = new AssemblyFileSet();
        private FileInfo _inputFile;
        private FileInfo _outputFile;
        private string _target;
        private string _programFileName;
        private DirectoryInfo _workingDirectory;

        // framework configuration settings
        private bool _supportsAssemblyReferences;
        private bool _hasCommandLineCompiler = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Input file to process.
        /// </summary>
        [TaskAttribute("input", Required=true)]
        public FileInfo InputFile {
            get { return _inputFile; }
            set { _inputFile = value; }
        }

        /// <summary>
        /// Name of the license file to output.
        /// </summary>
        [TaskAttribute("output", Required=false)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Names of the references to scan for the licensed component.
        /// </summary>
        [BuildElement("assemblies")]
        public AssemblyFileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// Specifies the executable for which the .licenses file is generated.
        /// </summary>
        [TaskAttribute("licensetarget", Required=false)]
        [StringValidator(AllowEmpty=false)]
        [Obsolete("Use the \"target\" attribute instead.", false)]
        public string LicenseTarget {
            get { return Target; }
            set { Target = value; }
        }

        // TODO: we need to mark "target" as required after the release of
        // NAnt 0.85
        //
        // We can't do this right now, as it would be a major breaking change
        // (build authors still use "licensetarget" right now).

        /// <summary>
        /// Specifies the executable for which the .licenses file is generated.
        /// </summary>
        [TaskAttribute("target", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string Target {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Indicates whether assembly references are supported by the current
        /// target framework. The default is <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// Applies only to frameworks having a command line tool for compiling
        /// licenses files.
        /// </remarks>
        [FrameworkConfigurable("supportsassemblyreferences")]
        public bool SupportsAssemblyReferences {
            get { return _supportsAssemblyReferences; }
            set { _supportsAssemblyReferences = value; }
        }

        /// <summary>
        /// Indicates whether the current target framework has a command line
        /// tool for compiling licenses files. The default is 
        /// <see langword="true" />.
        /// </summary>
        [FrameworkConfigurable("hascommandlinecompiler")]
        public bool HasCommandLineCompiler {
            get { return _hasCommandLineCompiler; }
            set { _hasCommandLineCompiler = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes the <see cref="LicenseTask" /> class.
        /// </summary>
        protected override void Initialize() {
            // check if target was specified
            // TODO: remove this check after NAnt 0.85 as this should be handled 
            //       by setting Required to "true"
            if (Target == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA2013"), Name), Location);
            }

            // check if input file exists
            if (!InputFile.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2014"), InputFile.FullName), 
                    Location);
            }
        }

        #endregion Override implementation of Task

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
        /// The command-line arguments for the external program.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing these elements in build file.
        /// </remarks>
        public override ArgumentCollection Arguments {
            get { return base.Arguments; }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments { 
            get { return string.Empty; }
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

                // inherit verbose setting from license task
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

                // determine target directory
                string targetDir = Path.GetDirectoryName(Path.Combine(
                    BaseDirectory.FullName, Target));
                // ensure target directory exists
                if (!String.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }
            } else {
                foreach (string assembly in Assemblies.FileNames) {
                    Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture,
                        "/i:\"{0}\"", assembly)));
                }
            }

            // further delegate preparation to base class
            base.PrepareProcess(process);
        }

        /// <summary>
        /// Generates the license file.
        /// </summary>
        protected override void ExecuteTask() {
            FileInfo licensesFile = null;

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Assemblies.BaseDirectory == null) {
                Assemblies.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // get the output .licenses file
            if (OutputFile == null) {
                try {
                    licensesFile = new FileInfo(Project.GetFullPath(Target + ".licenses"));
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA2015"), Target), Location, ex);
                }
            } else {
                licensesFile = OutputFile;
            }

            // make sure the directory for the .licenses file exists
            if (!licensesFile.Directory.Exists) {
                licensesFile.Directory.Create();
            }

            // determine whether .licenses file need to be recompiled
            if (!NeedsCompiling(licensesFile)) {
                return;
            }

            Log(Level.Verbose, ResourceUtils.GetString("String_CompilingLicenseUsingTarget"),
                InputFile.FullName, licensesFile.FullName, Target);

            if (HasCommandLineCompiler) {
                // the command line compiler does not allow us to specify the 
                // full path to the output file, so we have it create the licenses
                // file in a temp directory, and copy it to its actual output
                // location

                // use a newly created temporary directory as working directory
                BaseDirectory = FileUtils.GetTempDirectory();

                try {
                    // set target assembly for generated licenses file
                    Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture,
                        "/target:\"{0}\"", Target)));
                    // set input filename
                    Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture,
                        "/complist:\"{0}\"", InputFile.FullName)));
                    // set output directory
                    Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture,
                        "/outdir:\"{0}\"", BaseDirectory.FullName)));
                    // suppress display of startup banner
                    Arguments.Add(new Argument("/nologo"));
                    // adjust verbosity of tool if necessary
                    if (Verbose) {
                        Arguments.Add(new Argument("/v"));
                    }
                    // use command line tool to compile licenses file
                    base.ExecuteTask();

                    // delete any existing output file
                    if (File.Exists(licensesFile.FullName)) {
                        File.Delete(licensesFile.FullName);
                    }

                    // copy licenses file to output file (with overwrite)
                    File.Copy(Path.Combine(BaseDirectory.FullName, Target + ".licenses"), 
                        licensesFile.FullName, true);
                } finally {
                    // delete temporary directory and all files in it
                    DeleteTask deleteTask = new DeleteTask();
                    deleteTask.Project = Project;
                    deleteTask.Parent = this;
                    deleteTask.InitializeTaskConfiguration();
                    deleteTask.Directory = BaseDirectory;
                    deleteTask.Threshold = Level.None; // no output in build log
                    deleteTask.Execute();
                }
            } else {
                // create new domain
#if (NET_4_0)
                AppDomain newDomain = AppDomain.CreateDomain("LicenseGatheringDomain");
                LicenseGatherer licenseGatherer = (LicenseGatherer)
                    newDomain.CreateInstanceAndUnwrap(typeof(LicenseGatherer).Assembly.FullName,
                    typeof(LicenseGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance,
                    null, new object[0], CultureInfo.InvariantCulture, new object[0]);
#else
                AppDomain newDomain = AppDomain.CreateDomain("LicenseGatheringDomain", 
                    AppDomain.CurrentDomain.Evidence);
                LicenseGatherer licenseGatherer = (LicenseGatherer)
                    newDomain.CreateInstanceAndUnwrap(typeof(LicenseGatherer).Assembly.FullName,
                    typeof(LicenseGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance,
                    null, new object[0], CultureInfo.InvariantCulture, new object[0],
                    AppDomain.CurrentDomain.Evidence);
#endif
                licenseGatherer.CreateLicenseFile(this, licensesFile.FullName);

                // unload newly created domain
                AppDomain.Unload(newDomain);
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Private Instance Methods

        /// <summary>
        /// Determines whether the <c>.licenses</c> file needs to be recompiled
        /// or is uptodate.
        /// </summary>
        /// <param name="licensesFile">The <c>.licenses</c> file.</param>
        /// <returns>
        /// <see langword="true" /> if the <c>.licenses</c> file needs compiling; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        private bool NeedsCompiling(FileInfo licensesFile) {
            if (!licensesFile.Exists) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileDoesNotExist"),
                    licensesFile.FullName);
                return true;
            }

            // check if assembly references were updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(Assemblies.FileNames, licensesFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                    fileName);
                return true;
            }

            // check if input file was updated
            if (InputFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(InputFile.FullName, licensesFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        #endregion Private Instance Methods

        /// <summary>
        /// Responsible for reading the license and writing them to a license 
        /// file.
        /// </summary>
        private class LicenseGatherer : MarshalByRefObject {
            #region Override implementation of MarshalByRefObject

            /// <summary>
            /// Obtains a lifetime service object to control the lifetime policy for 
            /// this instance.
            /// </summary>
            /// <returns>
            /// An object of type <see cref="ILease" /> used to control the lifetime 
            /// policy for this instance. This is the current lifetime service object 
            /// for this instance if one exists; otherwise, a new lifetime service 
            /// object initialized with a lease that will never time out.
            /// </returns>
            public override Object InitializeLifetimeService() {
                ILease lease = (ILease) base.InitializeLifetimeService();
                if (lease.CurrentState == LeaseState.Initial) {
                    lease.InitialLeaseTime = TimeSpan.Zero;
                }
                return lease;
            }

            #endregion Override implementation of MarshalByRefObject

            #region Public Instance Methods

            /// <summary>
            /// Creates the whole license file.
            /// </summary>
            /// <param name="licenseTask">The <see cref="LicenseTask" /> instance for which the license file should be created.</param>
            /// <param name="licensesFile">The .licenses file to create.</param>
            public void CreateLicenseFile(LicenseTask licenseTask, string licensesFile) {
                ArrayList assemblies = new ArrayList();

                // create assembly resolver
                AssemblyResolver assemblyResolver = new AssemblyResolver(licenseTask);

                // attach assembly resolver to the current domain
                assemblyResolver.Attach();

                licenseTask.Log(Level.Verbose, ResourceUtils.GetString("String_LoadingAssemblies"));

                try {
                    // first, load all the assemblies so that we can search for the 
                    // licensed component
                    foreach (string assemblyFileName in licenseTask.Assemblies.FileNames) {
                        Assembly assembly = Assembly.LoadFrom(assemblyFileName);
                        if (assembly != null) {
                            // output assembly filename to build log
                            licenseTask.Log(Level.Verbose, ResourceUtils.GetString("String_AssemblyLoaded"), 
                                assemblyFileName);
                            // add assembly to list of loaded assemblies
                            assemblies.Add(assembly);
                        }
                    }

                    DesigntimeLicenseContext dlc = new DesigntimeLicenseContext();
                    LicenseManager.CurrentContext = dlc;

                    // read the input file
                    using (StreamReader sr = new StreamReader(licenseTask.InputFile.FullName)) {
                        Hashtable licenseTypes = new Hashtable();

                        licenseTask.Log(Level.Verbose, ResourceUtils.GetString("String_CreatingLicenses"));

                        while (true) {
                            string line = sr.ReadLine();

                            if (line == null) {
                                break;
                            }

                            line = line.Trim();
                            // Skip comments, empty lines and already processed assemblies
                            if (line.StartsWith("#") || line.Length == 0 || licenseTypes.Contains(line)) {
                                continue;
                            }

                            licenseTask.Log(Level.Verbose, "{0}: ", line);

                            // Strip off the assembly name, if it exists
                            string typeName;

                            if (line.IndexOf(',') != -1) {
                                typeName = line.Split(',')[0];
                            } else {
                                typeName = line;
                            }

                            Type tp = null;

                            // try to locate the type in each assembly
                            foreach (Assembly assembly in assemblies) {
                                if (tp == null) {
                                    tp = assembly.GetType(typeName, false, true);
                                }

                                if (tp != null) {
                                    break;
                                }
                            }

                            if (tp == null) {
                                try {
                                    // final attempt, assuming line contains
                                    // assembly qualfied name
                                    tp = Type.GetType(line, false, false);
                                } catch {
                                    // ignore error, we'll report the load
                                    // failure later
                                }
                            }

                            if (tp == null) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,  
                                    ResourceUtils.GetString("NA2016"), typeName), licenseTask.Location);
                            } else {
                                // add license type to list of processed license types
                                licenseTypes[line] = tp;
                            }

                            // ensure that we've got a licensed component
                            if (tp.GetCustomAttributes(typeof(LicenseProviderAttribute), true).Length == 0) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,  
                                    ResourceUtils.GetString("NA2017"), tp.FullName), 
                                    licenseTask.Location);
                            }

                            try {
                                LicenseManager.CreateWithContext(tp, dlc);
                            } catch (Exception ex) {
                                if (IsSerializable(ex)) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        ResourceUtils.GetString("NA2018"), tp.FullName), 
                                        licenseTask.Location, ex);
                                }

                                // do not directly pass the exception as inner 
                                // exception to BuildException as the exception
                                // is not serializable, so construct a new 
                                // exception with message set to message of 
                                // original exception
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA2018"), tp.FullName), 
                                    licenseTask.Location, new Exception(ex.Message));
                            }
                        }
                    }

                    // overwrite the existing file, if it exists - is there a better way?
                    if (File.Exists(licensesFile)) {
                        File.SetAttributes(licensesFile, FileAttributes.Normal);
                        File.Delete(licensesFile);
                    }

                    // write out the license file, keyed to the appropriate output 
                    // target filename
                    // this .license file will only be valid for this exe/dll
                    using (FileStream fs = new FileStream(licensesFile, FileMode.Create)) {
                        DesigntimeLicenseContextSerializer.Serialize(fs, licenseTask.Target, dlc);
                        licenseTask.Log(Level.Verbose, ResourceUtils.GetString("String_CreatedNewLicense"),
                            licensesFile);
                    }

                    dlc = null;
                } catch (BuildException) {
                    // re-throw exception
                    throw;
                } catch (Exception ex) {
                    if (IsSerializable(ex)) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA2019"), licenseTask.InputFile.FullName), 
                            licenseTask.Location, ex);
                    } else {
                        // do not directly pass the exception as inner exception to 
                        // BuildException as the exception might not be serializable, 
                        // so construct a
                        // new exception with message set to message of
                        // original exception
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA2019"), licenseTask.InputFile.FullName), 
                            licenseTask.Location, new Exception(ex.Message));
                    }
                } finally {
                    // detach assembly resolver from the current domain
                    assemblyResolver.Detach();
                }
            }

            #endregion Public Instance Methods

            #region Private Instance Methods

            /// <summary>
            /// Determines whether the given object is serializable in binary
            /// format.
            /// </summary>
            /// <param name="value">The object to check.</param>
            /// <returns>
            /// <see langword="true" /> if <paramref name="value" /> is 
            /// serializable in binary format; otherwise, <see langword="false" />.
            /// </returns>
            private bool IsSerializable(object value) {
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();

                try {
                    formatter.Serialize(stream, value);
                    return true;
                } catch (SerializationException) {
                    return false;
                } finally {
                    stream.Close();
                }
            }

            #endregion Private Instance Methods
        }
    }
}
