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
// Anthony LoveFrancisco (ants@fu.org)
// Hani Atassi (haniatassi@users.sourceforge.net)
//
// TODO: review interface for future compatibility/customizations issues

using System;
using System.Globalization;
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
    /// Compiles C/C++ programs using <c>cl.exe</c>, Microsoft's C/C++ compiler.
    /// </summary>
    /// <remarks>
    ///   <para>This task is intended for version 13.00.9466 of <c>cl.exe</c>.</para>
    /// </remarks>
    /// <example>
    ///   <para>Compiles <c>helloworld.cpp</c> for the Common Language Runtime.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cl outputdir="build" options="/clr">
    ///     <sources>
    ///         <include name="helloworld.cpp" />
    ///     </sources>
    /// </cl>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cl")]
    public class ClTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private DirectoryInfo _outputDir;
        private string _pchFile;
        private PrecompiledHeaderMode _precompileHeaderMode = PrecompiledHeaderMode.Use;
        private string _pchThroughFile;
        private FileSet _sources = new FileSet();
        private FileSet _includeDirs = new FileSet();
        private FileSet _metaDataIncludeDirs = new FileSet();
        private FileSet _forcedUsingFiles = new FileSet();
        private bool _managedExtensions;
        private CharacterSet _characterSet = CharacterSet.NotSet;
        private string _options;
        private OptionCollection _defines = new OptionCollection();
        private OptionCollection _undefines = new OptionCollection();
        private string _objectFile;
        private string _pdbFile;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Directory where all output files are placed.
        /// </summary>
        [TaskAttribute("outputdir", Required=true)]
        public DirectoryInfo OutputDir {
            get { return _outputDir; }
            set { _outputDir = value; }
        }

        /// <summary>
        /// Specifies the path and/or name of the generated precompiled header 
        /// file - given either relative to <see cref="OutputDir" /> or as an 
        /// absolute path. 
        /// </summary>
        [TaskAttribute("pchfile")]
        public string PchFile {
            get { return (_pchFile != null) ? Path.Combine(OutputDir.FullName, _pchFile) : null; }
            set { _pchFile = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The path of the boundary file when generating/using the 
        /// specified <see cref="PchFile" />.  If a precompiled header file is
        /// not specified then this attribute is ignored.
        /// </summary>
        [TaskAttribute("pchthroughfile")]
        public string PchThroughFile {
            get { return _pchThroughFile; }
            set { _pchThroughFile = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The mode in which the specified <see cref="PchFile" /> (if any) is
        /// used. The default is <see cref="PrecompiledHeaderMode.Use" />.
        /// </summary>
        [TaskAttribute("pchmode")]
        public PrecompiledHeaderMode PchMode {
            get { return _precompileHeaderMode; }
            set { 
                if (!Enum.IsDefined(typeof(PrecompiledHeaderMode), value)) {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, 
                        "An invalid type {0} was specified.", value)); 
                } else {
                    _precompileHeaderMode = value;
                }
            }
        }


        /// <summary>
        /// Specifies whether Managed Extensions for C++ should be enabled.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("managedextensions")]
        [BooleanValidator()]
        public bool ManagedExtensions {
            get { return _managedExtensions; }
            set { _managedExtensions = value;}
        }

        /// <summary>
        /// Tells the compiler to use the specified character set.
        /// </summary>
        [TaskAttribute("characterset", Required=false)]
        public CharacterSet CharacterSet {
            get { return _characterSet; }
            set {
                if (!Enum.IsDefined(typeof(CharacterSet), value)) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, 
                        "An invalid character set '{0}' was specified.", value)); 
                } else {
                    this._characterSet = value;
                }
            }
        }

        /// <summary>
        /// Options to pass to the compiler.
        /// </summary>
        [TaskAttribute("options")]
        public string Options {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// The list of files to compile.
        /// </summary>
        [BuildElement("sources")]
        public FileSet Sources {
            get { return _sources; }
            set { _sources = value; }
        }

        /// <summary>
        /// The list of directories in which to search for include files.
        /// </summary>
        [BuildElement("includedirs")]
        public FileSet IncludeDirs {
            get { return _includeDirs; }
            set { _includeDirs = value; }
        }

        /// <summary>
        /// Directories that the compiler will search to resolve file references 
        /// passed to the <c>#using</c> directive.
        /// </summary>
        [BuildElement("metadataincludedirs")]
        public FileSet MetaDataIncludeDirs {
            get { return _metaDataIncludeDirs; }
            set { _metaDataIncludeDirs = value; }
        }

        /// <summary>
        /// Specifies metadata files to reference in this compilation as an
        /// alternative to passing a file name to <c>#using</c> in source code.
        /// </summary>
        [BuildElement("forcedusingfiles")]
        public FileSet ForcedUsingFiles {
            get { return _forcedUsingFiles; }
            set { _forcedUsingFiles = value; }
        }

        /// <summary>
        /// Macro definitions to pass to cl.exe.
        /// Each entry will generate a /D
        /// </summary>
        [BuildElementCollection("defines", "define")]
        public OptionCollection Defines {
            get { return _defines; }
        }

        /// <summary>
        /// Macro undefines (/U) to pass to cl.exe.
        /// </summary>
        [BuildElementCollection("undefines", "undefine")]
        public OptionCollection Undefines {
            get { return _undefines; }
        }

        /// <summary>
        /// A name to override the default object file name; can be either a file
        /// or directory name. The default is the output directory.
        /// </summary>
        [TaskAttribute("objectfile")]
        public string ObjectFile {
            get { return (_objectFile != null ? Path.Combine(OutputDir.FullName, _objectFile) : OutputDir.FullName + "/"); }
            set { _objectFile = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// A name for the compiler-generated PDB file; can be either a file or 
        /// directory name. The default is the output directory.
        /// </summary>
        [TaskAttribute("pdbfile")]
        public string ProgramDatabaseFile {
            get { return (_pdbFile != null ? Path.Combine(OutputDir.FullName, _pdbFile) : OutputDir.FullName + "/"); }
            set { _pdbFile = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {get {return Name;}}

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

        /// <summary>
        /// Determines whether the precompiled header file is up-to-date.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if no precompiled header file was specified;
        /// otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// In order to determine accurately whether the precompile header file
        /// is up-to-date, we'd need scan all the header files that are pulled 
        /// in. As this is not implemented right now, its safer to always
        /// recompile.
        /// </remarks>
        private bool IsPchfileUpToDate() {
            // if no pch file, then then it is theoretically up to date
            if (PchFile == null) {
                return true;
            }

            // if pch file declared, but doesn't exist, it must be stale
            FileInfo pchFileInfo = new FileInfo(PchFile);
            if (!pchFileInfo.Exists) {
                Log(Level.Verbose, "'{0}' does not exist, recompiling.", 
                    pchFileInfo.FullName);
                return false;
            }

            // if sources fresher than pch file,
            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, pchFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' is newer than pch file, recompiling.", 
                    fileName);
                return false;
            }

            // for now, always return false, thereby forcing recompile
            return false;
        }

        private bool IsObjUpToDate(string srcFileName) {
            // if obj file doesn't exist, it must be stale
            string objFileName = Path.ChangeExtension(Path.Combine(OutputDir.FullName, 
                Path.GetFileName(srcFileName)), ".obj");
            FileInfo objFileInfo = new FileInfo(objFileName);
            if (!objFileInfo.Exists) {
                Log(Level.Verbose, "'{0}' does not exist, recompiling.", 
                    objFileName);
                return false;
            }

            // if obj file is older the source file, it is stale
            string fileName = FileSet.FindMoreRecentLastWriteTime(srcFileName, objFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' is out of date, recompiling.", 
                    fileName);
                return false;
            }

            return true;
        }

        private bool AreObjsUpToDate() {
            foreach(string filename in Sources.FileNames) {
                if (!IsObjUpToDate(filename)) {
                    return false;
                }
            }

            return true;
        }

        private bool AreSourcesUpToDate() {
            //$ TODO:   Add code here that parses the C/CPP files to determine
            //          file dependencies.
            //$ TODO:   To prevent perpetual reparsing, need to figure out
            //          a place to cache the results of the dependency checks
            //          and do test to validate the cache.
            return true;
        }

        /// <summary>
        /// Determines if the sources need compiling.
        /// </summary>
        protected virtual bool NeedsCompiling() {
            return !(IsPchfileUpToDate() && AreObjsUpToDate() && AreSourcesUpToDate());
        }

        #region Override implementation of Task

        /// <summary>
        /// Compiles the sources.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Sources.BaseDirectory == null) {
                Sources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (IncludeDirs.BaseDirectory == null) {
                IncludeDirs.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (MetaDataIncludeDirs.BaseDirectory == null) {
                MetaDataIncludeDirs.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (ForcedUsingFiles.BaseDirectory == null) {
                ForcedUsingFiles.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (NeedsCompiling()) {
                Log(Level.Info, "Compiling {0} files to '{1}'.", 
                    Sources.FileNames.Count, OutputDir.FullName);
 
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);
 
                try {
                    // write basic switches
                    writer.WriteLine("/c"); // compile only

                    if (Options != null) {
                        // write user defined options
                        writer.WriteLine(Options);
                    }

                    if (ManagedExtensions) {
                        // enables Managed Extensions for C++
                        writer.WriteLine("/clr");
                    }

                    // write preprocesser define(s)
                    foreach (Option define in _defines) {
                        if (define.Value == null)  {
                            writer.WriteLine("/D " + QuoteArgumentValue(define.OptionName));
                        } else {
                            writer.WriteLine("/D " + QuoteArgumentValue(define.OptionName 
                                + "=" + ArgumentUtils.DuplicateTrailingBackslash(define.Value)));
                        }
                    }

                    // write preprocesser undefine(s)
                    foreach (Option undefine in _undefines) {
                        writer.WriteLine("/U " + QuoteArgumentValue(undefine.OptionName));
                    }

                    // write user provided include directories
                    foreach (string include in IncludeDirs.DirectoryNames) {
                        writer.WriteLine("/I {0}", QuoteArgumentValue(include));
                    }

                    // write directories that the compiler will search to resolve 
                    // file references  passed to the #using directive
                    foreach (string metaDataIncludeDir in MetaDataIncludeDirs.DirectoryNames) {
                        writer.WriteLine("/AI {0}", QuoteArgumentValue(metaDataIncludeDir));
                    }

                    // writes metadata files to reference in this compilation 
                    // as an alternative to passing a file name to #using in 
                    // source code
                    foreach (string forcedUsingFile in ForcedUsingFiles.FileNames) {
                        writer.WriteLine("/FU {0}", QuoteArgumentValue(forcedUsingFile));
                    }

                    // program database file
                    writer.WriteLine("/Fd{0}", QuoteArgumentValue(ProgramDatabaseFile));

                    // the object file
                    writer.WriteLine("/Fo{0}", QuoteArgumentValue(ObjectFile));

                    // specify pch file, if user specified one
                    if (PchFile != null) {
                        writer.WriteLine("/Fp{0}", QuoteArgumentValue(PchFile));

                        switch (PchMode) {
                            case PrecompiledHeaderMode.Use:
                                writer.Write("/Yu");
                                break;
                            case PrecompiledHeaderMode.Create:
                                writer.Write("/Yc");
                                break;
                            case PrecompiledHeaderMode.AutoCreate:
                                writer.Write("/YX");
                                break;
                        }

                        if (PchThroughFile != null) {
                            writer.WriteLine("{0}", QuoteArgumentValue(PchThroughFile));
                        }
                    }

                    // write each of the filenames
                    foreach (string filename in Sources.FileNames) {
                        writer.WriteLine(QuoteArgumentValue(filename));
                    }

                    // tell compiler which character set to use
                    switch (CharacterSet) {
                        case CharacterSet.Unicode:
                            writer.WriteLine("/D \"_UNICODE\"");
                            writer.WriteLine("/D \"UNICODE\"");
                            break;
                        case CharacterSet.MultiByte:
                            writer.WriteLine("/D \"_MBCS\"");
                            break;
                    }

                    writer.Close();

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Info, "Contents of {0}.", _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Info, reader.ReadToEnd());
                        reader.Close();
                    }

                    // suppresses display of the sign-on banner
                    // (this has no effect in response file)
                    this.Arguments.Add(new Argument("/nologo"));

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

        /// <summary>
        /// Defines the supported modes for the use of precompiled header files.
        /// </summary>
        public enum PrecompiledHeaderMode : int {
            /// <summary>
            /// Create a precompiled header file.
            /// </summary>
            /// <remarks>
            /// For further information on the use of this option
            /// see the Microsoft documentation on the C++ compiler flag /Yc.
            /// </remarks>
            Create = 1,

            /// <summary>
            /// Automatically create a precompiled header file if necessary.
            /// </summary>
            /// <remarks>
            /// For further information on the use of this option
            /// see the Microsoft documentation on the C++ compiler flag /YX.
            /// </remarks>
            AutoCreate = 2,

            /// <summary>
            /// Use a (previously generated) precompiled header file.
            /// </summary>
            /// <remarks>
            /// For further information on the use of this option
            /// see the Microsoft documentation on the C++ compiler flag /Yu.
            /// </remarks>
            Use = 0
        }
    }
}
#if unused
Microsoft (R) 32-bit C/C++ Optimizing Compiler Version 13.00.9466 for 80x86
Copyright (C) Microsoft Corporation 1984-2001. All rights reserved.

C/C++ COMPILER OPTIONS

-OPTIMIZATION-

/O1 minimize space                       /Op[-] improve floating-pt consistency
/O2 maximize speed                       /Os favor code space
/Oa assume no aliasing                   /Ot favor code speed
/Ob<n> inline expansion (default n=0)    /Ow assume cross-function aliasing
/Od disable optimizations (default)      /Ox maximum opts. (/Ogityb2 /Gs)
/Og enable global optimization           /Oy[-] enable frame pointer omission
/Oi enable intrinsic functions

-CODE GENERATION-

/G3 optimize for 80386                   /GH enable _pexit function call
/G4 optimize for 80486                   /GR[-] enable C++ RTTI
/G5 optimize for Pentium                 /GX[-] enable C++ EH (same as /EHsc)
/G6 optimize for PPro, P-II, P-III       /EHs enable C++ EH (no SEH exceptions)
/GB optimize for blended model (default) /EHa enable C++ EH (w/ SEH exceptions)
/Gd __cdecl calling convention           /EHc extern "C" defaults to nothrow
/Gr __fastcall calling convention        /GT generate fiber-safe TLS accesses
/Gz __stdcall calling convention         /Gm[-] enable minimal rebuild
/GA optimize for Windows Application     /GL[-] enable link-time code generation
/Gf enable string pooling                /QIfdiv[-] enable Pentium FDIV fix
/GF enable read-only string pooling      /QI0f[-] enable Pentium 0x0f fix
/Gy separate functions for linker        /QIfist[-] use FIST instead of ftol()
/GZ Enable stack checks (/RTCs)          /RTC1 Enable fast checks (/RTCsu)
/Ge force stack checking for all funcs   /RTCc Convert to smaller type checks
/Gs[num] control stack checking calls    /RTCs Stack Frame runtime checking
/GS enable security checks               /RTCu Uninitialized local usage checks
/Gh enable _penter function call
/clr[:noAssembly] compile for the common language runtime
noAssembly - do not produce an assembly

-OUTPUT FILES-

/Fa[file] name assembly listing file     /Fo<file> name object file
/FA[sc] configure assembly listing       /Fp<file> name precompiled header file
/Fd[file] name .PDB file                 /Fr[file] name source browser file
/Fe<file> name executable file           /FR[file] name extended .SBR file
/Fm[file] name map file

-PREPROCESSOR-

/AI<dir> add to assembly search path     /Fx merge injected code to file
/FU<file> forced using assembly/module   /FI<file> name forced include file
/C don't strip comments                  /U<name> remove predefined macro
/D<name>{=|#}<text> define macro         /u remove all predefined macros
/E preprocess to stdout                  /I<dir> add to include search path
/EP preprocess to stdout, no #line       /X ignore "standard places"
/P preprocess to file

-LANGUAGE-

/Zi enable debugging information         /Zl omit default library name in .OBJ
/ZI enable Edit and Continue debug info  /Zg generate function prototypes
/Z7 enable old-style debug info          /Zs syntax check only
/Zd line number debugging info only      /vd{0|1} disable/enable vtordisp
/Zp[n] pack structs on n-byte boundary   /vm<x> type of pointers to members
/Za disable extensions (implies /Op)     /noBool disable "bool" keyword
/Ze enable extensions (default)
/Zc:arg1[,arg2] C++ language conformance, where arguments can be:
forScope - enforce Standard C++ for scoping rules
wchar_t - wchar_t is the native type, not a typedef

-MISCELLANEOUS-

@<file> options response file            /wo<n> issue warning n once
/?, /help print this help message        /w<l><n> set warning level 1-4 for n
/c compile only, no link                 /W<n> set warning level (default n=1)
/H<num> max external name length         /Wall enable all warnings
/J default char type is unsigned         /Wp64 enable 64 bit porting warnings
/nologo suppress copyright message       /WX treat warnings as errors
/showIncludes show include file names    /WL enable one line diagnostics
/Tc<source file> compile file as .c      /Yc[file] create .PCH file
/Tp<source file> compile file as .cpp    /Yd put debug info in every .OBJ
/TC compile all files as .c              /Yl[sym] inject .PCH ref for debug lib
/TP compile all files as .cpp            /Yu[file] use .PCH file
/V<string> set version string            /YX[file] automatic .PCH
/w disable all warnings                  /Y- disable all PCH options
/wd<n> disable warning n                 /Zm<n> max memory alloc (% of default)
/we<n> treat warning n as an error

                                 -LINKING-

/MD link with MSVCRT.LIB                 /MDd link with MSVCRTD.LIB debug lib
/ML link with LIBC.LIB                   /MLd link with LIBCD.LIB debug lib
/MT link with LIBCMT.LIB                 /MTd link with LIBCMTD.LIB debug lib
/LD Create .DLL                          /F<num> set stack size
/LDd Create .DLL debug library           /link [linker options and libraries]

#endif
