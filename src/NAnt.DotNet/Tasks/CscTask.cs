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
    using System.Text.RegularExpressions;

    /// <summary>Compiles C# programs.</summary>
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
    public class CscTask : MsftFXCompilerBase {
       
        string _doc = null;
        bool _nostdlib = false;
        bool _noconfig = false;
        bool _checked = false;
        bool _unsafe = false;
        
        public override string ExeName {           
            get { return Project.CurrentFramework.CSharpCompilerName; }
        }
        // C# specific compiler options
        /// <summary>The name of the XML documentation file to generate.
        ///     This attribute corresponds to the <c>/doc:</c> flag.</summary>
        [TaskAttribute("doc")]
        public string Doc        { get { return _doc; } set {_doc = value; } }

        /// <summary>Instructs the compiler not to import mscorlib.dll (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c>.</summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/nostdlib[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("nostdlib")]
        public bool NoStdLib     { get { return _nostdlib; } set {_nostdlib = value; } }

        /// <summary>Instructs the compiler not to use implicit references to assemblies (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c>.</summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/noconfig</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("noconfig")]
        public bool NoConfig     { get { return _noconfig; } set {_noconfig = value; } }

        /// <summary>
        /// Specifies whether an integer arithmetic statement that is not in the scope of the 
        /// <c>checked</c> or <c>unchecked</c> keywords and that results in a value outside the 
        /// range of the data type should cause a run-time exception (<c>true</c>/<c>false</c>). 
        /// Default is <c>&quot;false&quot;</c>.</summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/checked[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("checked")]
        public bool Checked     { get { return _checked; } set {_checked = value; } }

        /// <summary>
        /// Instructs the compiler to allow code that uses the <c>unsafe</c> keyword 
        /// (<c>true</c>/<c>false</c>). Default is <c>&quot;false&quot;</c>.</summary>
        /// <remarks>
        /// <para>
        /// This attribute corresponds to the <c>/unsafe[+|-]</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("unsafe")]
        public bool Unsafe      { get { return _unsafe; } set {_unsafe = value; } }

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

            if (NoConfig && ! Args.Contains("/noconfig")) {
                Args.Add("/noconfig");
            }
        }
        #region overrides      
        protected override string GetExtension(){ return "cs";}
        
        protected override bool UsesRuntimeEngine { 
            get {                
                // find better way of doing this
                if ( Project.CurrentFramework.Name.IndexOf( "mono", 0 ) != -1 ) {          
                    return true;
                }                
                return false;                
            }
        }
        #endregion
    }
}
