// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Functions;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Checks the conditional attributes and executes the children if
    /// <see langword="true" />.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   If no conditions are checked, all child tasks are executed. 
    ///   </para>
    ///   <para>
    ///   If more than one attribute is used, they are &amp;&amp;'d. The first 
    ///   to fail stops the check.
    ///   </para>
    ///   <para>
    ///   The order of condition evaluation is, <see cref="TargetNameExists" />, 
    ///   <see cref="PropertyNameExists" />, <see cref="PropertyNameTrue" />, 
    ///   <see cref="UpToDateFile" />.
    ///   </para>
    ///   <note>
    ///   instead of using the deprecated attributes, we advise you to use the
    ///   following functions in combination with the <see cref="Test" />
    ///   attribute:
    ///   </note>
    ///   <list type="table">
    ///     <listheader>
    ///         <term>Function</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="PropertyFunctions.Exists(string)" /></term>
    ///         <description>Checks whether the specified property exists.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TargetFunctions.Exists(string)" /></term>
    ///         <description>Checks whether the specified target exists.</description>
    ///     </item>
    ///   </list>  
    /// </remarks>
    /// <example>
    ///   <para>Tests the value of a property using expressions.</para>
    ///   <code>
    ///     <![CDATA[
    /// <if test="${build.configuration=='release'}">
    ///     <echo>Build release configuration</echo>
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Tests the the output of a function.</para>
    ///   <code>
    ///     <![CDATA[
    /// <if test="${not file::exists(filename) or file::get-length(filename) = 0}">
    ///     <echo message="The version file ${filename} doesn't exist or is empty!" />
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para><c>(Deprecated)</c> Check that a target exists.</para>
    ///   <code>
    ///     <![CDATA[
    /// <target name="myTarget" />
    /// <if targetexists="myTarget">
    ///     <echo message="myTarget exists" />
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para><c>(Deprecated)</c> Check existence of a property.</para>
    ///   <code>
    ///     <![CDATA[
    /// <if propertyexists="myProp">
    ///     <echo message="myProp Exists. Value='${myProp}'" />
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para><c>(Deprecated)</c> Check that a property value is true.</para>
    ///   <code>
    ///     <![CDATA[
    /// <if propertytrue="myProp">
    ///     <echo message="myProp is true. Value='${myProp}'" />
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   <c>(Deprecated)</c> Check that a property exists and is <see langword="true" /> 
    ///   (uses multiple conditions).
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <if propertyexists="myProp" propertytrue="myProp">
    ///     <echo message="myProp is '${myProp}'" />
    /// </if>
    ///     ]]>
    ///   </code>
    ///   <para>which is the same as</para>
    ///   <code>
    ///     <![CDATA[
    /// <if propertyexists="myProp">
    ///     <if propertytrue="myProp">
    ///         <echo message="myProp is '${myProp}'" />
    ///     </if>
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   <c>(Deprecated)</c> Check file dates. If <c>myfile.dll</c> is uptodate,
    ///   then do stuff.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <if uptodatefile="myfile.dll" comparefile="myfile.cs">
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs" />
    /// </if>
    ///     ]]>
    ///   </code>
    ///   <para>or</para>
    ///   <code>
    ///     <![CDATA[
    /// <if uptodatefile="myfile.dll">
    ///     <comparefiles>
    ///         <include name="*.cs" />
    ///     </comparefiles>
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs" />
    /// </if>
    ///     ]]>
    ///   </code>
    ///   <para>or</para>
    ///   <code>
    ///     <![CDATA[
    /// <if>
    ///     <uptodatefiles>
    ///         <include name="myfile.dll" />
    ///     </uptodatefiles>
    ///     <comparefiles>
    ///         <include name="*.cs" />
    ///     </comparefiles>
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs" />
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("if")]
    public class IfTask : TaskContainer {
        #region Private Instance Fields

        private string _propNameTrue;
        private string _propNameExists;
        private string _targetName;
        private string _test;
        private FileSet _compareFiles;
        private FileSet _uptodateFiles;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to compare if uptodate.
        /// </summary>
        [TaskAttribute("uptodatefile")]
        [System.Obsolete("Use <if test=\"${file::up-to-date(comparefile, uptodatefile)}\"> instead.", false)]
        public string UpToDateFile {
            set {
                if (_uptodateFiles == null) {
                    _uptodateFiles = new FileSet();
                    _uptodateFiles.Parent = this;
                    _uptodateFiles.Project = Project;
                    _uptodateFiles.NamespaceManager = NamespaceManager;
                }
                _uptodateFiles.Includes.Add(value); 
            }
        }

        /// <summary>
        /// The file to check against for the uptodate file.
        /// </summary>
        [TaskAttribute("comparefile")]
        [System.Obsolete("Use <if test=\"${file::up-to-date(comparefile, uptodatefile)}\"> instead.", false)]
        public string CompareFile {
            set { 
                if (_compareFiles == null) {
                    _compareFiles = new FileSet();
                    _compareFiles.Parent = this;
                    _compareFiles.Project = Project;
                    _compareFiles.NamespaceManager = NamespaceManager;
                }
                _compareFiles.Includes.Add(value); 
            }
        }

        /// <summary>
        /// The <see cref="FileSet" /> that contains the comparison files for 
        /// the <see cref="UpToDateFile" />(s) check.
        /// </summary>
        [BuildElement("comparefiles")]
        [System.Obsolete("Use <uptodate /> task instead.", false)]
        public FileSet CompareFiles {
            get { return _compareFiles; }
            set { _compareFiles = value; }
        } 

        /// <summary>
        /// The <see cref="FileSet" /> that contains the uptodate files for 
        /// the <see cref="CompareFile" />(s) check.
        /// </summary>
        [BuildElement("uptodatefiles")]
        [System.Obsolete("Use <uptodate /> task instead.", false)]
        public FileSet UpToDateFiles {
            get { return _uptodateFiles; }
            set { _uptodateFiles = value; }
        }

        /// <summary>
        /// Used to test whether a property is true.
        /// </summary>
        [TaskAttribute("propertytrue")]
        [System.Obsolete("Use <if test=\"${propertyname}\"> instead.", false)]
        public string PropertyNameTrue {
            get { return _propNameTrue; }
            set { _propNameTrue = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Used to test whether a property exists.
        /// </summary>
        [TaskAttribute("propertyexists")]
        [System.Obsolete("Use <if test=\"${property::exists('propertyname')}\"> instead.", false)]
        public string PropertyNameExists {
            get { return _propNameExists;}
            set { _propNameExists = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Used to test whether a target exists.
        /// </summary>
        [TaskAttribute("targetexists")]
        [System.Obsolete("Use <if test=\"${target::exists('targetname')}\"> instead.", false)]
        public string TargetNameExists {
            get { return _targetName; }
            set { _targetName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Used to test arbitrary boolean expression.
        /// </summary>
        [TaskAttribute("test")]
        [BooleanValidator()]
        public string Test {
            get { return _test; }
            set { _test = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        protected virtual bool ConditionsTrue {
            get {
                bool ret = true;

                if (Test != null) {
                    if (!Convert.ToBoolean(Test, CultureInfo.InvariantCulture)) {
                        return false;
                    }
                }

                // check if target exists
                if (TargetNameExists != null) {
                    ret = ret && (Project.Targets.Find(TargetNameExists) != null);
                    if (!ret) {
                        return false;
                    }
                }

                // check if property exists
                if (PropertyNameExists != null) {
                    ret = ret && Properties.Contains(PropertyNameExists);
                    if (!ret) {
                        return false;
                    }
                }

                // check if value of property is boolean true
                if (PropertyNameTrue != null) {
                    try {
                        ret = ret && bool.Parse(Properties[PropertyNameTrue]);
                        if (!ret) {
                            return false;
                        }
                    } catch (Exception ex) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1126"), PropertyNameTrue), Location, ex);
                    }
                }

                // check if file is up-to-date
                if (UpToDateFiles != null) {
                    FileInfo primaryFile = UpToDateFiles.MostRecentLastWriteTimeFile;
                    if (primaryFile == null || !primaryFile.Exists) {
                        ret = false;
                        Log(Level.Verbose, "Uptodatefile(s) do(es) not exist.");
                    } else {
                        string newerFile = FileSet.FindMoreRecentLastWriteTime(_compareFiles.FileNames, primaryFile.LastWriteTime);
                        bool needsAnUpdate = (newerFile != null);
                        if (needsAnUpdate) {
                            Log(Level.Verbose, "{0} is newer than {1}.", newerFile, primaryFile.Name);
                        }
                        ret = ret && !needsAnUpdate;
                    }
                    if (!ret) {
                        return false;
                    }
                }

                return ret;
            }
        }

        #endregion Protected Instance Properties

        #region Override implementation of TaskContainer

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            if (ConditionsTrue) {
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of TaskContainer

        #region Override implementation of Task

        /// <summary>
        /// Automatically exclude build elements that are defined on the task
        /// from things that get executed, as they are evaluated normally during
        /// XML task initialization.
        /// </summary>
        /// <exception cref="BuildException">At least one if condition +
        ///                          must be set (test, propertytrue, targetexists, etc...):</exception>
        protected override void Initialize() {
            base.Initialize();

            //check that we have something to do.
            if ((UpToDateFiles == null || CompareFiles == null) && Test == null && PropertyNameExists == null && PropertyNameTrue == null && TargetNameExists == null) {
                throw new BuildException("At least one if condition" +
                        " must be set (test, propertytrue, targetexists, etc...):", Location);
            }
        }

        #endregion Override implementation of Task
    }

    /// <summary>
    /// The opposite of the <c>if</c> task.
    /// </summary>
    /// <example>
    ///   <para>Check that a property does not exist.</para>
    ///   <code>
    ///     <![CDATA[
    /// <ifnot propertyexists="myProp">
    ///     <echo message="myProp does not exist."/>
    /// </if>
    ///     ]]>
    ///   </code>
    ///   <para>Check that a property value is not true.</para>
    ///   <code>
    ///     <![CDATA[
    /// <ifnot propertytrue="myProp">
    ///     <echo message="myProp is not true."/>
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Check that a target does not exist.</para>
    ///   <code>
    ///     <![CDATA[
    /// <ifnot targetexists="myTarget">
    ///     <echo message="myTarget does not exist."/>
    /// </if>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("ifnot")]
    [Obsolete("Use the <if> task instead.", false)]
    public class IfNotTask : IfTask {
        #region Override implementation of IfTask

        protected override bool ConditionsTrue {
            get { return !base.ConditionsTrue; }
        }

        #endregion Override implementation of IfTask
    }
}
