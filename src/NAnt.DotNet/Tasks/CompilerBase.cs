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
using System.IO;
using System.Text.RegularExpressions;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Provides the abstract base class for a compiler tasks.
    /// </summary>
    public abstract class CompilerBase : ExternalProgramBase {
        #region Private Instance Fields

        string _responseFileName;
        string _output = null;
        string _target = null;
        bool _debug = false;
        string _define = null;
        string _win32icon = null;
        bool _warnAsError = false;
        string _mainType = null;
        FileSet _references = new FileSet();
        ResourceFileSet _resources = new ResourceFileSet();
        FileSet _modules = new FileSet();
        FileSet _sources = new FileSet();
        ResGenTask _resgenTask = null;
        ResourceFileSet[] _resourcesList = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>Output directory for the compilation target.</summary>
        [TaskAttribute("output", Required=true)]
        public string Output        { get { return _output; } set { _output = value; }}

        /// <summary>Output type (<c>library</c> or <c>exe</c>).</summary>
        [TaskAttribute("target", Required=true)]
        public string OutputTarget  { get { return _target; } set { _target = value; }}

        /// <summary>Generate debug output (<c>true</c>/<c>false</c>).</summary>
        [BooleanValidator()]
        [TaskAttribute("debug")]
        public bool Debug           { get { return _debug; } set { _debug = value; }}

        /// <summary>Define conditional compilation symbol(s). Corresponds to <c>/d[efine]:</c> flag.</summary>
        [TaskAttribute("define")]
        public string Define        { get { return _define; } set { _define = value; }}

        /// <summary>Icon to associate with the application. Corresponds to <c>/win32icon:</c> flag.</summary>
        [TaskAttribute("win32icon")]
        public string Win32Icon     { get { return _win32icon; } set { _win32icon = value; }}

        /// <summary>
        /// Instructs the compiler to treat all warnings as errors (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c></summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/warnaserror[+|-]</c> flag of the compiler.
        /// </para>
        /// <para>
        /// When this attribute is set to <c>true</c>, any messages that would ordinarily be reported 
        /// as warnings will instead be reported as errors.
        /// </para>
        /// </remarks>
        [BooleanValidator()]
        [TaskAttribute("warnaserror")]
        public bool WarnAsError     { get { return _warnAsError; } set { _warnAsError = value; }}

        /// <summary>
        /// Specifies which type contains the Main method that you want to use as the entry point into 
        /// the program.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/m[ain]:</c> flag of the compiler.
        /// </para>
        /// <para>
        /// Use this attribute when creating an executable file. If this attribute is omitted, the 
        /// compiler searches for a valid Main in all public classes.
        /// </para>
        /// </remarks>
        [TaskAttribute("main")]
        public string MainType        { get { return _mainType; } set { _mainType = value; }}

        /// <summary>Reference metadata from the specified assembly files.</summary>
        [FileSet("references")]
        public FileSet References   { get { return _references; } set { _references = value; }}

        /// <summary>Set resources to embed.</summary>
        ///<remarks>This can be a combination of resx files and file resources. .resx files will be compiled by resgen and then embedded into the 
        ///resulting executable. The Prefix attribute is used to make up the resourcename added to the assembly manifest for non resx files. For resx files the namespace from the matching source file is used as the prefix. 
        ///This matches the behaviour of Visual Studio. Multiple resources tags with different namespace prefixes may be specified </remarks>    
        [BuildElementArray("resources")]
        public ResourceFileSet[]  ResourcesList { get { return _resourcesList; } set { _resourcesList = value; }}

        /// <summary>Link the specified modules into this assembly.</summary>
        [FileSet("modules")]
        public FileSet Modules      { get { return _modules; } set { _modules = value; }}

        /// <summary>The set of source files for compilation.</summary>
        [FileSet("sources")]
        public FileSet Sources { get { return _sources; } set { _sources = value; }}

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>The file extension required by the current compiler.</value>
        protected abstract string Extension {
            get;
        }

        /// <summary>
        /// Gets the complete output path.
        /// </summary>
        /// <value>The complete output path.</value>
        protected string OutputPath {
            get { return Path.GetFullPath(Path.Combine(BaseDirectory, Output)); }
        }

        #endregion Protected Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get { return ExeName; }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return "@" + _responseFileName; }
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

                    Log.WriteLine(LogPrefix + "Compiling {0} files to {1}", Sources.FileNames.Count, OutputPath);

                    // specific compiler options
                    WriteOptions(writer);

                    // Microsoft common compiler options
                    WriteOption(writer, "nologo");
                    WriteOption(writer, "target", OutputTarget);
                    if (Define != null) {
                        WriteOption(writer, "define", Define);
                    }

                    WriteOption(writer, "out", OutputPath);
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

                    //TODO: I changed the References.FileNames to References.Includes
                    //      Otherwise the system dlls never make it into the references.
                    //      Not too sure of the other implications of this change, but the
                    //      new Nant can run it's own build in addition to the VB6 stuff 
                    //      I've thrown at it.
                    // gs: This will not work when the user gives a reference in terms of a pattern.
                    // The problem is with the limitaions of the FileSet scanner.
                    // I've changed it back to .FileNames for now
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
                            if (prefix == null || prefix.Length == 0)
                                prefix = resources.Prefix;                        
                            string actualFileName = Path.GetFileNameWithoutExtension(fileName);
                            string tmpResourcePath = Path.ChangeExtension(fileName, "resources");
                            string manifestResourceName = Path.GetFileName(tmpResourcePath);
                            
                            // cater for asax/aspx special cases ...
                            if (manifestResourceName.IndexOf(".aspx") > -1){
                                manifestResourceName = manifestResourceName.Replace(".aspx", "");
                                actualFileName = actualFileName.Replace(".aspx", "");
                            }
                            else if (manifestResourceName.IndexOf(".asax") > -1){
                                manifestResourceName = manifestResourceName.Replace(".asax", "");
                                actualFileName = actualFileName.Replace(".asax", "");
                            }
                            else if (manifestResourceName.IndexOf(".ascx") > -1) {
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
                        Log.WriteLine(LogPrefix + "Contents of " + _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log.WriteLine(reader.ReadToEnd());
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

            FileInfo outputFileInfo = new FileInfo(OutputPath);
            if (!outputFileInfo.Exists) {
                return true;
            }

            //Sources Updated?
            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //References Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(References.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Modules Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(Modules.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Resources Updated?
            foreach (ResourceFileSet resources in ResourcesList) {
                
                fileName = FileSet.FindMoreRecentLastWriteTime(resources.FileNames, outputFileInfo.LastWriteTime);
                if (fileName != null) {
                    Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                    return true;
                }
            }
 
            // check the args for /res or /resource options.
            StringCollection resourceFileNames = new StringCollection();
            foreach (string arg in Args) {
                if (arg.StartsWith("/res:") || arg.StartsWith("/resource:")) {
                    string path = arg.Substring(arg.IndexOf(':') + 1);

                    int indexOfComma = path.IndexOf(',');
                    if (indexOfComma != -1) {
                        path = path.Substring(0, indexOfComma);
                    }
                    resourceFileNames.Add(path);
                }
            }
            fileName = FileSet.FindMoreRecentLastWriteTime(resourceFileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        /// <summary>
        /// Opens matching source file to find the correct namespace. This may 
        /// need to be overidden by the particular compiler if the namespace 
        /// syntax is different for that language.
        /// </summary>
        /// <param name="resxPath"></param>
        /// <returns></returns>
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
            } catch (FileNotFoundException) { // if no matching file, dump out
                return "";
            } finally {
                if(sr != null) {
                    sr.Close();
                }
            }
            return retnamespace;
        }

        /// <summary>
        /// Compiles the resx files to temp .resources files.
        /// </summary>
        /// <param name="resourceFileSet"></param>
        protected void CompileResxResources(FileSet resourceFileSet) {
            ResGenTask resgen = new ResGenTask(); 
            resgen.Resources = resourceFileSet; // set the fileset
            resgen.Verbose = false; 
            resgen.Parent = this.Parent;
            resgen.Project = this.Project;

            _resgenTask = resgen;

            // Fix up the indent level --
            Log.IndentLevel++;
            resgen.Execute();
            Log.IndentLevel--;
        }

        #endregion Protected Instance Methods
    }
}
