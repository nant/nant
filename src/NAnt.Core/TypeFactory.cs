// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Comprises all of the loaded, and available, tasks. 
    /// Use these static methods to register, initialize and create a task.
    /// </summary>
    public class TypeFactory {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        private static TaskBuilderCollection _taskBuilders = new TaskBuilderCollection();
        private static DataTypeBaseBuilderCollection _dataTypeBuilders = new DataTypeBaseBuilderCollection();
        private static Hashtable _methodInfoCollection = new Hashtable();
        private static ArrayList _projects = new ArrayList();

        #endregion Private Static Fields

        #region Static Constructor

        /// <summary> 
        /// Initializes the tasks in the executing assembly, and basedir of the 
        /// current domain.
        /// </summary>
        static TypeFactory() {
            // initialize builtin tasks
            AddTasks(Assembly.GetExecutingAssembly());
            AddTasks(Assembly.GetCallingAssembly());
            
            // TO-DO combine these two AddTasks and AddDataTypes
            AddDataTypes(Assembly.GetExecutingAssembly());
            AddDataTypes(Assembly.GetCallingAssembly());

            AddFunctionSets(Assembly.GetExecutingAssembly());
            AddFunctionSets(Assembly.GetCallingAssembly());
        }

        #endregion Static Constructor

        #region Public Static Methods

        /// <summary>
        /// Scans the given assembly for extensions.
        /// </summary>
        /// <param name="assemblyFile">The assembly to scan for extensions.</param>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static void ScanAssembly(string assemblyFile) {
            logger.Info(string.Format(CultureInfo.InvariantCulture, 
                "Scanning '{0}' for extensions.", assemblyFile));

            try {
                Assembly assembly = Assembly.LoadFrom(assemblyFile);

                AddTasks(assembly);
                AddDataTypes(assembly);
                AddFunctionSets(assembly);
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning '{0}' for extensions.", 
                    assemblyFile), ex);
            }
        }

        /// <summary>
        /// Scans the path for any task assemblies and adds them.
        /// </summary>
        /// <param name="path">The directory to scan in.</param>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static void ScanDir(string path) {
            // don't do anything if we don't have a valid directory path
            if (StringUtils.IsNullOrEmpty(path)) {
                return;
            }

            // scan all dll's for extensions
            DirectoryScanner scanner = new DirectoryScanner();
            scanner.BaseDirectory = new DirectoryInfo(path);
            scanner.Includes.Add("*.dll");

            foreach (string assemblyFile in scanner.FileNames) {
                TypeFactory.ScanAssembly(assemblyFile);
            }
        }

        /// <summary>
        /// Adds any extension assemblies in the project base directory
        /// and its <c>extensions</c> subdirectory.
        /// </summary>
        /// <param name="project">The project to work from.</param>
        public static void AddProject(Project project) {
            if (!StringUtils.IsNullOrEmpty(project.BaseDirectory)) {
                ScanDir(project.BaseDirectory);
                ScanDir(Path.Combine(project.BaseDirectory, "extensions"));
            }
            // create weakref to project. It is possible that project may go 
            // away, we don't want to hold it
            _projects.Add(new WeakReference(project));
            foreach (TaskBuilder tb in TaskBuilders) {
                UpdateProjectWithBuilder(project, tb);
            }
        }

        /// <summary>
        /// Gets the list of loaded <see cref="TaskBuilder" /> instances.
        /// </summary>
        /// <value>
        /// List of loaded <see cref="TaskBuilder" /> instances.
        /// </value>
        public static TaskBuilderCollection TaskBuilders {
            get { return _taskBuilders; }
        }

        public static DataTypeBaseBuilderCollection DataTypeBuilders {
            get { return _dataTypeBuilders; }
        }

        /// <summary>
        /// Scans the given assembly for any classes derived from 
        /// <see cref="FunctionSetBase" /> and scans them for custom functions.
        /// </summary>
        /// <param name="taskAssembly">The <see cref="Assembly" /> containing the new custom functions to be loaded.</param>
        /// <returns>
        /// The number of functions found in the assembly.
        /// </returns>
        public static int AddFunctionSets(Assembly taskAssembly) {
            int functionSetCount = 0;

            try {
                foreach (Type type in taskAssembly.GetTypes()) {
                    FunctionSetAttribute functionSetAttribute = (FunctionSetAttribute) 
                        Attribute.GetCustomAttribute(type, typeof(FunctionSetAttribute));
                    if (functionSetAttribute != null) {
                        bool acceptType = (type == typeof(ExpressionEvaluator));
                        
                        if (type.IsSubclassOf(typeof(FunctionSetBase)) && !type.IsAbstract) {
                            acceptType = true;
                        }

                        if (acceptType) {
                            string prefix = functionSetAttribute.Prefix;
                            if (prefix != null && prefix != String.Empty) {
                                prefix += "::";
                            } else {
                                continue;
                            }

                            //
                            // add instance methods
                            // 
                            foreach (MethodInfo info in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                                FunctionAttribute functionAttribute = (FunctionAttribute)
                                    Attribute.GetCustomAttribute(info, typeof(FunctionAttribute));
                                if (functionAttribute != null) {
                                    // look at scoping each class by namespace prefix
                                    if (! _methodInfoCollection.ContainsKey(prefix + functionAttribute.Name)) {
                                        _methodInfoCollection.Add(prefix + functionAttribute.Name, info);
                                    }
                                }
                            }

                            //
                            // add static methods
                            // 
                            foreach ( MethodInfo info in type.GetMethods(BindingFlags.Public | BindingFlags.Static )) {
                                FunctionAttribute functionAttribute = (FunctionAttribute)
                                    Attribute.GetCustomAttribute(info, typeof(FunctionAttribute));
                                if (functionAttribute != null) {
                                    // look at scoping each class by prefix
                                    if (! _methodInfoCollection.ContainsKey(prefix + functionAttribute.Name)) {
                                        _methodInfoCollection.Add(prefix + functionAttribute.Name, info);
                                    }
                                }
                            }
                            functionSetCount++;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error loading functions from '{0}' ({1}).", taskAssembly.FullName, 
                    taskAssembly.Location), ex);
            }
            return functionSetCount;
        }
        /// <summary>
        /// Scans the given assembly for any classes derived from 
        /// <see cref="Task" /> and adds a new builder for them.
        /// <note>
        ///     If the taskname is already loaded then a new assembly scan 
        ///     that finds new tasks that are already loaded will not replace it. 
        ///     Once tasks are added, they cannot be removed.
        /// </note>
        /// </summary>
        /// <param name="taskAssembly">The <see cref="Assembly" /> containing the new tasks to be loaded.</param>
        /// <returns>
        /// The number of tasks found in the assembly.
        /// </returns>
        public static int AddTasks(Assembly taskAssembly) {
            int taskCount = 0;

            try {
                foreach (Type type in taskAssembly.GetTypes()) {
                    TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                        Attribute.GetCustomAttribute(type, typeof(TaskNameAttribute));

                    if (type.IsSubclassOf(typeof(Task)) && !type.IsAbstract && taskNameAttribute != null) {
                        logger.Info(string.Format(CultureInfo.InvariantCulture, 
                            "Creating TaskBuilder for '{0}'", type.Name));
                        TaskBuilder tb = new TaskBuilder(type.FullName, taskAssembly.Location);
                        if (TaskBuilders[tb.TaskName] == null) {
                            TaskBuilders.Add(tb);
                            foreach(WeakReference wr in _projects) {
                                if (!wr.IsAlive) {
                                    logger.Error("Project WeakRef is dead.");
                                    continue;
                                }
                                Project p = wr.Target as Project;
                                if (p == null) {
                                    logger.Error("WeakRef not a project! This should not be possible.");
                                    continue;
                                }
                                UpdateProjectWithBuilder(p, tb);
                            }
                            logger.Debug(string.Format(CultureInfo.InvariantCulture,
                                "Adding '{0}' from {1}:{2}", tb.TaskName, 
                                tb.AssemblyFileName, tb.ClassName));

                            // increment number of tasks loaded from assembly
                            taskCount++;
                        }
                    }
                }
            } catch (Exception ex) { // for assemblies that don't have types
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error loading tasks from {0}({1}).", taskAssembly.FullName, 
                    taskAssembly.Location), ex);
            }

            return taskCount;
        }

        public static int AddDataTypes(Assembly taskAssembly) {
            int typeCount = 0;

            try {
                foreach (Type type in taskAssembly.GetTypes()) {
                    ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                        Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

                    if (type.IsSubclassOf(typeof(DataTypeBase)) && !type.IsAbstract && elementNameAttribute != null) {
                        logger.Info(string.Format(CultureInfo.InvariantCulture, "Creating DataTypeBaseBuilder for {0}", type.Name));
                        DataTypeBaseBuilder dtb = new DataTypeBaseBuilder(type.FullName, taskAssembly.Location);
                        if (DataTypeBuilders[dtb.DataTypeName ] == null) {
                            DataTypeBuilders.Add(dtb);
                            foreach (WeakReference wr in _projects) {
                                if (!wr.IsAlive) {
                                    logger.Error("Project WeakRef is dead.");
                                    continue;
                                }
                                Project p = wr.Target as Project;
                                if (p == null) {
                                    logger.Error("WeakRef not a project! This should not be possible.");
                                    continue;
                                }
                            }
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, 
                                "Adding '{0}' from {1}:{2}", dtb.DataTypeName, 
                                dtb.AssemblyFileName, dtb.ClassName));

                            // increment number of types loaded from assembly
                            typeCount++;
                        }
                    }
                }
            } catch (Exception ex) { // for assemblies that don't have types
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error loading types from {0}({1}).", taskAssembly.FullName, 
                    taskAssembly.Location), ex);
            }

            return typeCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo LookupFunction(string methodName){
            return (MethodInfo)_methodInfoCollection[methodName];
        }

        /// <summary> 
        /// Creates a new <see cref="Task" /> instance for the given XML and 
        /// <see cref="Project" />.
        /// </summary>
        /// <param name="taskNode">The XML to initialize the task with.</param>
        /// <param name="proj">The <see cref="Project" /> that the <see cref="Task" /> belongs to.</param>
        /// <returns>
        /// The new <see cref="Task" /> instance.
        /// </returns>
        public static Task CreateTask(XmlNode taskNode, Project proj) {
            if (taskNode == null) {
                throw new ArgumentNullException("taskNode");
            }
            if (proj == null) {
                throw new ArgumentNullException("proj");
            }

            string taskName = taskNode.Name;

            TaskBuilder builder = TaskBuilders[taskName];
            if (builder == null) {
                Location location = proj.LocationMap.GetLocation(taskNode);
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown task <{0}>.", taskName), location);
            }

            Task task = builder.CreateTask();
            task.Project = proj;
            task.NamespaceManager = proj.NamespaceManager;

            // check whether the task (or its base class) is deprecated
            ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) 
                Attribute.GetCustomAttribute(task.GetType(), 
                typeof(ObsoleteAttribute), true);

            if (obsoleteAttribute != null) {
                Location location = proj.LocationMap.GetLocation(taskNode);
                string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                    "{0} Task <{1}> is deprecated.  {2}", location, taskName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    proj.Log(Level.Error, obsoleteMessage);
                } else {
                    proj.Log(Level.Warning, obsoleteMessage);
                }
            }

            return task;
        }

        public static DataTypeBase CreateDataType(XmlNode elementNode, Project proj) {
            if (elementNode == null) {
                throw new ArgumentNullException("elementNode");
            }
            if (proj == null) {
                throw new ArgumentNullException("proj");
            }

            string dataTypeName = elementNode.Name;

            DataTypeBaseBuilder builder = DataTypeBuilders[dataTypeName];
            if (builder == null) {
                Location location = proj.LocationMap.GetLocation(elementNode);
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown element <{0}>.", dataTypeName), location);
            }

            DataTypeBase element = (DataTypeBase) builder.CreateDataTypeBase();
            element.Project = proj;
            element.NamespaceManager = proj.NamespaceManager;

            // check whether the type (or its base class) is deprecated
            ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) 
                Attribute.GetCustomAttribute(element.GetType(), 
                typeof(ObsoleteAttribute), true);

            if (obsoleteAttribute != null) {
                Location location = proj.LocationMap.GetLocation(elementNode);
                string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                    "{0} Type <{1}> is deprecated.  {2}", location, dataTypeName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    proj.Log(Level.Error, obsoleteMessage);
                } else {
                    proj.Log(Level.Warning, obsoleteMessage);
                }
            }
            return element;
        }

        #endregion Public Static Methods

        #region Protected Static Methods

        protected static void UpdateProjectWithBuilder(Project p, TaskBuilder tb) {
            // add a true property for each task (use in build to test for task existence).
            // add a property for each task with the assembly location.
            p.Properties.AddReadOnly("nant.tasks." + tb.TaskName, Boolean.TrueString);
            p.Properties.AddReadOnly("nant.tasks." + tb.TaskName + ".location", tb.AssemblyFileName);
        }

        #endregion Protected Static Methods
    }
}
