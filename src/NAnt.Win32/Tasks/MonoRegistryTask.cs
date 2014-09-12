// NAnt - A .NET build tool
// Copyright (C) 2002 Ryan Boggs
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using NAnt.Core;
using NAnt.Core.Attributes;
using Microsoft.Win32;

namespace NAnt.Win32.Tasks {

    /// <summary>
    /// Reads the mono registry path into a NAnt property.
    /// </summary>
    /// <remarks>
    /// The mono registry keyes can exist in one of two places depending on the platform. This
    /// task will check to see which registry path that Mono is using.
    /// </remarks>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <monoregistry property="mono.reg" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("monoregistry")]
    internal class MonoRegistryTask : Task {

        #region Private Static Fields

        /// <summary>
        /// The Mono reg key to default to if none of the keys in _regKeys are found
        /// in the running machine.
        /// </summary>
        private const string _defaultRegKey = @"SOFTWARE\Mono";

        #endregion Private Static Fields

        #region Private Instance Fields

        /// <summary>
        /// Private property name to assign the Mono registry path to.
        /// </summary>
        private string _propName;

        /// <summary>
        /// Private array of Mono registry paths to test in order.
        /// </summary>
        /// <remarks>
        /// If new registry paths are used by the Mono team, add them to this array.
        /// </remarks>
        private string[] _regKeys = new string[] {
            @"SOFTWARE\Wow6432Node\Novell\Mono",
            @"SOFTWARE\Novell\Mono"
        };

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// <para>
        /// The property to set to the Mono registry path.
        /// </para>
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public virtual string PropertyName {
            get { return _propName; }
            set { _propName = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Locates the appropriate Mono registry path to use.
        /// </summary>
        protected override void ExecuteTask() {
            foreach(string key in _regKeys) {
                RegistryKey checkKey = Registry.LocalMachine.OpenSubKey(key);

                if (checkKey != null) {
                    Properties[_propName] = key;
                    return;
                }
            }
            // If none of the paths found in the _regKeys array, assign the default value
            // to the property.
            Properties[_propName] = _defaultRegKey;
        }

        #endregion Override implementation of Task
    }
}