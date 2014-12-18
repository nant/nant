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
// Shawn Van Ness (nantluver@arithex.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian@maclean.ms)
// Eric V. Smith (ericsmith@windsor.com)
//
// TODO: review interface for future compatibility/customizations issues

using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.VisualCpp.Types;
using NAnt.VisualCpp.Util;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// Links files using <c>link.exe</c>, Microsoft's Incremental Linker.
    /// </summary>
    /// <remarks>
    ///   <para>This task is intended for version 7.00.9466 of <c>link.exe</c>.</para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Combine all object files in the current directory into <c>helloworld.exe</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <link output="helloworld.exe">
    ///     <sources>
    ///         <include name="*.obj" />
    ///     </sources>
    /// </link>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("link")]
    public class LinkTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private FileInfo _outputFile;
        private FileInfo _pdbFile;
        private FileInfo _moduleDefinition;
        private bool _debug;
        private FileSet _sources = new FileSet();
        private FileSet _libdirs = new FileSet();
        private FileSet _modules = new FileSet();
        private FileSet _delayLoadedDlls = new FileSet();
        private FileSet _embeddedResources = new FileSet();
        private SymbolCollection _symbols = new SymbolCollection();
        private LibraryCollection _ignoreLibraries = new LibraryCollection();
        private string _options;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Options to pass to the compiler.
        /// </summary>
        [TaskAttribute("options")]
        public string Options {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// Create debugging information for the .exe file or DLL. The default is
        /// <see langword="false" />.
        /// </summary>
        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        }

        /// <summary>
        /// The output file.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// A user-specified name for the program database (PDB) that the linker 
        /// creates. The default file name for the PDB has the base name of the 
        /// <see cref="OutputFile" /> and the extension .pdb.
        /// </summary>
        [TaskAttribute("pdbfile")]
        public FileInfo ProgramDatabaseFile {
            get { 
                if (Debug && _pdbFile == null) {
                    _pdbFile = new FileInfo(Path.ChangeExtension(OutputFile.FullName, ".pdb"));
                }
                return _pdbFile; 
            }
            set { _pdbFile = value; }
        }

        /// <summary>
        /// The name of a module-definition file (.def) to be passed to the
        /// linker.
        /// </summary>
        [TaskAttribute("moduledefinition")]
        public FileInfo ModuleDefinition {
            get { return _moduleDefinition; }
            set { _moduleDefinition = value; }
        }


        /// <summary>
        /// Specified DLLs for delay loading.
        /// </summary>
        [BuildElement("delayloaded")]
        public FileSet DelayLoadedDlls {
            get { return _delayLoadedDlls; }
            set { _delayLoadedDlls = value; }
        }

        /// <summary>
        /// The list of files to combine into the output file.
        /// </summary>
        [BuildElement("sources")]
        public FileSet Sources {
            get { return _sources; }
            set { _sources = value; }
        }

        /// <summary>
        /// The list of additional library directories to search.
        /// </summary>
        [BuildElement("libdirs")]
        public FileSet LibDirs {
            get { return _libdirs; }
            set { _libdirs = value; }
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
        /// Embed the specified resources into this assembly.
        /// </summary>
        [BuildElement("embeddedresources")]
        public FileSet EmbeddedResources {
            get { return _embeddedResources; }
            set { _embeddedResources = value; }
        }

        /// <summary>
        /// Symbols to add to the symbol table.
        /// </summary>
        [BuildElementCollection("symbols", "symbol")]
        public SymbolCollection Symbols {
            get { return _symbols; }
            set { _symbols = value; }
        }

        /// <summary>
        /// Names of libraries that you want the linker to ignore when it 
        /// resolves external references.
        /// </summary>
        [BuildElementCollection("ignorelibraries", "library")]
        public LibraryCollection IgnoreLibraries {
            get { return _ignoreLibraries; }
            set { _ignoreLibraries = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get { return Name; }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return "@" + "\"" + _responseFileName + "\""; }
        }

        #endregion Override implementation of ExternalProgramBase
        
        #region Override implementation of Task

        /// <summary>
        /// Links the sources.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Sources.BaseDirectory == null) {
                Sources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (LibDirs.BaseDirectory == null) {
                LibDirs.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (Modules.BaseDirectory == null) {
                Modules.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (EmbeddedResources.BaseDirectory == null) {
                EmbeddedResources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (NeedsLinking()) {
                Log(Level.Info, "Linking {0} files.", 
                    Sources.FileNames.Count);
  
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();

                StreamWriter writer = new StreamWriter(_responseFileName);
  
                try {
                    // specify the output file
                    writer.WriteLine("/OUT:\"{0}\"", OutputFile.FullName);

                    // write user provided options
                    if (Options != null) {
                        writer.WriteLine(Options);
                    }

                    // module definition file
                    if (ModuleDefinition != null) {
                        writer.WriteLine("/DEF:\"{0}\"", ModuleDefinition.FullName);
                    }
  
                    // write each of the libdirs
                    foreach (string libdir in LibDirs.DirectoryNames) {
                        writer.WriteLine("/LIBPATH:{0}", QuoteArgumentValue(libdir));
                    }

                    // write each of the module references
                    foreach (string module in Modules.FileNames) {
                        writer.WriteLine("/ASSEMBLYMODULE:{0}", QuoteArgumentValue(module));
                    }

                    // write delay loaded DLLs
                    foreach (string dll in DelayLoadedDlls.FileNames) {
                        writer.WriteLine("/DELAYLOAD:{0}", QuoteArgumentValue(dll));
                    }

                    // write each of the embedded resources
                    foreach (string resource in EmbeddedResources.FileNames) {
                        writer.WriteLine("/ASSEMBLYRESOURCE:{0}", QuoteArgumentValue(resource));
                    }

                    // write symbols
                    foreach (Symbol symbol in Symbols) {
                        if (symbol.IfDefined && !symbol.UnlessDefined) {
                            writer.WriteLine("/INCLUDE:{0}", QuoteArgumentValue(
                                symbol.SymbolName));
                        }
                    }

                    // names of default libraries to ignore
                    foreach (Library ignoreLibrary in IgnoreLibraries) {
                        if (ignoreLibrary.IfDefined && !ignoreLibrary.UnlessDefined) {
                            writer.WriteLine("/NODEFAULTLIB:{0}", QuoteArgumentValue(
                                ignoreLibrary.LibraryName));
                        }
                    }

                    if (Debug) {
                        writer.WriteLine("/DEBUG");
                    }

                    // write program database file
                    if (ProgramDatabaseFile != null) {
                        writer.WriteLine("/PDB:{0}", QuoteArgumentValue(
                            ProgramDatabaseFile.FullName));
                    }

                    // suppresses display of the sign-on banner
                    writer.WriteLine("/nologo");

                    // write each of the filenames
                    foreach (string filename in Sources.FileNames) {
                        writer.WriteLine(QuoteArgumentValue(filename));
                    }

                    writer.Close();
  
                    if (Verbose) {
                        // display response file contents
                        Log(Level.Info, "Contents of {0}.", _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Info, reader.ReadToEnd());
                        reader.Close();
                    }

                    // call base class to do the actual work
                    base.ExecuteTask();
                } finally {
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
                    File.Delete(_responseFileName);
                    _responseFileName = null;
                }
            }
            
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Determines if the output needs linking.
        /// </summary>
        protected virtual bool NeedsLinking() {
            string fileName;

            // return true as soon as we know we need to compile
            if (ProgramDatabaseFile != null) {
                if (!ProgramDatabaseFile.Exists) {
                    Log(Level.Verbose, "PDB file '{0}' does not exist, relinking.", 
                        ProgramDatabaseFile.FullName);
                    return true;
                }

                // check if sources were updated
                fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, ProgramDatabaseFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, "'{0}' has been updated, relinking.", fileName);
                    return true;
                }
            }

            if (!OutputFile.Exists) {
                Log(Level.Verbose, "Output file '{0}' does not exist, relinking.", 
                    OutputFile.FullName);
                return true;
            }

            // check if sources were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, relinking.", fileName);
                return true;
            }

            if (ModuleDefinition != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(ModuleDefinition.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            return false;
        }

        #endregion Protected Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Quotes an argument value and duplicates trailing backslahes.
        /// </summary>
        /// <param name="value">The argument value to quote.</param>
        /// <returns>
        /// The quotes argument value.
        /// </returns>
        public static string QuoteArgumentValue(string value) {
            return ArgumentUtils.QuoteArgumentValue(value, 
                BackslashProcessingMethod.Duplicate);
        }

        #endregion Public Static Methods
    }
}
#if unused
Microsoft (R) Incremental Linker Version 7.00.9466
Copyright (C) Microsoft Corporation.  All rights reserved.

 usage: LINK [options] [files] [@commandfile]

   options:

      /ALIGN:#
      /ALLOWBIND[:NO]
      /ASSEMBLYMODULE:filename
      /ASSEMBLYRESOURCE:filename
      /BASE:{address|@filename,key}
      /DEBUG
      /DEF:filename
      /DEFAULTLIB:library
      /DELAY:{NOBIND|UNLOAD}
      /DELAYLOAD:dll
      /DLL
      /DRIVER[:{UPONLY|WDM}]
      /ENTRY:symbol
      /EXETYPE:DYNAMIC
      /EXPORT:symbol
      /FIXED[:NO]
      /FORCE[:{MULTIPLE|UNRESOLVED}]
      /HEAP:reserve[,commit]
      /IDLOUT:filename
      /IGNOREIDL
      /IMPLIB:filename
      /INCLUDE:symbol
      /INCREMENTAL[:NO]
      /LARGEADDRESSAWARE[:NO]
      /LIBPATH:dir
      /LTCG[:{NOSTATUS|PGINSTRUMENT|PGOPTIMIZE|STATUS}]
             (PGINSTRUMENT and PGOPTIMIZE are only available for IA64)
      /MACHINE:{AM33|ARM|IA64|M32R|MIPS|MIPS16|MIPSFPU|MIPSFPU16|MIPSR41XX|
                PPC|PPCFP|SH3|SH3DSP|SH4|SH5|THUMB|TRICORE|X86}
      /MAP[:filename]
      /MAPINFO:{EXPORTS|LINES}
      /MERGE:from=to
      /MIDL:@commandfile
      /NOASSEMBLY
      /NODEFAULTLIB[:library]
      /NOENTRY
      /NOLOGO
      /OPT:{ICF[=iterations]|NOICF|NOREF|NOWIN98|REF|WIN98}
      /ORDER:@filename
      /OUT:filename
      /PDB:filename
      /PDBSTRIPPED:filename
      /PGD:filename
      /RELEASE
      /SECTION:name,[E][R][W][S][D][K][L][P][X][,ALIGN=#]
      /STACK:reserve[,commit]
      /STUB:filename
      /SUBSYSTEM:{CONSOLE|EFI_APPLICATION|EFI_BOOT_SERVICE_DRIVER|
                  EFI_ROM|EFI_RUNTIME_DRIVER|NATIVE|POSIX|WINDOWS|
                  WINDOWSCE}[,#[.##]]
      /SWAPRUN:{CD|NET}
      /TLBOUT:filename
      /TSAWARE[:NO]
      /TLBID:#
      /VERBOSE[:LIB]
      /VERSION:#[.#]
      /VXD
      /WINDOWSCE:{CONVERT|EMULATION}
      /WS:AGGRESSIVE
#endif
