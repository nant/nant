// NAnt - A .NET build tool// Copyright (C) 2001-2003 Scott Hernandez//// This program is free software; you can redistribute it and/or modify// it under the terms of the GNU General Public License as published by// the Free Software Foundation; either version 2 of the License, or// (at your option) any later version.//// This program is distributed in the hope that it will be useful,// but WITHOUT ANY WARRANTY; without even the implied warranty of// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the// GNU General Public License for more details.//// You should have received a copy of the GNU General Public License// along with this program; if not, write to the Free Software// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA//// Scott Hernandez (ScottHernandez@hotmail.com)
using System;using System.Collections;using System.Collections.Specialized;using System.Diagnostics;using System.IO;using System.Reflection;using System.Xml;using System.Xml.Schema;
using SourceForge.NAnt.Attributes;
namespace SourceForge.NAnt.Tasks {    /// <summary>    /// Creates an XSD File for all available Tasks.    /// </summary>    /// <remarks>    ///   <para>This can be used in conjuntion with the command line option to do XSD Schema validation on the build file.</para>    /// </remarks>    /// <example>    ///   <para>Creates an NAnt.xsd file in the current project directory</para>    ///   <code><![CDATA[<NAntSchema name="NAnt.xsd"/>]]></code>    /// </example>    [TaskName("nantschema")]    public class NAntSchemaTask : Task {        #region Private Instance Fields

        string  _file               = null;        string  _forType            = null;        string  _targetNamespace    = null;
        #endregion Private Instance Fields

        #region Public Instance Properties
        [TaskAttribute("output",Required=true)]        public virtual string FileName {            get { return _file; }            set { _file = value; }        }
        [TaskAttribute("target-ns",Required=false)]        public virtual string TargetNamespace{            get { return _targetNamespace; }            set { _targetNamespace = value; }        }
        [TaskAttribute("class", Required=false)]        public virtual string ForType {            get { return _forType; }            set { _forType = value; }        }
        #endregion Public Instance Properties
        #region Override implementation of Task

        protected override void ExecuteTask() {            ArrayList taskTypes;
            if(_forType == null) {                taskTypes = new ArrayList(TaskFactory.Builders.Count);
                foreach(TaskBuilder tb in TaskFactory.Builders) {                    taskTypes.Add(Assembly.LoadFrom(tb.AssemblyFileName).GetType(tb.ClassName, true, true));                }            } else {                taskTypes = new ArrayList(1);                taskTypes.Add(Type.GetType(_forType, true, true));            }
            FileStream file = File.Open(Project.GetFullPath(_file), FileMode.Create, FileAccess.Write, FileShare.Read);
            WriteSchema(file, (Type[])taskTypes.ToArray(typeof(Type)),_targetNamespace);
            file.Flush();            file.Close();
            Log.WriteLine("Wrote Schema to: {0} ", _file);        }
        #endregion Override implementation of Task

