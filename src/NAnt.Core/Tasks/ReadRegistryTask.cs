// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez
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


namespace SourceForge.NAnt.Tasks {

    using System;
    using SourceForge.NAnt.Attributes;
    using Microsoft.Win32;
    
    /// <summary>
    /// A task that reads a value or set of values from the Windows Registry into one or more NAnt properties.
    /// </summary>
    /// <example>
    ///     <para>Reads a single value from the registry</para>
    ///     <code><![CDATA[<readregistry property="sdkRoot" key="\SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />]]></code>
    ///     <para>Reads all the registry values in a key</para>
    ///     <code><![CDATA[<readregistry prefix="dotNetFX" key="\SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />]]></code>
    /// </example>
    [TaskName("readregistry")]
    public class ReadRegistryTask : Task {
        protected string _propName = null;
        protected string _propPrefix = null;
        protected string _regKey = null;
        protected string _regKeyValueName = null;
        protected RegistryHive[] _regHive = {RegistryHive.LocalMachine};
        private string _regHiveString = RegistryHive.LocalMachine.ToString();

        
        /// <summary>The property to set to the specified registry key value.</summary>
        [TaskAttribute("property")]
        public virtual string PropertyName {
            set { _propName = value; }
        }

        /// <summary>The prefix to use for the specified registry key values.</summary>
        [TaskAttribute("prefix")]
        public virtual string PropertyPrefix{
            set { _propPrefix = value; }
        }

        /// <summary>The registry key to read.</summary>
        [TaskAttribute("key", Required=true)]
        public virtual string RegistryKey {
            set { 
                string[] pathParts = value.Split("\\".ToCharArray(0,1)[0]);
                _regKeyValueName = pathParts[pathParts.Length - 1];
                _regKey = value.Substring(0, (value.Length - _regKeyValueName.Length));
            }
        }

        /// <summary>The registry hive to use.</summary>
        [TaskAttribute("hive")]
        public virtual string RegistryHiveName {
            set {
                _regHiveString = value;
                string[] tempRegHive = _regHiveString.Split(" ".ToCharArray()[0]);
                _regHive = (RegistryHive[]) Array.CreateInstance(typeof(RegistryHive), tempRegHive.Length);
                for (int x=0; x<tempRegHive.Length; x++) {
                    _regHive[x] = (RegistryHive)System.Enum.Parse(typeof(RegistryHive), tempRegHive[x], true);
                }
            }
        }

        protected override void ExecuteTask() {
            object regKeyValue = null;

            if(_regKey == null)
                throw new BuildException("Missing registry key!");

            RegistryKey mykey = null;
            if (_propName != null) {
                mykey = LookupRegKey(_regKey, _regHive, Verbose);
                regKeyValue = mykey.GetValue(_regKeyValueName);
                if (regKeyValue != null) {
                    string val = regKeyValue.ToString();
                    Properties[_propName] = val;
                } else {
                    throw new BuildException(string.Format( "Registry Value Not Found! - key='{0}';hive='{1}';", _regKey + "\\" + _regKeyValueName, _regHiveString ));
                }
            }
            else if (_propName == null && _propPrefix != null ) {
                mykey = LookupRegKey(_regKey, _regHive, Verbose);
                foreach(string name in mykey.GetValueNames()) {
                    Properties[_propPrefix + "." + name] = mykey.GetValue(name).ToString();
                }
            }
            else {
                throw new BuildException("Missing both a property name and property prefix; atleast one if required!");
            }
        }

        protected static RegistryKey LookupRegKey(string key, RegistryHive[] registries, bool verbose) {
            foreach(RegistryHive hive in registries){
                Log.WriteLineIf(verbose, "Opening {0}:{1}", hive.ToString(), key);
                RegistryKey returnkey = GetHiveKey(hive).OpenSubKey(key, false);
                if(returnkey != null) {
                    return returnkey;
                }
            }
            throw new BuildException(string.Format("Registry Path Not Found! - key='{0}';hive='{1}';", key, registries.ToString()));
        }
        protected static RegistryKey GetHiveKey(RegistryHive hive) {
            
            switch(hive) {
                case RegistryHive.LocalMachine:
                    return Registry.LocalMachine;
                case RegistryHive.Users:
                    return Registry.Users;
                case RegistryHive.CurrentUser:
                    return Registry.CurrentUser;
                case RegistryHive.ClassesRoot:
                    return Registry.ClassesRoot;
                default:
                    Log.WriteLine("Registry not found for {0}!", hive.ToString());
                    return null;
            }
        }
    }
}
