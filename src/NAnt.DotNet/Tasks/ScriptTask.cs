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
// Sergey Chaban (serge@wildwestsoftware.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean ( ian at maclean.ms )

using System;
using System.Collections;
using System.Collections.Specialized;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.DotNet.Types;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Executes the code contained within the task. This code can include custom extension function definitions. 
    /// Once the script task has executed those custom functions will be available for use in the buildfile.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The <see cref="ScriptTask" /> must contain a single <c>code</c> 
    ///     element, which in turn contains the script code.
    ///     </para>
    ///     <para>
    ///     A static entry point named <c>ScriptMain</c> is required if no custom functions have been defined. It must 
    ///     have a single <see cref="Project"/> parameter.
    ///     </para>
    ///     <para>
    ///     The following namespaces are loaded by default:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>System</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Collections</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Collections.Specialized</description>
    ///         </item>
    ///         <item>
    ///             <description>System.IO</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Text</description>
    ///         </item>
    ///         <item>
    ///             <description>System.Text.RegularExpressions</description>
    ///         </item>
    ///         <item>
    ///             <description>NAnt.Core</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///   <para>Run C# code that writes a message to the build log.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot;&gt;
    ///             &lt;code&gt;&lt;![CDATA[
    ///                 public static void ScriptMain(Project project) {
    ///                     project.Log(Level.Info, &quot;Hello World from a script task using C#&quot;); 
    ///                 }
    ///             ]]&gt;&lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Define a custom function and call it using C#.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot; prefix=&quot;test&quot; &gt;
    ///             &lt;code&gt;&lt;![CDATA[                 
    ///
    ///                 [Function("test-func")]
    ///                 public static string Testfunc(  ) {
    ///                         return "some result !!!!!!!!";
    ///                 }
    ///         ]]&gt;&lt;/code&gt;
    ///         &lt;/script&gt;
    ///         &lt;echo message='${test::test-func()}'/&gt;
    ///
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Run Visual Basic.NET code that writes a message to the build log.</para>
    ///   <code>
    ///         &lt;script language=&quot;VB&quot;&gt;
    ///             &lt;code&gt;&lt;![CDATA[
    ///                 Public Shared Sub ScriptMain(project As Project)
    ///                     project.Log(Level.Info, &quot;Hello World from a script task using Visual Basic.NET&quot;)
    ///                 End Sub
    ///             ]]&gt;&lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    [TaskName("script")]
    public class ScriptTask : NAnt.Core.Task {
        #region Private Instance Fields

        private string _language = "Unknown";
        private AssemblyFileSet _references = new AssemblyFileSet();
        private string _mainClass = "";
        private static Hashtable _compilerMap;
        private string _rootClassName;
        private string _code;
        private string _prefix = "script";
        private NamespaceImportCollection _imports = new NamespaceImportCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly string[] _defaultNamespaces = {
                                                                  "System",
                                                                  "System.Collections",
                                                                  "System.Collections.Specialized",
                                                                  "System.IO",
                                                                  "System.Text",
                                                                  "System.Text.RegularExpressions",
                                                                  "NAnt.Core",
                                                                  "NAnt.Core.Attributes"};

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// The language of the script block (VB, C# or JS).
        /// </summary>
        [TaskAttribute("language", Required=true)]
        public string Language {
            get { return _language; }
            set { _language = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Any required references.
        /// </summary>
        [BuildElement("references")]
        public AssemblyFileSet References {
            get { return _references; }
            set { _references = value; }
        }

        /// <summary>
        /// The name of the main class containing the static <c>ScriptMain</c> entry point. 
        /// </summary>
        [TaskAttribute("mainclass", Required=false)]
        public string MainClass {
            get { return _mainClass; }
            set { _mainClass = StringUtils.ConvertEmptyToNull(value); }
        }
        
        /// <summary>
        /// The namespace prefix for any custom functions defined in the script. If ommitted the prefix will default to 'script'
        /// </summary>
        [TaskAttribute("prefix", Required=false)]
        public string Prefix {
            get { return _prefix; }
            set { _prefix = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The namespaces to import.
        /// </summary>
        [BuildElementCollection("imports", "import")]
        public NamespaceImportCollection Imports {
            get { return _imports; }
        }

        #endregion Public Instance Properties

        #region Static Constructor

        static ScriptTask() {
            _compilerMap = new Hashtable(
                CaseInsensitiveHashCodeProvider.Default,
                CaseInsensitiveComparer.Default);

            CompilerInfo csCompiler = new CompilerInfo(LanguageId.CSharp);
            CompilerInfo vbCompiler = new CompilerInfo(LanguageId.VisualBasic);
            CompilerInfo jsCompiler = new CompilerInfo(LanguageId.JScript);

            _compilerMap["CSHARP"] = csCompiler;
            _compilerMap["C#"] = csCompiler;

            _compilerMap["VISUALBASIC"] = vbCompiler;
            _compilerMap["VB"] = vbCompiler;

            _compilerMap["JSCRIPT"] = jsCompiler;
            _compilerMap["JS"] = jsCompiler;
        }

        #endregion Static Constructor

        #region Override implementation of Task

        /// <summary>
        /// Initializes the task using the specified xml node.
        /// </summary>
        protected override void InitializeTask(XmlNode taskNode) {
            //TODO: Replace XPath Expressions. (Or use namespace/prefix'd element names)
            XmlNodeList codeList = taskNode.SelectNodes("nant:code", NamespaceManager);
            if (codeList.Count < 1) {
                throw new BuildException("<code> block not found.", Location);
            }

            if (codeList.Count > 1) {
                throw new BuildException("Only one <code> block allowed.", Location);
            }
            _code = codeList.Item(0).InnerText;

            _rootClassName = "nant" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Executes the script block.
        /// </summary>
        protected override void ExecuteTask() {
            CompilerInfo compilerInfo = _compilerMap[Language] as CompilerInfo;

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (References.BaseDirectory == null) {
                References.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (compilerInfo == null) {
                throw new BuildException("Unknown language '" + _language + "'.", Location);
            }

            ICodeCompiler compiler = compilerInfo.Compiler;
            CompilerParameters options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = true;
            options.MainClass = MainClass;

            // Add all available assemblies.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    if (!StringUtils.IsNullOrEmpty(asm.Location)) {
                        options.ReferencedAssemblies.Add(asm.Location);
                    }
                } catch (NotSupportedException) {
                    // Ignore - this error is sometimes thrown by asm.Location for certain dynamic assemblies
                }
            }

            foreach (string assemblyName in References.Includes) {
                if (!StringUtils.IsNullOrEmpty(assemblyName)) {
                    options.ReferencedAssemblies.Add(assemblyName);
                }
            }

            StringCollection imports = new StringCollection();

            foreach (NamespaceImport import in Imports) {
                if (import.IfDefined && !import.UnlessDefined) {
                    imports.Add(import.Namespace);
                }
            }

            string code = compilerInfo.GenerateCode(_rootClassName, _code, imports, Prefix );
            Log( Level.Debug, "generated code for the script looks like : \n {0}", code );
            CompilerResults results = compiler.CompileAssemblyFromSource(options, code);

            Assembly compiled = null;
            if (results.Errors.Count > 0) {
                string errors = "Compilation failed:" + Environment.NewLine;
                foreach (CompilerError err in results.Errors) {
                    errors += err.ToString() + Environment.NewLine;
                }
                throw new BuildException(errors, Location);
            } else {
                compiled = results.CompiledAssembly;
            }
            // scan the new assembly for tasks, types and functions
            // Its unlikely that tasks will be defined in buildfiles though.
            int functionSetCount = TypeFactory.AddFunctionSets( compiled );
            TypeFactory.AddDataTypes( compiled );
            TypeFactory.AddTasks( compiled );
            
            string mainClass = _rootClassName;
            if (!StringUtils.IsNullOrEmpty(MainClass)) {
                mainClass += "+" + MainClass;
            }

            Type mainType = compiled.GetType(mainClass);
            if (mainType == null ) {
                throw new BuildException("Invalid mainclass.", Location);
            }

            MethodInfo entry = mainType.GetMethod("ScriptMain");
            // check for task or function definitions.
            if (entry == null ) {
                if (functionSetCount <= 0) {
                    throw new BuildException("Missing entry point.", Location);
                } else {
                    return; // no entry point so nothing to do here beyond loading task and function defs
                }
            }
            
            if (!entry.IsStatic) {
                throw new BuildException("Invalid entry point declaration (should be static).", Location);
            }

            ParameterInfo[] entryParams = entry.GetParameters();

            if (entryParams.Length != 1) {
                throw new BuildException("Invalid entry point declaration (wrong number of parameters).", Location);
            }

            if (entryParams[0].ParameterType.FullName != "NAnt.Core.Project") {
                throw new BuildException("Invalid entry point declaration (invalid parameter type, Project expected).", Location);
            }
        
            try {
                entry.Invoke(null, new object[] {Project});
            } catch (Exception e) {
                // This exception is not likely to tell us much, BUT the 
                // InnerException normally contains the runtime exception
                // thrown by the executed script code.
                throw new BuildException("Script exception.", Location, e.InnerException);
            }
        }

        #endregion Override implementation of Task

        internal enum LanguageId : int {
            CSharp      = 1,
            VisualBasic = 2,
            JScript     = 3
        }

        internal class CompilerInfo {
            private LanguageId _lang;
            public readonly ICodeCompiler Compiler;
            private readonly ICodeGenerator CodeGen;
            private readonly string CodePrologue;

            public CompilerInfo(LanguageId languageId) {
                _lang = languageId;
                Compiler = null;

                CodeDomProvider provider = null;

                switch (languageId) {
                    case LanguageId.CSharp:
                        provider = new Microsoft.CSharp.CSharpCodeProvider();
                        break;
                    case LanguageId.VisualBasic:
                        provider = new Microsoft.VisualBasic.VBCodeProvider();
                        break;
                    case LanguageId.JScript:
                        provider = new Microsoft.JScript.JScriptCodeProvider();
                        break;
                }

                if (provider != null) {
                    Compiler = provider.CreateCompiler();
                    CodeGen = provider.CreateGenerator();
                }

                // Generate default imports section.
                CodePrologue = "";
                CodePrologue += GenerateImportCode(ScriptTask._defaultNamespaces);
        }

            private string GenerateImportCode(IList namespaces) {
                CodeNamespace nspace = new CodeNamespace();
                foreach (string nameSpace in namespaces) {
                    nspace.Imports.Add(new CodeNamespaceImport(nameSpace));
                }

                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                CodeGen.GenerateCodeFromNamespace(nspace, sw, null);
                return sw.ToString();
            }

            public string GenerateCode(string typeName, string codeBody, StringCollection imports, string prefix) {
                CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeName);
                typeDecl.IsClass = true;
                typeDecl.TypeAttributes = TypeAttributes.Public;
                
                // create constructor
                CodeConstructor constructMember = new CodeConstructor();
                constructMember.Attributes = MemberAttributes.Public;
                constructMember.Parameters.Add( new CodeParameterDeclarationExpression( "Project", "project" ));
                constructMember.Parameters.Add( new CodeParameterDeclarationExpression( "PropertyDictionary", "propDict" ));
                
                constructMember.BaseConstructorArgs.Add( new CodeVariableReferenceExpression( "project" ));
                constructMember.BaseConstructorArgs.Add( new CodeVariableReferenceExpression ( "propDict" ));
                typeDecl.Members.Add( constructMember );
                
                typeDecl.BaseTypes.Add( typeof( NAnt.Core.FunctionSetBase ) );
                
                // add FunctionSet attribute
                CodeAttributeDeclaration attrDecl = new CodeAttributeDeclaration( "FunctionSet" );                
                attrDecl.Arguments.Add( new CodeAttributeArgument(new CodeVariableReferenceExpression( "\"" + prefix + "\"" ) ));
                attrDecl.Arguments.Add( new CodeAttributeArgument(new CodeVariableReferenceExpression("\"" + prefix + "\"") ));
             
                typeDecl.CustomAttributes.Add( attrDecl );
                
                // perform some manipulation at the string level.
                StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
                CodeGen.GenerateCodeFromType(typeDecl, sw, null);
                string decl = sw.ToString();
                string extraImports = "";
                if ( imports != null && imports.Count > 0) {
                    extraImports = GenerateImportCode(imports);
                }
                string result = "";
                string declEnd = "}";
             
                if (_lang == LanguageId.VisualBasic) {
                    declEnd = "End";
                }
                int i = decl.LastIndexOf(declEnd);
                result =  CodePrologue + extraImports + decl.Substring(0, i-1) + codeBody + Environment.NewLine + decl.Substring(i);
                
                return result;
            }
        }
    }
}