        protected class NAntSchemaGenerator {            #region Private Instance Fields
            IDictionary _nantComplexTypes = null;            Type[] _tasks = null;            XmlSchema _nantSchema = new XmlSchema();            string _namespaceURI = string.Empty;
            #endregion Private Instance Fields
            #region Public Instance Constructors
            /// <summary>            /// Creates a new SchemaGenerator.            /// </summary>            /// <param name="tasks"></param>            /// <param name="targetNS">The namespace to use.            /// <example> http://tempuri.org/nant.xsd </example>            /// </param>            public NAntSchemaGenerator(Type[] tasks, string targetNS) {                //setup namespace stuff                if(targetNS != null && targetNS != string.Empty) {                    _nantSchema.TargetNamespace = targetNS;                    _nantSchema.Namespaces.Add("nant", _nantSchema.TargetNamespace);                    _namespaceURI = targetNS;                }
                _nantSchema.Namespaces.Add("vs","urn:schemas-microsoft-com:HTML-Intellisense");
                // Add XSD Namespace so that all xsd elements are prefix'd                _nantSchema.Namespaces.Add("xs",XmlSchema.Namespace);
                //_nantSchema.ElementFormDefault = XmlSchemaForm.Unqualified;                //_nantSchema.AttributeFormDefault = XmlSchemaForm.Unqualified;
                //initialize stuff                _nantComplexTypes = new HybridDictionary(tasks.Length);                _tasks = tasks;                XmlSchemaComplexType empty_fake = CreateXSDCT("fake-empty", null, null);
                //add timestamp and info for project.                AddDocumentation(empty_fake, DateTime.Now + " \nGenerated by" + GetType().ToString());
                _nantSchema.Items.Add(empty_fake);
                //create temp list of task Complex Types                IList taskCTs = new ArrayList(tasks.Length);
                foreach(Type t in tasks) {                    XmlSchemaComplexType taskCT = CreateComplexType(t, ((TaskNameAttribute) GetDerivedAttribute(t, typeof(TaskNameAttribute), false, true)).Name, true);                    taskCTs.Add(taskCT);                }
                Compile();
                //create target ComplexType                XmlSchemaComplexType targetCT = CreateTaskListCT(taskCTs);                targetCT.Name="target";
                //name attribute                targetCT.Attributes.Add(CreateXSDAttr("name", true));
                //default attribute                targetCT.Attributes.Add(CreateXSDAttr("depends", false));
                //description attribute                targetCT.Attributes.Add(CreateXSDAttr("description", false));
                _nantSchema.Items.Add(targetCT);
                //add to the list of ComplexTypes so that project will get it.                taskCTs.Add(targetCT);
                Compile();
                // Generate project Element and ComplexType                XmlSchemaElement projectElement = new XmlSchemaElement();                projectElement.Name = "project";
                XmlSchemaComplexType projectCT = CreateTaskListCT(taskCTs);
                projectElement.SchemaType =  projectCT;
                //name attribute                projectCT.Attributes.Add(CreateXSDAttr("name", true));
                //default attribute                projectCT.Attributes.Add(CreateXSDAttr("default", false));
                //basedir attribute                projectCT.Attributes.Add(CreateXSDAttr("basedir", false));
                _nantSchema.Items.Add(projectElement);
                Compile();            }
            /// <summary>            /// Creates a new SchemaGenerator without a TargetNamespace.            /// </summary>            /// <param name="tasks">The Collection of Type(s) that represent the Task Classes to generation XSD for.</param>            public NAntSchemaGenerator(Type[] tasks) : this(tasks, null) {            }
            #endregion Public Instance Constructors
            #region Public Instance Properties

            public XmlSchema Schema {                get{                    if(!_nantSchema.IsCompiled) {                        Compile();                    }
                    return _nantSchema;                }            }
            #endregion Public Instance Properties

            #region Public Instance Methods

            public void Compile() {                _nantSchema.Compile(new ValidationEventHandler(ValidationEH));            }
            #endregion Public Instance Methods

            #region Protected Instance Methods

