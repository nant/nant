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

using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.CodeDom;
using System.CodeDom.Compiler;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Executes the code contained within the task.</summary>
    /// <remarks>
    ///     <para>
    ///         The <c>script</c> element must contain a single <c>code</c> element, which in turn contains the script code.
    ///     </para>
    ///     <para>
    ///         A static entry point named <c>ScriptMain</c> is required.   It must have a single <see cref="Project"/> parameter.
    ///     </para>
    ///     <para>
    ///         The following namespaces are loaded by default:
    ///         System,
    ///         System.Collections,
    ///         System.Collections.Specialized,
    ///         System.IO,
    ///         System.Text,
    ///         System.Text.RegularExpressions and
    ///         SourceForge.NAnt.
    ///     </para>
    /// </remarks>
    /// <example>
    ///   <para>Run C# code.</para>
    ///   <code>
    ///         &lt;script language=&quot;C#&quot;&gt;
    ///             &lt;code&gt;&lt;![CDATA[
    ///                 public static void ScriptMain(Project project) {
    ///                     System.Console.WriteLine(&quot;Hello World from a script task using C#&quot;); 
    ///                 }
    ///             ]]&gt;&lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Run Visual Basic.NET code.</para>
    ///   <code>
    ///         &lt;script language=&quot;VB&quot;&gt;
    ///             &lt;code&gt;&lt;![CDATA[
    ///                 Public Shared Sub ScriptMain(project As Project)
    ///                     System.Console.WriteLine(&quot;Hello World from a script task using Visual Basic.NET&quot;)
    ///                 End Sub
    ///             ]]&gt;&lt;/code&gt;
    ///         &lt;/script&gt;
    ///   </code>
    /// </example>
    [TaskName("script")]
    public class ScriptTask : Task {

        string _language = "Unknown";
        FileSet _references = new FileSet();        
        string _mainClass = String.Empty;
        private static Hashtable _compilerMap;
        private string _rootClassName;
        private string _code;
        private ArrayList _imports = new ArrayList();
        private static readonly string[] _defaultNamespaces =  {
            "System",
            "System.Collections",
            "System.Collections.Specialized",
            "System.IO",
            "System.Text",
            "System.Text.RegularExpressions",
            "SourceForge.NAnt",
        };

        /// <summary>The language of the script block (VB, C# or JS).</summary>
        [TaskAttribute("language", Required=true)]
        public string Language { get { return _language; } set { _language = value; } }

        /// <summary>Any required references.</summary>
        [FileSet("references")]
        public FileSet References { get { return _references; } }

        /// <summary>The name of the main class containing the static <c>ScriptMain</c> entry point.</summary>
        [TaskAttribute("mainclass", Required=false)]
        public string MainClass { get { return _mainClass; } set { _mainClass = value; } }

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

        protected override void InitializeTask(XmlNode taskNode) {
            XmlNodeList codeList = taskNode.SelectNodes("code");
            if (codeList.Count < 1) {
                throw new BuildException("<code> block not found.", Location);
            }

            if (codeList.Count > 1) {
                throw new BuildException("Only one <code> block allowed.", Location);
            }
            _code = codeList.Item(0).InnerText;

            _rootClassName = /*Target.Name*/ "xx" + "_script_" +
                (taskNode.GetHashCode() ^ (taskNode.ParentNode.GetHashCode() << 1)).ToString("X");


            _imports.Clear();
            XmlNodeList importsList = taskNode.SelectNodes("imports/import");
            foreach (XmlNode import in importsList) {
                _imports.Add(import.Attributes["name"].InnerText);
            }
        }

        protected override void ExecuteTask() {
            CompilerInfo compilerInfo = _compilerMap[Language] as CompilerInfo;

            if (compilerInfo == null) {
                throw new BuildException("Unknown language '" + _language + "'.", Location);
            }

            ICodeCompiler compiler = compilerInfo.Compiler;
            CompilerParameters options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = true;
            options.MainClass = MainClass;

            // Add NAnt to references.
            // options.ReferencedAssemblies.Add(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName);

            // Add all available assemblies.
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                options.ReferencedAssemblies.Add(asm.Location);
            }

            if (References.BaseDirectory == null) {
                References.BaseDirectory = Project.BaseDirectory;
            }

            foreach (string assemblyName in References.Includes) {
                options.ReferencedAssemblies.Add(assemblyName);
            }

            string code = compilerInfo.GenerateCode(_rootClassName, _code, _imports);

            CompilerResults results = compiler.CompileAssemblyFromSource(options, code);

            Assembly compiled = null;
            if (results.Errors.Count > 0) {
                string errors = "Compilation failed:\n";
                foreach (CompilerError err in results.Errors) {
                    errors += err.ToString() + "\n";
                }
                throw new BuildException(errors, Location);
            } else {
                compiled = results.CompiledAssembly;
            }

            string mainClass = _rootClassName;
            if (MainClass != String.Empty) {
                mainClass += "+" + MainClass;
            }

            Type mainType = compiled.GetType(mainClass);
            if (mainType == null) {
                throw new BuildException("Invalid mainclass.", Location);
            }
            MethodInfo entry = mainType.GetMethod("ScriptMain");

            if (entry == null) {
                throw new BuildException("Missing entry point.", Location);
            }

            if (!entry.IsStatic) {
                throw new BuildException("Invalid entry point declaration (should be static).", Location);
            }

            ParameterInfo[] entryParams = entry.GetParameters();

            if (entryParams.Length != 1) {
                throw new BuildException("Invalid entry point declaration (wrong number of parameters).", Location);
            }

            if (entryParams[0].ParameterType.FullName != "SourceForge.NAnt.Project") {
                throw new BuildException("Invalid entry point declaration (invalid parameter type, Project expected).", Location);
            }

            try {
                entry.Invoke(null, new Object[] { Project });
            } catch (Exception e) {
                // This exception is not likely to tell us much, BUT the 
                // InnerException normally contains the runtime exception
                // thrown by the executed script code.
                throw new BuildException("Script exception.", Location, e.InnerException);
            }
        }

        internal enum LanguageId : int {
            CSharp      = 1,
            VisualBasic = 2,
            JScript     = 3
        }

        internal class CompilerInfo {
            private LanguageId _lang;
            public readonly ICodeCompiler Compiler;
            public readonly ICodeGenerator CodeGen;
            public readonly string CodePrologue;

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

                StringWriter sw = new StringWriter();
                CodeGen.GenerateCodeFromNamespace(nspace, sw, null);
                return sw.ToString();
            }

            public string GenerateCode(string typeName, string codeBody, IList imports) {
                CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeName);
                typeDecl.IsClass = true;
                typeDecl.TypeAttributes = TypeAttributes.Public;
                StringWriter sw = new StringWriter();
                CodeGen.GenerateCodeFromType(typeDecl, sw, null);
                string decl = sw.ToString();

                string extraImports = "";
                if (imports != null && imports.Count > 0) {
                    extraImports = GenerateImportCode(imports);
                }

                string declEnd = "}";
                if (_lang == LanguageId.VisualBasic) declEnd = "End";
                int i = decl.LastIndexOf(declEnd);
                return CodePrologue + extraImports + decl.Substring(0, i-1) + codeBody + "\n" + decl.Substring(i);
            }
        }
    }
}
