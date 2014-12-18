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

using NAnt.VisualCpp.Types;
using NAnt.VisualCpp.Util;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// Run <c>lib.exe</c>, Microsoft's Library Manager.
    /// </summary>
    /// <example>
    ///   <para>Create a library.</para>
    ///   <code>
    ///     <![CDATA[
    /// <lib output="library.lib">
    ///     <sources>
    ///         <include name="library.obj" />
    ///     </sources>
    /// </lib>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("lib")]
    public class LibTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private FileInfo _outputFile;
        private FileInfo _moduleDefinitionFile;
        private FileSet _sources = new FileSet();
        private SymbolCollection _symbols = new SymbolCollection();
        private LibraryCollection _ignoreLibraries = new LibraryCollection();
        private FileSet _libdirs = new FileSet();
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
        /// The output file.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The module definition file.
        /// </summary>
        [TaskAttribute("moduledefinition")]
        public FileInfo ModuleDefinitionFile {
            get { return _moduleDefinitionFile; }
            set { _moduleDefinitionFile = value; }
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
        /// Symbols to add to the symbol table.
        /// </summary>
        [BuildElementCollection("symbols", "symbol")]
        public SymbolCollection Symbols {
            get { return _symbols; }
            set { _symbols = value; }
        }

        /// <summary>
        /// Names of default libraries to ignore.
        /// </summary>
        [BuildElementCollection("ignorelibraries", "library")]
        public LibraryCollection IgnoreLibraries {
            get { return _ignoreLibraries; }
            set { _ignoreLibraries = value; }
        }

        /// <summary>
        /// The list of additional library directories to search.
        /// </summary>
        [BuildElement("libdirs")]
        public FileSet LibDirs {
            get { return _libdirs; }
            set { _libdirs = value; }
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
        /// Creates the library.
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

            if (!NeedsCompiling()) {
                return;
            }

            Log(Level.Info, "Combining {0} files to '{1}'.", 
                Sources.FileNames.Count, OutputFile.FullName);

            // Create temp response file to hold compiler options
            _responseFileName = Path.GetTempFileName();
            StreamWriter writer = new StreamWriter(_responseFileName);

            try {
                // specify the output file
                writer.WriteLine("/OUT:\"{0}\"", OutputFile.FullName);

                // write user provided options
                if (Options != null) {
                    writer.WriteLine(Options);
                }

                // write each of the filenames
                foreach (string filename in Sources.FileNames) {
                    writer.WriteLine(ArgumentUtils.QuoteArgumentValue(filename, 
                        BackslashProcessingMethod.None));
                }

                // write symbols
                foreach (Symbol symbol in Symbols) {
                    if (symbol.IfDefined && !symbol.UnlessDefined) {
                        writer.WriteLine("/INCLUDE:{0}", ArgumentUtils.QuoteArgumentValue(
                            symbol.SymbolName, BackslashProcessingMethod.Duplicate));
                    }
                }

                // names of default libraries to ignore
                foreach (Library ignoreLibrary in IgnoreLibraries) {
                    if (ignoreLibrary.IfDefined && !ignoreLibrary.UnlessDefined) {
                        writer.WriteLine("/NODEFAULTLIB:{0}", ArgumentUtils.QuoteArgumentValue(
                            ignoreLibrary.LibraryName, BackslashProcessingMethod.Duplicate));
                    }
                }

                // write each of the libdirs
                foreach (string libdir in LibDirs.DirectoryNames) {
                    writer.WriteLine("/LIBPATH:{0}", ArgumentUtils.QuoteArgumentValue(
                        libdir, BackslashProcessingMethod.None));
                }

                if (ModuleDefinitionFile != null) {
                    writer.WriteLine("/DEF:\"{0}\"", ModuleDefinitionFile.FullName);
                }

                // suppresses display of the sign-on banner                    
                writer.WriteLine("/nologo");

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

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Determines if the sources need to be linked.
        /// </summary>
        protected virtual bool NeedsCompiling() {
			// check if output file exists - if not, rebuild
			if (!OutputFile.Exists) {
				Log(Level.Verbose, "Output file '{0}' does not exist, rebuilding library.", 
					OutputFile.FullName);
				return true;
			}

			// check if .OBJ files were updated
			string fileName = FileSet.FindMoreRecentLastWriteTime(
                Sources.FileNames, OutputFile.LastWriteTime);
			if (fileName != null) {
				Log(Level.Verbose, "'{0}' has been updated, relinking.", fileName);
				return true;
			}

			return false;
        }

        #endregion Protected Instance Methods
    }
}
#if unused
Microsoft (R) Library Manager Version 7.00.9466
Copyright (C) Microsoft Corporation.  All rights reserved.

usage: LIB [options] [files]

   options:

      /DEF[:filename]
      /EXPORT:symbol
      /EXTRACT:membername
      /INCLUDE:symbol
      /LIBPATH:dir
      /LIST[:filename]
      /MACHINE:{AM33|ARM|IA64|M32R|MIPS|MIPS16|MIPSFPU|MIPSFPU16|MIPSR41XX|
                PPC|PPCFP|SH3|SH3DSP|SH4|SH5|THUMB|TRICORE|X86}
      /NAME:filename
      /NODEFAULTLIB[:library]
      /NOLOGO
      /OUT:filename
      /REMOVE:membername
      /SUBSYSTEM:{CONSOLE|EFI_APPLICATION|EFI_BOOT_SERVICE_DRIVER|
                  EFI_ROM|EFI_RUNTIME_DRIVER|NATIVE|POSIX|WINDOWS|
                  WINDOWSCE}[,#[.##]]
      /VERBOSE
#endif
