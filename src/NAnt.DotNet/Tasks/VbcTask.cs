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

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;


namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles Visual Basic.NET programs.
    /// </summary>
    /// <example>
    ///   <para>Example build file using this task.</para>
    ///   <code>
    ///     <![CDATA[
    ///<project name="Hello World" default="build" basedir=".">
    ///   <property name="basename" value="HelloWorld" />
    ///   <property name="debug" value="true" />
    ///   <target name="clean">
    ///      <delete file="${basename}-vb.exe" failonerror="false" />
    ///      <delete file="${basename}-vb.pdb" failonerror="false" />
    ///   </target>
    ///   <target name="build">
    ///      <vbc target="exe" output="${basename}-vb.exe">
    ///         <sources>
    ///            <includes name="${basename}.vb" />
    ///         </sources>
    ///      </vbc>
    ///   </target>
    ///   <target name="debug" depends="clean">
    ///      <vbc target="exe" output="${basename}-vb.exe" debug="${debug}">
    ///         <sources>
    ///            <includes name="${basename}.vb" />
    ///         </sources>
    ///      </vbc>
    ///   </target>
    ///</project>
    ///    ]]>
    ///   </code>
    /// </example>
    [TaskName("vbc")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class VbcTask : CompilerBase {
        #region Private Instance Fields

        private string _baseAddress = null;
        private string _imports = null;
        private string _optionCompare = null;
        private bool _optionExplicit = false;
        private bool _optionStrict = false;
        private bool _optionOptimize = false;
        private bool _removeintchecks = false;
        private string _rootNamespace = null;

        #endregion Private Instance Fields
        
        #region Private Static Fields

        private static Regex _classNameRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.]+|Class\s+(?<class>\w+)|(?<keyword>\w+))*");
        private static Regex _namespaceRegex = new Regex(@"^((?<comment>/\*.*?(\*/|$))|[\s\.]+|Namespace\s+(?<namespace>(\w+(\.\w+)*)+)|(?<keyword>\w+))*");

        #endregion Private Static Fields
     
        #region Public Instance Properties

        /// <summary>
        /// Specifies whether the <c>/baseaddress</c> option gets passed to the 
        /// compiler.
        /// </summary>
        /// <value>
        /// The value of this property is a string that makes up a 32bit hexadecimal 
        /// number.
        /// </value>
        /// <remarks>
        /// <a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfbaseaddressspecifybaseaddressofdll.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("baseaddress")]
        public string BaseAddress {
            get { return _baseAddress; }
            set { _baseAddress = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the <c>/imports</c> option gets passed to the 
        /// compiler.
        /// </summary>
        /// <value>
        /// The value of this attribute is a string that contains one or more 
        /// namespaces separated by commas.
        /// </value>
        /// <remarks>
        /// <a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfImportImportNamespaceFromSpecifiedAssembly.htm">See the Microsoft.NET Framework SDK documentation for details.</a>
        /// </remarks>
        /// <example>Example of an imports attribute
        /// <code><![CDATA[imports="Microsoft.VisualBasic, System, System.Collections, System.Data, System.Diagnostics"]]></code>
        /// </example>
        [TaskAttribute("imports")]
        public string Imports {
            get { return _imports; }
            set { _imports = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether <c>/optioncompare</c> option gets passed to the 
        /// compiler.
        /// </summary>
        /// <value>
        /// <c>text</c>, <c>binary</c>, or an empty string.  If the value is 
        /// <see langword="false" /> or an empty string, the option will not be 
        /// passed to the compiler.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptioncompareSpecifyHowStringsAreCompared.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optioncompare")]
        public string OptionCompare {
            get { return _optionCompare; }
            set { _optionCompare = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies whether the <c>/optionexplicit</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionexplicitRequireExplicitDeclarationOfVariables.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionexplicit")]
        [BooleanValidator()]
        public bool OptionExplicit {
            get { return _optionExplicit; }
            set { _optionExplicit = value; }
        }
        
        /// <summary>
        /// Specifies whether the <c>/optimize</c> option gets passed to the 
        /// compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfoptimizeenabledisableoptimizations.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionoptimize")]
        [BooleanValidator()]
        public bool OptionOptimize {
            get { return _optionOptimize; }
            set { _optionOptimize = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/optionstrict</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfOptionstrictEnforceStrictTypeSemantics.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("optionstrict")]
        [BooleanValidator()]
        public bool OptionStrict {
            get { return _optionStrict; }
            set { _optionStrict = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/removeintchecks</c> option gets passed to 
        /// the compiler. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the option should be passed to the compiler; 
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRemoveintchecksRemoveInteger-OverflowChecks.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("removeintchecks")]
        [BooleanValidator()]
        public bool RemoveIntChecks {
            get { return _removeintchecks; }
            set { _removeintchecks = value; }
        }

        /// <summary>
        /// Specifies whether the <c>/rootnamespace</c> option gets passed to 
        /// the compiler.
        /// </summary>
        /// <value>
        /// The value of this attribute is a string that contains the root 
        /// namespace of the project.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/vblr7net/html/valrfRootnamespace.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("rootnamespace")]
        public string RootNamespace {
            get { return _rootNamespace; }
            set { _rootNamespace = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of CompilerBase

        /// <summary>
        /// Writes additional directories to search in for assembly references
        /// to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to which the compiler options should be written.</param>
        protected override void WriteLibOptions(TextWriter writer) {
            foreach (string libPath in Lib.DirectoryNames) {
                WriteOption(writer, "libpath", libPath);
            }
        }

        /// <summary>
        /// Local override to ensure the Rootnamespace is prefixed
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override ResourceLinkage GetFormResourceLinkage(string fileName ) {
            ResourceLinkage resourceLinkage = base.GetFormResourceLinkage(fileName); // try and get it from matching form
          
            if (!StringUtils.IsNullOrEmpty(RootNamespace)) {
                if (!StringUtils.IsNullOrEmpty(resourceLinkage.NamespaceName )) {
                    resourceLinkage.NamespaceName = RootNamespace + "." + resourceLinkage.NamespaceName;
                } else {
                    resourceLinkage.NamespaceName =  RootNamespace;
                }
            } 
            return resourceLinkage;
        }

        /// <summary>
        /// Writes the compiler options to the specified <see cref="TextWriter" />.
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

            if (OptionCompare != null && OptionCompare.ToUpper(CultureInfo.InvariantCulture) != "FALSE") {
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

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>
        /// For the VB.NET compiler, the file extension is always <c>vb</c>.
        /// </value>
        protected override string Extension {
            get { return "vb"; }
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
