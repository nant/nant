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

using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
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
        private ResourceFileSet _resources = new ResourceFileSet();
        private FileSet _modules = new FileSet();
        private FileSet _sources = new FileSet();
        private ResGenTask _resgenTask = null;
        private ResourceFileSetCollection _resourcesList = new ResourceFileSetCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file created by the compiler. 
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { _output = SetStringValue(value); }
        }

        /// <summary>
        /// Output type. Possible values are <c>exe</c>, <c>winexe</c>, 
        /// <c>library</c> or <c>module</c>.
        /// </summary>
        [TaskAttribute("target", Required=true)]
        public string OutputTarget  {
            get { return _target; }
            set { _target = SetStringValue(value); }
        }

        /// <summary>
        /// Generate debug output. Default is <c>false</c>.
        /// </summary>
        [BooleanValidator()]
        [TaskAttribute("debug")]
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
            set { _define = SetStringValue(value); }
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
            set { _win32icon = SetStringValue(value); }
        }

        /// <summary>
        /// Instructs the compiler to treat all warnings as errors. Default is 
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/warnaserror[+|-]</c> flag of the compiler.
        /// </para>
        /// <para>
        /// When this property is set to <c>true</c>, any messages that would 
        /// ordinarily be reported as warnings will instead be reported as 
        /// errors.
        /// </para>
        /// </remarks>
        [BooleanValidator()]
        [TaskAttribute("warnaserror")]
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
        /// is set to <c>false</c>, the compiler searches for a valid Main method 
        /// in all public classes.
        /// </para>
        /// </remarks>
        [TaskAttribute("main")]
        public string MainType {
            get { return _mainType; }
            set { _mainType = SetStringValue(value); }
        }

        /// <summary>
        /// Reference metadata from the specified assembly files.
        /// </summary>
        [FileSet("references")]
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
        [FileSet("modules")]
        public FileSet Modules {
            get { return _modules; }
            set { _modules = value; }
        }

        /// <summary>
        /// The set of source files for compilation.
        /// </summary>
        [FileSet("sources")]
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

                    // Writes the option that specifies the class containing the Main method that should 
                    // be called when the program starts.
                    if (this.MainType != null) {
                        WriteOption(writer, "main", this.MainType);
                    }

                    // Writes the option that specifies whether the compiler should consider warnings
                    // as errors.
                    if (this.WarnAsError) {
                        WriteOption(writer, "warnaserror");
                    }

                    foreach (string fileName in References.FileNames) {
                        WriteOption(writer, "reference", fileName);
                    }

                    foreach (string fileName in Modules.FileNames) {
                        WriteOption(writer, "addmodule", fileName);
                    }

                    // compile resources
                    foreach (ResourceFileSet resources in ResourcesList) {
                        if(resources.ResxFiles.FileNames.Count > 0) {
                            CompileResxResources(resources.ResxFiles);
                        }

                        // Resx args
                        foreach (string fileName in resources.ResxFiles.FileNames) {
                            string prefix = GetFormNamespace(fileName); // try and get it from matching form
                            if (prefix == null || prefix.Length == 0) {
                                prefix = resources.Prefix;
                            }
                            string actualFileName = Path.GetFileNameWithoutExtension(fileName);
                            string tmpResourcePath = Path.ChangeExtension(fileName, "resources");
                            string manifestResourceName = Path.GetFileName(tmpResourcePath);
                            
                            // cater for asax/aspx special cases ...
                            if (manifestResourceName.IndexOf(".aspx") > -1){
                                manifestResourceName = manifestResourceName.Replace(".aspx", "");
                                actualFileName = actualFileName.Replace(".aspx", "");
                            } else if (manifestResourceName.IndexOf(".asax") > -1){
                                manifestResourceName = manifestResourceName.Replace(".asax", "");
                                actualFileName = actualFileName.Replace(".asax", "");
                            } else if (manifestResourceName.IndexOf(".ascx") > -1) {
                                manifestResourceName = manifestResourceName.Replace(".ascx", "");
                                actualFileName = actualFileName.Replace(".ascx", "");
                            }
                            if(prefix != null && prefix.Length != 0) {
                                manifestResourceName = manifestResourceName.Replace(actualFileName, prefix + "." + actualFileName);
                            }
                            string resourceoption = tmpResourcePath + "," + manifestResourceName;
                            WriteOption(writer, "resource", resourceoption);
                        }

                        // other resources
                        foreach (string fileName in resources.NonResxFiles.FileNames) {
                            string resourceoption = fileName + "," + resources.GetManifestResourceName(fileName);
                            WriteOption(writer, "resource", resourceoption);
                        }
                    }

                    foreach (string fileName in Sources.FileNames) {
                        writer.WriteLine("\"" + fileName + "\"");
                    }

                    // Make sure to close the response file otherwise contents
                    // will not be written to disc and EXecuteTask() will fail.
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

                    // clean up generated resources.
                    if (_resgenTask != null) {
                        _resgenTask.RemoveOutputs();
                    }
                } finally {
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
                    File.Delete(_responseFileName);
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods
        
        /// <summary>Allows derived classes to provide compiler-specific options.</summary>
        protected virtual void WriteOptions(TextWriter writer) {
        }

        /// <summary>
        /// Writes an option using the default output format.
        /// </summary>
        protected virtual void WriteOption(TextWriter writer, string name) {
            writer.WriteLine("/{0}", name);
        }

        /// <summary>
        /// Writes an option and its value using the default output format.
        /// </summary>
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
        /// <param name="resxPath"></param>
        /// <returns>
        /// The namespace of the source file matching the resource, or 
        /// <see langword="null" /> if there's no matching source file.
        /// </returns>
        /// <remarks>
        /// This behaviour may need to be overidden by a particular compiler 
        /// if the namespace syntax is different for that language.
        /// </remarks>
        protected virtual string GetFormNamespace(string resxPath){
            string retnamespace = "";
            StreamReader sr = null;
            
            // open matching source file if it exists
            string sourceFile = resxPath.Replace("resx", Extension);
        
            try {
                sr = File.OpenText(sourceFile);
             
                while (sr.Peek() > -1) {
                    string str = sr.ReadLine();
                    string matchnamespace =  @"namespace ((\w+.)*)";
                    string matchnamespaceCaps =  @"Namespace ((\w+.)*)";
                    Regex matchNamespaceRE = new Regex(matchnamespace);
                    Regex matchNamespaceCapsRE = new Regex(matchnamespaceCaps);
                    
                    if (matchNamespaceRE.Match(str).Success){
                        Match namematch = matchNamespaceRE.Match(str);
                        retnamespace = namematch.Groups[1].Value; 
                        retnamespace = retnamespace.Replace("{", "");
                        retnamespace = retnamespace.Trim();
                        break;
                    } else if (matchNamespaceCapsRE.Match(str).Success) {
                        Match namematch = matchNamespaceCapsRE.Match(str);
                        retnamespace = namematch.Groups[1].Value;
                        retnamespace = retnamespace.Trim();
                        break;
                    }
                }
                return retnamespace;
            } catch (FileNotFoundException) {
                // if no matching file, dump out
                return null;
            } finally {
                if(sr != null) {
                    sr.Close();
                }
            }
        }

        /// <summary>
        /// Compiles the resx files to temp .resources files.
        /// </summary>
        /// <param name="resourceFileSet"></param>
        protected void CompileResxResources(FileSet resourceFileSet) {
            ResGenTask resgen = new ResGenTask(); 
            resgen.Project = this.Project;
            resgen.Parent = this.Parent;

            // temporary hack to force configuration settings to be 
            // read from NAnt configuration file
            //
            // TO-DO : Remove this temporary hack when a permanent solution is 
            // available for loading the default values from the configuration
            // file if a build element is constructed from code.
            resgen.InitializeTaskConfiguration();

            // set the fileset
            resgen.Resources = resourceFileSet; 
            // inherit Verbose setting from current task
            resgen.Verbose = this.Verbose; 

            _resgenTask = resgen;

            // Fix up the indent level --
            Project.Indent();
            resgen.Execute();
            Project.Unindent();
        }

        #endregion Protected Instance Methods
    }
}
