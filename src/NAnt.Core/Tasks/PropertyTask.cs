// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets a property in the current project.
    /// </summary>
    /// <remarks>
    ///   <note>NAnt uses a number of predefined properties.</note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Define a <c>debug</c> property with value <see langword="true" />.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <property name="debug" value="true" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Use the user-defined <c>debug</c> property.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <property name="trace" value="${debug}" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Define a read-only property. This is just like passing in the param 
    ///   on the command line.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <property name="do_not_touch_ME" value="hammer" readonly="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("property")]
    public class PropertyTask : Task {
        #region Private Instance Fields

        private string _name = null;
        private string _value = string.Empty;
        private bool _readOnly = false;
        private bool _dynamic = false;
        private bool _overwrite = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the NAnt property to set.
        /// </summary>
        [TaskAttribute("name", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string PropertyName {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// The value to assign to the NAnt property.
        /// </summary>
        [TaskAttribute("value", Required=true, ExpandProperties=false)]
        [StringValidator(AllowEmpty=true)]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Specifies whether the property is read-only or not. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("readonly", Required=false)]
        [BooleanValidator()]
        public bool ReadOnly {
            get { return _readOnly; }
            set { _readOnly = value; }
        }

        /// <summary>
        /// Specifies whether references to other properties should not be 
        /// expanded when the value of the property is set, but expanded when
        /// the property is actually used.  By default, properties will be
        /// expanded when set.
        /// </summary>
        [TaskAttribute("dynamic", Required=false)]
        [BooleanValidator()]
        public bool Dynamic {
            get { return _dynamic; }
            set { _dynamic = value; }
        }

        /// <summary>
        /// Specifies whether the value of a property should be overwritten if
        /// the property already exists (unless the property is read-only). 
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("overwrite", Required=false)]
        [BooleanValidator()]
        public bool Overwrite {
            get { return _overwrite; }
            set { _overwrite = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            string propertyValue;

            if (!Dynamic) {
                propertyValue = Project.ExpandProperties(Value, Location);
            } else {
                propertyValue = Value;
            }

            // Special check for framework setting.
            if (PropertyName == "nant.settings.currentframework") {
                if (Project.Frameworks.Contains(propertyValue)) {
                    Project.TargetFramework = Project.Frameworks[propertyValue];
                    return;
                } else {
                    ArrayList validvalues = new ArrayList();
                    foreach (FrameworkInfo framework in Project.Frameworks) {
                        validvalues.Add(framework.Name);
                    }
                    string validvaluesare = string.Empty;
                    if (validvalues.Count > 0) {
                        validvaluesare = string.Format(CultureInfo.InvariantCulture, 
                            "Valid values are: {0}.", string.Join(", ", (string[]) validvalues.ToArray(typeof(string))));
                    }
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Target framework could not be changed. \"{0}\" is not a valid framework identifier. {1}", 
                        propertyValue, validvaluesare), Location);
                }
            }

            if (!Project.Properties.Contains(PropertyName)) {
                if (ReadOnly) {
                    Properties.AddReadOnly(PropertyName, propertyValue);
                } else {
                    Properties[PropertyName] = propertyValue;
                }

                if (Dynamic) {
                    Properties.MarkDynamic(PropertyName);
                }
            } else {
                if (Overwrite) {
                    if (Project.Properties.IsReadOnlyProperty(PropertyName)) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Read-only property \"{0}\" cannot be overwritten.", 
                            PropertyName), Location);
                    } else {
                        Properties[PropertyName] = propertyValue;

                        if (Dynamic) {
                            Properties.MarkDynamic(PropertyName);
                        }
                    }
                } else {
                    Log(Level.Verbose, "Property \"{0}\" already exists, and \"overwrite\" is set to false.", PropertyName);
                }
            }
        }

        #endregion Override implementation of Task
    }
}
