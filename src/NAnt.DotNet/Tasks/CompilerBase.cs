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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Mike Krueger (mike@icsharpcode.net)
// Ian MacLean (ian_maclean@another.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Provides the abstract base class for compiler tasks.
    /// </summary>
    public abstract class CompilerBase : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private FileInfo _outputFile;
        private string _target;
        private bool _debug;
        private string _define;
        private FileInfo _win32icon;
        private bool _warnAsError;
        private bool _forceRebuild;
        private string _mainType;
        private FileSet _references = new FileSet();
        private FileSet _lib = new FileSet();
        private FileSet _modules = new FileSet();
        private FileSet _sources = new FileSet();
        private ResourceFileSetCollection _resourcesList = new ResourceFileSetCollection();

        #endregion Private Instance Fields

        #region Protected Static Fields

        /// <summary>
        /// Contains a list of extensions for all file types that should be treated as
        /// 'code-behind' when looking for resources.  Ultimately this will determine
        /// if we use the "namespace+filename" or "namespace+classname" algorithm, since
        /// code-behind will use the "namespace+classname" algorithm.
        /// </summary>
        protected static string[] CodebehindExtensions = {".aspx", ".asax", ".ascx", ".asmx"};
        
        /// <summary>
        /// Case-insensitive list of valid culture names for this platform.
        /// </summary>
        /// <remarks>
        /// The key of the <see cref="Hashtable" /> is the culture name and 
        /// the value is <see langword="null" />.
        /// </remarks>
        protected readonly static Hashtable CultureNames;

        #endregion Protected Static Fields

        #region Static Constructor
        
        /// <summary>
        /// Class constructor for <see cref="CompilerBase" />.
        /// </summary>
        static CompilerBase() {
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // initialize hashtable to necessary size
            CultureNames = CollectionsUtil.CreateCaseInsensitiveHashtable(
                allCultures.Length);

            // fill the culture list
            foreach (CultureInfo ci in allCultures) {
                CultureNames[ci.Name] = null;
            }
        }
        
        #endregion Static Constructor

        #region Public Instance Properties

        /// <summary>
        /// The output file created by the compiler.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Output type. Possible values are <c>exe</c>, <c>winexe</c>,
        /// <c>library</c> or <c>module</c>.
        /// </summary>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string OutputTarget  {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Generate debug output. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("debug")]
        [BooleanValidator()]
        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        }

        /// <summary>
        /// Define conditional compilation symbol(s).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to <c>/d[efine]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("define")]
        public string Define {
            get { return _define; }
            set { _define = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Icon to associate with the application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to <c>/win32icon:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("win32icon")]
        public FileInfo Win32Icon {
            get { return _win32icon; }
            set { _win32icon = value; }
        }

        /// <summary>
        /// Instructs the compiler to treat all warnings as errors. The default
        /// is <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/warnaserror[+|-]</c> flag of the compiler.
        /// </para>
        /// <para>
        /// When this property is set to <see langword="true" />, any messages
        /// that would ordinarily be reported as warnings will instead be
        /// reported as errors.
        /// </para>
        /// </remarks>
        [TaskAttribute("warnaserror")]
        [BooleanValidator()]
        public bool WarnAsError {
            get { return _warnAsError; }
            set { _warnAsError = value; }
        }

        /// <summary>
        /// Instructs NAnt to recompile the output file regardless of the file timestamps.
        /// </summary>
        /// <remarks>
        /// When this parameter is to <see langword="true" />, NAnt will always
        /// run the compiler to rebuild the output file, regardless of the file timestamps.
        /// </remarks>
        [TaskAttribute("rebuild")]
        [BooleanValidator()]
        public bool ForceRebuild {
            get { return _forceRebuild; }
            set { _forceRebuild = value; }
        }

        /// <summary>
        /// Specifies which type contains the Main method that you want to use
        /// as the entry point into the program.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/m[ain]:</c> flag of the compiler.
        /// </para>
        /// <para>
        /// Use this property when creating an executable file. If this property
        /// is not set, the compiler searches for a valid Main method in all
        /// public classes.
        /// </para>
        /// </remarks>
        [TaskAttribute("main")]
        public string MainType {
            get { return _mainType; }
            set { _mainType = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Additional directories to search in for assembly references.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/lib[path]:</c> flag.
        /// </para>
        /// </remarks>
        [BuildElement("lib")]
        public FileSet Lib {
            get { return _lib; }
            set {_lib = value; }
        }

        /// <summary>
        /// Reference metadata from the specified assembly files.
        /// </summary>
        [BuildElement("references")]
        public FileSet References {
            get { return _references; }
            set { _references = value; }
        }

        /// <summary>
        /// Resources to embed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be a combination of resx files and file resources.
        /// </para>
        /// <para>
        /// .resx files will be compiled by <see cref="ResGenTask" /> and then
        /// embedded into the resulting executable.
        /// </para>
        /// <para>
        /// The <see cref="ResourceFileSet.Prefix" /> property is used to make
        /// up the resource name added to the assembly manifest for non-resx
        /// files.
        /// </para>
        /// <para>
        /// For .resx files the namespace from the matching source file is used
        /// as prefix. This matches the behaviour of Visual Studio.
        /// </para>
        /// <para>
        /// Multiple resources tags with different namespace prefixes may be
        /// specified.
        /// </para>
        /// </remarks>
        [BuildElementArray("resources")]
        public ResourceFileSetCollection ResourcesList {
            get { return _resourcesList; }
        }

        /// <summary>
        /// Link the specified modules into this assembly.
        /// </summary>
        [BuildElement("modules")]
        public FileSet Modules {
            get { return _modules; }
            set { _modules = value; }
        }

        /// <summary>
        /// The set of source files for compilation.
        /// </summary>
        [BuildElement("sources", Required=true)]
        public FileSet Sources {
            get { return _sources; }
            set { _sources = value; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>
        /// The file extension required by the current compiler.
        /// </value>
        public abstract string Extension {
            get;
        }
        /// <summary>
        /// Gets the class name regular expression for the language of the current compiler.
        /// </summary>
        /// <value> class name regular expression for the language of the current compiler</value>
        protected abstract Regex ClassNameRegex {
            get;
        }
        /// <summary>
        /// Gets the namespace regular expression for the language of the current compiler.
        /// </summary>
        /// <value> namespace regular expression for the language of the current compiler</value>
        protected abstract Regex NamespaceRegex {
            get;
        }

        #endregion Protected Instance Properties

        #region Override implementation of ExternalProgramBase


        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return "@" + "\"" + _responseFileName + "\""; }
        }

        /// <summary>
        /// Compiles the sources and resources.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);
                // culture names are not case-sensitive
                Hashtable cultureResources = CollectionsUtil.CreateCaseInsensitiveHashtable();
                // will hold temporary compiled resources 
                StringCollection compiledResourceFiles = new StringCollection();
                
                try {
                    // ensure base directory is set, even if fileset was not initialized
                    // from XML
                    if (References.BaseDirectory == null) {
                        References.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
                    }
                    if (Lib.BaseDirectory == null) {
                        Lib.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
                    }
                    if (Modules.BaseDirectory == null) {
                        Modules.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);   
                    }   
                    if (Sources.BaseDirectory == null) {   
                        Sources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);   
                    } 

                    Log(Level.Info, LogPrefix + "Compiling {0} files to '{1}'.", 
                        Sources.FileNames.Count, OutputFile.FullName);

                    // specific compiler options
                    WriteOptions(writer);

                    // suppresses display of the sign-on banner
                    WriteOption(writer, "nologo");

                    // specify output file format
                    WriteOption(writer, "target", OutputTarget);

                    if (Define != null) {
                        WriteOption(writer, "define", Define);
                    }

                    // the name of the output file
                    WriteOption(writer, "out", OutputFile.FullName);

                    if (Win32Icon != null) {
                        WriteOption(writer, "win32icon", Win32Icon.FullName);
                    }

                    // writes the option that specifies the class containing 
                    // the Main method that should be called when the program 
                    // starts.
                    if (this.MainType != null) {
                        WriteOption(writer, "main", this.MainType);
                    }

                    // writes the option that specifies whether the compiler 
                    // should consider warnings as errors.
                    if (this.WarnAsError) {
                        WriteOption(writer, "warnaserror");
                    }

                    // writes assembly references to the response file
                    WriteAssemblyReferences(writer);

                    // writes module references to the response file
                    WriteModuleReferences(writer);

                    // compile resources
                    foreach (ResourceFileSet resources in ResourcesList) {
                        // Resx args
                        foreach (string fileName in resources.ResxFiles.FileNames) {
                            // determine manifest resource name
                            string manifestResourceName = this.GetManifestResourceName(
                                resources, fileName);
                            
                            string tmpResourcePath = Path.ChangeExtension(fileName, ".resources");
                            compiledResourceFiles.Add(tmpResourcePath);
                            
                            // compile to a temp .resources file
                            CompileResxResource(new FileInfo(fileName), new FileInfo(tmpResourcePath));
                            
                            // check if resource is localized
                            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(fileName,
                                Path.ChangeExtension(fileName, Extension));
                            if (resourceCulture != null) {
                                if (!cultureResources.ContainsKey(resourceCulture.Name)) {
                                    // initialize collection for holding 
                                    // resource file for this culture
                                    cultureResources.Add(resourceCulture.Name, new Hashtable());
                                }
                                // store resulting .resources file for later linking 
                                ((Hashtable) cultureResources[resourceCulture.Name])[manifestResourceName] = tmpResourcePath;
                            } else {
                                // regular embedded resources (using filename and manifest resource name).
                                string resourceoption = string.Format(CultureInfo.InvariantCulture, "{0},{1}", tmpResourcePath, manifestResourceName);
                                // write resource option to response file
                                WriteOption(writer, "resource", resourceoption);
                            }
                        }

                        // other resources
                        foreach (string fileName in resources.NonResxFiles.FileNames) {
                            // determine manifest resource name
                            string manifestResourceName = this.GetManifestResourceName(
                                resources, fileName);

                            // check if resource is localized
                            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(fileName,
                                Path.ChangeExtension(fileName, Extension));
                            if (resourceCulture != null) {
                                if (!cultureResources.ContainsKey(resourceCulture.Name)) {
                                    // initialize collection for holding 
                                    // resource file for this culture
                                    cultureResources.Add(resourceCulture.Name, new Hashtable());
                                }
                                // store resource filename for later linking
                                ((Hashtable) cultureResources[resourceCulture.Name])[manifestResourceName] = fileName;
                            } else {                   
                                string resourceoption = string.Format(CultureInfo.InvariantCulture, "{0},{1}",fileName, manifestResourceName);
                                WriteOption(writer, "resource", resourceoption);
                            }
                        }
                    }

                    // write sources to compile to response file
                    foreach (string fileName in Sources.FileNames) {
                        writer.WriteLine("\"" + fileName + "\"");
                    }

                    // make sure to close the response file otherwise contents
                    // will not be written to disk and ExecuteTask() will fail.
                    writer.Close();

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Info, LogPrefix + "Contents of {0}.", _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Info, reader.ReadToEnd());
                        reader.Close();
                    }

                    // call base class to do the work
                    base.ExecuteTask();

                    // create a satellite assembly for each culture name
                    foreach (string culture in cultureResources.Keys) {
                        // determine directory for satellite assembly
                        string culturedir = Path.Combine(OutputFile.DirectoryName, culture);
                        // ensure diretory for satellite assembly exists
                        Directory.CreateDirectory(culturedir);
                        // determine filename of satellite assembly
                        FileInfo outputFile = new FileInfo(Path.Combine(culturedir, 
                            Path.GetFileNameWithoutExtension(OutputFile.Name) 
                            + ".resources.dll"));
                        // generate satellite assembly
                        LinkResourceAssembly((Hashtable)cultureResources[culture], 
                            outputFile, culture);
                    }
                } finally {
                    // cleanup .resource files
                    foreach (string compiledResourceFile in compiledResourceFiles) {
                        File.Delete(compiledResourceFile);
                    }
                    
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
                    File.Delete(_responseFileName);
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Public Instance Methods

        /// <summary>
        /// Determines the manifest resource name of the given resource file.
        /// </summary>
        /// <param name="resources">The <see cref="ResourceFileSet" /> containing information that will used to assemble the manifest resource name.</param>
        /// <param name="resourceFile">The resource file of which the manifest resource name should be determined.</param>
        /// <param name="dependentFile">The source file on which the resource file depends.</param>
        /// <returns>
        /// The manifest resource name of the specified resource file.
        /// </returns>
        public string GetManifestResourceName(ResourceFileSet resources, string resourceFile, string dependentFile) {
            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            if (resourceFile == null) {
                throw new ArgumentNullException("resourceFile");
            }

            // make sure the resource file exists
            if (!File.Exists(resourceFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Resource '{0}' does not exist.", resourceFile), Location);
            }

            // will hold the manifest resource name
            string manifestResourceName = null;
          
            // check if we're dealing with a localized resource
            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(resourceFile, dependentFile);

            // determine the resource type
            switch (Path.GetExtension(resourceFile)) {
                case ".resx":
                    // try and get manifest resource name from dependent file
                    ResourceLinkage resourceLinkage = GetResourceLinkage(
                        dependentFile, resourceCulture);

                    if (resourceLinkage != null && !resourceLinkage.HasNamespaceName) {
                        resourceLinkage.NamespaceName = resources.Prefix;
                    }

                    string actualFileName = Path.GetFileNameWithoutExtension(
                        resourceFile);
                    
                    manifestResourceName = Path.ChangeExtension(
                        Path.GetFileName(resourceFile), ".resources");

                    // cater for asax/aspx special cases ...
                    foreach (string extension in CodebehindExtensions) {
                        if (manifestResourceName.IndexOf(extension) > -1) {
                            manifestResourceName = manifestResourceName.Replace(extension, "");
                            actualFileName = actualFileName.Replace(extension, "");
                            break;
                        }
                    }
                            
                    if (resourceLinkage != null && !resourceLinkage.HasClassName) {
                        resourceLinkage.ClassName = actualFileName;
                    }

                    if (resourceLinkage != null && resourceLinkage.IsValid) {
                        manifestResourceName = manifestResourceName.Replace(
                            actualFileName, resourceLinkage.ToString());
                    }

                    if (resourceLinkage == null) {
                        manifestResourceName = Path.ChangeExtension(
                            resources.GetManifestResourceName(resourceFile), 
                            "resources");
                    }
                    break;
                default:
                    // VS.NET handles an embedded resource file named licenses.licx
                    // in the root of the project and without culture in a special
                    // way
                    if (Path.GetFileName(resourceFile) == "licenses.licx") {
                        // the manifest resource name will be <output file>.licenses
                        // eg. TestAssembly.exe.licenses
                        manifestResourceName = Path.GetFileName(OutputFile.FullName)
                            + ".licenses";
                    } else {
                        // check if resource is localized
                        if (resourceCulture != null) {
                            // determine resource name
                            manifestResourceName = resources.GetManifestResourceName(
                                resourceFile);

                            // remove culture name from name of resource
                            int cultureIndex = manifestResourceName.LastIndexOf("." + resourceCulture.Name);
                            manifestResourceName = manifestResourceName.Substring(0, cultureIndex) 
                                + manifestResourceName.Substring(cultureIndex).Replace("." 
                                + resourceCulture.Name, string.Empty);
                        } else {
                            manifestResourceName = resources.GetManifestResourceName(
                                resourceFile);
                        }
                    }
                    break;
            }

            return manifestResourceName;
        }

        /// <summary>
        /// Determines the manifest resource name of the given resource file.
        /// </summary>
        /// <param name="resources">The <see cref="ResourceFileSet" /> containing information that will used to assemble the manifest resource name.</param>
        /// <param name="resourceFile">The resource file of which the manifest resource name should be determined.</param>
        /// <returns>
        /// The manifest resource name of the specified resource file.
        /// </returns>
        /// <remarks>
        /// For .resx resources, the name of the dependent is determined by
        /// replacing the extension of the file with the extension of the 
        /// source files for the compiler, and removing the culture name from
        /// the file name for localized resources.
        /// </remarks>
        public string GetManifestResourceName(ResourceFileSet resources, string resourceFile) {
            if (resources == null) {
                throw new ArgumentNullException("resources");
            }

            if (resourceFile == null) {
                throw new ArgumentNullException("resourceFile");
            }

            // make sure the resource file exists
            if (!File.Exists(resourceFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Resource '{0}' does not exist.", resourceFile), Location);
            }

            // determine the resource type
            switch (Path.GetExtension(resourceFile)) {
                case ".resx":
                    // open matching source file if it exists
                    string dependentFile = Path.ChangeExtension(resourceFile, Extension);

                    // check if we're dealing with a localized resource
                    CultureInfo resourceCulture = CompilerBase.GetResourceCulture(resourceFile, dependentFile);

                    // remove last occurrence of culture name from dependent file 
                    // for localized resources
                    if (resourceCulture != null) {
                        int index = dependentFile.LastIndexOf("." + resourceCulture.Name);
                        if (index >= 0) {
                            if ((index + resourceCulture.Name.Length + 1) < dependentFile.Length) {
                                dependentFile = dependentFile.Substring(0, index) 
                                    + dependentFile.Substring(index + resourceCulture.Name.Length + 1);
                            } else {
                                dependentFile = dependentFile.Substring(0, index);
                            }
                        }
                    }

                    // determine the manifest resource name using the given
                    // dependent file
                    return GetManifestResourceName(resources, resourceFile, dependentFile);
                default:
                    // for non-resx resources, a dependent file has no influence 
                    // on the manifest resource name
                    return GetManifestResourceName(resources, resourceFile, null);
            }
        }

        /// <summary>
        /// Extracts the associated namespace/classname linkage found in the 
        /// given stream.
        /// </summary>
        /// <param name="sr">The read-only stream of the source file to search.</param>
        /// <returns>
        /// The namespace/classname of the source file matching the resource.
        /// </returns>
        public virtual ResourceLinkage PerformSearchForResourceLinkage(TextReader sr) {
            Regex matchNamespaceRE = NamespaceRegex;
            Regex matchClassNameRE = ClassNameRegex;
            
            string namespaceName  = "";
            string className = "";
    
            while (sr.Peek() > -1) {
                string str = sr.ReadLine();
                            
                Match matchNamespace = matchNamespaceRE.Match(str);
                if (matchNamespace.Success) {
                    Group group = matchNamespace.Groups["namespace"];
                    if (group.Success) {
                        foreach (Capture capture in group.Captures) {
                            namespaceName += (namespaceName.Length > 0 ? "." : "") + capture.Value;
                        }
                    }
                }

                Match matchClassName = matchClassNameRE.Match(str);
                if (matchClassName.Success) {
                    Group group = matchClassName.Groups["class"];
                    if (group.Success) {
                        className = group.Value;
                        break;
                    }
                }
            }
            return new ResourceLinkage(namespaceName, className);
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Writes assembly references to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the assembly references should be written.</param>
        protected void WriteAssemblyReferences(TextWriter writer) {
            // fix references to system assemblies and assemblies that
            // can be resolved using directories specified with the
            // <lib> element
            ResolveReferences(References);

            // write references to the TextWriter
            foreach (string fileName in References.FileNames) {
                WriteOption(writer, "reference", fileName);
            }
        }

        /// <summary>
        /// Writes module references to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the module references should be written.</param>
        protected void WriteModuleReferences(TextWriter writer) {
            // fix references to system modules and modules that
            // can be resolved using directories specified with the
            // <lib> element
            ResolveReferences(Modules);

            // write references to the TextWriter
            foreach (string fileName in Modules.FileNames) {
                WriteOption(writer, "addmodule", fileName);
            }
        }

        /// <summary>
        /// Resolves references to system assemblies and assemblies that can be 
        /// resolved using directories specified in <see cref="Lib" />.
        /// </summary>
        /// <param name="fileSet">The <see cref="FileSet" /> in which references should be resolved.</param>
        protected void ResolveReferences(FileSet fileSet) {
            foreach (string pattern in fileSet.Includes) {
                if (Path.GetFileName(pattern) == pattern) {
                    string localPath = Path.Combine(fileSet.BaseDirectory.FullName, 
                        pattern);

                    // check if a file match the pattern exists in the 
                    // base directory of the references fileset
                    if (File.Exists(localPath)) {
                        // the file will already be included as part of
                        // the fileset scan process
                        continue;
                    }

                    foreach (string libPath in Lib.DirectoryNames) {
                        string fullPath = Path.Combine(libPath, pattern);

                        // check whether an assembly matching the pattern
                        // exists in the assembly directory of the current
                        // framework
                        if (File.Exists(fullPath)) {
                            // found a system reference
                            fileSet.FileNames.Add(fullPath);

                            // continue with the next pattern
                            continue;
                        }
                    }

                    if (Project.TargetFramework != null) {
                        string frameworkDir = Project.TargetFramework.FrameworkAssemblyDirectory.FullName;
                        string fullPath = Path.Combine(frameworkDir, pattern);

                        // check whether an assembly matching the pattern
                        // exists in the assembly directory of the current
                        // framework
                        if (File.Exists(fullPath)) {
                            // found a system reference
                            fileSet.FileNames.Add(fullPath);

                            // continue with the next pattern
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allows derived classes to provide compiler-specific options.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected virtual void WriteOptions(TextWriter writer) {
        }

        /// <summary>
        /// Writes an option using the default output format.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the compiler options should be written.</param>
        /// <param name="name">The name of the option which should be passed to the compiler.</param>
        protected virtual void WriteOption(TextWriter writer, string name) {
            writer.WriteLine("/{0}", name);
        }

        /// <summary>
        /// Writes an option and its value using the default output format.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the compiler options should be written.</param>
        /// <param name="name">The name of the option which should be passed to the compiler.</param>
        /// <param name="arg">The value of the option which should be passed to the compiler.</param>
        protected virtual void WriteOption(TextWriter writer, string name, string arg) {
            // always quote arguments
            writer.WriteLine("\"/{0}:{1}\"", name, arg);
        }

        /// <summary>
        /// Determines whether compilation is needed.
        /// </summary>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile
            if (ForceRebuild) {
                Log(Level.Verbose, LogPrefix + "'rebuild' attribute set to true, recompiling.");
                return true;
            }

            if (!OutputFile.Exists) {
                return true;
            }

            // check if sources were updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if reference assemblies were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(References.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if modules were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(Modules.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if resources were updated
            foreach (ResourceFileSet resources in ResourcesList) {
                fileName = FileSet.FindMoreRecentLastWriteTime(resources.FileNames, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                    return true;
                }
            }

            // check if win32icon was updated
            if (Win32Icon != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(Win32Icon.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                    return true;
                }
            }

            // check the args for /res or /resource options.
            StringCollection resourceFileNames = new StringCollection();
            foreach (Argument argument in Arguments) {
                if (argument.IfDefined && !argument.UnlessDefined) {
                    string argumentValue = argument.Value;
                    // check whether argument specified resource file to embed
                    if (argumentValue != null && (argumentValue.StartsWith("/res:") || argumentValue.StartsWith("/resource:"))) {
                        // determine path to resource file
                        string path = argumentValue.Substring(argumentValue.IndexOf(':') + 1);
                        int indexOfComma = path.IndexOf(',');
                        if (indexOfComma != -1) {
                            path = path.Substring(0, indexOfComma);
                        }
                        // resolve path to full path (relative to project base dir)
                        path = Project.GetFullPath(path);
                        // add path to collection of resource files
                        resourceFileNames.Add(path);
                    }
                }
            }

            fileName = FileSet.FindMoreRecentLastWriteTime(resourceFileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        /// <summary>
        /// Finds the correct namespace/classname for a resource file from the 
        /// given dependent source file.
        /// </summary>
        /// <param name="dependentFile">The file from which the resource linkage of the resource file should be determined.</param>
        /// <param name="resourceCulture">The culture of the resource file for which the resource linkage should be determined.</param>
        /// <returns>
        /// The namespace/classname of the source file matching the resource or
        /// <see langword="null" /> if the dependent source file does not exist.
        /// </returns>
        /// <remarks>
        /// This behaviour may be overidden by each particular compiler to 
        /// support the namespace/classname syntax for that language.
        /// </remarks>
        protected virtual ResourceLinkage GetResourceLinkage(string dependentFile, CultureInfo resourceCulture) {
            StreamReader sr = null;
            ResourceLinkage resourceLinkage  = null;

            // resource linkage cannot be determined if there's no dependent file
            if (dependentFile == null) {
                return null;
            }
  
            try {
                // open matching source file
                sr = File.OpenText(dependentFile);
                // get resource linkage
                resourceLinkage = PerformSearchForResourceLinkage(sr);
                // set resource culture
                resourceLinkage.Culture = resourceCulture;
            } catch (FileNotFoundException) { // if no matching file, dump out
                Log(Level.Debug, LogPrefix + "Did not find dependent file {0}.", dependentFile);
                return null;
            } finally {
                if (sr != null) {
                    sr.Close();
                }
            }

            // output some debug information about resource linkage found...
            if (resourceLinkage.IsValid) {
                Log(Level.Debug, LogPrefix + "Found resource linkage '{0}' in dependent file '{1}'.", resourceLinkage.ToString(), dependentFile);
            } else {
                Log(Level.Debug, LogPrefix + "Could not find any resource linkage in dependent file '{0}'.", dependentFile);
            }

            return resourceLinkage;
        }

        /// <summary>
        /// Link a list of files into a resource assembly.
        /// </summary>
        /// <param name="resourceFiles">The collection of resources.</param>
        /// <param name="resourceAssemblyFile">Resource assembly to generate</param>
        /// <param name="culture">Culture of the generated assembly.</param>
        protected void LinkResourceAssembly(Hashtable resourceFiles, FileInfo resourceAssemblyFile, string culture) {
            // defer to the assembly linker task
            AssemblyLinkerTask alink = new AssemblyLinkerTask();

            // inherit project from current task
            alink.Project = Project;

            // inherit namespace manager from current task
            alink.NamespaceManager = NamespaceManager;

            // current task is parent
            alink.Parent = this;

            // make sure framework specific information is set
            alink.InitializeTaskConfiguration();

            // set task properties
            alink.OutputFile = resourceAssemblyFile;
            alink.Culture = culture;
            alink.OutputTarget = "lib";
            alink.TemplateFile = OutputFile;

            // add resource files using the Arguments collection.
            foreach (string manifestname in resourceFiles.Keys) {
                string resourcefile = (string) resourceFiles[manifestname];
                // add resources to embed 
                Argument arg = new Argument();
                arg.Value = string.Format(CultureInfo.InvariantCulture, "/embed:\"{0}\",{1}", resourcefile, manifestname);
                alink.Arguments.Add(arg);
            }
            
            // increment indentation level
            Project.Indent();
            try {
                // execute the nested task
                alink.Execute();
            } finally {
                // restore indentation level
                Project.Unindent();
            }
        }
        
        /// <summary>
        /// Compiles a resx files to a .resources file.
        /// </summary>
        /// <param name="inputFile">The resx file to compile.</param>
        /// <param name="outputFile">The name of the resource file to create.</param>
        protected void CompileResxResource(FileInfo inputFile, FileInfo outputFile) {
            ResGenTask resgen = new ResGenTask();

            // inherit project from current task
            resgen.Project = Project;

            // inherit namespace manager from current task
            resgen.NamespaceManager = NamespaceManager;

            // current task is parent
            resgen.Parent = this;

            // make sure framework specific information is set
            resgen.InitializeTaskConfiguration();
           
            // inherit Verbose setting from current task
            resgen.Verbose = Verbose;

            resgen.InputFile = inputFile;
            resgen.OutputFile = outputFile;

            // increment indentation level
            Project.Indent();
            try {
                // execute the task
                resgen.Execute();
            } finally {
                // restore indentation level
                Project.Unindent();
            }
        }
        
        #endregion Protected Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Determines the culture associated with a given resource file by
        /// scanning the filename for valid culture names.
        /// </summary>
        /// <param name="resourceFile">The resource file path to check for culture info.</param>
        /// <param name="dependentFile">The file on which the resource file depends.</param>
        /// <returns>
        /// A valid <see cref="CultureInfo" /> instance if the resource is 
        /// associated with a specific culture; otherwise, <see langword="null" />.
        /// </returns>
        public static CultureInfo GetResourceCulture(string resourceFile, string dependentFile) {
            string noextpath = Path.GetFileNameWithoutExtension(resourceFile);

            if (dependentFile != null && File.Exists(dependentFile)) {
                // there might be cases where the dependent file actually has
                // a filename containing a valid culture name (eg. Form2.nl-BE.cs)
                // in this case resx files for that file will have filenames
                // containing culture names too (eg. Form2.nl-BE.resx), 
                // although the files are not localized
                if (Path.GetFileNameWithoutExtension(dependentFile) == noextpath) {
                    return null;
                }
            } 

            int index = noextpath.LastIndexOf('.');
            if (index >= 0 && index <= noextpath.Length) {
                string possibleculture = noextpath.Substring(index + 1, noextpath.Length - (index + 1));
                // check that its in our list of culture names
                if (CultureNames.ContainsKey(possibleculture)) {
                    return new CultureInfo(possibleculture);
                }
            }
            return null;
        }

        #endregion Public Static Methods

        /// <summary>
        /// Holds class and namespace information for resource (*.resx) linkage.
        /// </summary>
        public class ResourceLinkage {
            #region Private Instance Fields
            
            private string _namespaceName;
            private string _className;
            private CultureInfo _culture;

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ResourceLinkage" />
            /// class.
            /// </summary>
            /// <param name="namespaceName">The namespace the resource is under.</param>
            /// <param name="className">The class name the resource is associated with.</param>
            public ResourceLinkage(string namespaceName, string className) {
                _namespaceName = namespaceName;
                _className = className;
            }

            #endregion Public Instance Constructors

            #region Override implementation of Object
 
            /// <summary>
            /// Returns the resource linkage as a string.
            /// </summary>
            /// <returns>
            /// A string representation of the resource linkage.
            /// </returns>
            public override string ToString() {
                string resourceName;

                if (!IsValid) {
                    return string.Empty;
                }

                if (HasNamespaceName) {
                    if (HasClassName) {
                        resourceName = NamespaceName + "." + ClassName;
                    } else {
                        resourceName = NamespaceName;
                    }
                } else {
                    resourceName = ClassName;
                }

                if (Culture != null) {
                    resourceName = string.Format("{0}.{1}", resourceName, Culture.Name);
                }

                return resourceName;
            }

            #endregion Override implementation of Object

            #region Public Instance Properties
  
            /// <summary>
            /// Gets a value indicating whether the <see cref="ResourceLinkage" />
            /// instances contains valid data.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if the <see cref="ResourceLinkage" />
            /// instance contains valid data; otherwise, <see langword="false" />.
            /// </value>
            public bool IsValid {
                get { return !StringUtils.IsNullOrEmpty(_namespaceName) || !StringUtils.IsNullOrEmpty(_className); }
            }
  
            /// <summary>
            /// Gets a value indicating whether a namespace name is available
            /// for this <see cref="ResourceLinkage" /> instance.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if a namespace name is available for 
            /// this <see cref="ResourceLinkage" /> instance; otherwise, 
            /// <see langword="false" />.
            /// </value>
            public bool HasNamespaceName {
                get { return !StringUtils.IsNullOrEmpty(_namespaceName); }
            }
  
            /// <summary>
            /// Gets a value indicating whether a class name is available
            /// for this <see cref="ResourceLinkage" /> instance.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if a class name is available for 
            /// this <see cref="ResourceLinkage" /> instance; otherwise, 
            /// <see langword="false" />.
            /// </value>
            public bool HasClassName {
                get { return !StringUtils.IsNullOrEmpty(_className); }
            }
  
            /// <summary>
            /// Gets the name of namespace the resource is under.  
            /// </summary>
            /// <value>
            /// The name of namespace the resource is under.  
            /// </value>
            public string NamespaceName  {
                get { return _namespaceName; }
                set { _namespaceName = (value != null) ? value.Trim() : null; }
            }
  
            /// <summary>
            /// Gets the name of the class (most likely a form) that the resource 
            /// is associated with.  
            /// </summary>
            /// <value>
            /// The name of the class the resource is associated with.  
            /// </value>
            public string ClassName {
                get { return _className; }
                set { _className = (value != null) ? value.Trim() : null; }
            }

            /// <summary>
            /// Gets the culture that the resource is associated with.
            /// </summary>
            /// <value>
            /// The culture that the resource is associated with.
            /// </value>
            public CultureInfo Culture {
                get { return _culture; }
                set { _culture = value; }
            }

            #endregion Public Instance Properties
        }
    }
}
