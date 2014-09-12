// NAnt - A .NET build tool
// Copyright (C) 2001-2011 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System;
using System.Reflection;

namespace NAnt.MSBuild.BuildEngine {
    internal class Project {
        object _obj;
        Type _t;

        public Project(Engine engine) {
            _t = engine.Assembly.GetType("Microsoft.Build.BuildEngine.Project");
            _obj = Activator.CreateInstance(_t, engine.Object);                      
        }

        public string FullFileName {
            get { return (string)_t.GetProperty("FullFileName").GetValue(_obj, null); }
            set { _t.GetProperty("FullFileName").SetValue(_obj, value, null); }
        }

        private PropertyInfo ToolsVersionPI {
            get { return _t.GetProperty("ToolsVersion"); }
        }

        public string ToolsVersion {
            get { PropertyInfo pi = ToolsVersionPI; if (pi == null) return "2.0"; return (string)pi.GetValue(_obj, null); }
            set { PropertyInfo pi = ToolsVersionPI; if (pi == null) return; pi.SetValue(_obj, value, null); }
        }

        public void LoadXml(string projectXml) {
            _t.GetMethod("LoadXml", new Type[] { typeof(string) }).Invoke(_obj, new object[] { projectXml });
        }

        public BuildPropertyGroup GlobalProperties {
            get { return new BuildPropertyGroup(_t.GetProperty("GlobalProperties").GetValue(_obj, null)); }
        }

        public string GetEvaluatedProperty(string propertyName) {
            return (string)_t.GetMethod("GetEvaluatedProperty").Invoke(_obj, new object[] { propertyName });
        }

        public BuildItemGroup GetEvaluatedItemsByName(string itemName) {
            return new BuildItemGroup(_t.GetMethod("GetEvaluatedItemsByName").Invoke(_obj, new object[] { itemName }));
        }

        public void RemoveItemsByName(string itemName) {
            _t.GetMethod("RemoveItemsByName").Invoke(_obj, new object[] { itemName });            
        }

        public BuildItemGroup AddNewItemGroup() {
            return new BuildItemGroup(_t.GetMethod("AddNewItemGroup").Invoke(_obj, null));
        }

        public bool Build() {
            return (bool) _t.GetMethod("Build", new Type[] {}).Invoke(_obj, null);
        }
    }
}
