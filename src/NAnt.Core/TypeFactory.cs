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
using NAnt.Core.Extensibility;
using NAnt.Core.Filters;
using NAnt.Core.Tasks;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Comprises all of the loaded, and available, tasks. 
    /// Use these static methods to register, initialize and create a task.
    /// </summary>
    public sealed class TypeFactory {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static TaskBuilderCollection _taskBuilders = new TaskBuilderCollection();
        private static DataTypeBaseBuilderCollection _dataTypeBuilders = new DataTypeBaseBuilderCollection();
        private static FilterBuilderCollection _filterBuilders = new FilterBuilderCollection();
        private static Hashtable _methodInfoCollection = new Hashtable();
        private static PluginScanner _pluginScanner = new PluginScanner();

        #endregion Private Static Fields

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

        internal static PluginScanner PluginScanner {
            get { return _pluginScanner; }
        }

        #endregion Internal Static Properties

        #region Public Static Methods

        /// <summary>
        /// Scans the given assembly for tasks, types, functions and filters.
        /// </summary>
        /// <param name="assemblyFile">The assembly to scan for tasks, types, functions and filters.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static bool ScanAssembly(string assemblyFile, Task task) {
            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            return ScanAssembly(assembly, task);
        }

        /// <summary>
        /// Scans the given assembly for tasks, types, functions and filters.
        /// </summary>
        /// <param name="assembly">The assembly to scan for tasks, types, functions and filters.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="assembly" /> contains at 
        /// least one "extension"; otherwise, <see langword="false" />.
        /// </returns>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static bool ScanAssembly(Assembly assembly, Task task) {
            task.Log(Level.Verbose, "Scanning assembly \"{0}\" for extensions.", 
                assembly.GetName().Name);
                
            foreach (Type type in assembly.GetExportedTypes()) {
                foreach (MethodInfo methodInfo in type.GetMethods()) {
                    if (methodInfo.IsStatic) {
                        task.Log(Level.Verbose, "Found method {0}.",
                            methodInfo.Name);
                    }
                }
            }

            bool isExtensionAssembly = false;

            ExtensionAssembly extensionAssembly = new ExtensionAssembly (
                assembly);

            Type[] types;

            try {
                types = assembly.GetTypes();
            }
            catch(ReflectionTypeLoadException ex) {
                if(ex.LoaderExceptions != null && ex.LoaderExceptions.Length > 0) {
                    throw ex.LoaderExceptions[0];
                }

                throw;
            }

            foreach (Type type in types) {
                //
                // each extension type is exclusive, meaning a given type 
                // cannot be both a task and data type
                //
                // so it doesn't make sense to scan a type for, for example,
                // data types if the type has already been positively
                // identified as a task
                //

                bool extensionFound = ScanTypeForTasks(extensionAssembly,
                    type, task);

                if (!extensionFound) {
                    extensionFound = ScanTypeForDataTypes(extensionAssembly,
                        type, task);
                }

                if (!extensionFound) {
                    extensionFound = ScanTypeForFunctions(type, task);
                }

                if (!extensionFound) {
                    extensionFound = ScanTypeForFilters(extensionAssembly,
                        type, task);
                }

                if (!extensionFound) {
                    extensionFound = _pluginScanner.ScanTypeForPlugins(
                        extensionAssembly, type, task);
                }

                // if extension is found in type, then mark assembly as
                // extension assembly
                isExtensionAssembly = isExtensionAssembly || extensionFound;
            }

            // if no extension could be found at all, then we might be dealing
            // with an extension assembly that was built using an older version
            // of NAnt(.Core)
            if (!isExtensionAssembly) {
                AssemblyName coreAssemblyName = Assembly.GetExecutingAssembly().
                    GetName(false);

                foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies()) {
                    if (assemblyName.Name == coreAssemblyName.Name) {
                        // the given assembly references NAnt.Core, so check whether
                        // it doesn't reference an older version of NAnt.Core
                        if (assemblyName.Version != coreAssemblyName.Version) {
                            task.Log(Level.Warning, "Assembly \"{0}\" is built"
                                + " using version {1} of NAnt. If any problems"
                                + " arise, then try using a version that is built"
                                + " for NAnt version {2}.", assembly.GetName().Name, 
                                assemblyName.Version, coreAssemblyName.Version);
                        }
                    }
                }
            }

            return isExtensionAssembly;
        }

        /// <summary>
        /// Scans the path for any task assemblies and adds them.
        /// </summary>
        /// <param name="path">The directory to scan in.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <param name="failOnError"><see cref="bool" /> indicating whether scanning of the directory should halt on first error.</param>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static void ScanDir(string path, Task task, bool failOnError) {
            // don't do anything if we don't have a valid directory path
            if (String.IsNullOrEmpty(path)) {
                return;
            }

            task.Log(Level.Info, "Scanning directory \"{0}\" for extension"
                + " assemblies.", path);

            // scan all dll's for tasks, types and functions
            DirectoryScanner scanner = new DirectoryScanner();
            scanner.BaseDirectory = new DirectoryInfo(path);
            scanner.Includes.Add("*.dll");

            foreach (string assemblyFile in scanner.FileNames) {
                try {
                    TypeFactory.ScanAssembly(assemblyFile, task);
                } catch (Exception ex) {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "Failure scanning \"{0}\" for extensions", assemblyFile);

                    if (failOnError) {
                        throw new BuildException(msg + ".", 
                            Location.UnknownLocation, ex);
                    }
                    
                    task.Log(Level.Error, msg + ": " + assemblyFile, ex.Message);
                }
            }
        }

        /// <summary>
        /// Adds any task assemblies in the project base directory
        /// and its <c>tasks</c> subdirectory.
        /// </summary>
        /// <param name="project">The project to work from.</param>
        internal static void AddProject(Project project) {
            AddProject(project, true);
        }

        /// <summary>
        /// Registers the project with <see cref="TypeFactory" />, and optionally
        /// scan the <see cref="Project.BaseDirectory" /> for extension assemblies.
        /// </summary>
        /// <param name="project">The project to work from.</param>
        /// <param name="scan">Specified whether to scan the <see cref="Project.BaseDirectory" /> for extension assemblies.</param>
        internal static void AddProject(Project project, bool scan) {
            if (!scan || String.IsNullOrEmpty(project.BaseDirectory))
                return;

            LoadTasksTask loadTasks = new LoadTasksTask();
            loadTasks.Project = project;
            loadTasks.NamespaceManager = project.NamespaceManager;
            loadTasks.Parent = project;
            loadTasks.FailOnError = false;
            loadTasks.Threshold = (project.Threshold == Level.Debug) ? 
                Level.Debug : Level.Warning;

            string tasksDir = Path.Combine(project.BaseDirectory, "extensions");
            string commonTasksDir = Path.Combine(tasksDir, "common");

            // scan framework-neutral and version-neutral assemblies
            ScanDir(Path.Combine(commonTasksDir, "neutral"), loadTasks,
                false);

            // skip further processing if runtime framework has not yet 
            // been set
            if (project.RuntimeFramework == null) {
                return;
            }

            // scan framework-neutral but version-specific assemblies
            ScanDir(Path.Combine(commonTasksDir, project.RuntimeFramework.
                ClrVersion.ToString (2)), loadTasks, false);

            string frameworkTasksDir = Path.Combine(tasksDir, 
                project.RuntimeFramework.Family);

            // scan framework-specific but version-neutral assemblies
            ScanDir(Path.Combine(frameworkTasksDir, "neutral"), loadTasks,
                false);

            // scan framework-specific and version-specific assemblies
            ScanDir(Path.Combine(frameworkTasksDir, project.RuntimeFramework
                .Version.ToString()), loadTasks, false);
        }

        /// <summary>
        /// Looks up a function by name and argument count.
        /// </summary>
        /// <param name="functionName">The name of the function to lookup, including namespace prefix.</param>
        /// <param name="args">The argument of the function to lookup.</param>
        /// <param name="project">The <see cref="Project" /> in which the function is invoked.</param>
        /// <returns>
        /// A <see cref="MethodInfo" /> representing the function, or 
        /// <see langword="null" /> if a function with the given name and
        /// arguments does not exist.
        /// </returns>
        internal static MethodInfo LookupFunction(string functionName, FunctionArgument[] args, Project project) {
            object function = _methodInfoCollection[functionName];
            if (function == null)
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1052"), functionName));

            MethodInfo mi = function as MethodInfo;
            if (mi != null) {
                if (mi.GetParameters ().Length == args.Length) {
                    CheckDeprecation(functionName, mi, project);
                    return mi;
                }
            } else {
                ArrayList matches = (ArrayList) function;
                for (int i = 0; i < matches.Count; i++) {
                    mi = (MethodInfo) matches [i];
                    if (mi.GetParameters ().Length == args.Length) {
                        CheckDeprecation(functionName, mi, project);
                        return mi;
                    }
                }
            }

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                ResourceUtils.GetString("NA1044"), functionName, args.Length));
        }

        private static void CheckDeprecation(string functionName, MethodInfo function, Project project) {
            // check whether the function is deprecated
            ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute) 
                Attribute.GetCustomAttribute(function, 
                typeof(ObsoleteAttribute), true);

            // if function itself is not deprecated, check if its declaring
            // type is deprecated
            if (obsoleteAttribute == null) {
                obsoleteAttribute = (ObsoleteAttribute) 
                    Attribute.GetCustomAttribute(function.DeclaringType, 
                    typeof(ObsoleteAttribute), true);
            }

            if (obsoleteAttribute != null) {
                string obsoleteMessage = string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1087"), functionName, 
                    obsoleteAttribute.Message);
                if (obsoleteAttribute.IsError) {
                    throw new BuildException(obsoleteMessage, Location.UnknownLocation);
                } else {
                    project.Log(Level.Warning, "{0}", obsoleteMessage);
                }
            }
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
                    ResourceUtils.GetString("NA1083"), taskName), location);
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
                    ResourceUtils.GetString("NA1086"), taskName, 
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
                    ResourceUtils.GetString("NA1082"), filterName), location);
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
                    ResourceUtils.GetString("NA1079"), filterName, 
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

        /// <summary>
        /// Creates the <see cref="DataTypeBase"/> instance.
        /// </summary>
        /// <param name="elementNode">The element XML node.</param>
        /// <param name="proj">The current project.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="System.ArgumentNullException">If elementNode or proj is <c>null</c>.
        /// </exception>
        /// <exception cref="BuildException">If no builder for the elment can be found.
        /// </exception>
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
                    ResourceUtils.GetString("NA1081"), dataTypeName), location);
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
                    ResourceUtils.GetString("NA1085"), dataTypeName, 
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

        #region Private Static Methods

        /// <summary>
        /// Scans a given <see cref="Type" /> for tasks.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="Type" /> to scan.</param>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// <see cref="Task" />; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForTasks(ExtensionAssembly extensionAssembly, Type type, Task task) {
            try {
                TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(TaskNameAttribute));

                if (type.IsSubclassOf(typeof(Task)) && !type.IsAbstract && taskNameAttribute != null) {
                    task.Log(Level.Debug, string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("String_CreatingTaskBuilder"), 
                        type.Name));

                    TaskBuilder tb = new TaskBuilder(extensionAssembly, type.FullName);
                    if (TaskBuilders[tb.TaskName] == null) {
                        task.Log(Level.Debug, string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("String_AddingTask"), tb.TaskName, 
                            GetAssemblyLocation(tb.Assembly), tb.ClassName));

                        TaskBuilders.Add(tb);
                    }

                    // specified type represents a task
                    return true;
                } else {
                    // specified type does not represent valid task
                    return false;
                }
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for tasks.", 
                    type.AssemblyQualifiedName);
                throw;
            }
        }

        /// <summary>
        /// Scans a given <see cref="Type" /> for data type.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="Type" /> to scan.</param>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// data type; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForDataTypes(ExtensionAssembly extensionAssembly, Type type, Task task) {
            try {
                ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

                if (type.IsSubclassOf(typeof(DataTypeBase)) && !type.IsAbstract && elementNameAttribute != null) {
                    logger.InfoFormat(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("String_CreatingDataTypeBaseBuilder"), 
                        type.Name);
                    DataTypeBaseBuilder dtb = new DataTypeBaseBuilder(extensionAssembly, type.FullName);
                    if (DataTypeBuilders[dtb.DataTypeName] == null) {
                        logger.DebugFormat(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("String_AddingDataType"), 
                            dtb.DataTypeName, GetAssemblyLocation(dtb.Assembly), dtb.ClassName);

                        DataTypeBuilders.Add(dtb);
                    }

                    // specified type represents a data type
                    return true;
                } else {
                    // specified type does not represent valid data type
                    return false;
                }
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for data types.", 
                    type.AssemblyQualifiedName);
                throw;
            }
        }        

        /// <summary>
        /// Scans a given <see cref="Type" /> for functions.
        /// </summary>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// valid set of funtions; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForFunctions(Type type, Task task) {
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
                    if (prefix != null && prefix.Length > 0) {
                        prefix += "::";
                    } else {
                        task.Log(Level.Warning, "Ignoring functions in type \"{0}\":"
                            + " no prefix was set.", type.AssemblyQualifiedName);

                        // specified type does not represent a valid functionset
                        return false;
                    }

                    //
                    // add public static/instance methods
                    // 
                    foreach (MethodInfo info in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
                        FunctionAttribute functionAttribute = (FunctionAttribute)
                            Attribute.GetCustomAttribute(info, typeof(FunctionAttribute));
                        if (functionAttribute != null)
                            RegisterFunction(prefix + functionAttribute.Name, info);
                    }

                    // specified type represents a valid functionset
                    return true;
                } else {
                    // specified type does not represent a valid functionset
                    return false;
                }
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for functions.", 
                    type.AssemblyQualifiedName);
                throw;
            }
        }

        private static void RegisterFunction(string key, MethodInfo info) {
            object functions = _methodInfoCollection [key];
            if (functions == null) {
                _methodInfoCollection.Add(key, info);
            } else {
                MethodInfo mi = functions as MethodInfo;
                if (mi == null) {
                    ArrayList overloads = (ArrayList) functions;
                    overloads.Add (info);
                } else {
                    ArrayList overloads = new ArrayList (3);
                    overloads.Add (mi);
                    overloads.Add (info);
                    _methodInfoCollection [key] = overloads;
                }
            }
        }

        /// <summary>
        /// Scans a given <see cref="Type" /> for filters.
        /// </summary>
        /// <param name="extensionAssembly">The <see cref="ExtensionAssembly" /> containing the <see cref="Type" /> to scan.</param>
        /// <param name="type">The <see cref="Type" /> to scan.</param>
        /// <param name="task">The <see cref="Task" /> which will be used to output messages to the build log.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="type" /> represents a
        /// <see cref="Filter" />; otherwise, <see langword="false" />.
        /// </returns>
        private static bool ScanTypeForFilters(ExtensionAssembly extensionAssembly, Type type, Task task) {
            try {
                ElementNameAttribute elementNameAttribute = (ElementNameAttribute) 
                    Attribute.GetCustomAttribute(type, typeof(ElementNameAttribute));

                if (type.IsSubclassOf(typeof(Filter)) && !type.IsAbstract && elementNameAttribute != null) {
                    task.Log(Level.Debug, "Creating FilterBuilder for \"{0}\".", 
                        type.Name);
                    FilterBuilder builder = new FilterBuilder(extensionAssembly, type.FullName);
                    if (FilterBuilders[builder.FilterName] == null) {
                        FilterBuilders.Add(builder);

                        task.Log(Level.Debug, "Adding filter \"{0}\" from {1}:{2}.", 
                            builder.FilterName, GetAssemblyLocation(builder.Assembly), 
                            builder.ClassName);
                    }

                    // specified type represents a filter
                    return true;
                }

                // specified type does not represent a valid filter
                return false;
            } catch {
                task.Log(Level.Error, "Failure scanning \"{0}\" for filters.", 
                    type.AssemblyQualifiedName);
                throw;
            }
        }

        private static string GetAssemblyLocation(Assembly assembly) {
            try {
                return assembly.Location;
            } catch (NotSupportedException) {
                // Location is not supported in dynamic assemblies, so instead
                // return name
                return assembly.GetName().Name;
            }
        }

        #endregion Private Static Methods
    }
}
