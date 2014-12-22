// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Joe Jones (joejo@microsoft.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Wraps <c>al.exe</c>, the assembly linker for the .NET Framework.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   All specified sources will be embedded using the <c>/embed</c> flag.
    ///   Other source types are not supported.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Create a library containing all icon files in the current directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <al output="MyIcons.dll" target="lib">
    ///     <sources>
    ///         <include name="*.ico" />
    ///     </sources>
    /// </al>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Create an executable assembly manifest from modules.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <al output="Client.exe" target="exe" main="Program.Main">
    ///     <modules>
    ///         <include name="Client.netmodule" />
    ///         <include name="Common.netmodule" />
    ///     </modules>
    /// </al>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("al")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class AssemblyLinkerTask : NAnt.Core.Tasks.ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private FileInfo _outputFile;
        private string _target;
        private string _algorithmID;
        private string _company;
        private string _configuration;
        private string _copyright;
        private string _culture;
        private DelaySign _delaySign;
        private string _description;
        private FileInfo _evidenceFile;
        private string _fileVersion;
        private string _flags;
        private string _keyContainer;
        private FileInfo _keyfile;
        private string _mainMethod;
        private ModuleSet _modules = new ModuleSet();
        private string _product;
        private string _productVersion;
        private FileSet _resources = new FileSet();
        private EmbeddedResourceCollection _embeddedResources = new EmbeddedResourceCollection();
        private FileInfo _templateFile;
        private string _title;
        private string _trademark;
        private string _version;
        private FileInfo _win32Icon;
        private FileInfo _win32Res;

        // framework configuration settings
        private bool _supportsTemplate = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies an algorithm (in hexadecimal) to hash all files in a 
        /// multifile assembly except the file that contains the assembly 
        /// manifest. The default algorithm is CALG_SHA1.
        /// </summary>
        [TaskAttribute("algid", Required=false)]
        [Int32Validator(Base=16)]
        public string AlgorithmID {
            get { return _algorithmID; }
            set { _algorithmID = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a string for the <b>Company</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Company</b> field in the assembly.
        /// </value>
        /// <remarks>
        /// If <see cref="Company" /> is an empty string (""), the Win32 
        /// <b>Company</b> resource appears as a single space.
        /// </remarks>
        [TaskAttribute("company", Required=false)]
        public string Company {
            get { return _company; }
            set { _company = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Configuration</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Configuration</b> field in the assembly.
        /// </value>
        /// <remarks>
        /// If <see cref="Configuration" /> is an empty string (""), the Win32
        /// <b>Configuration</b> resource appears as a single space.
        /// </remarks>
        [TaskAttribute("configuration", Required=false)]
        public string Configuration {
            get { return _configuration; }
            set { _configuration = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Copyright</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Copyright</b> field in the assembly.
        /// </value>
        /// <remarks>
        /// If <see cref="Copyright" /> is an empty string (""), the Win32
        /// <b>Copyright</b> resource appears as a single space.
        /// </remarks>
        [TaskAttribute("copyright", Required=false)]
        public string Copyright {
            get { return _copyright; }
            set { _copyright = value; }
        }

        /// <summary>
        /// The culture string associated with the output assembly.
        /// The string must be in RFC 1766 format, such as "en-US".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/c[ulture]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("culture", Required=false)]
        public string Culture {
            get { return _culture; }
            set { _culture = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the assembly should be partially signed. The default
        /// is <see langword="NAnt.DotNet.Types.DelaySign.NotSet" />.
        /// </summary>
        [TaskAttribute("delaysign", Required=false)]
        public DelaySign DelaySign {
            get { return _delaySign; }
            set { _delaySign = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Description</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Description</b> field in the assembly.
        /// </value>
        /// <remarks>
        /// If <see cref="Description" /> is an empty string (""), the Win32
        /// <b>Description</b> resource appears as a single space.
        /// </remarks>
        [TaskAttribute("description", Required=false)]
        public string Description {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// Security evidence file to embed.
        /// </summary>
        /// <value>
        /// The security evidence file to embed.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/e[vidence]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("evidence", Required=false)]
        public FileInfo EvidenceFile {
            get { return _evidenceFile; }
            set { _evidenceFile = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>File Version</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>File Version</b> field in the assembly.
        /// </value>
        [TaskAttribute("fileversion", Required=false)]
        public string FileVersion {
            get { return _fileVersion; }
            set { _fileVersion = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a value (in hexadecimal) for the <b>Flags</b> field in 
        /// the assembly.
        /// </summary>
        /// <value>
        /// A value (in hexadecimal) for the <b>Flags</b> field in the assembly.
        /// </value>
        [TaskAttribute("flags", Required=false)]
        [Int32Validator(Base=16)]
        public string Flags {
            get { return _flags; }
            set { _flags = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a container that holds a key pair.
        /// </summary>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer; }
            set { _keyContainer = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a file (filename) that contains a key pair or
        /// just a public key to sign an assembly.
        /// </summary>
        /// <value>
        /// The complete path to the key file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/keyf[ile]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("keyfile", Required=false)]
        public FileInfo KeyFile {
            get { return _keyfile; }
            set { _keyfile = value; }
        }

        /// <summary>
        /// Specifies the fully-qualified name (class.method) of the method to 
        /// use as an entry point when converting a module to an executable file.
        /// </summary>
        /// <value>
        /// The fully-qualified name (class.method) of the method to use as an 
        /// entry point when converting a module to an executable file.
        /// </value>
        [TaskAttribute("main")]
        public string MainMethod {
            get { return _mainMethod; }
            set { _mainMethod = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// One or more modules to be compiled into an assembly.
        /// </summary>
        [BuildElement("modules")]
        public ModuleSet ModuleSet {
            get { return _modules; }
            set { _modules = value; }
        }

        /// <summary>
        /// The name of the output file for the assembly manifest.
        /// </summary>
        /// <value>
        /// The complete output path for the assembly manifest.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/out</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The target type (one of <c>lib</c>, <c>exe</c>, or <c>winexe</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/t[arget]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string OutputTarget {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a string for the <b>Product</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Product</b> field in the assembly.
        /// </value>
        [TaskAttribute("product", Required=false)]
        public string Product {
            get { return _product; }
            set { _product = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Product Version</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Product Version</b> field in the assembly.
        /// </value>
        [TaskAttribute("productversion", Required=false)]
        public string ProductVersion {
            get { return _productVersion; }
            set { _productVersion = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The set of resources to embed.
        /// </summary>
        [BuildElement("sources")]
        public FileSet Resources {
            get { return _resources; }
            set { _resources = value; }
        }

        /// <summary>
        /// The set of compiled resources to embed.
        /// </summary>
        /// <remarks>
        /// Do not yet expose this to build authors.
        /// </remarks>
        public EmbeddedResourceCollection EmbeddedResources {
            get { return _embeddedResources; }
            set { _embeddedResources = value; }
        }

        /// <summary>
        /// Indicates whether the assembly linker for a given target framework
        /// supports the "template" option, which takes an assembly from which
        /// to get all options except the culture field.
        /// The default is <see langword="true" />.
        /// </summary>
        /// <remarks>
        /// TODO: remove this once Mono bug #74814 is fixed.
        /// </remarks>
        [FrameworkConfigurable("supportstemplate")]
        public bool SupportsTemplate {
            get { return _supportsTemplate; }
            set { _supportsTemplate = value; }
        }

        /// <summary>
        /// Specifies an assembly from which to get all options except the 
        /// culture field.
        /// </summary>
        /// <value>
        /// The complete path to the assembly template.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/template:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("template", Required=false)]
        public FileInfo TemplateFile {
            get { return _templateFile; }
            set { _templateFile = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Title</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Title</b> field in the assembly.
        /// </value>
        [TaskAttribute("title", Required=false)]
        public string Title {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        /// Specifies a string for the <b>Trademark</b> field in the assembly.
        /// </summary>
        /// <value>
        /// A string for the <b>Trademark</b> field in the assembly.
        /// </value>
        [TaskAttribute("trademark", Required=false)]
        public string Trademark {
            get { return _trademark; }
            set { _trademark = value; }
        }

        /// <summary>
        /// Specifies version information for the assembly. The format of the 
        /// version string is <c>major</c>.<c>minor</c>.<c>build</c>.<c>revision</c>.
        /// </summary>
        [TaskAttribute("version", Required=false)]
        public string Version {
            get { return _version; }
            set { _version = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Icon to associate with the assembly.
        /// </summary>
        [TaskAttribute("win32icon", Required=false)]
        public FileInfo Win32Icon {
            get { return _win32Icon; }
            set { _win32Icon = value; }
        }

        /// <summary>
        /// Inserts a Win32 resource (.res file) in the output file.
        /// </summary>
        [TaskAttribute("win32res", Required=false)]
        public FileInfo Win32Res {
            get { return _win32Res; }
            set { _win32Res = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program or 
        /// <see langword="null" /> if the task is not being executed.
        /// </value>
        public override string ProgramArguments {
            get { 
                if (_responseFileName != null) {
                    return "@" + "\"" + _responseFileName + "\""; 
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Generates an assembly manifest.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Resources.BaseDirectory == null) {
                Resources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (ModuleSet.Dir == null) {
                ModuleSet.Dir = new DirectoryInfo(Project.BaseDirectory);
            }

            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);

                try {
                    Log(Level.Info, ResourceUtils.GetString("String_CompilingFiles"),
                        Resources.FileNames.Count + EmbeddedResources.Count +
                        ModuleSet.Modules.Count, OutputFile.FullName);

                    // write modules to compile into assembly
                    foreach (Module module in ModuleSet.Modules) {
                        writer.WriteLine("\"{0}\"", module.ToString());
                    }

                    // write output target
                    writer.WriteLine("/target:\"{0}\"", OutputTarget);

                    // write output file
                    writer.WriteLine("/out:\"{0}\"", OutputFile.FullName);

                    // algorithm (in hexadecimal)
                    if (AlgorithmID != null) {
                        writer.WriteLine("/algid:\"{0}\"", AlgorithmID);
                    }

                    // company field
                    if (Company != null) {
                        writer.WriteLine("/company:\"{0}\"", Company);
                    }

                    // configuration field
                    if (Configuration != null) {
                        writer.WriteLine("/configuration:\"{0}\"", Configuration);
                    }

                    // copyright field
                    if (Copyright != null) {
                        writer.WriteLine("/copyright:\"{0}\"", Copyright);
                    }

                    // write culture associated with output assembly
                    if (Culture != null) {
                        writer.WriteLine("/culture:\"{0}\"", Culture);
                    }

                    // delay sign the assembly
                    switch (DelaySign) {
                        case DelaySign.NotSet:
                            break;
                        case DelaySign.Yes:
                            writer.WriteLine("/delaysign+");
                            break;
                        case DelaySign.No:
                            writer.WriteLine("/delaysign-");
                            break;
                        default:
                            throw new BuildException (string.Format (CultureInfo.InvariantCulture,
                                "Value {0} is not supported for \"delaysign\".",
                                DelaySign), Location);
                    }

                    // description field
                    if (Description != null) {
                        writer.WriteLine("/description:\"{0}\"", Description);
                    }

                    // write path to security evidence file
                    if (EvidenceFile != null) {
                        writer.WriteLine("/evidence:\"{0}\"", EvidenceFile.FullName);
                    }

                    // file version field
                    if (FileVersion != null) {
                        writer.WriteLine("/fileversion:\"{0}\"", FileVersion);
                    }

                    // flags field
                    if (Flags != null) {
                        writer.WriteLine("/flags:\"{0}\"", Flags);
                    }

                    // main method
                    if (MainMethod != null) {
                        writer.WriteLine("/main:\"{0}\"", MainMethod);
                    }

                    // keycontainer
                    if (KeyContainer != null) {
                        writer.WriteLine("/keyname:\"{0}\"", KeyContainer);
                    }

                    // product field
                    if (Product != null) {
                        writer.WriteLine("/product:\"{0}\"", Product);
                    }

                    // product version field
                    if (ProductVersion != null) {
                        writer.WriteLine("/productversion:\"{0}\"", ProductVersion);
                    }

                    // write path to template assembly
                    if (TemplateFile != null) {
                        if (SupportsTemplate) {
                            writer.WriteLine("/template:\"{0}\"", TemplateFile.FullName);
                        } else {
                            Log(Level.Warning,
                                ResourceUtils.GetString("String_LinkerDoesNotSupportTemplateAssembly"),
                                Project.TargetFramework.Description);
                        }
                    }

                    // title field
                    if (Title != null) {
                        writer.WriteLine("/title:\"{0}\"", Title);
                    }

                    // trademark field
                    if (Trademark != null) {
                        writer.WriteLine("/trademark:\"{0}\"", Trademark);
                    }

                    // key file
                    if (KeyFile != null) {
                        writer.WriteLine("/keyfile:\"{0}\"", KeyFile.FullName);
                    }

                    // assembly version
                    if (Version != null) {
                        writer.WriteLine("/version:\"{0}\"", Version);
                    }

                    // win32 icon
                    if (Win32Icon != null) {
                        writer.WriteLine("/win32icon:\"{0}\"", Win32Icon.FullName);
                    }

                    // win32 resource
                    if (Win32Res != null) {
                        writer.WriteLine("/win32res:\"{0}\"", Win32Res.FullName);
                    }

                    // write embedded resources to response file
                    foreach (string resourceFile in Resources.FileNames) {
                        writer.WriteLine("/embed:\"{0}\"", resourceFile);
                    }

                    // write embedded resources to response file
                    foreach (EmbeddedResource embeddedResource in EmbeddedResources) {
                        writer.WriteLine("/embed:\"{0}\",{1}", embeddedResource.File,
                            embeddedResource.ManifestResourceName);
                    }

                    // suppresses display of the sign-on banner
                    writer.WriteLine("/nologo");

                    // make sure to close the response file otherwise contents
                    // Will not be written to disk and ExecuteTask() will fail.
                    writer.Close();

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Verbose, ResourceUtils.GetString("String_ContentsOf"), _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Verbose, reader.ReadToEnd());
                        reader.Close();
                    }

                    // call base class to do the work
                    base.ExecuteTask();
                } finally {
                    // make sure stream is closed or response file cannot be deleted
                    writer.Close(); 
                    // make sure we delete response file even if an exception is thrown
                    File.Delete(_responseFileName);
                    // initialize name of response file
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the assembly manifest needs compiling or is 
        /// uptodate.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the assembly manifest needs compiling; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            if (!OutputFile.Exists) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileDoesNotExist"), 
                    OutputFile.FullName);
                return true;
            }

            string fileName = null;

            // check if modules were updated
            foreach (Module module in ModuleSet.Modules) {
                fileName = FileSet.FindMoreRecentLastWriteTime(module.File, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if (embedded)resources were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(Resources.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                    fileName);
                return true;
            }

            // check if evidence file was updated
            if (EvidenceFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(EvidenceFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if template file was updated
            if (TemplateFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(TemplateFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if key file was updated
            if (KeyFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(KeyFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if win32 icon file was updated
            if (Win32Icon != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(Win32Icon.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if win32 resource file was updated
            if (Win32Res != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(Win32Res.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if embedded resource files were updated
            foreach (EmbeddedResource embeddedResource in EmbeddedResources) {
                fileName = FileSet.FindMoreRecentLastWriteTime(embeddedResource.File, 
                    OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check the arguments for /embed or /embedresource options
            StringCollection embeddedResourceFiles = new StringCollection();
            foreach (Argument argument in Arguments) {
                if (argument.IfDefined && !argument.UnlessDefined) {
                    string argumentValue = argument.Value;
                    // check whether argument specifies resource file to embed
                    if (argumentValue != null && (argumentValue.StartsWith("/embed:") || argumentValue.StartsWith("/embedresource:"))) {
                        // determine path to resource file
                        string path = argumentValue.Substring(argumentValue.IndexOf(':') + 1);
                        int indexOfComma = path.IndexOf(',');
                        if (indexOfComma != -1) {
                            path = path.Substring(0, indexOfComma);
                        }

                        bool isQuoted = path.Length > 2 && path.StartsWith("\"") && path.EndsWith("\"");
                        if (isQuoted) {
                            path = path.Substring(1, path.Length - 2);
                        }

                        // resolve path to full path (relative to project base dir)
                        path = Project.GetFullPath(path);
                        // add path to collection of resource files
                        embeddedResourceFiles.Add(path);
                    }
                }
            }

            // check if embedded resources passed as arguments were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(embeddedResourceFiles, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                    fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        #endregion Protected Instance Methods
    }
}
