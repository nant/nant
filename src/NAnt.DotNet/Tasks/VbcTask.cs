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
// GNU General Public License for details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Mike Krueger (mike@icsharpcode.net)
// Aaron A. Anderson (aaron@skypoint.com | aaron.anderson@farmcreditbank.com)

using System.IO;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Compiles Visual Basic.NET programs.
    /// </summary>
    /// <example>
    ///   <para>Example build file using this task.</para>
    ///   <code><![CDATA[
    ///<project name="Hello World" default="build" basedir=".">
    ///   <property name="basename" value="HelloWorld"/>
    ///   <property name="debug" value="true"/>
    ///   <target name="clean">
    ///      <delete file="${basename}-vb.exe" failonerror="false"/>
    ///      <delete file="${basename}-vb.pdb" failonerror="false"/>
    ///   </target>
    ///   <target name="build">
    ///      <vbc target="exe" output="${basename}-vb.exe">
    ///         <sources>
    ///            <includes name="${basename}.vb"/>
    ///         </sources>
    ///      </vbc>
    ///   </target>
    ///   <target name="debug" depends="clean">
    ///      <vbc target="exe" output="${basename}-vb.exe" debug="${debug}">
    ///         <sources>
    ///            <includes name="${basename}.vb"/>
    ///         </sources>
    ///      </vbc>
    ///   </target>
    ///</project>]]>
    ///   </code>
    /// </example>
    [TaskName("vbc")]
    public class VbcTask : FXCompilerBase {
        #region Private Instance Fields

        string _baseAddress     = null;
        string _imports         = null;
        string _optionCompare   = null;
        bool   _optionExplicit  = false;
        bool   _optionStrict    = false;
        bool   _optionOptimize  = false;
        bool   _removeintchecks = false;
        string _rootNamespace   = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>Specifies whether <c>/baseaddress</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfbaseaddressspecifybaseaddressofdll.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this property is a string that makes up a 32bit hexidecimal number.</value>
        [TaskAttribute("baseaddress")]
        public string BaseAddress   { get { return _baseAddress; } set {_baseAddress = value;}}

        /// <summary>Specifies whether the <c>/imports</c> option gets passed to the compiler</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfImportImportNamespaceFromSpecifiedAssembly.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute is a string that contains one or more namespaces separated by commas.</value>
        /// <example>Example of an imports attribute
        /// <code><![CDATA[imports="Microsoft.VisualBasic, System, System.Collections, System.Data, System.Diagnostics"]]></code></example>
        [TaskAttribute("imports")]
        public string Imports         { get { return _imports; } set {_imports = value;}}

        /// <summary>Specifies whether <c>/optioncompare</c> option gets passed to the compiler</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptioncompareSpecifyHowStringsAreCompared.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this property must be either <c>text</c>, <c>binary</c>, or an empty string.  If the value is <c>false</c> or empty string, the switch is omitted.</value>
        [TaskAttribute("optioncompare")]
        public string OptionCompare   { get { return _optionCompare; } set {_optionCompare = value;}}

        /// <summary>Specifies whether the <c>/optionexplicit</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionexplicitRequireExplicitDeclarationOfVariables.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute must be either <c>true</c> or <c>false</c>.  If <c>false</c>, the switch is omitted.</value>
        [TaskAttribute("optionexplicit")]
        [BooleanValidator()]
        public bool   OptionExplicit  { get { return _optionExplicit; } set {_optionExplicit = value;}}
        
        /// <summary>Specifies whether the <c>/optimize</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfoptimizeenabledisableoptimizations.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute must be either <c>true</c> or <c>false</c>.  If <c>false</c>, the switch is omitted.</value>
        [TaskAttribute("optionoptimize")]
        [BooleanValidator()]
        public bool   OptionOptimize{ get { return _optionOptimize; } set {_optionOptimize = value;}}

        /// <summary>Specifies whether the <c>/optionstrict</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionstrictEnforceStrictTypeSemantics.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute must be either <c>true</c> or <c>false</c>.  If <c>false</c>, the switch is omitted.</value>
        [TaskAttribute("optionstrict")]
        [BooleanValidator()]
        public bool   OptionStrict    { get { return _optionStrict; } set {_optionStrict = value;}}

        /// <summary>Specifies whether the <c>/removeintchecks</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRemoveintchecksRemoveInteger-OverflowChecks.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute must be either <c>true</c> or <c>false</c>.  If <c>false</c>, the switch is omitted.</value>
        [TaskAttribute("removeintchecks")]
        [BooleanValidator()]
        public bool   RemoveIntChecks { get { return _removeintchecks; } set {_removeintchecks = value;}}

        /// <summary>Specifies whether the <c>/rootnamespace</c> option gets passed to the compiler.</summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRootnamespace.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The value of this attribute is a string that contains the root namespace of the project.</value>
        [TaskAttribute("rootnamespace")]
        public string RootNamespace   { get { return _rootNamespace; } set {_rootNamespace = value;}}

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
                    return Project.CurrentFramework.BasicCompilerName;
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
                    if (Project.CurrentFramework.Name.IndexOf("mono", 0) != -1) {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Override implementation of CompilerBase

        /// <summary>
        /// Local override to ensure the Rootnamespace is prefixed
        /// </summary>
        /// <param name="resxPath"></param>
        /// <returns></returns>
        protected override string GetFormNamespace(string resxPath){
            string baseNamespace = base.GetFormNamespace( resxPath );
            if ( RootNamespace  != null ) {
                if ( baseNamespace.Length > 0 ) {
                    return RootNamespace +  "." + baseNamespace;
                }
                else {
                    return RootNamespace;
                }
            } 
            else {
                return baseNamespace;
            }            
        }

        /// <summary>
        /// Writes the compiler options to the specified TextWriter.
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteOptions(TextWriter writer) {
            if (BaseAddress != null) {
                WriteOption(writer, "baseaddress", BaseAddress);
            }

            if (Debug) {
                WriteOption(writer, "debug");
                WriteOption(writer, "define", "DEBUG=True");
                WriteOption(writer, "define", "TRACE=True");
            }

            if (Imports != null) {
                WriteOption(writer, "imports", Imports); 
            }

            if (OptionCompare != null) {
                WriteOption(writer, "optioncompare", OptionCompare);
            }

            if (OptionExplicit) {
                WriteOption(writer, "optionexplicit");
            }

            if (OptionStrict) {
                WriteOption(writer, "optionstrict");
            }

            if (RemoveIntChecks) {
                WriteOption(writer, "removeintchecks");
            }	

            if (OptionOptimize) {
                WriteOption(writer, "optimize");
            }	

            if (RootNamespace != null) {
                WriteOption(writer, "rootnamespace", RootNamespace);
            }
        }    
    
        protected override string Extension { 
            get { return "vb"; }
        }

        #endregion Override implementation of CompilerBase
    }
}
