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

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles JScript.NET programs.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>helloworld.js</c> to <c>helloworld.exe</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <jsc target="exe" output="helloworld.exe" debug="true">
    ///     <sources>
    ///         <includes name="helloworld.js" />
    ///     </sources>
    /// </jsc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("jsc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class JscTask : CompilerBase {
        #region Private Instance Fields

        private string _warningLevel = null;
        private string _codepage = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies the warning level for the compiler to display. Valid 
        /// values are <c>0</c>-<c>4</c>. Default is <c>4</c>.
        /// </summary>
        /// <value>
        /// The warning level for the compiler to display.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/warn</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("warninglevel")]
        [Int32Validator(0, 4)]
        public string WarningLevel {
            get { return _warningLevel; }
            set { _warningLevel = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies the code page to use for all source code files in the 
        /// compilation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/codepage</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("codepage")]
        public string Codepage {
            get { return _codepage; }
            set { _codepage = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
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
        /// <value>
        /// For the JScript.NET compiler, the file extension is always <c>js</c>.
        /// </value>
        protected override string Extension { 
            get { return "js"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
