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
using NAnt.Core.Filters;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Comprises all of the loaded, and available, tasks. 
    /// Use these static methods to register, initialize and create a task.
    /// </summary>
    public sealed class TypeFactory {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        private static TaskBuilderCollection _taskBuilders = new TaskBuilderCollection();
        private static DataTypeBaseBuilderCollection _dataTypeBuilders = new DataTypeBaseBuilderCollection();
        private static FilterBuilderCollection _filterBuilders = new FilterBuilderCollection();
        private static Hashtable _methodInfoCollection = new Hashtable();
        private static ArrayList _projects = new ArrayList();

        #endregion Private Static Fields

        #region Static Constructor

        /// <summary> 
        /// Initializes the tasks in the executing assembly, and basedir of the 
        /// current domain.
        /// </summary>
        static TypeFactory() {
            ScanAssembly(Assembly.GetExecutingAssembly());
            ScanAssembly(Assembly.GetCallingAssembly());
        }

        #endregion Static Constructor

        #region Internal Static Properties

        /// <summary>
        /// Gets the list of loaded <see cref="TaskBuilder" /> instances.
        /// </summary>
        /// <value>
        /// List of loaded <see cref="TaskBuilder" /> instances.
        /// </value>
        public static TaskBuilderCollection TaskBuilders {
            get { return _taskBuilders; }
        }

        /// <summary>
        /// Gets the list of loaded <see cref="DataTypeBaseBuilder" /> instances.
        /// </summary>
        /// <value>
        /// List of loaded <see cref="DataTypeBaseBuilder" /> instances.
        /// </value>
        public static DataTypeBaseBuilderCollection DataTypeBuilders {
            get { return _dataTypeBuilders; }
        }

        /// <summary>
        /// Gets the list of loaded <see cref="FilterBuilder" /> instances.
        /// </summary>
        /// <value>
        /// List of loaded <see cref="FilterBuilder" /> instances.
        /// </value>
        public static FilterBuilderCollection FilterBuilders {
            get { return _filterBuilders; }
        }

        #endregion Internal Static Properties

        #region Public Static Methods

        /// <summary>
        /// Scans the given assembly for tasks, types, functions and filters.
        /// </summary>
        /// <param name="assemblyFile">The assembly to scan for tasks, types, functions and filters.</param>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static bool ScanAssembly(string assemblyFile) {
            Assembly assembly = null;

            try {
                assembly = Assembly.LoadFrom(assemblyFile);
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error loading assembly '{0}' for scan.", 
                    assemblyFile), ex);
            }

            if (assembly != null) {
                return ScanAssembly(assembly);
            } else {
                return false;
            }
        }

        /// <summary>
        /// Scans the given assembly for tasks, types, functions and filters.
        /// </summary>
        /// <param name="assembly">The assembly to scan for tasks, types, functions and filters.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="assembly" /> contains at 
        /// least one "extension"; otherwise, <see langword="false" />.
        /// </returns>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static bool ScanAssembly(Assembly assembly) {
            logger.Info(string.Format(CultureInfo.InvariantCulture, 
                "Scanning '{0}' for tasks, types, filters and functions.", 
                assembly.GetName().Name));

            bool extensionAssembly = false;

            try {
                foreach (Type type in assembly.GetTypes()) {
                    //
                    // each extension type is exclusive, meaning a given type 
                    // cannot be both a task and data type
                    //
                    // so it doesn't make sense to scan a type for, for example,
                    // data types if the type has already been positively
                    // identified as a task
                    //

                    bool extensionFound = ScanTypeForTasks(type);

                    if (!extensionFound) {
                        extensionFound = ScanTypeForDataTypes(type);
                    }

                    if (!extensionFound) {
                        extensionFound = ScanTypeForFunctions(type);
                    }

                    if (!extensionFound) {
                        extensionFound = ScanTypeForFilters(type);
                    }

                    // if extension is found in type, then mark assembly as
                    // extension assembly
                    extensionAssembly = extensionAssembly || extensionFound;
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning '{0}' for tasks, types, functions and filters.", 
                    assembly.GetName().Name), ex);
            }

            return extensionAssembly;
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

            // scan all dll's for tasks, types and functions
            DirectoryScanner scanner = new DirectoryScanner();
            scanner.BaseDirectory = new DirectoryInfo(path);
            scanner.Includes.Add("*.dll");

            foreach (string assemblyFile in scanner.FileNames) {
                TypeFactory.ScanAssembly(assemblyFile);
            }
        }

        /// <summary>
        /// Adds any task assemblies in the project base directory
        /// and its <c>tasks</c> subdirectory.
        /// </summary>
        /// <param name="project">The project to work from.</param>
        public static void AddProject(Project project) {
            if (!StringUtils.IsNullOrEmpty(project.BaseDirectory)) {
                ScanDir(project.BaseDirectory);

                // add framework-neutral assemblies
                ScanDir(Path.Combine(project.BaseDirectory, "tasks"));

                // add framework family assemblies
                ScanDir(Path.Combine(
                    Path.Combine(project.BaseDirectory, "tasks"), 
                    project.RuntimeFramework.Family));

                // add framework version specific assemblies
                ScanDir(Path.Combine(Path.Combine(Path.Combine(
                    project.BaseDirectory, "tasks"), 
                    project.RuntimeFramework.Family), project.RuntimeFramework.Version));
            }
            // create weakref to project. It is possible that project may go 
            // away, we don't want to hold it
            _projects.Add(new WeakReference(project));

            foreach (TaskBuilder tb in TaskBuilders) {
                UpdateProjectWithBuilder(project, tb);
            }
        }

        /// <summary>
        /// Looks up a function by name.
        /// </summary>
        /// <param name="methodName">The name of the function to lookup.</param>
        /// <returns>
        /// A <see cref="MethodInfo" /> representing the function, or 
        /// <see langword="null" /> if a function with the given name does not
        /// exist.
        /// </returns>
        public static MethodInfo LookupFunction(string methodName){
            return (MethodInfo) _methodInfoCollection[methodName];
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
                    "Task <{0}> is deprecated.  {1}", taskName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    throw new BuildException(obsoleteMessage, location);
                } else {
                    proj.Log(Level.Warning, "{0} {1}", location, obsoleteMessage);
                }
            }

            return task;
        }

        public static Filter CreateFilter(XmlNode elementNode, Element parent) {
            if (elementNode == null) {
                throw new ArgumentNullException("elementNode");
            }
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }

            string filterName = elementNode.Name;

            FilterBuilder builder = FilterBuilders[filterName];
            if (builder == null) {
                Location location = parent.Project.LocationMap.GetLocation(elementNode);
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown filter <{0}>.", filterName), location);
            }

            Filter filter = (Filter) builder.CreateFilter();
            filter.Parent = parent;
            filter.Project = parent.Project;
            filter.NamespaceManager = parent.Project.NamespaceManager;
            filter.Initialize(elementNode);

            // check whether the type (or its base class) is deprecated
            ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) 
                Attribute.GetCustomAttribute(filter.GetType(), 
                typeof(ObsoleteAttribute), true);

            if (obsoleteAttribute != null) {
                Location location = parent.Project.LocationMap.GetLocation(elementNode);
                string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                    "Filter <{0}> is deprecated.  {1}", filterName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    throw new BuildException(obsoleteMessage, location);
                } else {
                    parent.Project.Log(Level.Warning, "{0} {1}", location, 
                        obsoleteMessage);
                }
            }
            return filter;
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
                    "Type <{0}> is deprecated.  {1}", dataTypeName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    throw new BuildException(obsoleteMessage, location);
                } else {
                    proj.Log(Level.Warning, "{0} {1}", location, obsoleteMessage);
                }
            }
            return element;
        }

        #endregion Public Static Methods

        #region Internal Static Methods

        internal static void UpdateProjectWithBuilder(Project p, TaskBuilder tb) {
            // add a true property for each task (use in build to test for task existence).
            // add a property for each task with the assembly location.
            p.Properties.AddReadOnly("nant.tasks." + tb.TaskName, Boolean.TrueString);
            p.Properties.AddReadOnly("nant.tasks." + tb.TaskName + ".location", tb.AssemblyFileName);
        }

        #endregion Internal Static Methods

        #region Private Static Methods

        /// <summary>
        /// Scans a given <see cref="Type" /> for tasks.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// <see cref="Task" />; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForTasks(Type type) {
            try {
                TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(TaskNameAttribute));

                if (type.IsSubclassOf(typeof(Task)) && !type.IsAbstract && taskNameAttribute != null) {
                    logger.Info(string.Format(CultureInfo.InvariantCulture, 
                        "Creating TaskBuilder for '{0}'", type.Name));
                    TaskBuilder tb = new TaskBuilder(type.FullName, type.Assembly.Location);
                    if (TaskBuilders[tb.TaskName] == null) {
                        logger.Debug(string.Format(CultureInfo.InvariantCulture,
                            "Adding '{0}' from {1}:{2}", tb.TaskName, 
                            tb.AssemblyFileName, tb.ClassName));

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

                        // specified type represents a task
                        return true;
                    }
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning type '{0}' for tasks.", 
                    type.AssemblyQualifiedName), ex);
            }

            // specified type does not represent valid task
            return false;
        }

        /// <summary>
        /// Scans a given <see cref="Type" /> for data type.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// data type; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForDataTypes(Type type) {
            try {
                ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

                if (type.IsSubclassOf(typeof(DataTypeBase)) && !type.IsAbstract && elementNameAttribute != null) {
                    logger.Info(string.Format(CultureInfo.InvariantCulture, 
                        "Creating DataTypeBaseBuilder for {0}", type.Name));
                    DataTypeBaseBuilder dtb = new DataTypeBaseBuilder(type.FullName, type.Assembly.Location);
                    if (DataTypeBuilders[dtb.DataTypeName] == null) {
                        logger.Debug(string.Format(CultureInfo.InvariantCulture, 
                            "Adding '{0}' from {1}:{2}", dtb.DataTypeName, 
                            dtb.AssemblyFileName, dtb.ClassName));

                        DataTypeBuilders.Add(dtb);
                    }

                    // specified type represents a data type
                    return true;
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning type '{0}' for data types.", 
                    type.AssemblyQualifiedName), ex);
            }

            // specified type does not represent valid data type
            return false;
        }        

        /// <summary>
        /// Scans a given <see cref="Type" /> for functions.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// valid set of funtions; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForFunctions(Type type) {
            try {
                FunctionSetAttribute functionSetAttribute = (FunctionSetAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(FunctionSetAttribute));
                if (functionSetAttribute == null) {
                    // specified type does not represent a valid functionset
                    return false;
                }

                bool acceptType = (type == typeof(ExpressionEvaluator));
                
                if (type.IsSubclassOf(typeof(FunctionSetBase)) && !type.IsAbstract) {
                    acceptType = true;
                }

                if (acceptType) {
                    string prefix = functionSetAttribute.Prefix;
                    if (prefix != null && prefix != String.Empty) {
                        prefix += "::";
                    } else {
                        logger.Warn(string.Format(CultureInfo.InvariantCulture, 
                            "Ignoring functions in type '{0}': no prefix was set.",
                            type.AssemblyQualifiedName));
                        // specified type does not represent a valid functionset
                        return false;
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
                    foreach (MethodInfo info in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
                        FunctionAttribute functionAttribute = (FunctionAttribute)
                            Attribute.GetCustomAttribute(info, typeof(FunctionAttribute));
                        if (functionAttribute != null) {
                            // look at scoping each class by prefix
                            if (!_methodInfoCollection.ContainsKey(prefix + functionAttribute.Name)) {
                                _methodInfoCollection.Add(prefix + functionAttribute.Name, info);
                            }
                        }
                    }

                    // specified type represents a valid functionset
                    return true;
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning type '{0}' for functions.", 
                    type.AssemblyQualifiedName), ex);
            }

            // specified type does not represent a valid functionset
            return false;
        }

        /// <summary>
        /// Scans a given <see cref="Type" /> for filters.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// <see cref="Filter" />; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForFilters(Type type) {
            try {
                ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

                if (type.IsSubclassOf(typeof(Filter)) && !type.IsAbstract && elementNameAttribute != null) {
                    logger.Info(string.Format(CultureInfo.InvariantCulture, 
                        "Creating FilterBuilder for {0}", type.Name));
                    FilterBuilder builder = new FilterBuilder(type.FullName, type.Assembly.Location);
                    if (FilterBuilders[builder.FilterName] == null) {
                        FilterBuilders.Add(builder);

                        logger.Debug(string.Format(CultureInfo.InvariantCulture, 
                            "Adding filter '{0}' from {1}:{2}", builder.FilterName, 
                            builder.AssemblyFileName, builder.ClassName));
                    }

                    // specified type represents a filter
                    return true;
                }
            } catch (Exception ex) {
                logger.Error(string.Format(CultureInfo.InvariantCulture, 
                    "Error scanning type '{0}' for filters.", 
                    type.AssemblyQualifiedName), ex);
            }

            // specified type does not represent a valid filter
            return false;
        }

        #endregion Private Static Methods
    }
}
