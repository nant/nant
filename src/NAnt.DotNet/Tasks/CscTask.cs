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

    /// <summary>Compiles C# programs using csc, Microsoft's C# compiler.</summary>
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
    public class CscTask : CompilerBase {
       
        string _doc = null;
        
        // C# specific compiler options
        /// <summary>The name of the XML documentation file to generate.
        ///     This attribute corresponds to the <c>/doc:</c> flag.</summary>
        [TaskAttribute("doc")]
        public string Doc        { get { return _doc; } set {_doc = value; } }

        protected override void WriteOptions(TextWriter writer) {
            //writer.WriteLine("/fullpaths");
            WriteOption(writer, "fullpaths");
            if (_doc != null) {             
                WriteOption(writer, "doc", _doc);
            }
          
            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG");
                WriteOption(writer, "define", "TRACE");
            }
        }
    }
}
