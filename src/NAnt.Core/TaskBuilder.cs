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

using System;
using System.Reflection;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt {

    public class TaskBuilder {
        
        string _className;
        string _assemblyFileName;
        string _taskName;

        public TaskBuilder(string className) : this(className, null) {
        }

        public TaskBuilder(string className, string assemblyFileName) {
            _className = className;
            _assemblyFileName = assemblyFileName;

            // get task name from attribute
            Assembly assembly = GetAssembly();
            TaskNameAttribute taskNameAttribute = (TaskNameAttribute) 
                Attribute.GetCustomAttribute(assembly.GetType(ClassName), typeof(TaskNameAttribute));

            _taskName = taskNameAttribute.Name;
        }

        public string ClassName {
            get { return _className; }
        }

        public string AssemblyFileName {
            get { return _assemblyFileName; }
        }

        public string TaskName {
            get { return _taskName; }
        }

        private Assembly GetAssembly() {
            Assembly assembly;
            if (AssemblyFileName == null) {
                assembly = Assembly.GetExecutingAssembly();
            } else {
                assembly = Assembly.LoadFrom(AssemblyFileName);
            }
            return assembly;
        }

        public Task CreateTask() {
            Assembly assembly = GetAssembly();
            return (Task) assembly.CreateInstance(ClassName, true);
        }
    }
}
