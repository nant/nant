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
        private string _output = null;
        private string _target = null;
        private bool _debug = false;
        private string _define = null;
        private string _win32icon = null;
        private bool _warnAsError = false;
        private string _mainType = null;
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
        /// List of valid culture names for this platform
        /// </summary>
        protected static StringCollection CultureNames = new StringCollection();

        #endregion Protected Static Fields

        #region Static Constructor
        
        /// <summary>
        /// Class constructor for <see cref="CompilerBase" />.
        /// </summary>
        static CompilerBase() {
            // fill the culture list
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures)) {
                CultureNames.Add(ci.Name);
            }
        }
        
        #endregion Static Constructor

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file created by the compiler.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { _output = StringUtils.ConvertEmptyToNull(value); }
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
        public string Win32Icon {
            get { return (_win32icon != null) ? Project.GetFullPath(_win32icon) : null; }
            set { _win32icon = StringUtils.ConvertEmptyToNull(value); }
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
        protected abstract string Extension {
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
                Hashtable cultureResources = new Hashtable();
                StringCollection compiledResourceFiles = new StringCollection();
                
                try {
                    if (References.BaseDirectory == null) {
                        References.BaseDirectory = BaseDirectory;
                    }
                    if (Modules.BaseDirectory == null) {
                        Modules.BaseDirectory = BaseDirectory;
                    }
                    if (Sources.BaseDirectory == null) {
                        Sources.BaseDirectory = BaseDirectory;
                    }

                    Log(Level.Info, LogPrefix + "Compiling {0} files to {1}.", Sources.FileNames.Count, Output);

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
                    WriteOption(writer, "out", Output);

                    if (Win32Icon != null) {
                        WriteOption(writer, "win32icon", Win32Icon);
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
                            
                            string tmpResourcePath = fileName.Replace(Path.GetFileName(fileName), manifestResourceName);
                            compiledResourceFiles.Add(tmpResourcePath);
                            
                            // compile to a temp .resources file
                            CompileResxResource(fileName, tmpResourcePath);
                            
                            // check if resource is localized
                            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(fileName);
                            if (resourceCulture != null) {
                                if (!cultureResources.ContainsKey(resourceCulture.Name)) {
                                    // initialize collection for holding 
                                    // resource file for this culture
                                    cultureResources.Add(resourceCulture.Name, new StringCollection());
                                }

                                // store resulting .resources file for later linking 
                                ((StringCollection) cultureResources[resourceCulture.Name]).Add(tmpResourcePath);
                            } else {
                                // regular embedded resources
                                string resourceoption = tmpResourcePath;

                                // write resource option to response file
                                WriteOption(writer, "resource", resourceoption);
                            }
                        }

                        // other resources
                        foreach (string fileName in resources.NonResxFiles.FileNames) {
                            // determine manifest resource name
                            string manifestResourceName = this.GetManifestResourceName(
                                resources, fileName);

                            string tmpResourcePath = fileName.Replace(Path.GetFileName(fileName), manifestResourceName);
                            if (tmpResourcePath != fileName) {
                                // copy resource file to filename matching 
                                // manifest resource name
                                File.Copy(fileName, tmpResourcePath, true);

                                // make sure copy is removed later on
                                compiledResourceFiles.Add(tmpResourcePath);
                            }

                            // check if resource is localized
                            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(fileName);
                            if (resourceCulture != null) {
                                if (!cultureResources.ContainsKey(resourceCulture.Name)) {
                                    // initialize collection for holding 
                                    // resource file for this culture
                                    cultureResources.Add(resourceCulture.Name, new StringCollection());
                                }

                                // store resource filename for later linking; 
                                ((StringCollection) cultureResources[resourceCulture.Name]).Add(tmpResourcePath);
                            } else {
                                string resourceoption = tmpResourcePath;
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
                        string culturedir = Path.GetDirectoryName(Output) + Path.DirectorySeparatorChar + culture;
                        // ensure diretory for satellite assembly exists
                        Directory.CreateDirectory(culturedir);
                        // determine filename of satellite assembly
                        string outputFile =  Path.Combine(culturedir, Path.GetFileNameWithoutExtension(Output) + ".resources.dll");
                        // generate satellite assembly
                        LinkResourceAssembly((StringCollection) cultureResources[culture], 
                            outputFile, culture);
                    }
                } finally {
                    // cleanup .resource files
                    foreach (string fileName in compiledResourceFiles) {
                        File.Delete(fileName); 
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
        /// <returns>
        /// The manifest resource name of the specified resource file.
        /// </returns>
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

            // will hold the manifest resource name
            string manifestResourceName = null;

            // determine the resource type
            switch (Path.GetExtension(resourceFile)) {
                case ".resx":
                    // try and get manifest resource name from matching form
                    ResourceLinkage resourceLinkage = GetFormResourceLinkage(resourceFile);

                    if (resourceLinkage != null && !resourceLinkage.HasNamespaceName) {
                        resourceLinkage.NamespaceName = resources.Prefix;
                    }

                    string actualFileName = Path.GetFileNameWithoutExtension(resourceFile);
                    
                    manifestResourceName = Path.ChangeExtension(
                        Path.GetFileName(resourceFile), ".resources");

                    // cater for asax/aspx special cases ...
                    foreach (string extension in CodebehindExtensions){
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
                    // check if resource is localized
                    CultureInfo resourceCulture = CompilerBase.GetResourceCulture(resourceFile);
                    if (resourceCulture != null) {
                        // determine resource name
                        manifestResourceName = resources.GetManifestResourceName(resourceFile);

                        // remove culture name from name of resource
                        int cultureIndex = manifestResourceName.LastIndexOf("." + resourceCulture.Name);
                        manifestResourceName = manifestResourceName.Substring(0, cultureIndex) 
                            + manifestResourceName.Substring(cultureIndex).Replace("." 
                            + resourceCulture.Name, string.Empty);
                    } else {
                        manifestResourceName = resources.GetManifestResourceName(resourceFile);
                    }
                    break;
            }

            return manifestResourceName;
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
                    string localPath = Path.Combine(fileSet.BaseDirectory, pattern);

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

                    if (Project.CurrentFramework != null) {
                        string frameworkDir = Project.CurrentFramework.FrameworkAssemblyDirectory.FullName;
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
            // Always quote arguments ( )
            writer.WriteLine("\"/{0}:{1}\"", name, arg);
        }

        /// <summary>
        /// Determines whether compilation is needed.
        /// </summary>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            FileInfo outputFileInfo = new FileInfo(Output);
            if (!outputFileInfo.Exists) {
                return true;
            }

            //Sources Updated?
            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //References Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(References.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Modules Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(Modules.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Resources Updated?
            foreach (ResourceFileSet resources in ResourcesList) {
                fileName = FileSet.FindMoreRecentLastWriteTime(resources.FileNames, outputFileInfo.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                    return true;
                }
            }

            // check the args for /res or /resource options.
            StringCollection resourceFileNames = new StringCollection();
            foreach (Argument argument in Arguments) {
                if (argument.IfDefined && !argument.UnlessDefined) {
                    string argumentValue = argument.Value;
                    if (argumentValue != null && (argumentValue.StartsWith("/res:") || argumentValue.StartsWith("/resource:"))) {
                        string path = argumentValue.Substring(argumentValue.IndexOf(':') + 1);
                        int indexOfComma = path.IndexOf(',');
                        if (indexOfComma != -1) {
                            path = path.Substring(0, indexOfComma);
                        }
                        resourceFileNames.Add(path);
                    }
                }
            }

            fileName = FileSet.FindMoreRecentLastWriteTime(resourceFileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        /// <summary>
        /// Opens matching source file to find the correct namespace for the
        /// specified rsource file.
        /// </summary>
        /// <returns>
        /// The namespace/classname of the source file matching the resource or
        /// <see langword="null" /> if there's no matching source file.
        /// </returns>
        /// <remarks>
        /// This behaviour may be overidden by each particular compiler to 
        /// support the namespace/classname syntax for that language.
        /// </remarks>
        protected virtual ResourceLinkage GetFormResourceLinkage(string resxPath) {
            // open matching source file if it exists
            string sourceFile = resxPath.Replace("resx", Extension);
            
            // check if we're dealing with a localized resource
            CultureInfo resourceCulture = CompilerBase.GetResourceCulture(resxPath);
            if (resourceCulture != null) {
                sourceFile = sourceFile.Replace(string.Format(CultureInfo.InvariantCulture,
                    ".{0}", resourceCulture.Name), "");
            }

            StreamReader sr = null;
            ResourceLinkage resourceLinkage  = null; 
  
            try {
                // open matching source file
                sr = File.OpenText(sourceFile);
                // get resource linkage
                resourceLinkage = PerformSearchForResourceLinkage(sr);
                // set resource culture
                resourceLinkage.Culture = resourceCulture;
            } catch (FileNotFoundException) { // if no matching file, dump out
                Log(Level.Debug, LogPrefix + "Did not find associated source file for resource {0}.", resxPath);
                return null;
            } finally {
                if (sr != null) {
                    sr.Close();
                }
            }

            // output some debug information about resource linkage found...
            if (resourceLinkage.IsValid) {
                Log(Level.Debug, LogPrefix + "Found resource linkage '{0}' for resource {1}.", resourceLinkage.ToString(), resxPath);
            } else {
                Log(Level.Debug, LogPrefix + "Could not find any resource linkage in matching source file for resource {0}.", resxPath);
            }

            return resourceLinkage;
        }

        /// <summary>
        /// Link a list of files into a resource assembly.
        /// </summary>
        /// <param name="resourceFiles">The collection of resources.</param>
        /// <param name="outputFile">Resource assembly to generate</param>
        /// <param name="culture">Culture of the generated assembly.</param>
        protected void LinkResourceAssembly(StringCollection resourceFiles, string outputFile, string culture) {
            // defer to the assembly linker task
            AssemblyLinkerTask alink = new AssemblyLinkerTask();

            // inherit project from current task
            alink.Project = this.Project;

            // inherit parent from current task
            alink.Parent = this.Parent;

            // make sure framework specific information is set
            alink.InitializeTaskConfiguration();

            // set task properties
            alink.Output = outputFile;
            alink.Culture = culture;
            alink.OutputTarget = "lib";
            alink.Template = Output;

            // add resource files
            foreach (string resourceFile in resourceFiles) {
                alink.Resources.FileNames.Add(resourceFile);
            }
            
            // fix up the indent level
            Project.Indent();
            
            // execute the nested task
            alink.Execute();

            // restore indent level
            Project.Unindent();
        }
        
        /// <summary>
        /// Compiles a resx files to a .resources file.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        protected void CompileResxResource(string inputFile, string outputFile) {
            ResGenTask resgen = new ResGenTask();
            resgen.Project = this.Project;
            resgen.Parent = this.Parent;

            // make sure framework specific information is set
            resgen.InitializeTaskConfiguration();
           
            // inherit Verbose setting from current task
            resgen.Verbose = this.Verbose;

            resgen.Input = inputFile;
            resgen.Output = Path.GetFileName(outputFile);
            resgen.ToDirectory = Path.GetDirectoryName(outputFile);
            resgen.BaseDirectory = Path.GetDirectoryName(inputFile);

            // fix up the indent level
            Project.Indent();

            // execute the task
            resgen.Execute();

            // restore the indent level
            Project.Unindent();
        }
        
        #endregion Protected Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Determines the culture associated with a given resource file by
        /// scanning the filename for valid culture names.
        /// </summary>
        /// <param name="resXFile">The resx file path to check for culture info.</param>
        /// <returns>
        /// A valid <see cref="CultureInfo" /> instance if the resource is 
        /// associated with a specific culture; otherwise, <see langword="null" />.
        /// </returns>
        public static CultureInfo GetResourceCulture(string resXFile) {
            string noextpath = Path.GetFileNameWithoutExtension(resXFile);
            int index = noextpath.LastIndexOf('.');
            if (index >= 0 && index <= noextpath.Length) {
                string possibleculture = noextpath.Substring(index + 1, noextpath.Length - (index + 1));
                // check that its in our list of culture names
                if (CultureNames.Contains(possibleculture)) {
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
