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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Compiles JScript.NET programs.
    /// </summary>
    /// <example>
    ///   <para>Compile helloworld.js to helloworld.exe.</para>
    ///   <code>
    ///     <![CDATA[
    /// <jsc target="exe" output="helloworld.exe" debug="true">
    ///     <sources>
    ///         <includes name="helloworld.js"/>
    ///     </sources>
    /// </jsc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("jsc")]
    public class JscTask : FXCompilerBase {
        #region Private Instance Fields

        string _warningLevel = null;
        string _codepage = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

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
        public string WarningLevel  { get { return _warningLevel; } set {_warningLevel = value;}}

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

        #region Override implementation of ExternalProgramBase
           
        /// <summary>
        /// Gets the name of the executable that should be used to launch the
        /// external program.
        /// </summary>
        /// <value>
        /// The name of the executable that should be used to launch the
        /// external program.
        /// </value>
        /// <remarks>
        /// If a current framework is defined, the name of the executable will
        /// be retrieved from the configuration of the framework; otherwise the
        /// <see cref="Task.Name" /> will be used.
        /// </remarks>
        protected override string ExeName {           
            get { 
                if (Project.CurrentFramework != null) {
                    return Project.CurrentFramework.JScriptCompilerName; 
                } else {
                    return Name;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the external program should be executed
        /// using a runtime engine, if configured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the program should be executed using a runtime engine;
        /// otherwise, <c>false</c>.
        /// </value>
        protected override bool UsesRuntimeEngine { 
            get {
                if (Project.CurrentFramework != null) {
                    // TO-DO : find better of doing this than relying on the name of the framework
                    if (Project.CurrentFramework.Name.IndexOf("sscli", 0) != -1) {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes the compiler options to the specified TextWriter.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG");
                WriteOption(writer, "define", "TRACE");
            }

            if (WarningLevel != null) {
                WriteOption(writer, "warn" , WarningLevel);
            }

            if (Codepage != null) {
                WriteOption(writer, "codepage", Codepage);
            }
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>For the JScript.NET compiler, the file extension is always <c>js</c>.</value>
        protected override string Extension { 
            get { return "js"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
