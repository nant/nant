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
// Tom Jordan (tdjordan@users.sourceforge.net)

using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles Visual J# programs using vjc, Microsoft's J# compiler.
    /// </summary>
    /// <example>
    ///   <para>Compile <c>helloworld.jsl</c> to <c>helloworld.exe</c>.</para>
    ///   <code>
    ///     <![CDATA[
    ///<vjc target="exe" output="helloworld.exe" debug="true">
    ///   <sources>
    ///      <includes name="helloworld.jsl" />
    ///   </sources>
    ///</vjc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("vjc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class VjcTask : CompilerBase {
        #region Private Instance Fields

        private bool _secureScoping = false;
        private string _x = null;
        private string _libPath = null;
        private string _jcpa = null;
        private string _codepage = null;
        private string _warningLevel = null;
        private string _noWarn = null;

        #endregion Private Instance Fields
           
        #region Public Instance Properties

        /// <summary>
        /// Specifies whether package-scoped members are accessible outside of 
        /// the assembly. In other words, package scope is treated as assembly 
        /// scope when emitting metadata. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/securescoping</c> flag.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfsecurescopingmakepackage-scopedmembersinaccessibleoutsideassembly.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code><![CDATA[<vjc securescoping='true'/>]]></code>
        /// </example>
        [TaskAttribute("securescoping")]
        [BooleanValidator()]
        public bool SecureScoping {
            get { return _secureScoping; }
            set { _secureScoping = value; }
        }
       
        /// <summary>
        /// Specifies whether to disable language extensions.
        /// </summary>
        /// <value>
        /// The value of this property must be either <c>all</c>, <c>net</c>, 
        /// or an empty string.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/x</c> flag.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfxdisablelanguageextensions.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>To disable only the .NET Framework extensions:<c><![CDATA[
        /// <vjc x='net'/>
        /// ]]></c></para>
        /// <para>To disable the .NET Framework extensions and the VJ++ 6.0 extensions:<c><![CDATA[
        /// <vjc x='all'/>
        /// ]]></c></para>
        /// </example>
        [TaskAttribute("x")]
        public string X {
            get { return _x; }
            set { _x = StringUtils.ConvertEmptyToNull(value); }
        }
       
        /// <summary>
        /// Specifies the location of assemblies referenced by way of the <c>/reference</c> flag.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/libpath:dir[;dir2]</c> flag.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrflibpathspecifyassemblyreferencelocations.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        [TaskAttribute("libpath")]
        public string LibPath {
            get { return _libPath; }
            set { _libPath = StringUtils.ConvertEmptyToNull(value); }
        }
       
        /// <summary>
        /// Associate Java-language/COM package names.
        /// </summary>
        /// <value>
        /// The value of this propery. must be <c>package=namespace</c>, <c>@filename</c>, 
        /// or an empty string.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/jcpa:package=namespace</c> and <c>/jcpa:@filename</c> flags.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfjcpaassociatejava-compackages.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        /// <example>
        /// <para>Map package 'x' to namespace 'y':<c><![CDATA[
        /// <vjc jcpa='x=y'/>
        /// ]]></c></para>
        /// </example>
        [TaskAttribute("jcpa")]
        public string Jcpa {
            get { return _jcpa; }
            set { _jcpa = StringUtils.ConvertEmptyToNull(value); }
        }
       
        /// <summary>
        /// Specifies the code page to use for all source code files in the 
        /// compilation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/codepage</c> flag.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjlrfcodepagespecifycodepageforsourcecodefiles.htm">See the Visual J# Reference for details.</a>
        /// </para>
        /// </remarks>
        [TaskAttribute("codepage")]
        public string Codepage {
            get { return _codepage; }
            set { _codepage = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies the warning level for the compiler to display. Valid values 
        /// are <c>0</c>-<c>4</c>. Default is <c>4</c>.
        /// </summary>
        /// <value>
        /// The warning level for the compiler to display.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/warn</c> option.
        /// </para>
        /// </remarks>
        [TaskAttribute("warninglevel")]
        [Int32Validator(0, 4)]
        public string WarningLevel {
            get { return _warningLevel; }
            set { _warningLevel = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies a comma-separated list of warnings that should be suppressed 
        /// by the compiler.
        /// </summary>
        /// <value>
        /// Comma-separated list of warnings that should be suppressed by the 
        /// compiler.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/nowarn</c> option.
        /// </para>
        /// </remarks>
        [TaskAttribute("nowarn")]
        public string NoWarn {
            get { return _noWarn; }
            set { _noWarn = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            // handle secure scoping.
            if (SecureScoping) {
                WriteOption(writer, "securescoping"); 
            }

            // handle the disable framework extensions option.
            if (X != null) {
                WriteOption(writer, "x", X);
            }

            // handle the libpath option.
            if (LibPath != null ) {
                WriteOption(writer, "libpath", LibPath);
            }

            // handle the jcpa option.
            if (Jcpa != null) {
                WriteOption(writer, "jcpa", Jcpa);
            }

            // handle the codepage option.
            if (Codepage != null) {
                WriteOption(writer, "codepage", Codepage);
            }

            // handle debug builds.
            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG");
                WriteOption(writer, "define", "TRACE");
            }

            if (WarningLevel != null) {
                WriteOption(writer, "warn", WarningLevel);
            }

            if (NoWarn != null) {
                WriteOption(writer, "nowarn", NoWarn);
            }
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>For the J# compiler, the file extension is always <c>jsl</c>.</value>
        protected override string Extension {
            get { return "jsl"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
