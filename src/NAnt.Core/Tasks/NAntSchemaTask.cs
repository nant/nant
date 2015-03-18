// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Creates an XSD File for all available tasks.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This can be used in conjuntion with the command-line option to do XSD 
    ///   Schema validation on the build file.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Creates a <c>NAnt.xsd</c> file in the current project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <nantschema output="NAnt.xsd" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nantschema")]
    public class NAntSchemaTask : Task {
        #region Private Instance Fields

        private FileInfo _outputFile;
        private string _forType;
        private string _targetNamespace = "http://tempuri.org/nant-donotuse.xsd";

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        // Contains names of tasks that should have the "mixed" attribute for the complex types.
        private static readonly string[] mixedTaskNames = new string[] {
            "NAnt.Core.Tasks.DescriptionTask",
            "NAnt.Core.Tasks.EchoTask",
        };

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file to which the XSD should be written.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public virtual FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The target namespace for the output. Defaults to "http://tempuri.org/nant-donotuse.xsd"
        /// </summary>
        [TaskAttribute("target-ns", Required=false)]
        public virtual string TargetNamespace {
            get { return _targetNamespace; }
            set { _targetNamespace = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The <see cref="Type" /> for which an XSD should be created. If not
        /// specified, an XSD will be created for all available tasks.
        /// </summary>
        [TaskAttribute("class", Required=false)]
        public virtual string ForType {
            get { return _forType; }
            set { _forType = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">I the generated XML schema is not valid.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        protected override void ExecuteTask() {
            ArrayList taskTypes;
            ArrayList dataTypes;

            if (ForType == null) {
                taskTypes = new ArrayList(TypeFactory.TaskBuilders.Count);
                dataTypes = new ArrayList(TypeFactory.DataTypeBuilders.Count);

                foreach (TaskBuilder tb in TypeFactory.TaskBuilders) {
                    taskTypes.Add(tb.Assembly.GetType(tb.ClassName, true, true));
                }

                foreach (DataTypeBaseBuilder db in TypeFactory.DataTypeBuilders) {
                    dataTypes.Add(db.Assembly.GetType(db.ClassName, true, true));
                }
            } else {
                taskTypes = new ArrayList(1);
                taskTypes.Add(Type.GetType(ForType, true, true));
                dataTypes = new ArrayList();
            }
            
            MemoryStream ms = new MemoryStream();
            WriteSchema(ms, (Type[]) taskTypes.ToArray(typeof(Type)), 
                (Type[]) dataTypes.ToArray(typeof(Type)), TargetNamespace);

            // reset position of memorystream
            ms.Position = 0;

            // let's validate whether we emitted a valid XML Schema
            try {
                XmlSchema schema = XmlSchema.Read(ms, null);
                schema.Compile(null);
            } catch (XmlSchemaException ex) {
                throw new BuildException ("The generated XML schema is not valid.", 
                    Location, ex);
            }
            // reset position of memorystream
            ms.Position = 0;
            FileIOPermission FilePermission = new FileIOPermission(FileIOPermissionAccess.AllAccess, OutputFile.FullName); 
            FilePermission.Assert();
            using (FileStream file = File.Open(OutputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                byte[] buffer = new byte[4096];
                int bytesRead = ms.Read(buffer, 0, buffer.Length);
                while (bytesRead != 0) {
                    file.Write(buffer, 0, bytesRead);
                    bytesRead = ms.Read(buffer, 0, buffer.Length);
                }
                file.Flush();
                file.Close();
            }

            Log(Level.Info, "Wrote schema to '{0}'.", OutputFile.FullName);
        }

        #endregion Override implementation of Task

        #region Public Static Methods

        /// <summary>
        /// Creates a NAnt Schema for given types
        /// </summary>
        /// <param name="stream">The output stream to save the schema to. If <see langword="null" />, writing is ignored, no exception generated.</param>
        /// <param name="tasks">The list of tasks to generate XML Schema for.</param>
        /// <param name="dataTypes">The list of datatypes to generate XML Schema for.</param>
        /// <param name="targetNS">The target namespace to output.</param>
        /// <returns>The new NAnt Schema.</returns>
        public static XmlSchema WriteSchema(System.IO.Stream stream, Type[] tasks, Type[] dataTypes, string targetNS) {
            NAntSchemaGenerator gen = new NAntSchemaGenerator(tasks, dataTypes, targetNS);

            if (!gen.Schema.IsCompiled) {
                gen.Compile();
            }

            if (stream != null) {
                gen.Schema.Write(stream);
            }

            return gen.Schema;
        }

        #endregion Public Static Methods

        #region Protected Static Methods

        protected static string GenerateIDFromType(Type type) {
            return type.ToString().Replace("+", "-").Replace("[", "_").Replace("]", "_");
        }

        /// <summary>
        /// Creates a new <see cref="XmlSchemaAttribute" /> instance.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="required">Value indicating whether the attribute should be required.</param>
        /// <returns>The new <see cref="XmlSchemaAttribute" /> instance.</returns>
        protected static XmlSchemaAttribute CreateXsdAttribute(string name, bool required) {
            XmlSchemaAttribute newAttr = new XmlSchemaAttribute();

            newAttr.Name= name;

            if (required) {
                newAttr.Use = XmlSchemaUse.Required;
            } else {
                newAttr.Use = XmlSchemaUse.Optional;
            }

            return newAttr;
        }

        /// <summary>
        /// Creates a new <see cref="XmlSchemaSequence" /> instance.
        /// </summary>
        /// <param name="min">The minimum value to allow for this choice</param>
        /// <param name="max">The maximum value to allow, Decimal.MaxValue sets it to 'unbound'</param>
        /// <returns>The new <see cref="XmlSchemaSequence" /> instance.</returns>
        protected static XmlSchemaSequence CreateXsdSequence(Decimal min, Decimal max) {
            XmlSchemaSequence newSeq = new XmlSchemaSequence();

            newSeq.MinOccurs = min;

            if (max != Decimal.MaxValue) {
                newSeq.MaxOccurs = max;
            } else {
                newSeq.MaxOccursString = "unbounded";
            }
            
            return newSeq;
        }    

        protected static XmlNode[] TextToNodeArray(string text) {
            XmlDocument doc = new XmlDocument();

            return new XmlNode[1] {doc.CreateTextNode(text)};
        }

        #endregion Protected Static Methods

        private class NAntSchemaGenerator {
            #region Private Instance Fields

            private IDictionary _nantComplexTypes;
            private XmlSchemaComplexType _targetCT;
            private XmlSchema _nantSchema = new XmlSchema();

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Creates a new instance of the <see cref="NAntSchemaGenerator" />
            /// class.
            /// </summary>
            /// <param name="tasks">Tasks for which a schema should be generated.</param>
            /// <param name="dataTypes">Data Types for which a schema should be generated.</param>
            /// <param name="targetNS">The namespace to use.
            /// <example> http://tempuri.org/nant.xsd </example>
            /// </param>
            public NAntSchemaGenerator(Type[] tasks, Type[] dataTypes, string targetNS) {
                //setup namespace stuff
                if (targetNS != null) {
                    _nantSchema.TargetNamespace = targetNS;
                    _nantSchema.Namespaces.Add("nant", _nantSchema.TargetNamespace);
                }

                // add XSD namespace so that all xsd elements are prefix'd
                _nantSchema.Namespaces.Add("xs", XmlSchema.Namespace);

                _nantSchema.ElementFormDefault = XmlSchemaForm.Qualified;

                // initialize stuff
                _nantComplexTypes = new HybridDictionary(tasks.Length + dataTypes.Length);

                XmlSchemaAnnotation schemaAnnotation = new XmlSchemaAnnotation();
                XmlSchemaDocumentation schemaDocumentation = new XmlSchemaDocumentation();

                string doc = String.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("String_SchemaGenerated"), DateTime.Now);
                schemaDocumentation.Markup = TextToNodeArray(doc);
                schemaAnnotation.Items.Add(schemaDocumentation);
                _nantSchema.Items.Add(schemaAnnotation);

                // create temp list of taskcontainer Complex Types
                ArrayList taskContainerComplexTypes = new ArrayList(4);

                XmlSchemaComplexType containerCT = FindOrCreateComplexType(typeof(TaskContainer));
                if (containerCT.Particle == null) {
                    // just create empty sequence to which elements will 
                    // be added later
                    containerCT.Particle = CreateXsdSequence(0, Decimal.MaxValue);
                }
                taskContainerComplexTypes.Add(containerCT);

                // create temp list of task Complex Types
                ArrayList dataTypeComplexTypes = new ArrayList(dataTypes.Length);

                foreach (Type t in dataTypes) {
                    dataTypeComplexTypes.Add(FindOrCreateComplexType(t));
                }

                foreach (Type t in tasks) {
                    XmlSchemaComplexType taskCT = FindOrCreateComplexType(t);

                    // allow any tasks...
                    if (t.IsSubclassOf(typeof(TaskContainer))) {
                        taskContainerComplexTypes.Add(taskCT);
                    }
                }


                Compile();

                // update the taskcontainerCTs to allow any other task and the 
                // list of tasks generated
                foreach(XmlSchemaComplexType ct in taskContainerComplexTypes) {
                    XmlSchemaSequence seq = ct.Particle as XmlSchemaSequence;

                    if (seq != null) {
                        seq.Items.Add(CreateTaskListComplexType(tasks).Particle);
                    } else {
                        logger.Error("Unable to fixup complextype with children. Particle is not XmlSchemaSequence");
                    }
                }
                Compile();

                // create target ComplexType
                _targetCT = CreateTaskListComplexType(tasks, dataTypes, false);
                _targetCT.Name = "Target";
                // This is a response to Bug#: 3058913.  If not considered neccessary, remove line below.
                _targetCT.IsMixed = true;

                // name attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("name", true));

                // depends attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("depends", false));

                // description attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("description", false));

                // if attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("if", false));

                // unless attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("unless", false));

                _nantSchema.Items.Add(_targetCT);

                Compile();

                // Generate project Element and ComplexType
                XmlSchemaElement projectElement = new XmlSchemaElement();
                projectElement.Name = "project";

                XmlSchemaComplexType projectCT = CreateTaskListComplexType(tasks, dataTypes, true);

                projectElement.SchemaType = projectCT;

                //name attribute
                projectCT.Attributes.Add(CreateXsdAttribute("name", true));

                //default attribute
                projectCT.Attributes.Add(CreateXsdAttribute("default", false));

                //basedir attribute
                projectCT.Attributes.Add(CreateXsdAttribute("basedir", false));

                _nantSchema.Items.Add(projectElement);

                Compile();
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            public XmlSchema Schema {
                get {
                    if (!_nantSchema.IsCompiled) {
                        Compile();
                    }
                    return _nantSchema;
                }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            public void Compile() {
                _nantSchema.Compile(new ValidationEventHandler(ValidationEH));
            }

            #endregion Public Instance Methods

            #region Protected Instance Methods

            protected XmlSchemaComplexType CreateTaskListComplexType(Type[] tasks) {
                return CreateTaskListComplexType(tasks, new Type[0], false);
            }

            protected XmlSchemaComplexType CreateTaskListComplexType(Type[] tasks, Type[] dataTypes, bool includeProjectLevelItems) {
                XmlSchemaComplexType tasklistCT = new XmlSchemaComplexType();
                XmlSchemaChoice choice = new XmlSchemaChoice();
                choice.MinOccurs = 0;
                choice.MaxOccursString = "unbounded";

                tasklistCT.Particle = choice;

                foreach (Type t in tasks) {
                    XmlSchemaElement taskElement = new XmlSchemaElement();
                    string typeId = GenerateIDFromType(t);
                    XmlSchemaComplexType taskCT = FindComplexTypeByID(typeId);

                    taskElement.Name = GetTaskName(t);
                    taskElement.SchemaTypeName = taskCT.QualifiedName;

                    choice.Items.Add(taskElement);
                }

                foreach (Type t in dataTypes) {
                    XmlSchemaElement dataTypeElement = new XmlSchemaElement();
                    string typeId = GenerateIDFromType(t);
                    XmlSchemaComplexType dataTypeCT = FindComplexTypeByID(typeId);

                    dataTypeElement.Name = GetDataTypeName(t);
                    dataTypeElement.SchemaTypeName = dataTypeCT.QualifiedName;

                    choice.Items.Add(dataTypeElement);
                }

                if (includeProjectLevelItems) {
                    XmlSchemaElement targetElement = new XmlSchemaElement();

                    targetElement.Name = "target";
                    targetElement.SchemaTypeName = _targetCT.QualifiedName;

                    choice.Items.Add(targetElement);
                }

                // allow elements from other namespaces
                XmlSchemaAny otherNamespaceAny = new XmlSchemaAny();
                otherNamespaceAny.MinOccurs = 0;
                otherNamespaceAny.MaxOccurs = Decimal.MaxValue;
                otherNamespaceAny.Namespace = "##other";
                otherNamespaceAny.ProcessContents = XmlSchemaContentProcessing.Strict;
                choice.Items.Add(otherNamespaceAny);

                return tasklistCT;
            }

            protected void ValidationEH(object sender, ValidationEventArgs args) {
                if (args.Severity == XmlSeverityType.Warning) {
                    logger.Info("WARNING: ");
                } else if (args.Severity == XmlSeverityType.Error) {
                    logger.Error("ERROR: ");
                }

                XmlSchemaComplexType source = args.Exception.SourceSchemaObject as XmlSchemaComplexType;

                logger.Info(args.ToString());

                if (source != null)
                {
                    logger.InfoFormat(CultureInfo.InvariantCulture,
                        "{0}", 
                        source.Name);
                }
            }

            protected XmlSchemaComplexType FindComplexTypeByID(string id) {
                if (_nantComplexTypes.Contains(id)) {
                    return (XmlSchemaComplexType)_nantComplexTypes[id];
                }
                return null;
            }

            protected XmlSchemaComplexType FindOrCreateComplexType(Type t) {
                XmlSchemaComplexType ct;
                string typeId = GenerateIDFromType(t);

                ct = FindComplexTypeByID(typeId);
                if (ct != null) {
                    return ct;
                }

                ct = new XmlSchemaComplexType();
                ct.Name = typeId;

                // Force mixed attribute for tasks names in the mixedTaskNames array.  Fixes Bug#: 3058913
                if (Array.IndexOf(mixedTaskNames, ct.Name) != -1) {
                    ct.IsMixed = true;
                }

                // add complex type to collection immediately to avoid stack 
                // overflows, when we allow a type to be nested
                _nantComplexTypes.Add(typeId, ct);

#if NOT_IMPLEMENTED
                //
                // TODO - add task/type documentation in the future
                //

                ct.Annotation = new XmlSchemaAnnotation();
                XmlSchemaDocumentation doc = new XmlSchemaDocumentation();
                ct.Annotation.Items.Add(doc);
                doc.Markup = ...;
#endif

                XmlSchemaSequence group1 = null;
                XmlSchemaObjectCollection attributesCollection = ct.Attributes;

                foreach (MemberInfo memInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                    if (memInfo.DeclaringType.Equals(typeof(object))) {
                        continue;
                    }
                   
                    //Check for any return type that is derived from Element

                    // add Attributes
                    TaskAttributeAttribute taskAttrAttr = (TaskAttributeAttribute) 
                        Attribute.GetCustomAttribute(memInfo, typeof(TaskAttributeAttribute),
                        false);
                    BuildElementAttribute buildElemAttr = (BuildElementAttribute)
                        Attribute.GetCustomAttribute(memInfo, typeof(BuildElementAttribute),
                        false);

                    if (taskAttrAttr != null) {
                        XmlSchemaAttribute newAttr = CreateXsdAttribute(taskAttrAttr.Name, taskAttrAttr.Required);
                        attributesCollection.Add(newAttr);
                    } else if (buildElemAttr != null) {
                        // Create individial choice for any individual child Element
                        Decimal min = 0;

                        if (buildElemAttr.Required) {
                            min = 1;
                        }

                        XmlSchemaElement childElement = new XmlSchemaElement();
                        childElement.MinOccurs = min;
                        childElement.MaxOccurs = 1;
                        childElement.Name = buildElemAttr.Name;

                        //XmlSchemaGroupBase elementGroup = CreateXsdSequence(min, Decimal.MaxValue);

                        Type childType;

                        // We will only process child elements if they are defined for Properties or Fields, this should be enforced by the AttributeUsage on the Attribute class
                        if (memInfo is PropertyInfo) {
                            childType = ((PropertyInfo) memInfo).PropertyType;
                        } else if (memInfo is FieldInfo) {
                            childType = ((FieldInfo) memInfo).FieldType;
                        } else  if (memInfo is MethodInfo) {
                            MethodInfo method = (MethodInfo) memInfo;
                            if (method.GetParameters().Length == 1) {
                                childType = method.GetParameters()[0].ParameterType;
                            } else {
                                throw new ApplicationException("Method should have one parameter.");
                            }
                        } else {
                            throw new ApplicationException("Member Type != Field/Property/Method");
                        }

                        BuildElementArrayAttribute buildElementArrayAttribute = (BuildElementArrayAttribute)
                            Attribute.GetCustomAttribute(memInfo, typeof(BuildElementArrayAttribute), false);

                        // determine type of child elements

                        if (buildElementArrayAttribute != null)  {
                            if (buildElementArrayAttribute.ElementType == null) {
                                if (childType.IsArray) {
                                    childType = childType.GetElementType();
                                } 
                                else {
                                    Type elementType = null;

                                    // locate Add method with 1 parameter, type of that parameter is parameter type
                                    foreach (MethodInfo method in childType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                                        if (method.Name == "Add" && method.GetParameters().Length == 1) {
                                            ParameterInfo parameter = method.GetParameters()[0];
                                            elementType = parameter.ParameterType;
                                            break;
                                        }
                                    }

                                    childType = elementType;
                                }
                            } 
                            else {
                                childType = buildElementArrayAttribute.ElementType;
                            }

                            if (childType == null || !typeof(Element).IsAssignableFrom(childType)) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    ResourceUtils.GetString("NA1140"), memInfo.DeclaringType.FullName, memInfo.Name));
                            }
                        }

                        BuildElementCollectionAttribute buildElementCollectionAttribute = (BuildElementCollectionAttribute) Attribute.GetCustomAttribute(memInfo, typeof(BuildElementCollectionAttribute), false);
                        if (buildElementCollectionAttribute != null) {
                            XmlSchemaComplexType collectionType = new XmlSchemaComplexType();
                            XmlSchemaSequence sequence = new XmlSchemaSequence();
                            collectionType.Particle = sequence;
                            
                            sequence.MinOccurs = 0;
                            sequence.MaxOccursString = "unbounded";
                            
                            XmlSchemaElement itemType = new XmlSchemaElement();
                            itemType.Name = buildElementCollectionAttribute.ChildElementName;
                            itemType.SchemaTypeName = FindOrCreateComplexType(childType).QualifiedName;

                            sequence.Items.Add(itemType);

                            childElement.SchemaType = collectionType;
                        } else {
                            childElement.SchemaTypeName = FindOrCreateComplexType(childType).QualifiedName;
                        }

                        // lazy init of sequence
                        if (group1 == null) {
                            group1 = CreateXsdSequence(0, Decimal.MaxValue);
                            ct.Particle = group1;
                        }

                        group1.Items.Add(childElement);
                    }
                }

                // allow attributes from other namespace
                ct.AnyAttribute = new XmlSchemaAnyAttribute();
                ct.AnyAttribute.Namespace = "##other";
                ct.AnyAttribute.ProcessContents = XmlSchemaContentProcessing.Skip;

                Schema.Items.Add(ct);
                Compile();

                return ct;
            }

            #endregion Protected Instance Methods

            #region Private Instance Methods

            private string GetTaskName(Type t) {
                TaskNameAttribute[] attrs = (TaskNameAttribute[])t.GetCustomAttributes(typeof(TaskNameAttribute), false);
                if (attrs.Length == 1) {
                    return attrs[0].Name;
                } else {
                    return null;
                }
            }

            private string GetDataTypeName(Type t) {
                ElementNameAttribute[] attrs = (ElementNameAttribute[]) t.GetCustomAttributes(typeof(ElementNameAttribute), false);
                if (attrs.Length == 1) {
                    return attrs[0].Name;
                } else {
                    return null;
                }
            }

            #endregion Private Instance Methods
        }
    }
}
