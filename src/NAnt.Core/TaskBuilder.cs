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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {
    public class TaskBuilder {
        #region Public Instance Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="TaskBuilder" /> class
        /// for the specified task class.
        /// </summary>
        /// <param name="className">The class representing the task.</param>
        public TaskBuilder(string className) : this(className, null) {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="TaskBuilder" /> class
        /// for the specified task class in the assembly specified.
        /// </summary>
        /// <param name="className">The class representing the task.</param>
        /// <param name="assemblyFileName">The assembly containing the task.</param>/// 
        public TaskBuilder(string className, string assemblyFileName) {
            _className = className;
            _assemblyFileName = assemblyFileName;

            Assembly assembly = GetAssembly();
            // get task name from attribute
            TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                Attribute.GetCustomAttribute(assembly.GetType(ClassName), typeof(TaskNameAttribute));

            _taskName = taskNameAttribute.Name;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string ClassName {
            get { return _className; }
        }

        public string AssemblyFileName {
            get { return _assemblyFileName; }
        }

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

        private Assembly GetAssembly() {
            Assembly assembly = null;

            if (AssemblyFileName == null) {
                assembly = Assembly.GetExecutingAssembly();
            } else {
                //check to see if it is loaded already
                Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < ass.Length; i++){
                    if (ass[i].Location != null && ass[i].Location == AssemblyFileName) { 
                        assembly = ass[i];
                        return assembly;
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

        string _className;
        string _assemblyFileName;
        string _taskName;

        #endregion Private Instance Fields
    }
}
