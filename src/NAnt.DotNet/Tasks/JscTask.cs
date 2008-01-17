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
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

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
    ///         <include name="helloworld.js" />
    ///     </sources>
    /// </jsc>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("jsc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class JscTask : CompilerBase {
        #region Private Instance Fields

        private bool _autoRef;
        private bool _nostdlib;
        private string _warningLevel;
        private string _codepage;
        private string _platform;
        private bool _versionSafe;

        // framework configuration settings
        private bool _supportsPlatform;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static Regex _classNameRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.\{]+|class\s+(?<class>\w+)|(?<keyword>\w+))*");
        private static Regex _namespaceRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.\{]+|namespace\s+(?<namespace>(\w+(\.\w+)*)+)|(?<keyword>\w+))*");

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Automatically references assemblies if they have the same name as 
        /// an imported namespace or as a type annotation when declaring a 
        /// variable. The default is <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/autoref</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("autoref")]
        [BooleanValidator()]
        public bool AutoRef {
            get { return _autoRef; }
            set { _autoRef = value; }
        }

        /// <summary>
        /// Instructs the compiler not to import standard library, and changes
        /// <see cref="AutoRef" /> to <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/noconfig</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("nostdlib")]
        [BooleanValidator()]
        public bool NoStdLib {
            get { return _nostdlib; }
            set { _nostdlib = value; }
        }

        /// <summary>
        /// Specifies which platform version of common language runtime (CLR)
        /// can run the output file.
        /// </summary>
        /// <value>
        /// The platform version of common language runtime (CLR) that can run
        /// the output file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/platform</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("platform")]
        public string Platform {
            get { return _platform; }
            set { _platform = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Causes the compiler to generate errors for implicit method 
        /// overrides. The default is <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/versionsafe</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("versionsafe")]
        [BooleanValidator()]
        public bool VersionSafe {
            get { return _versionSafe; }
            set { _versionSafe = value; }
        }

        /// <summary>
        /// Specifies the warning level for the compiler to display. Valid 
        /// values are <c>0</c>-<c>4</c>. The default is <c>4</c>.
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
        /// Controls which warnings should be reported as errors.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// compiler does not allow control over which warnings should be
        /// reported as errors.
        /// </remarks>
        public override WarningAsError WarningAsError {
            get { return base.WarningAsError; }
        }

        /// <summary>
        /// Specifies a comma-separated list of warnings that should be suppressed
        /// by the compiler.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// compiler does not support package references.
        /// </remarks>
        [Obsolete("Use the <nowarn> element instead.", false)]
        public override string NoWarn {
            get { return base.NoWarn; }
            set { base.NoWarn = value; }
        }

        /// <summary>
        /// Specifies a list of warnings that you want the compiler to suppress.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// compiler does not support suppressing warnings.
        /// </remarks>
        public override CompilerWarningCollection SuppressWarnings {
            get { return base.SuppressWarnings; }
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

        /// <summary>
        /// Specifies the key pair container used to strongname the assembly.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override string KeyContainer {
            get { return base.KeyContainer; }
            set { base.KeyContainer = value; }
        }

        /// <summary>
        /// Specifies a strong name key file.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override FileInfo KeyFile {
            get { return base.KeyFile; }
            set { base.KeyFile = value; }
        }

        /// <summary>
        /// Specifies whether to delay sign the assembly using only the public
        /// portion of the strong name key.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override DelaySign DelaySign {
            get { return base.DelaySign; }
            set { base.DelaySign = value; }
        }

        /// <summary>
        /// Indicates whether the compiler for a given target framework supports
        /// the "keycontainer" option. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override bool SupportsKeyContainer {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Indicates whether the compiler for a given target framework supports
        /// the "keyfile" option. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override bool SupportsKeyFile {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Indicates whether the compiler for a given target framework supports
        /// the "delaysign" option. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// does not support this.
        /// </remarks>
        public override bool SupportsDelaySign {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Specifies whether the compiler for the active target framework
        /// supports limiting the platform on which the compiled code can run.
        /// The default is <see langword="false" />.
        /// </summary>
        [FrameworkConfigurable("supportsplatform")]
        public bool SupportsPlatform {
            get { return _supportsPlatform; }
            set { _supportsPlatform = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Link the specified modules into this assembly.
        /// </summary>
        /// <remarks>
        /// Override to avoid exposing this to build authors, as the JScript.NET
        /// compiler does not support linking modules.
        /// </remarks>
        public override AssemblyFileSet Modules {
            get { return base.Modules; }
            set { base.Modules = value; }
        }

        /// <summary>
        /// Writes module references to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the module references should be written.</param>
        protected override void WriteModuleReferences(TextWriter writer) {
            if (Modules.FileNames.Count > 0) {
                Log(Level.Warning, ResourceUtils.GetString("String_JscDoesNotSupportLinkingModules"));
            }
        }

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

            if (NoStdLib) {
                WriteOption(writer, "nostdlib");
            } else if (AutoRef) {
                WriteOption(writer, "autoref");
            }

            if (WarningLevel != null) {
                WriteOption(writer, "warn" , WarningLevel);
            }

            if (Codepage != null) {
                WriteOption(writer, "codepage", Codepage);
            }

            // platform
            if (Platform != null) {
                if (SupportsPlatform) {
                    WriteOption(writer, "platform", Platform);
                } else {
                    Log(Level.Warning, ResourceUtils.GetString("String_CompilerDoesNotSupportPlatform"),
                        Project.TargetFramework.Description);
                }
            }

            if (VersionSafe) {
                WriteOption(writer, "versionsafe");
            }

            // win32res
            if (Win32Res != null) {
                WriteOption (writer, "win32res", Win32Res.FullName);
            }
        }

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>
        /// For the JScript.NET compiler, the file extension is always <c>js</c>.
        /// </value>
        public override string Extension { 
            get { return "js"; }
        }

        /// <summary>
        /// Gets the class name regular expression for the language of the 
        /// current compiler.
        /// </summary>
        /// <value>
        /// Class name regular expression for the language of the current 
        /// compiler.
        /// </value>
        protected override Regex ClassNameRegex {
            get { return _classNameRegex; }
        }

        /// <summary>
        /// Gets the namespace regular expression for the language of the 
        /// current compiler.
        /// </summary>
        /// <value>
        /// Namespace regular expression for the language of the current 
        /// compiler.
        /// </value>
        protected override Regex NamespaceRegex {
            get { return _namespaceRegex; }
        }

        #endregion Override implementation of CompilerBase
    }
}
