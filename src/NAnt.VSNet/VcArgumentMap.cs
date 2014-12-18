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
// Dmitry Jemerov <yole@yole.ru>
// Hani Atassi (haniatassi@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using NAnt.VisualCpp.Tasks;
using NAnt.VisualCpp.Util;

namespace NAnt.VSNet {

    /// <summary>
    /// A mapping from properties in the .vcproj file to command line arguments.
    /// </summary>
    public class VcArgumentMap {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VcArgumentMap" />
        /// class.
        /// </summary>
        public VcArgumentMap() {
            _htArgs = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        public void AddString(string propName, string argName) {
            AddString(ArgGroup.Unassigned, propName, argName);
        }
        public void AddString(string propName, string argName, bool ignoreEmptyValue) {
            AddString(ArgGroup.Unassigned, propName, argName, ignoreEmptyValue);
        }
        public void AddString(ArgGroup group, string propName, string argName) {
            _htArgs [propName] = new VcStringArgument(group, argName);
        }
        public void AddString(ArgGroup group, string propName, string argName, bool ignoreEmptyValue) {
            _htArgs [propName] = new VcStringArgument(group, argName, ignoreEmptyValue);
        }

        public void AddLinkerString(string propName, string argName) {
            AddLinkerString(ArgGroup.Unassigned, propName, argName);
        }
        public void AddLinkerString(string propName, string argName, bool ignoreEmptyValue) {
            AddLinkerString(ArgGroup.Unassigned, propName, argName, ignoreEmptyValue);
        }
        public void AddLinkerString(ArgGroup group, string propName, string argName) {
            _htArgs [propName] = new LinkerStringArgument(group, argName);
        }
        public void AddLinkerString(ArgGroup group, string propName, string argName, bool ignoreEmptyValue) {
            _htArgs [propName] = new LinkerStringArgument(group, argName, ignoreEmptyValue);
        }

        public void AddQuotedLinkerString(string propName, string argName) {
            AddQuotedLinkerString(ArgGroup.Unassigned, propName, argName);
        }
        public void AddQuotedLinkerString(string propName, string argName, bool ignoreEmptyValue) {
            AddQuotedLinkerString(ArgGroup.Unassigned, propName, argName, ignoreEmptyValue);
        }
        public void AddQuotedLinkerString(ArgGroup group, string propName, string argName) {
            _htArgs [propName] = new QuotedLinkerStringArgument(group, argName);
        }
        public void AddQuotedLinkerString(ArgGroup group, string propName, string argName, bool ignoreEmptyValue) {
            _htArgs [propName] = new QuotedLinkerStringArgument(group, argName, ignoreEmptyValue);
        }

        public void AddBool(string propName, string argName) {
            AddBool(ArgGroup.Unassigned, propName, argName);
        }
        public void AddBool(string propName, string argName, string match) {
            AddBool(ArgGroup.Unassigned, propName, argName, match);
        }
        public void AddBool(ArgGroup group, string propName, string argName) {
            _htArgs [propName] = new VcBoolArgument(group, argName);
        }
        public void AddBool(ArgGroup group, string propName, string argName, string match) {
            _htArgs [propName] = new VcBoolArgument(group, argName, match);
        }

        public void AddEnum(string propName, string argName, params string[] values) {
            AddEnum(ArgGroup.Unassigned, propName, argName, values);
        }
        public void AddEnum(ArgGroup group, string propName, string argName, params string[] values) {
            _htArgs [propName] = new VcEnumArgument(group, argName, values);
        }

        /// <summary>
        /// Gets the argument string corresponding with a configuration property 
        /// named <paramref name="propName" /> with value <paramref name="propValue" />.
        /// An ignore mask can be used to eliminate some arguments from the search.
        /// </summary>
        /// <param name="propName">The name of the configuration property.</param>
        /// <param name="propValue">The value of the configuration property.</param>
        /// <param name="useIgnoreGroup">Specify any groups that needs to be ignored.</param>
        /// <returns>
        /// The argument string corresponding with a configuration property 
        /// named <paramref name="propName" /> with value <paramref name="propValue" />,
        /// or <see langword="null" /> if no corresponding argument exists.
        /// </returns>
        public string GetArgument(string propName, string propValue, ArgGroup useIgnoreGroup) {
            VcArgument arg = (VcArgument) _htArgs [propName];
            if (arg == null) {
                return null;
            }
            if (arg.Group != ArgGroup.Unassigned && (arg.Group & useIgnoreGroup) != 0) {
                return null;
            }
            return arg.MapValue(propValue);
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Creates a mapping between configuration properties for the Visual
        /// C++ compiler and corresponding command-line arguments.
        /// </summary>
        /// <returns>
        /// A mapping between configuration properties for the Visual C++
        /// compiler and corresponding command-line arguments.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   The following configuration properties are processed by
        ///   <see cref="VcProject" />:
        ///   </para>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Category</term>
        ///       <description>Property</description>
        ///     </listheader>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Addtional Include Directories (/I[path])</description>
        ///     </item>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Resolve #using References (/AI[path])</description>
        ///     </item>
        ///     <item>
        ///       <term>Preprocessor</term>
        ///       <description>Preprocessor Definitions (/D[macro])</description>
        ///     </item>
        ///     <item>
        ///       <term>Code Generation</term>
        ///       <description>Enable C++ Exceptions (/EHsc)</description>
        ///     </item>
        ///     <item>
        ///       <term>Precompiled Headers</term>
        ///       <description>Create/Use Precompiled Header</description>
        ///     </item>
        ///     <item>
        ///       <term>Precompiled Headers</term>
        ///       <description>Create/Use PCH Through File</description>
        ///     </item>
        ///     <item>
        ///       <term>Precompiled Headers</term>
        ///       <description>Precompiled Header File</description>
        ///     </item>
        ///     <item>
        ///       <term>Output Files</term>
        ///       <description>Assembler Output</description>
        ///     </item>
        ///     <item>
        ///       <term>Output Files</term>
        ///       <description>ASM List Location</description>
        ///     </item>
        ///     <item>
        ///       <term>Browse Information</term>
        ///       <description>Enable Browse Information</description>
        ///     </item>
        ///     <item>
        ///       <term>Browse Information</term>
        ///       <description>Browse File</description>
        ///     </item>
        ///     <item>
        ///       <term>Advanced</term>
        ///       <description>Force Includes (/FI[name])</description>
        ///     </item>
        ///     <item>
        ///       <term>Advanced</term>
        ///       <description>Force #using (/FU[name])</description>
        ///     </item>
        ///     <item>
        ///       <term>Advanced</term>
        ///       <description>Undefine Preprocessor Definitions (/U[macro])</description>
        ///     </item>
        ///   </list>
        /// </remarks>
        public static VcArgumentMap CreateCLArgumentMap() {
            VcArgumentMap map = new VcArgumentMap();

            // General
            map.AddEnum("DebugInformationFormat", null, null, "/Z7", "/Zd", "/Zi", "/ZI");
            map.AddEnum("CompileAsManaged", null, null, null, "/clr"); // file-level only
            map.AddEnum("WarningLevel", null, "/W0", "/W1", "/W2", "/W3", "/W4");
            map.AddBool("Detect64BitPortabilityProblems", "/Wp64");
            map.AddBool("WarnAsError", "/WX");
            
            // Optimization
            map.AddEnum("Optimization", null, "/Od", "/O1", "/O2", "/Ox");
            map.AddBool(ArgGroup.OptiIgnoreGroup, "GlobalOptimizations", "/Og");
            map.AddEnum(ArgGroup.OptiIgnoreGroup, "InlineFunctionExpansion", null, "/Ob0", "/Ob1", "/Ob2");
            map.AddBool(ArgGroup.OptiIgnoreGroup, "EnableIntrinsicFunctions", "/Oi");
            map.AddBool("ImproveFloatingPointConsistency", "/Op");
            map.AddEnum("FavorSizeOrSpeed", null, null, "/Ot", "/Os");
            map.AddBool(ArgGroup.OptiIgnoreGroup, "OmitFramePointers", "/Oy");
            map.AddBool("EnableFiberSafeOptimizations", "/GT");
            map.AddEnum("OptimizeForProcessor", null, null, "/G5", "/G6", "/G7");
            map.AddBool("OptimizeForWindowsApplication", "/GA");

            // Preprocessor
            map.AddBool("IgnoreStandardIncludePath", "/X");
            map.AddEnum("GeneratePreprocessedFile", null, null, "/P", "/EP /P");
            map.AddBool("KeepComments", "/C");
            
            // Code Generation
            map.AddBool(ArgGroup.OptiIgnoreGroup, "StringPooling", "/GF");
            map.AddBool("MinimalRebuild", "/Gm");
            map.AddBool("SmallerTypeCheck", "/RTCc");
            map.AddEnum("BasicRuntimeChecks", null, null, "/RTCs", "/RTCu", "/RTC1");
            map.AddEnum("RuntimeLibrary", null, "/MT", "/MTd", "/MD", "/MDd", "/ML", "/MLd");
            map.AddEnum("StructMemberAlignment", null, null, "/Zp1", "/Zp2", "/Zp4", "/Zp8", "/Zp16");
            map.AddBool("BufferSecurityCheck", "/GS");
            map.AddBool(ArgGroup.OptiIgnoreGroup, "EnableFunctionLevelLinking", "/Gy");
            map.AddEnum("EnableEnhancedInstructionSet", null, null, "/arch:SSE", "/arch:SSE2");

            // Language

            map.AddBool("DisableLanguageExtensions", "/Za");
            map.AddBool("DefaultCharIsUnsigned", "/J");
            map.AddBool("TreatWChar_tAsBuiltInType", "/Zc:wchar_t");
            map.AddBool("ForceConformanceInForLoopScope", "/Zc:forScope");
            map.AddBool("RuntimeTypeInfo", "/GR");

            // Output Files
            map.AddBool("ExpandAttributedSource", "/Fx");
            map.AddEnum("AssemblerOutput", null, null, "/FA", "/FAcs", "/FAc", "/FAs");

            // Advanced
            map.AddEnum("CallingConvention", null, null, "/Gr", "/Gz");
            map.AddEnum("CompileAs", null, null, "/TC", "/TP");
            map.AddBool("ShowIncludes", "/showIncludes");  
            map.AddBool("UndefineAllPreprocessorDefinitions", "/u");

            return map;
        }

        /// <summary>
        /// Creates a mapping between configuration properties for the Visual
        /// C++ linker and corresponding command-line arguments.
        /// </summary>
        /// <returns>
        /// A mapping between configuration properties for the Visual C++
        /// linker and corresponding command-line arguments.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   The following configuration properties are processed by
        ///   <see cref="VcProject" />:
        ///   </para>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Category</term>
        ///       <description>Property</description>
        ///     </listheader>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Output File (/OUT:[file])</description>
        ///     </item>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Additional Library Directories (/LIBPATH:[dir])</description>
        ///     </item>
        ///     <item>
        ///       <term>Input</term>
        ///       <description>Additional Dependencies</description>
        ///     </item>
        ///     <item>
        ///       <term>Input</term>
        ///       <description>Add Module to Assembly (/ASSEMBLYMODULE:file)</description>
        ///     </item>
        ///     <item>
        ///       <term>Input</term>
        ///       <description>Embed Managed Resource File (/ASSEMBLYRESOURCE:file)</description>
        ///     </item>
        ///     <item>
        ///       <term>Debugging</term>
        ///       <description>Generate Debug Info (/DEBUG)</description>
        ///     </item>
        ///     <item>
        ///       <term>Debugging</term>
        ///       <description>Generate Program Database File (/PDB:name)</description>
        ///     </item>
        ///     <item>
        ///       <term>Debugging</term>
        ///       <description>Generate Map File (/MAP)</description>
        ///     </item>
        ///     <item>
        ///       <term>Debugging</term>
        ///       <description>Map File Name (/MAP:[filename])</description>
        ///     </item>
        ///     <item>
        ///       <term>System</term>
        ///       <description>Heap Reserve Size (/HEAP:reserve)</description>
        ///     </item>
        ///     <item>
        ///       <term>System</term>
        ///       <description>Heap Commit Size (/HEAP:reserve, commit)</description>
        ///     </item>
        ///     <item>
        ///       <term>System</term>
        ///       <description>Stack Reserve Size (/STACK:reserve)</description>
        ///     </item>
        ///     <item>
        ///       <term>System</term>
        ///       <description>Stack Commit Size (/STACK:reserve, commit)</description>
        ///     </item>
        ///   </list>
        ///   <para>
        ///   The following configuration properties are ignored:
        ///   </para>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Category</term>
        ///       <description>Property</description>
        ///     </listheader>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Show Progress (/VERBOSE, /VERBOSE:LIB)</description>
        ///     </item>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Suppress Startup Banner (/NOLOGO)</description>
        ///     </item>
        ///   </list>
        ///   <para>
        ///   Support for the following configuration properties still needs to
        ///   be implemented:
        ///   </para>
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Category</term>
        ///       <description>Property</description>
        ///     </listheader>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Ignore Import Library</description>
        ///     </item>
        ///     <item>
        ///       <term>General</term>
        ///       <description>Register Output</description>
        ///     </item>
        ///     <item>
        ///       <term>Input</term>
        ///       <description>Delay Loaded DLLs (/DELAYLOAD:[dll_name])</description>
        ///     </item>
        ///     <item>
        ///       <term>Embedded IDL</term>
        ///       <description>MIDL Commands (/MIDL:[file])</description>
        ///     </item>
        ///   </list>
        /// </remarks>
        public static VcArgumentMap CreateLinkerArgumentMap() {
            VcArgumentMap map = new VcArgumentMap();

            // General
            map.AddEnum("LinkIncremental", null, null, "/INCREMENTAL:NO", "/INCREMENTAL");
            map.AddLinkerString("Version", "/VERSION:", true);

            // Input
            map.AddBool("IgnoreAllDefaultLibraries", "/NODEFAULTLIB");
            map.AddQuotedLinkerString("ModuleDefinitionFile", "/DEF:", true);

            // Debugging
            map.AddQuotedLinkerString("StripPrivateSymbols", "/PDBSTRIPPED:", true);
            map.AddBool("MapExports", "/MAPINFO:EXPORTS");
            map.AddBool("MapLines", "/MAPINFO:LINES");
            map.AddEnum("AssemblyDebug", null, null, "/ASSEMBLYDEBUG", "/ASSEMBLYDEBUG:DISABLE");
            
            // System
            map.AddEnum("SubSystem", "/SUBSYSTEM:", null, "CONSOLE", "WINDOWS");
            map.AddEnum("LargeAddressAware", null, null, "/LARGEADDRESSAWARE:NO", "/LARGEADDRESSAWARE");
            map.AddEnum("TerminalServerAware", null, null, "/TSAWARE:NO", "/TSAWARE");
            map.AddBool("SwapRunFromCD", "/SWAPRUN:CD");
            map.AddBool("SwapRunFromNet", "/SWAPRUN:NET");

            // Optimization
            map.AddEnum("OptimizeReferences", "/OPT:", null, "NOREF", "REF");
            map.AddEnum("EnableCOMDATFolding", "/OPT:", null, "NOICF", "ICF");
            map.AddEnum("OptimizeForWindows98", "/OPT:", null, "NOWIN98", "WIN98");
            map.AddQuotedLinkerString("FunctionOrder", "/ORDER:", true);

            // Embedded IDL
            map.AddBool("IgnoreEmbeddedIDL", "/IGNOREIDL");
            map.AddQuotedLinkerString("MergedIDLBaseFileName", "/IDLOUT:", true);
            map.AddQuotedLinkerString("TypeLibraryFile", "/TLBOUT:", true);
            map.AddLinkerString("TypeLibraryResourceID", "/TLBID:");
            
            // Advanced
            map.AddQuotedLinkerString("EntryPointSymbol", "/ENTRY:", true);
            map.AddBool("ResourceOnlyDLL", "/NOENTRY");
            map.AddBool("SetChecksum", "/RELEASE");
            map.AddQuotedLinkerString("BaseAddress", "/BASE:", true);
            map.AddEnum("FixedBaseAddress", null, null, "/FIXED:NO", "/FIXED");
            map.AddBool("TurnOffAssemblyGeneration", "/NOASSEMBLY");
            map.AddBool("SupportUnloadOfDelayLoadedDLL", "/DELAY:UNLOAD");
            map.AddQuotedLinkerString("MergeSections", "/MERGE:", true);
            map.AddEnum("TargetMachine", null, null, "/MACHINE:X86");

            return map;
        }

        public static VcArgumentMap CreateMidlArgumentMap() {
            VcArgumentMap map = new VcArgumentMap();

            // General

            map.AddBool("IgnoreStandardIncludePath", "/no_def_idir");
            map.AddBool("MkTypLibCompatible", "/mktyplib203");
            map.AddEnum("WarningLevel", null, "/W0", "/W1", "/W2", "/W3", "/W4");
            map.AddBool("WarnAsError", "/WX");
            map.AddEnum("DefaultCharType", null, "unsigned", "signed", "ascii7");
            map.AddEnum("TargetEnvironment", null, null, "win32", "win64");
            map.AddBool("GenerateStublessProxies", "/Oicf");

            // Output
            map.AddBool("GenerateTypeLibrary", "/notlb", "false");

            // Advanced

            map.AddEnum("EnableErrorChecks", "/error ", null, "none", "all");
            map.AddBool("ErrorCheckAllocations", "/error allocation");
            map.AddBool("ErrorCheckBounds", "/error bounds_check");
            map.AddBool("ErrorCheckEnumRange", "/error enum");
            map.AddBool("ErrorCheckRefPointers", "/error ref");
            map.AddBool("ErrorCheckStubData", "/error stub_data");
            map.AddBool("ValidateParameters", "/robust");
            map.AddEnum("StructMemberAlignment", null, null, "/Zp1", "/Zp2", "/Zp4", "/Zp8");
            return map;
        }

        #endregion Public Static Methods

        #region Private Instance Fields

        private Hashtable _htArgs;

        #endregion Private Instance Fields

        private abstract class VcArgument {
            private string _name;
            private ArgGroup _group;
            
            protected VcArgument(ArgGroup group, string name) {
                _name = name;
                _group = group;
            }

            /// <summary>
            /// Gets the name of the command-line argument.
            /// </summary>
            /// <value>
            /// The name of the command-line argument.
            /// </value>
            public string Name {
                get { return _name; }
            }

            public ArgGroup Group {
                get { return _group; }
            }

            internal abstract string MapValue(string propValue);

            protected string FormatOption(string value) {
                if (_name == null) {
                    return value;
                }
                return _name + value;
            }
        }

        private class VcStringArgument: VcArgument {
            #region Private Instance Fields

            private bool _ignoreEmptyValue;

            #endregion Private Instance Fields

            #region Internal Instance Constructors

            internal VcStringArgument(ArgGroup group, string name): this(group, name, false) {
            }

            internal VcStringArgument(ArgGroup group, string name, bool ignoreEmptyValue): base(group, name) {
                _ignoreEmptyValue = ignoreEmptyValue;
            }

            #endregion Internal Instance Constructors

            #region Protected Instance Properties

            protected bool IgnoreEmptyValue {
                get { return _ignoreEmptyValue; }
            }

            #endregion Protected Instance Properties

            #region Override implementation of VcArgument

            internal override string MapValue(string propValue) {
                if (IgnoreEmptyValue && String.IsNullOrEmpty(propValue)) {
                    return null;
                }

                return FormatOption(propValue);
            }

            #endregion Override implementation of VcArgument
        }

        /// <summary>
        /// Represents a command-line arguments of which the trailing backslashes
        /// in the value should be duplicated.
        /// </summary>
        private class LinkerStringArgument: VcStringArgument {
            #region Internal Instance Constructors

            internal LinkerStringArgument(ArgGroup group, string name): this(group, name, false) {
            }

            internal LinkerStringArgument(ArgGroup group, string name, bool ignoreEmptyValue): base(group, name, ignoreEmptyValue) {
            }

            #endregion Internal Instance Constructors

            #region Override implementation of VcArgument

            internal override string MapValue(string value) {
                if (IgnoreEmptyValue && String.IsNullOrEmpty(value)) {
                    return null;
                }

                if (Name == null) {
                    return ArgumentUtils.DuplicateTrailingBackslash(value);
                }

                return Name + ArgumentUtils.DuplicateTrailingBackslash(value);
            }

            #endregion Override implementation of VcArgument
        }

        /// <summary>
        /// Represents a command-line argument of which the value should be
        /// quoted, and of which trailing backslahes should be duplicated.
        /// </summary>
        private class QuotedLinkerStringArgument: VcStringArgument {
            #region Internal Instance Constructors

            internal QuotedLinkerStringArgument(ArgGroup group, string name): this(group, name, false) {
            }

            internal QuotedLinkerStringArgument(ArgGroup group, string name, bool ignoreEmptyValue): base(group, name, ignoreEmptyValue) {
            }

            #endregion Internal Instance Constructors

            #region Override implementation of VcArgument

            internal override string MapValue(string value) {
                if (IgnoreEmptyValue && String.IsNullOrEmpty(value)) {
                    return null;
                }

                if (Name == null) {
                    return LinkTask.QuoteArgumentValue(value);
                }

                return Name + LinkTask.QuoteArgumentValue(value);
            }

            #endregion Override implementation of VcArgument
        }

        private class VcBoolArgument: VcArgument {
            #region Internal Instance Constructors

            internal VcBoolArgument(ArgGroup group, string name): this(group, name, "true") {
            }

            internal VcBoolArgument(ArgGroup group, string name, string match): base(group, name) {
                _match = match;
            }

            #endregion Internal Instance Constructors

            #region Internal Instance Properties

            /// <summary>
            /// Gets the string that the configuration setting should match in 
            /// order for the command line argument to be set.
            /// </summary>
            public string Match {
                get { return _match; }
            }

            #endregion Internal Instance Properties

            #region Override implementation of VcArgument

            internal override string MapValue(string propValue) {
                if (string.Compare(propValue, Match, true, CultureInfo.InvariantCulture) == 0) {
                    return FormatOption(string.Empty);
                }
                return null;
            }

            #endregion Override implementation of VcArgument

            #region Private Instance Methods

            private string _match = "true";

            #endregion Private Instance Methods
        }

        private class VcEnumArgument: VcArgument {
            #region Internal Instance Constructors

            internal VcEnumArgument(ArgGroup group, string name, string[] values): base(group, name) {
                _values = values;
            }

            #endregion Internal Instance Constructors

            #region Override implementation of VcArgument

            internal override string MapValue(string propValue) {
                int iValue = -1;
                try {
                    iValue = Int32.Parse(propValue);
                } catch(FormatException) {
                    return null;
                }
                
                if (iValue < 0 || iValue >= _values.Length || _values [iValue] == null) {
                    return null;
                }
                return FormatOption(_values [iValue]);
            }

            #endregion Override implementation of VcArgument

            #region Private Instance Methods

            private string[] _values;

            #endregion Private Instance Methods
        }

        /// <summary>
        /// Allow us to assign an argument to a specific group.
        /// </summary>
        [Flags]
        public enum ArgGroup {
            /// <summary>
            /// The argument is not assigned to any group.
            /// </summary>
            Unassigned = 0,

            /// <summary>
            /// The argument is ignored when the optimization level is set to 
            /// <b>Minimum Size</b> (1) or <b>Maximum Size</b> (2).
            /// </summary>
            OptiIgnoreGroup = 1
        }
    }
}
