// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Gordon Weakliem
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
// Gordon Weakliem (gweakliem@oddpost.com)
// Gert Driesen (gert.driesen@ardatis.com)
// Ian MacLean (ian_maclean@another.com)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Generates an AssemblyInfo file using the attributes given.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Create a C# AssemblyInfo file containing the specified assembly-level 
    ///   attributes.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <asminfo output="AssemblyInfo.cs" language="CSharp">
    ///     <imports>
    ///         <import name="System" />
    ///         <import name="System.Reflection" />
    ///         <import name="System.EnterpriseServices" />
    ///         <import name="System.Runtime.InteropServices" />
    ///     </imports>
    ///     <attributes>
    ///         <attribute type="ComVisibleAttribute" value="false" />
    ///         <attribute type="CLSCompliantAttribute" value="true" />
    ///         <attribute type="AssemblyVersionAttribute" value="1.0.0.0" />
    ///         <attribute type="AssemblyTitleAttribute" value="My fun assembly" />
    ///         <attribute type="AssemblyDescriptionAttribute" value="More fun than a barrel of monkeys" />
    ///         <attribute type="AssemblyCopyrightAttribute" value="Copyright (c) 2002, Monkeyboy, Inc." />
    ///         <attribute type="ApplicationNameAttribute" value="FunAssembly" />
    ///     </attributes>
    ///     <references>
    ///         <includes name="System.EnterpriseServices.dll" />
    ///     </references>
    /// </asminfo>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("asminfo")]
    public class AssemblyInfoTask : Task {
        #region Private Instance Fields

        private FileInfo _output;
        private CodeLanguage _language = CodeLanguage.CSharp;
        private AssemblyAttributeCollection _attributes = new AssemblyAttributeCollection();
        private NamespaceImportCollection _imports = new NamespaceImportCollection();
        private FileSet _references = new FileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of the AssemblyInfo file to generate.
        /// </summary>
        /// <value>
        /// The name of the AssemblyInfo file to generate.
        /// </value>
        [TaskAttribute("output", Required=true)]
        public FileInfo Output {
            get { return _output; }
            set { _output = value; }
        }

        /// <summary>
        /// The code language in which the AssemblyInfo file should be 
        /// generated - either <see cref="CodeLanguage.CSharp" />  or 
        /// <see cref="CodeLanguage.VB" />.
        /// </summary>
        /// <value>
        /// The code language in which the AssemblyInfo file should be 
        /// generated.
        /// </value>
        [TaskAttribute("language", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public CodeLanguage Language {
            get { return _language; }
            set { 
                if (!Enum.IsDefined(typeof(CodeLanguage), value)) {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, 
                        "An invalid type {0} was specified.", value)); 
                } else {
                    _language = value;
                }
            }
        }

        /// <summary>
        /// The assembly-level attributes to generate.
        /// </summary>
        /// <value>
        /// The assembly-level attributes to generate.
        /// </value>
        [BuildElementCollection("attributes", "attribute")]
        public AssemblyAttributeCollection AssemblyAttributes {
            get { return _attributes; }
        }

        /// <summary>
        /// The namespaces to import.
        /// </summary>
        /// <value>
        /// The namespaces to import.
        /// </value>
        [BuildElementCollection("imports", "import")]
        public NamespaceImportCollection Imports {
            get { return _imports; }
        }

        /// <summary>
        /// Assembly files used to locate the types of the specified attributes.
        /// </summary>
        [BuildElement("references")]
        public FileSet References {
            get { return _references; }
            set { _references = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Generates an AssemblyInfo file.
        /// </summary>
        protected override void ExecuteTask() {
            try {
                StringCollection imports = new StringCollection();

                foreach (NamespaceImport import in Imports) {
                    if (import.IfDefined && !import.UnlessDefined) {
                        imports.Add(import.Namespace);
                    }
                }

                // ensure base directory is set, even if fileset was not initialized
                // from XML
                if (References.BaseDirectory == null) {
                    References.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
                }

                // fix references to system assemblies
                if (Project.CurrentFramework != null) {
                    foreach (string pattern in References.Includes) {
                        if (Path.GetFileName(pattern) == pattern) {
                            string frameworkDir = Project.CurrentFramework.FrameworkAssemblyDirectory.FullName;
                            string localPath = Path.Combine(References.BaseDirectory.FullName, pattern);
                            string fullPath = Path.Combine(frameworkDir, pattern);

                            if (!File.Exists(localPath) && File.Exists(fullPath)) {
                                // found a system reference
                                References.FileNames.Add(fullPath);
                            }
                        }
                    }
                }

                using (StreamWriter writer = new StreamWriter(Output.FullName, false, System.Text.Encoding.Default)) {
                    // create new instance of CodeProviderInfo for specified CodeLanguage
                    CodeProvider codeProvider = new CodeProvider(Language);

                    // only generate imports here for C#, for VB we create the 
                    // imports as part of the assembly attributes compile unit
                    if (Language == CodeLanguage.CSharp) {
                        // generate imports code
                        codeProvider.GenerateImportCode(imports, writer);
                    }

                    // generate code for assembly attributes
                    codeProvider.GenerateAssemblyAttributesCode(AssemblyAttributes, imports, References.FileNames, writer);

                    // close writer
                    writer.Close();
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(
                    CultureInfo.InvariantCulture,
                    "AssemblyInfo file '{0}' could not be generated.",
                    Output.FullName), Location, ex);
            }
        }

        #endregion Override implementation of Task

        /// <summary>
        /// Defines the supported code languages for generating an AssemblyInfo
        /// file.
        /// </summary>
        public enum CodeLanguage : int {
            /// <summary>
            /// A value for generating C# code.
            /// </summary>
            CSharp = 0,

            /// <summary>
            /// A value for generating JScript code.
            /// </summary>
            JScript = 1,

            /// <summary>
            /// A value for generating Visual Basic code.
            /// </summary>
            VB = 2,
        }

        /// <summary> 
        /// Encapsulates functionality to generate a code file with imports
        /// and assembly-level attributes.
        /// </summary>
        internal class CodeProvider {
            #region Private Instance Fields

            private CodeLanguage _language;
            private readonly ICodeGenerator _generator;

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="CodeProvider" />
            /// for the specified <see cref="CodeLanguage" />.
            /// </summary>
            /// <param name="codeLanguage">The <see cref="CodeLanguage" /> for which an instance of the <see cref="CodeProvider" /> class should be initialized.</param>
            public CodeProvider(CodeLanguage codeLanguage) {
                CodeDomProvider provider = null;

                switch (codeLanguage) {
                    case CodeLanguage.CSharp:
                        provider = new Microsoft.CSharp.CSharpCodeProvider();
                        break;
                    case CodeLanguage.JScript:
                        throw new NotSupportedException("Generating a JSCript AssemblyInfo file is not supported at this moment.");
                    case CodeLanguage.VB:
                        provider = new Microsoft.VisualBasic.VBCodeProvider();
                        break;
                    default:
                        throw new NotSupportedException("The specified code language is not supported.");
                }

                _generator = provider.CreateGenerator();
                _language = codeLanguage;
            }

            #endregion Public Instance Constructors
    
            #region Private Instance Properties

            /// <summary>
            /// Gets the <see cref="CodeLanguage" /> in which the AssemblyInfo
            /// code will be generated.
            /// </summary>
            private CodeLanguage Language {
                get { return _language; }
            }

            /// <summary>
            /// Gets the <see cref="ICodeGenerator" /> that will be used to 
            /// generate the AssemblyInfo code.
            /// </summary>
            private ICodeGenerator Generator {
                get { return _generator; }
            }

            #endregion Private Instance Properties

            #region Public Instance Methods

            /// <summary>
            /// Generates code for the specified imports.
            /// </summary>
            /// <param name="imports">The imports for which code should be generated.</param>
            /// <param name="writer">The <see cref="TextWriter" /> to which the generated code will be written.</param>
            public void GenerateImportCode(StringCollection imports, TextWriter writer) {
                CodeNamespace codeNamespace = new CodeNamespace();

                foreach (string import in imports) {
                    codeNamespace.Imports.Add(new CodeNamespaceImport(import));
                }

                Generator.GenerateCodeFromNamespace(codeNamespace, writer, new CodeGeneratorOptions());
            }

            /// <summary>
            /// Generates code for the specified assembly attributes.
            /// </summary>
            /// <param name="assemblyAttributes">The assembly attributes for which code should be generated.</param>
            /// <param name="imports">Imports used to resolve the assembly attribute names to fully qualified type names.</param>
            /// <param name="assemblies">Assembly that will be used to resolve the attribute names to <see cref="Type" /> instances.</param>
            /// <param name="writer">The <see cref="TextWriter" /> to which the generated code will be written.</param>
            public void GenerateAssemblyAttributesCode(AssemblyAttributeCollection assemblyAttributes, StringCollection imports, StringCollection assemblies, TextWriter writer) {
                CodeCompileUnit codeCompileUnit = new CodeCompileUnit();

                // for C# the imports were already generated, as the # generator
                // will otherwise output the imports after the assembly attributes
                if (Language == CodeLanguage.VB) {
                    CodeNamespace codeNamespace = new CodeNamespace();

                    foreach (string import in imports) {
                        codeNamespace.Imports.Add(new CodeNamespaceImport(import));
                    }

                    codeCompileUnit.Namespaces.Add(codeNamespace);
                }

                foreach (AssemblyAttribute assemblyAttribute in assemblyAttributes) {
                    if (assemblyAttribute.IfDefined && !assemblyAttribute.UnlessDefined) {
                        // create new assembly-level attribute
                        CodeAttributeDeclaration codeAttributeDeclaration = new CodeAttributeDeclaration(assemblyAttribute.TypeName);

                        if (assemblyAttribute.AsIs) {
                            codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodeSnippetExpression(assemblyAttribute.Value)));
                        } else {
                            // convert string value to type expected by attribute constructor
                            object typedValue = GetTypedValue(assemblyAttribute, assemblies, imports);
                            if (typedValue != null) {
                                // add typed value to attribute arguments
                                codeAttributeDeclaration.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(typedValue)));
                            }
                        }

                        // add assembly-level argument to code compile unit
                        codeCompileUnit.AssemblyCustomAttributes.Add(codeAttributeDeclaration);
                    }
                }

                Generator.GenerateCodeFromCompileUnit(codeCompileUnit, writer, new CodeGeneratorOptions());
            }

            #endregion Public Instance Methods

            #region Private Instance Methods

            private object GetTypedValue(AssemblyAttribute attribute, StringCollection assemblies, StringCollection imports) {
                // locate type assuming TypeName is fully qualified typename
                AppDomain newDomain = AppDomain.CreateDomain("TypeGatheringDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup());
                TypedValueGatherer typedValueGatherer = (TypedValueGatherer) 
                    newDomain.CreateInstanceAndUnwrap(typeof(TypedValueGatherer).Assembly.FullName, 
                    typeof(TypedValueGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance, 
                    null, new object[0], CultureInfo.InvariantCulture, new object[0], 
                    AppDomain.CurrentDomain.Evidence);

                object typedValue = typedValueGatherer.GetTypedValue(
                    assemblies, imports, attribute.TypeName, attribute.Value);

                // unload newly created AppDomain
                AppDomain.Unload(newDomain);

                return typedValue;
            }

            #endregion Private Instance Methods
        }

        /// <summary>
        /// Responsible for returning the specified value converted to a 
        /// <see cref="Type" /> accepted by a constructor for a given
        /// <see cref="Type" />.
        /// </summary>
        private class TypedValueGatherer : MarshalByRefObject {
            /// <summary>
            /// Retrieves the specified <see cref="Type" /> corresponding with the specified 
            /// type name from a list of assemblies.
            /// </summary>
            /// <param name="assemblies">The collection of assemblies that the type should tried to be instantiated from.</param>
            /// <param name="imports">The list of imports that can be used to resolve the typename to a full typename.</param>
            /// <param name="typename">The typename that should be used to determine the type to which the specified value should be converted.</param>
            /// <param name="value">The <see cref="string" /> value that should be converted to a typed value.</param>
            /// <returns></returns>
            /// <exception cref="BuildException">
            /// <para><paramref name="value" /> is <see langword="null" /> and the <see cref="Type" /> identified by <paramref name="typename" /> has no default public constructor.</para>
            /// <para>-or-</para>
            /// <para><paramref name="value" /> cannot be converted to a value that's suitable for one of the constructors of the <see cref="Type" /> identified by <paramref name="typename" />.</para>
            /// <para>-or-</para>
            /// <para>The <see cref="Type" /> identified by <paramref name="typename" /> has no suitable constructor.</para>
            /// <para>-or-</para>
            /// <para>A <see cref="Type" /> identified by <paramref name="typename" /> could not be located or loaded.</para>
            /// </exception>
            public object GetTypedValue(StringCollection assemblies, StringCollection imports, string typename, string value) {
                // create assembly resolver
                AssemblyResolver assemblyResolver = new AssemblyResolver();

                // attach assembly resolver to the current domain
                assemblyResolver.Attach();

                try {
                    Type type = null;

                    // load each assembly and try to get type from it
                    foreach (string assemblyFileName in assemblies) {
                        // load assembly from filesystem
                        Assembly assembly = Assembly.LoadFrom(assemblyFileName, AppDomain.CurrentDomain.Evidence);
                        // try to load type from assembly
                        type = assembly.GetType(typename, false, false);
                        if (type == null) {
                            foreach (string import in imports) {
                                type = assembly.GetType(import + "." + typename, false, false);
                                if (type != null) {
                                    break;
                                }
                            }
                        }

                        if (type != null) {
                            break;
                        }
                    }

                    // try to load type from all assemblies loaded from disk, if
                    // it was not loaded from the references assemblies 
                    if (type == null) {
                        type = Type.GetType(typename, false, false);
                        if (type == null) {
                            foreach (string import in imports) {
                                type = Type.GetType(import + "." + typename, false, false);
                                if (type != null) {
                                    break;
                                }
                            }
                        }
                    }

                    if (type != null) {
                        object typedValue = null;
                        if (value == null) {
                            ConstructorInfo defaultConstructor = type.GetConstructor(
                                BindingFlags.Public | BindingFlags.Instance, null, 
                                new Type[0], new ParameterModifier[0]);
                            if (defaultConstructor != null) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture, 
                                    "Assembly attribute '{0}' has no default public constructor.",
                                    type.FullName), Location.UnknownLocation);
                            }
                            typedValue = null;
                        } else {
                            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                            for (int counter = 0; counter < constructors.Length; counter++) {
                                ParameterInfo[] parameters = constructors[counter].GetParameters();
                                if (parameters.Length == 1) {
                                    if (parameters[0].ParameterType.IsPrimitive || parameters[0].ParameterType == typeof(string)) {
                                        try {
                                            // convert value to type of constructor parameter
                                            typedValue = Convert.ChangeType(value, parameters[0].ParameterType, CultureInfo.InvariantCulture);
                                            break;
                                        } catch (Exception ex) {
                                            throw new BuildException(string.Format(
                                                CultureInfo.InvariantCulture, 
                                                "Value '{0}' cannot be converted to type '{1}' of assembly attribute {2}.",
                                                value, parameters[0].ParameterType.FullName, type.FullName), 
                                                Location.UnknownLocation, ex);
                                        }
                                    }
                                }
                            }

                            if (typedValue == null) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture, 
                                    "Value of assembly attribute '{0}' cannot be set as it has no constructor accepting a primitive type or string.",
                                    typename), Location.UnknownLocation);
                            }
                        }

                        return typedValue;
                    } else {
                        throw new BuildException(string.Format(
                            CultureInfo.InvariantCulture, 
                            "Assembly attribute with type '{0}' could not be loaded.",
                            typename), Location.UnknownLocation);
                    }
                } finally {
                    // detach assembly resolver from the current domain
                    assemblyResolver.Detach();
                }
            }
        }
    }
}

