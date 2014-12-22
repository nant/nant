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
// Shawn Van Ness (nantluver@arithex.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean ( ian@maclean.ms )
// Eric V. Smith (ericsmith@windsor.com)
// Hani Atassi (haniatassi@users.sourceforge.net)

// TODO: review interface for future compatibility/customizations issues

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

using NAnt.VisualCpp.Util;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// Compiles resources using <c>rc.exe</c>, Microsoft's Win32 resource 
    /// compiler.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Compile <c>text.rc</c> to <c>text.res</c> using the default options.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <rc rcfile="text.rc" output="text.res" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compile <c>text.rc</c>, passing an additional option.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <rc rcfile="text.rc" options="/r"/>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("rc")]
    public class RcTask : ExternalProgramBase {
        #region Private Instance Fields

        private FileInfo _outputFile;
        private string _options;
        private int _langId = 0;
        private FileInfo _rcFile;
        private FileSet _includeDirs = new FileSet();
        private OptionCollection _defines = new OptionCollection();

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
        /// Output file.
        /// </summary>
        [TaskAttribute("output")]
        public FileInfo OutputFile {
            get { 
                if (_outputFile == null) {
                    _outputFile = new FileInfo(Path.ChangeExtension(RcFile.FullName, "RES"));
                }
                return _outputFile;
            }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The resource file to compile.
        /// </summary>
        [TaskAttribute("rcfile", Required=true)]
        public FileInfo RcFile {
            get { return _rcFile; }
            set { _rcFile = value; }
        }

        /// <summary>
        /// Default language ID.
        /// </summary>
        [TaskAttribute("langid", Required=false)]
        public int LangId {
            get { return _langId; }
            set { _langId = value; }
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
        /// Macro definitions to pass to rc.exe.
        /// Each entry will generate a /d
        /// </summary>
        [BuildElementCollection("defines", "define")]
        public OptionCollection Defines {
            get { return _defines; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Filename of program to execute
        /// </summary>
        public override string ProgramFileName {
            get { return Name; }
        }

        /// <summary>
        /// Arguments of program to execute
        /// </summary>
        public override string ProgramArguments {
            get {
                StringBuilder str = new StringBuilder();

                if (Verbose) {
                    str.Append("/v ");
                }

                str.AppendFormat(CultureInfo.InvariantCulture, 
                    "/fo\"{0}\" ", OutputFile.FullName);

                if (Options != null) {
                    str.AppendFormat(CultureInfo.InvariantCulture,
                        "{0} ", Options);
                }

                if (LangId != 0) {
                    str.AppendFormat("/l 0x{0:X} ", LangId);
                }

                // append user provided include directories
                foreach (string include in IncludeDirs.DirectoryNames) {
                    str.AppendFormat("/i {0} ", ArgumentUtils.QuoteArgumentValue(
                        include, BackslashProcessingMethod.Duplicate));
                }

                // append user definitions
                foreach (Option define in Defines) {
                    if (!define.IfDefined || define.UnlessDefined) {
                        continue;
                    }

                    if (define.Value == null) {
                        str.AppendFormat("/d {0} ", ArgumentUtils.DuplicateTrailingBackslash(define.OptionName));
                    } else {
                        str.AppendFormat("/d {0}={1} ", define.OptionName, ArgumentUtils.DuplicateTrailingBackslash(define.Value));
                    }
                }
                
                str.AppendFormat(CultureInfo.InvariantCulture,
                        "\"{0}\" ", RcFile.FullName);

                return str.ToString();
            }
        }

        /// <summary>
        /// Compile the resource file
        /// </summary>
        protected override void ExecuteTask() {
            if (IncludeDirs.BaseDirectory == null) {
                IncludeDirs.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (NeedsCompiling()) {
                Log(Level.Info, "Compiling \"{0}\" to \"{1}\".", RcFile.FullName,
                    OutputFile.FullName);
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines if the resource need compiling.
        /// </summary>
        protected virtual bool NeedsCompiling() {
            if (!OutputFile.Exists) {
                Log(Level.Verbose, "'{0}' does not exist, recompiling.", 
                    OutputFile.FullName);
                return true;
            }

            // if output file file is older the resource file, it is stale
            string fileName = FileSet.FindMoreRecentLastWriteTime(
                RcFile.FullName, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' is out of date, recompiling.", 
                    fileName);
                return true;
            }

            // if resource file does not exist, then let compiler handle error
            if (!RcFile.Exists) {
                return true;
            }

            // check whether external files have been updated

            Regex regBitmap = new Regex("IDB_(?<name>\\w+)\\s+BITMAP\\s+\\\"(?<file>[^\\\"]+)\\\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex regIcon = new Regex("IDI_(?<name>\\w+)\\s+ICON\\s+\\\"(?<file>[^\\\"]+)\\\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Regex regBinary = new Regex("IDR_(?<name>\\w+)\\s+(?<Number>\\w+)\\s+\\\"(?<file>[^\\\"]+)\\\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            using (StreamReader sr = new StreamReader(RcFile.FullName)) {
                while (sr.Peek() != -1) {
                    string line = sr.ReadLine();

                    Match resourceMatch = regBitmap.Match(line);
                    if (resourceMatch.Success) {
                        return CheckResourceTimeStamp(resourceMatch.Groups["file"].Value);
                    }

                    resourceMatch = regIcon.Match(line);
                    if (resourceMatch.Success) {
                        return CheckResourceTimeStamp(resourceMatch.Groups["file"].Value);
                    }
                    
                    resourceMatch = regBinary.Match(line);
                    if (resourceMatch.Success) {
                        return CheckResourceTimeStamp(resourceMatch.Groups["file"].Value);
                    }
                }
            }

            // output file is up-to-date
            return false;
        }

        /// <summary>
        /// Check if a resource file has been updated.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CheckResourceTimeStamp(string filePath ) {
            string fileName;
            string externalFile = Path.Combine(RcFile.DirectoryName,
                                               filePath);
            fileName = FileSet.FindMoreRecentLastWriteTime(
                externalFile, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                    fileName);
                return true;
            }
            return false;
        }

        #endregion Protected Instance Methods
    }
}
#if unused
Microsoft (R) Windows (R) Resource Compiler, Version 5.1.2264.1 - Build 2264
Copyright (C) Microsoft Corp. 1985-1998. All rights reserved.

Usage:  rc [options] .RC input file
Switches:
   /r    Emit .RES file (optional)
   /v    Verbose (print progress messages)
   /d    Define a symbol
   /u    Undefine a symbol
   /fo   Rename .RES file
   /l    Default language ID in hex
   /i    Add a path for INCLUDE searches
   /x    Ignore INCLUDE environment variable
   /c    Define a code page used by NLS conversion
   /w    Warn on Invalid codepage in .rc (default is an error)
   /y    Don't warn if there are duplicate control ID's
   /n    Append null's to all strings in the string tables.
Flags may be either upper or lower case
#endif
