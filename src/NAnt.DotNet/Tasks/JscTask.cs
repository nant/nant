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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Mike Krueger (mike@icsharpcode.net)

namespace SourceForge.NAnt.Tasks {

    using System;
    using System.IO;
    using SourceForge.NAnt.Attributes;

    /// <summary>Compiles JScript.NET programs.</summary>
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
    public class JscTask : MsftFXCompilerBase {
        #region Override implementation of ExternalProgramBase
           
        public override string ExeName {           
            get { return Project.CurrentFramework.JScriptCompilerName; }
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
        }

        protected override string GetExtension() { 
            return "js";
        }

        #endregion Override implementation of CompilerBase
    }
}
