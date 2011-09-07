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

using System;
using System.Globalization;
using System.Security.Permissions;

using Microsoft.Win32;
using NAnt.Core;
using NAnt.Core.Attributes;

#if (!NET_4_0)
[assembly: RegistryPermissionAttribute(SecurityAction.RequestMinimum , Unrestricted=true)]
#endif
namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Reads a value or set of values from the Windows Registry into one or 
    /// more NAnt properties.
    /// </summary>
    /// <example>
    ///   <para>Read a single value from the registry.</para>
    ///   <code>
    ///     <![CDATA[
    /// <readregistry property="sdkRoot" key="SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />
    ///     ]]>
    ///   </code>
    ///   <para>Read all the registry values in a key.</para>
    ///   <code>
    ///     <![CDATA[
    /// <readregistry prefix="dotNetFX" key="SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot" hive="LocalMachine" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("readregistry")]
    public class ReadRegistryTask : Task {
        #region Private Instance Fields

        private string _propName;
        private string _propPrefix;
        private string _regKey;
        private string _regKeyValueName;
        private RegistryHive[] _regHive = {RegistryHive.LocalMachine};
        private string _regHiveString = RegistryHive.LocalMachine.ToString();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        ///     <para>The property to set to the specified registry key value.</para>
        ///     <para>If this attribute is used then a single value will be read.</para>
        /// </summary>
        [TaskAttribute("property")]
        public virtual string PropertyName {
            get { return _propName; }
            set { _propName = value; }
        }

        /// <summary>
        ///     <para>The prefix to use for the specified registry key values.</para>
        ///     <para>If this attribute is used then all registry values will be read and stored as properties with this prefix.</para>
        /// </summary>
        /// <example>
        ///     <para>Registry values a, b, c will be turned into prefixa, prefixb, prefixc named properties</para>
        /// </example>
        [TaskAttribute("prefix")]
        public virtual string PropertyPrefix {
            get { return _propPrefix; }
            set { _propPrefix = value; }
        }

        /// <summary>
        /// The registry key to read, including the path.
        /// </summary>
        /// <example>
        /// SOFTWARE\Microsoft\.NETFramework\sdkInstallRoot
        /// </example>
        [TaskAttribute("key", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public virtual string RegistryKey {
            get { return _regKey; }
            set {
                string key = value;
                if(value.StartsWith("\\")) {
                    key = value.Substring(1);
                }
                string[] pathParts = key.Split("\\".ToCharArray(0,1)[0]);
                //split the key/path apart.
                _regKeyValueName = pathParts[pathParts.Length - 1];
                _regKey = key.Substring(0, (value.Length - _regKeyValueName.Length));
            }
        }

        /// <summary>
        /// Space separated list of registry hives to search for <see cref="ReadRegistryTask.RegistryKey" />.
        /// For a list of possible values, see <see cref="RegistryHive" />. The 
        /// default is <see cref="RegistryHive.LocalMachine" />.
        /// </summary>
        /// <remarks>
        /// <seealso cref="RegistryHive" />
        /// </remarks>
        [TaskAttribute("hive")]
        public virtual string RegistryHiveName {
            get { return _regHiveString; }
            set {
                _regHiveString = value;
                string[] tempRegHive = _regHiveString.Split(" ".ToCharArray()[0]);
                _regHive = (RegistryHive[]) Array.CreateInstance(typeof(RegistryHive), tempRegHive.Length);
                for (int x=0; x<tempRegHive.Length; x++) {
                    _regHive[x] = (RegistryHive) Enum.Parse(typeof(RegistryHive), tempRegHive[x], true);
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task
        
        /// <summary>
        /// read the specified registry value
        /// </summary>
        protected override void ExecuteTask() {
            object regKeyValue = null;

            if (_regKey == null) {
                throw new BuildException("Missing registry key!");
            }

            RegistryKey mykey = null;
            if (_propName != null) {
                mykey = LookupRegKey(_regKey, _regHive);
                regKeyValue = mykey.GetValue(_regKeyValueName);
                if (regKeyValue != null) {
                    string val = regKeyValue.ToString();
                    Properties[_propName] = val;
                } else {
                    throw new BuildException(String.Format(CultureInfo.InvariantCulture, "Registry Value Not Found! - key='{0}';hive='{1}';", _regKey + "\\" + _regKeyValueName, _regHiveString));
                }
            } else if (_propName == null && _propPrefix != null) {
                mykey = LookupRegKey(_regKey, _regHive);
                foreach (string name in mykey.GetValueNames()) {
                    Properties[_propPrefix + "." + name] = mykey.GetValue(name).ToString();
                }
            } else {
                throw new BuildException("Missing both a property name and property prefix; atleast one if required!");
            }
        }

        #endregion Override implementation of Task

        #region Protected Instance Methods

        /// <summary>
        /// Returns the hive for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="registries"></param>
        /// <returns>
        /// The hive for a given key.
        /// </returns>
        protected RegistryKey LookupRegKey(string key, RegistryHive[] registries) {
            foreach (RegistryHive hive in registries) {
                Log(Level.Verbose, "Opening {0}:{1}.", hive.ToString(), key);
                RegistryKey returnkey = GetHiveKey(hive).OpenSubKey(key, false);
                if (returnkey != null) {
                    return returnkey;
                }
            }
            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                "Registry Path Not Found! - key='{0}';hive='{1}';", key, 
                _regHiveString));
        }

        /// <summary>
        /// Returns the key for a given registry hive.
        /// </summary>
        /// <param name="hive">The registry hive to return the key for.</param>
        /// <returns>
        /// The key for a given registry hive.
        /// </returns>
        protected RegistryKey GetHiveKey(RegistryHive hive) {
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
                    Log(Level.Verbose, "Registry not found for {0}.", 
                        hive.ToString());
                    return null;
            }
        }

        #endregion Protected Instance Methods
    }
}
