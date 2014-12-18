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
// Gert Driesen (drieseng@users.sourceforge.net)
// Ian MacLean (ian_maclean@another.com)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Security.Cryptography;
using System.Text;

using System.Security;
using System.Security.Permissions;
using NAnt.Core;
using NAnt.Core.Attributes;
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
    ///         <import namespace="System" />
    ///         <import namespace="System.Reflection" />
    ///         <import namespace="System.EnterpriseServices" />
    ///         <import namespace="System.Runtime.InteropServices" />
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
    ///         <include name="System.EnterpriseServices.dll" />
    ///     </references>
    /// </asminfo>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///     <para>
    ///     Create a C# AssemblyInfo file containing an attribute with multiple
    ///     named properties by setting the <see cref="AssemblyAttribute.AsIs" /> 
    ///     attribute on the <see cref="AssemblyAttribute" /> element to 
    ///     <see langword="true" />.
    ///     </para>
    ///   <code>
    ///     <![CDATA[
    /// <asminfo output="AssemblyInfo.cs" language="CSharp">
    ///     <imports>
    ///         <import namespace="log4net.Config" />
    ///     </imports>
    ///     <attributes>
    ///         <attribute type="DOMConfiguratorAttribute" value="ConfigFile=&quot;config.log4net&quot;,Watch=true" asis="true" />
    ///     </attributes>
    ///     <references>
    ///         <include name="log4net.dll" />
    ///     </references>
    /// </asminfo>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("asminfo")]
    [Serializable()]
    public class AssemblyInfoTask : Task {
        #region Private Instance Fields

        private FileInfo _output;
        private CodeLanguage _language = CodeLanguage.CSharp;
        private AssemblyAttributeCollection _attributes = new AssemblyAttributeCollection();
        private NamespaceImportCollection _imports = new NamespaceImportCollection();
        private AssemblyFileSet _references = new AssemblyFileSet();

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
        /// generated.
        /// </summary>
        [TaskAttribute("language", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public CodeLanguage Language {
            get { return _language; }
            set {
                if (!Enum.IsDefined(typeof(CodeLanguage), value)) {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA2002"), value));
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
        [BuildElement("imports")]
        public NamespaceImportCollection Imports {
            get { return _imports; }
            set { _imports = value; }
        }

        /// <summary>
        /// Assembly files used to locate the types of the specified attributes.
        /// </summary>
        [BuildElement("references")]
        public AssemblyFileSet References {
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

                // write out code to memory stream, so we can compare it later 
                // to what is already present (if necessary)
                MemoryStream generatedAsmInfoStream = new MemoryStream();

                using (StreamWriter writer = new StreamWriter(generatedAsmInfoStream, Encoding.Default)) {
                    // create new instance of CodeProviderInfo for specified CodeLanguage
                    CodeProvider codeProvider = new CodeProvider(this, Language);

                    // only generate imports here for C#, for VB we create the 
                    // imports as part of the assembly attributes compile unit
                    if (Language == CodeLanguage.CSharp) {
                        // generate imports code
                        codeProvider.GenerateImportCode(imports, writer);
                    }

                    // generate code for assembly attributes
                    codeProvider.GenerateAssemblyAttributesCode(AssemblyAttributes, 
                        imports, References.FileNames, writer);

                    // flush 
                    writer.Flush();

                    // check whether generated source should be persisted
                    if (NeedsPersisting(generatedAsmInfoStream)) {
                        using (FileStream fs = new FileStream(Output.FullName, FileMode.Create, FileAccess.Write)) {
                            byte[] buffer = generatedAsmInfoStream.ToArray();
                            fs.Write(buffer, 0, buffer.Length);
                            fs.Flush();
                            fs.Close();
                            generatedAsmInfoStream.Close();
                        }

                        Log(Level.Info, ResourceUtils.GetString("String_GeneratedFile"),
                            Output.FullName);
                    } else {
                        Log(Level.Verbose, ResourceUtils.GetString("String_FileUpToDate"),
                            Output.FullName);
                    }
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(
                    CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA2004"), Output.FullName), Location, ex);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Determines whether the specified AssemblyInfo file in the given
        /// <see cref="Stream" /> needs to be persisted.
        /// </summary>
        /// <param name="generatedAsmInfoStream"><see cref="Stream" /> holding the newly generated AssemblyInfo source.</param>
        /// <returns>
        /// <see langword="true" /> if the generated AssemblyInfo source needs
        /// to be persisted; otherwise, <see langword="false" />.
        /// </returns>
        private bool NeedsPersisting(Stream generatedAsmInfoStream) {
            // if output file doesn't exist, the stream will always need to be
            // persisted to the filesystem.
            if (!Output.Exists) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileDoesNotExist"),
                    Output.FullName);
                return true;
            }

            byte[] existingAssemblyInfoHash = null;
            byte[] generatedAssemblyInfoHash = null;

            SHA1 hasher = new SHA1CryptoServiceProvider();

            // hash existing AssemblyInfo source
            using (FileStream fs = new FileStream(Output.FullName, FileMode.Open, FileAccess.Read)) {
                existingAssemblyInfoHash = hasher.ComputeHash(fs);
            }

            // hash generated AssemblyInfo source
            generatedAsmInfoStream.Position = 0;
            generatedAssemblyInfoHash = hasher.ComputeHash(generatedAsmInfoStream);

            // release all resources
            hasher.Clear();

            //compare hash of generated source with of existing source
            if (Convert.ToBase64String(generatedAssemblyInfoHash) != Convert.ToBase64String(existingAssemblyInfoHash)) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileNotUpToDate"), 
                    Output.FullName);
                return true;
            } else {
                return false;
            }
        }

        #endregion Private Instance Methods

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

            private readonly CodeLanguage _language;
            private readonly ICodeGenerator _generator;

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="CodeProvider" />
            /// for the specified <see cref="CodeLanguage" />.
            /// </summary>
            /// <param name="assemblyInfoTask">The <see cref="AssemblyInfoTask" /> for which an instance of the <see cref="CodeProvider" /> class should be initialized.</param>
            /// <param name="codeLanguage">The <see cref="CodeLanguage" /> for which an instance of the <see cref="CodeProvider" /> class should be initialized.</param>
            public CodeProvider(AssemblyInfoTask assemblyInfoTask, CodeLanguage codeLanguage) {
                CodeDomProvider provider = null;

                switch (codeLanguage) {
                    case CodeLanguage.CSharp:
                        provider = new Microsoft.CSharp.CSharpCodeProvider();
                        break;
                    case CodeLanguage.JScript:
                        throw new NotSupportedException(ResourceUtils.GetString("NA2008"));
                    case CodeLanguage.VB:
                        provider = new Microsoft.VisualBasic.VBCodeProvider();
                        break;
                    default:
                        throw new NotSupportedException(ResourceUtils.GetString("NA2007"));
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
                PermissionSet domainPermSet = new PermissionSet(PermissionState.Unrestricted);
                AppDomain newDomain = AppDomain.CreateDomain("TypeGatheringDomain", AppDomain.CurrentDomain.Evidence, 
                    AppDomain.CurrentDomain.SetupInformation, domainPermSet);

#if NET_4_0
                TypedValueGatherer typedValueGatherer = (TypedValueGatherer) 
                    newDomain.CreateInstanceAndUnwrap(typeof(TypedValueGatherer).Assembly.FullName, 
                    typeof(TypedValueGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance, 
                    null, new object[0], CultureInfo.InvariantCulture, new object[0]);
#else
                TypedValueGatherer typedValueGatherer = (TypedValueGatherer) 
                    newDomain.CreateInstanceAndUnwrap(typeof(TypedValueGatherer).Assembly.FullName, 
                    typeof(TypedValueGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance, 
                    null, new object[0], CultureInfo.InvariantCulture, new object[0], 
                    AppDomain.CurrentDomain.Evidence);
#endif

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
            #region Override implementation of MarshalByRefObject

            /// <summary>
            /// Obtains a lifetime service object to control the lifetime policy for 
            /// this instance.
            /// </summary>
            /// <returns>
            /// An object of type <see cref="ILease" /> used to control the lifetime 
            /// policy for this instance. This is the current lifetime service object 
            /// for this instance if one exists; otherwise, a new lifetime service 
            /// object initialized with a lease that will never time out.
            /// </returns>
            public override Object InitializeLifetimeService() {
                ILease lease = (ILease) base.InitializeLifetimeService();
                if (lease.CurrentState == LeaseState.Initial) {
                    lease.InitialLeaseTime = TimeSpan.Zero;
                }
                return lease;
            }

            #endregion Override implementation of MarshalByRefObject

            #region Public Instance Methods

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
                    // Try to find the type from typename parameter.
                    Type type = FindType(assemblies, imports, typename);

                    if (type != null) {
                        object typedValue = null;
                        if (value == null) {
                            ConstructorInfo defaultConstructor = type.GetConstructor(
                                BindingFlags.Public | BindingFlags.Instance, null, 
                                new Type[0], new ParameterModifier[0]);
                            if (defaultConstructor == null) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA2005"), type.FullName), Location.UnknownLocation);
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
                                                CultureInfo.InvariantCulture, ResourceUtils.GetString("NA2006"),
                                                value, parameters[0].ParameterType.FullName, type.FullName), 
                                                Location.UnknownLocation, ex);
                                        }
                                    }
                                }
                            }

                            if (typedValue == null) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA2003"), typename),
                                    Location.UnknownLocation);
                            }
                        }

                        return typedValue;
                    } else {
                        if (!typename.EndsWith("Attribute"))
                        {
                            throw new BuildException(string.Format(
                                CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA2039"),typename),
                                Location.UnknownLocation);
                        }
                        else
                        {
                            throw new BuildException(string.Format(
                                CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA2001"),typename),
                                Location.UnknownLocation);
                        }
                    }
                } finally {
                    // detach assembly resolver from the current domain
                    assemblyResolver.Detach();
                }
            }

            #endregion Public Instance Methods

            #region Private Instance Methods

            /// <summary>
            /// Finds a given type from a given list of assemblies and import statements.
            /// </summary>
            /// <param name='assemblies'>
            /// A list of assemblies to search for a given type.
            /// </param>
            /// <param name='imports'>
            /// A list of import statements to search for a given type.
            /// </param>
            /// <param name='typename'>
            /// The name of the type to locate.
            /// </param>
            /// <returns>
            /// The type object found from assemblies and import statements based
            /// on the name of the type.
            /// </returns>
            private Type FindType(StringCollection assemblies, StringCollection imports, string typename)
            {
                Type type = null;

                // load each assembly and try to get type from it
                foreach (string assemblyFileName in assemblies) {
                    // load assembly from filesystem
                    Assembly assembly = Assembly.LoadFrom(assemblyFileName);
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

                return type;
            }

            #endregion Private Instance Methods
        }
    }
}
