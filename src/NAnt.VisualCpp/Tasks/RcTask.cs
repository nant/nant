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
using System.IO;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// Compiles resources using rc.exe, Microsoft's Win32 resource compiler.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>text.rc</c> using the default options.</para>
    ///   <code>
    ///     <![CDATA[
    /// <rc rcfile="text.rc"/>
    ///     ]]>
    ///   </code>
    ///   <para>Compile <c>text.rc</c>, passing an additional option.</para>
    ///   <code>
    ///     <![CDATA[
    /// <rc rcfile="text.rc" options="/r"/>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("rc")]
    public class RcTask : ExternalProgramBase {
        string _output = null;
        string _options = null;
        string _rcfile = null;

        /// <summary>
        /// Options to pass to the compiler.
        /// </summary>
        [TaskAttribute("options")]
        public string Options {get {return _options;} set {_options = value;}}

        /// <summary>
        /// Output filename.
        /// </summary>
        [TaskAttribute("output")]
        public string Output {get {return _output;} set {_output = value;}}

        /// <summary>
        /// Input filename.
        /// </summary>
        [TaskAttribute("rcfile", Required=true)]
        public string RcFile {get {return _rcfile;} set {_rcfile = value;}}

        // ExternalProgramBase implementation
        /// <summary>
        /// Filename of program to execute
        /// </summary>
        public override string ProgramFileName  { get {return Name;} }

        /// <summary>
        /// Arguments of program to execute
        /// </summary>
        public override string ProgramArguments {
            get {
                string str = "";

                if (Verbose) {
                    str += "/v ";
                }

                if (_output != null) {
                    str += String.Format("/fo\"{0}\" ", Output);
                }

                if (_options != null) {
                    str += String.Format("{0} ", _options);
                }

                str += _rcfile;

                return str.ToString();
            }
        }
        /// <summary>
        /// Compile the resource files
        /// </summary>
        protected override void ExecuteTask() {
            string message = string.Format( "Compiling {0}", RcFile );


            if (Output != null) {
               message+= string.Format(" to {0}", Output);
            }
            Log(Level.Info, message);
            base.ExecuteTask();
        }
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
