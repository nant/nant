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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

using NAnt.Core.Attributes;

namespace NAnt.Core {
    public class TaskBuilder {
        #region Public Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="TaskBuilder" /> class
        /// for the specified task class in the assembly specified.
        /// </summary>
        /// <param name="className">The class representing the task.</param>
        /// <param name="assemblyFileName">The assembly containing the task.</param>/// 
        public TaskBuilder(string className, string assemblyFileName) {
            _className = className;
            _assemblyFileName = assemblyFileName;

            // determine from which assembly the task will be created
            Assembly assembly = GetAssembly();

            // get task name from attribute
            TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                Attribute.GetCustomAttribute(assembly.GetType(ClassName), typeof(TaskNameAttribute));

            _taskName = taskNameAttribute.Name;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the task class that can be created using this
        /// <see cref="TaskBuilder" />.
        /// </summary>
        /// <value>
        /// The name of the task class that can be created using this
        /// <see cref="TaskBuilder" />.
        /// </value>
        public string ClassName {
            get { return _className; }
        }

        /// <summary>
        /// Gets the filename of the <see cref="Assembly" /> from which the
        /// task will be created.
        /// </summary>
        /// <value>
        /// The filename of the <see cref="Assembly" /> from which the task will
        /// be created, or <see langword="null" /> to create the task from the
        /// executing <see cref="Assembly" />.
        /// </value>
        public string AssemblyFileName {
            get { return _assemblyFileName; }
        }

        /// <summary>
        /// Gets the name of the task which the <see cref="TaskBuilder" />
        /// can create.
        /// </summary>
        /// <value>
        /// The name of the task which the <see cref="TaskBuilder" /> can 
        /// create.
        /// </value>
        public string TaskName {
            get { return _taskName; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public Task CreateTask() {
            Assembly assembly = GetAssembly();
            return (Task) assembly.CreateInstance(
                ClassName, 
                true, 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                null,
                CultureInfo.InvariantCulture,
                null);
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Gets the <see cref="Assembly" /> from which the task identified by
        /// <see cref="TaskName" /> will be created.
        /// </summary>
        /// <returns>
        /// The <see cref="Assembly" /> from which the task identified by 
        /// <see cref="TaskName" /> will be created.
        /// </returns>
        private Assembly GetAssembly() {
            Assembly assembly = null;

            if (AssemblyFileName == null) {
                assembly = Assembly.GetExecutingAssembly();
            } else {
                //check to see if it is loaded already
                Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < ass.Length; i++) {
                    try {
                        string assemblyLocation = ass[i].Location;

                        if (assemblyLocation != null && assemblyLocation == AssemblyFileName) {
                            assembly = ass[i];
                            break;
                        }
                    } catch (NotSupportedException) {
                        // dynamically loaded assemblies do not not have a location
                    }
                }

                //load if not loaded
                if (assembly == null) {
                    assembly = Assembly.LoadFrom(AssemblyFileName);
                }
            }
            return assembly;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _className;
        private string _assemblyFileName;
        private string _taskName;

        #endregion Private Instance Fields
    }
}
