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

using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles C# programs.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>helloworld.cs</c> to <c>helloworld.exe</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <csc target="exe" output="helloworld.exe" debug="true">
    ///     <sources>
    ///         <includes name="helloworld.cs"/>
    ///     </sources>
    /// </csc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("csc")]
    [ProgramLocation( LocationType.FrameworkDir ) ]
    public class CscTask : CompilerBase {
        #region Private Instance Fields
       
        string _doc = null;
        bool _nostdlib = false;
        bool _noconfig = false;
        bool _checked = false;
        bool _unsafe = false;
        bool _optimize = false;
        string _warningLevel = null;
        string _noWarn = null;
        string _codepage = null;

        #endregion Private Instance Fields

        #region Public Instance Properties
        
        /// <summary>
        /// The name of the XML documentation file to generate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/doc:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("doc")]
        public string Doc {
            get { return _doc; }
            set {_doc = value; }
        }

        /// <summary>
        /// Instructs the compiler not to import mscorlib.dll (<c>true</c>/<c>false</c>). 
        /// Default is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/nostdlib[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [FrameworkConfigurable("nostdlib")]
        [TaskAttribute("nostdlib")]
        public bool NoStdLib {
            get { return _nostdlib; }
            set {_nostdlib = value; }
        }

        /// <summary>
        /// Instructs the compiler not to use implicit references to assemblies (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/noconfig</c> flag.
        /// </para>
        /// </remarks>
        [FrameworkConfigurable("noconfig")]
        [TaskAttribute("noconfig")]
        public bool NoConfig {
            get { return _noconfig; }
            set {_noconfig = value; }
        }

        /// <summary>
        /// Specifies whether an integer arithmetic statement that is not in the scope of the
        /// <c>checked</c> or <c>unchecked</c> keywords and that results in a value outside the
        /// range of the data type should cause a run-time exception (<c>true</c>/<c>false</c>).
        /// Default is <c>&quot;false&quot;</c>.</summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/checked[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("checked")]
        public bool Checked {
            get { return _checked; }
            set {_checked = value; }
        }

        /// <summary>
        /// Instructs the compiler to allow code that uses the <c>unsafe</c> keyword
        /// (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/unsafe[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("unsafe")]
        public bool Unsafe {
            get { return _unsafe; }
            set {_unsafe = value; }
        }

        /// <summary>
        /// Specifies whether the compiler should perform optimizations to the make 
        /// output files smaller, faster, and more effecient.
        /// </summary>
        /// <value>
        /// The value of this attribute must be either <c>true</c> or <c>false</c>.
        /// If <c>false</c>, the switch is omitted.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/optimize[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("optimize")]
        [BooleanValidator()]
        public bool Optimize {
            get { return _optimize; }
            set {_optimize = value; }
        }

        /// <summary>
        /// Specifies the warning level for the compiler to display. Valid values are 0-4. Default is 4.
        /// </summary>
        /// <value>The warning level for the compiler to display.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/warn</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("warninglevel")]
        [Int32Validator(0, 4)]
        public string WarningLevel {
            get { return _warningLevel; }
            set {_warningLevel = value; }
        }

        /// <summary>
        /// Specifies a comma-separated list of warnings that should be suppressed 
        /// by the compiler.
        /// </summary>
        /// <value>Comma-separated list of warnings that should be suppressed by the compiler.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/nowarn</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("nowarn")]
        public string NoWarn {
            get { return _noWarn; }
            set {_noWarn = value; }
        }

        /// <summary>
        /// Specifies the code page to use for all source code files in the compilation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/codepage</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("codepage")]
        public string Codepage {
            get { return _codepage; }
            set { _codepage = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            WriteOption(writer, "fullpaths");

            if (Doc != null) {
                WriteOption(writer, "doc", this.Doc);
            }

            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG");
                WriteOption(writer, "define", "TRACE");
            }

            if (NoStdLib) {
                WriteOption(writer, "nostdlib");
            }

            if (Checked) {
                WriteOption(writer, "checked");
            }

            if (Unsafe) {
                WriteOption(writer, "unsafe");
            }

            if (Optimize) {
                WriteOption(writer, "optimize");
            }

            if (WarningLevel != null) {
                WriteOption(writer, "warn", WarningLevel);
            }

            if (NoWarn != null) {
                WriteOption(writer, "nowarn", NoWarn);
            }

            if (Codepage != null) {
                WriteOption(writer, "codepage", Codepage);
            }
        
            if (NoConfig && !Arguments.Contains("/noconfig")) {
                Arguments.Add(new Argument("/noconfig"));
            }
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>For the C# compiler, the file extension is always <c>cs</c>.</value>
        protected override string Extension {
            get { return "cs"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
