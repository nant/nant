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

// TODO: review interface for future compatibility/customizations issues

using System;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// Compiles resources using <c>rc.exe</c>, Microsoft's Win32 resource compiler.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>text.rc</c> using the default options.</para>
    ///   <code>
    ///     <![CDATA[
    /// <rc rcfile="text.rc"/>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Compile <c>text.rc</c>, passing an additional option.</para>
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
        private FileInfo _rcFile;

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
            get { return _outputFile; }
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
                string str = "";

                if (Verbose) {
                    str += "/v ";
                }

                if (OutputFile != null) {
                    str += string.Format(CultureInfo.InvariantCulture, 
                        "/fo\"{0}\" ", OutputFile.FullName);
                }

                if (Options != null) {
                    str += string.Format(CultureInfo.InvariantCulture,
                        "{0} ", Options);
                }
                
                str += string.Format(CultureInfo.InvariantCulture,
                        "\"{0}\" ", RcFile.FullName);

                return str.ToString();
            }
        }

        /// <summary>
        /// Compile the resource file
        /// </summary>
        protected override void ExecuteTask() {
            string message = string.Format(CultureInfo.InvariantCulture, 
                "Compiling '{0}'", RcFile.FullName);


            if (OutputFile != null) {
                message += string.Format(CultureInfo.InvariantCulture, 
                    " to '{0}'", OutputFile.FullName);
            }

            Log(Level.Info, message + ".");
            base.ExecuteTask();
        }

        #endregion Override implementation of ExternalProgramBase
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