            protected void ValidationEH(object sender, ValidationEventArgs args) {                if(args.Severity == XmlSeverityType.Warning) {                    Console.Write("WARNING: ");                } else if(args.Severity == XmlSeverityType.Error) {                    Console.Write("ERROR: ");                }

                XmlSchemaComplexType source = args.Exception.SourceSchemaObject as XmlSchemaComplexType;
                Console.WriteLine(args.ToString());
                if(source != null) {                    Console.WriteLine("{0}({1})", source.Name, source.Id);                }            }
            protected XmlSchemaComplexType FindCTByID(string id) {                if(_nantComplexTypes.Contains(id)) {                    return (XmlSchemaComplexType)_nantComplexTypes[id];                }
                return null;            }
/*            protected XmlSchemaComplexType CreateComplexTypeForTask(Type t, string name, bool useRefs) {
                XmlSchemaComplexType ct = CreateComplexType(t, name, useRefs);
                ct.Content = new XmlSchemaComplexContentExtension();
                ((XmlSchemaComplexContentExtension) ct.ContentModel).BaseTypeName = new XmlQualifiedName("Task", _namespaceURI);
                return ct;                
            }
*/
            protected XmlSchemaComplexType CreateComplexType(Type t, string name, bool useRefs) {                XmlSchemaComplexType ct = null;
                if(useRefs) {                    //lookup the type to see if we have done this already.                    ct = FindCTByID(GenerateIDFromClassType(t));                }
                if(ct != null) return ct;
                ct = CreateXSDCT(name, GenerateIDFromClassType(t), null);
                XmlSchemaGroupBase group1 = CreateXSDSequence(0, Decimal.MaxValue);
                foreach(MemberInfo memInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {                    if(memInfo.DeclaringType.Equals(typeof(object))) continue;                                       //Check for any return type that is derived from Element
                    //Add Attributes                    TaskAttributeAttribute taskAttrAttr = (TaskAttributeAttribute)Attribute.GetCustomAttribute(memInfo, typeof(TaskAttributeAttribute), true);                    BuildElementAttribute  buildElemAttr = (BuildElementAttribute) Attribute.GetCustomAttribute(memInfo, typeof(BuildElementAttribute ), true);
                    if(taskAttrAttr != null) {                        XmlSchemaAttribute newAttr = CreateXSDAttr(taskAttrAttr.Name, taskAttrAttr.Required);                        ct.Attributes.Add(newAttr);                    } else if(buildElemAttr != null) {                        // Create individial choice for any individual child Element                        Decimal min = 0;
                        if(buildElemAttr.Required) min = 1;
                        XmlSchemaGroupBase elementGroup = CreateXSDSequence(min, Decimal.MaxValue);                        XmlSchemaElement childElement = new XmlSchemaElement();                        childElement.Name = buildElemAttr.Name;
                        Type childType;
                        // We will only process child elements if they are defined for Properties or Fields, this should be enforced by the AttributeUsage on the Attribute class                        if(memInfo is PropertyInfo) {                            childType = ((PropertyInfo)memInfo).PropertyType;                        } else if(memInfo is FieldInfo) {                            childType = ((FieldInfo)memInfo).FieldType;                        } else {                            throw new ApplicationException("Member Type != Field/Property");                        }
                        //In xsd we use choices to define the array property. So we should treat this as the element type, not an array.                        if(childType.IsArray) {                            childType = childType.GetElementType();                        }
                        childElement.SchemaTypeName = CreateComplexType(childType, buildElemAttr.Name, useRefs).QualifiedName;
                        elementGroup.Items.Add(childElement);
                        group1.Items.Add(elementGroup);                    }                }
                if(group1.Items.Count > 0) ct.Particle = group1;
                Schema.Items.Add(ct);
                _nantComplexTypes.Add(ct.Id, ct);
                Compile();
                return ct;            }            #endregion Public Instance Methods        }
        #region Public Static Methods
        /// <summary>        /// Creates a NAnt Schema for given types        /// </summary>        /// <param name="stream">The output stream to save the schema to. If null, writing is ignored, no exception generated</param>        /// <param name="tasks">The list of Types to generate Schema for</param>        /// <param name="targetNS">The target Namespace to output</param>        /// <returns>The new NAnt Schema</returns>        public static XmlSchema WriteSchema(System.IO.Stream stream, Type[] tasks, string targetNS) {            NAntSchemaGenerator gen = new NAntSchemaGenerator(tasks, targetNS);
            if(!gen.Schema.IsCompiled) {                gen.Compile();            }
            if (stream != null) {                gen.Schema.Write(stream);            }
            return gen.Schema;        }
        #endregion Public Static Methods

        #region Protected Static Methods

        protected static string GenerateIDFromClassType(Type cls) {            return cls.ToString().Replace("+", "-").Replace("[","_").Replace("]","_");        }
        /// <summary>        /// Creates a new XmlSchemaAttribute        /// </summary>        /// <param name="name">XmlSchemaAttribute.Name</param>        /// <param name="required">sets XmlSchemaAttribute.Use</param>        /// <returns>The attribute</returns>        protected static XmlSchemaAttribute CreateXSDAttr(string name, bool required) {            XmlSchemaAttribute newAttr = new XmlSchemaAttribute();
            newAttr.Name= name;
            if (required) {                newAttr.Use = XmlSchemaUse.Required;            } else {                newAttr.Use = XmlSchemaUse.Optional;            }
            return newAttr;        }
        /// <summary>        ///         /// </summary>        /// <param name="name">ComplexType.Name</param>        /// <param name="id">ComplexType.id</param>        /// <param name="attrs">ComplexType.Attributes; null indicates none.</param>        /// <returns></returns>        protected static XmlSchemaComplexType CreateXSDCT(string name, string id, XmlSchemaAttribute[] attrs) {            XmlSchemaComplexType newCT = new XmlSchemaComplexType();
            newCT.Name = name;            newCT.Id = id;
            if(attrs != null) {                foreach(XmlSchemaAttribute attr in attrs) {                    newCT.Attributes.Add(attr);                }            }
            return newCT;        }
        /// <summary>        /// Generates a new object.        /// </summary>        /// <param name="min">The min value to allow for this choice</param>        /// <param name="max">The max value to allow, Decimal.MaxValue sets it to 'unbound'</param>        /// <returns></returns>        protected static XmlSchemaChoice CreateXSDChoice(Decimal min, Decimal max) {            XmlSchemaChoice newChoice = new XmlSchemaChoice();
            newChoice.MinOccurs = min;
            if (max != Decimal.MaxValue) {                newChoice.MaxOccurs = max;            } else {                newChoice.MaxOccursString = "unbounded";            }
                        return newChoice;        }
        /// <summary>        /// Generates a new object.        /// </summary>        /// <param name="min">The min value to allow for this choice</param>        /// <param name="max">The max value to allow, Decimal.MaxValue sets it to 'unbound'</param>        /// <returns></returns>        protected static XmlSchemaSequence CreateXSDSequence(Decimal min, Decimal max) {            XmlSchemaSequence newSeq = new XmlSchemaSequence();
            newSeq.MinOccurs = min;
            if(max != Decimal.MaxValue) {                newSeq.MaxOccurs = max;            } else {                newSeq.MaxOccursString = "unbounded";            }
            
            return newSeq;        }    
        protected static XmlSchemaComplexType CreateTaskListCT(IList taskComplexTypes) {            XmlSchemaComplexType tasklistCT = new XmlSchemaComplexType();
            tasklistCT.Particle = CreateXSDSequence(0, Decimal.MaxValue);
            XmlSchemaGroupBase group1 = CreateXSDSequence(0, Decimal.MaxValue);
            ((XmlSchemaGroupBase) tasklistCT.Particle).Items.Add(group1);
            //XmlSchemaGroupBase group2 = (XmlSchemaGroupBase)tasklistCT.Particle;
            foreach(XmlSchemaComplexType taskCT in taskComplexTypes) {                XmlSchemaGroupBase group2 = CreateXSDSequence(0, Decimal.MaxValue);
                group1.Items.Add(group2);
                XmlSchemaElement taskElement = new XmlSchemaElement();                taskElement.Name = taskCT.Name;                taskElement.SchemaTypeName = taskCT.QualifiedName;
                group2.Items.Add(taskElement);            }
            return tasklistCT;        }
        protected static XmlNode[] TextToNodeArray(string text) {            XmlDocument doc = new XmlDocument();
            return new XmlNode[1] {doc.CreateTextNode(text)};        }
        protected static void AddDocumentation(XmlSchemaAnnotated ann, string doc) {            if(ann.Annotation == null) {                ann.Annotation = new XmlSchemaAnnotation();            }
            XmlSchemaDocumentation schemaDoc = new XmlSchemaDocumentation();
            ann.Annotation.Items.Add(schemaDoc);
            schemaDoc.Markup = TextToNodeArray(doc);        }
        protected static void AddDocumentation(XmlSchemaAttribute attr, string doc) {            if(attr.Annotation == null) {                attr.Annotation = new XmlSchemaAnnotation();            }
            XmlSchemaDocumentation schemaDoc = new XmlSchemaDocumentation();
            attr.Annotation.Items.Add(schemaDoc);
            schemaDoc.Markup = TextToNodeArray(doc);        }
        protected static void ValidationCallback(object sender, ValidationEventArgs args) {            if(args.Severity == XmlSeverityType.Warning) {                Console.Write("WARNING: ");            } else if(args.Severity == XmlSeverityType.Error) {                Console.Write("ERROR: ");            }
            Console.WriteLine(args.ToString());        }
        /// <summary>        /// Searches throught custom attributes for any attribute based on attr        /// </summary>        /// <param name="meminfo">MemberInfo (includes Properties/Fields/Types)</param>        /// <param name="attr">Type of the Attribute you want; meaning that you want something derived by it.</param>        /// <param name="bSearchObjectHier">Search the MemberInfo class ancestry</param>        /// <param name="bSearchAttributeHier">Search the Attribute Type ancestry for a mactch to attr</param>        /// <returns></returns>        protected static Attribute GetDerivedAttribute(MemberInfo meminfo, Type attr, bool bSearchObjectHier, bool bSearchAttributeHier) {            if (bSearchAttributeHier) {                Attribute[] attrs = Attribute.GetCustomAttributes(meminfo, bSearchObjectHier);
                foreach(Attribute a in attrs) {                    Type aType = a.GetType();
                    while (!typeof(object).Equals(aType.BaseType) && aType.BaseType != null) {                        if (aType.Equals(attr)) {                            return a;                        }
                        aType = aType.BaseType;                    }                }            } else {                return Attribute.GetCustomAttribute(meminfo, attr, bSearchObjectHier);            }
            return null;        }
        #endregion Protected Static Methods    }}