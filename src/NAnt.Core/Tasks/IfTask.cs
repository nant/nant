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
    using System.IO;
    using System.Collections;
    using System.Collections.Specialized;
    using SourceForge.NAnt.Attributes;
    using System.Globalization;

    /// <summary>Checks the conditional attributes and executes the children if true.</summary>
    /// <remarks>
    ///     <para>If no conditions are checked, all child tasks are executed. 
    ///     True is the default condition result (with no conditions specified).</para>
    ///     <para>If more than one attribute is used, they are &amp;&amp;'d. The first to fail stops the check.</para>
    ///     <para>The order of condition evaluation is, target, propertyexists, propertytrue, uptodate.</para>
    /// </remarks>
    /// <example>
    ///   <para>Check that a target exists</para>
    ///   <code>
    ///   <![CDATA[
    ///   <target name="myTarget"/>
    ///   <if targetexists="myTarget">
    ///     <echo message="myTarget exists."/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    /// <example>
    ///   <para>Check existence of a property</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if propertyexists="myProp">
    ///     <echo message="myProp Exists. Value='${myProp}'"/>
    ///   </if>
    ///   ]]></code>
    ///   
    ///   <para>Check that a property value is true</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if propertytrue="myProp">
    ///     <echo message="myProp is true. Value='${myProp}'"/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    /// <example>
    ///   <para>Check that a property exists and is true (uses multiple conditions)</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if propertyexists="myProp" propertytrue="myProp">
    ///     <echo message="myProp is '${myProp}'"/>    
    ///   </if>
    ///   ]]></code>
    ///   <para>which is the same as</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if propertyexists="myProp">
    ///     <if propertytrue="myProp">
    ///         <echo message="myProp is '${myProp}'"/>    
    ///     </if>
    ///   </if>
    ///   ]]></code>
    /// </example>
    /// 
    /// <example>
    ///   <para>Checks file dates</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if uptodatefile="myfile.dll" comparefile="myfile.cs">
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs"/>
    ///   </if>
    ///   ]]></code>
    ///   or
    ///   <code>
    ///   <![CDATA[
    ///   <if uptodatefile="myfile.dll" comparefile="myfile.cs">
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs"/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    [TaskName("if")]
    public class IfTask : TaskContainer{
        
        protected string _propNameTrue = null;
        protected string _propNameExists = null;
        protected string _targetName = null;
        protected string _uptodateFile = null;
        protected FileSet _compareFiles = null;

        /// <summary>
        /// The file to compare if uptodate
        /// </summary>
        [TaskAttribute("uptodateFile")]
        public string PrimaryFile {
            set {_uptodateFile = Project.GetFullPath(value);}
        }

        /// <summary>
        /// The file to check against for the uptodate file.
        /// </summary>
        [TaskAttribute("compareFile")]
        public string CompareFile {
            set {
                //I'm really not sure this is the best way to do this!
                FileSet fs = new FileSet();
                fs.Parent = this;
                fs.Project = this.Project;
                fs.Includes.Add(value);
                CompareFiles = fs;
            }
        }
        /// <summary>
        /// The FileSet that contains the comparison files for the uptodateFile.
        /// </summary>
        [FileSet("comparefiles")]
        public FileSet CompareFiles {
            set {_compareFiles = value;}
        }

        /// <summary>
        /// Used to test whether a property is true.
        /// </summary>
        [TaskAttribute("propertytrue")]
        public string PropertyNameTrue {
            set {_propNameTrue = value;}
        }

        /// <summary>
        /// Used to test whether a property exists.
        /// </summary>
        [TaskAttribute("propertyexists")]
        public string PropertyNameExists {
            set {_propNameExists = value;}
        }

        /// <summary>
        /// Used to test whether a target exists.
        /// </summary>
        [TaskAttribute("targetexists")]
        public string TargetNameExists {
            set {_targetName = value;}
        }

        protected override void ExecuteTask() {
            if(ConditionsTrue) {
                base.ExecuteTask();
            }
        }

        protected virtual bool ConditionsTrue {
            get {
                bool ret = true;

                //check for target
                if(_targetName != null) {
                    ret = ret && (Project.Targets.Find(_targetName) != null);
                    if (!ret) return false;
                }

                //Check for Property existence
                if(_propNameExists != null) {
                    ret = ret && (Properties[_propNameExists] != null);
                    if (!ret) return false;
                }

                //Check for the Property value of true.
                if(_propNameTrue != null) {
                    try {
                        ret = ret && bool.Parse(Properties[_propNameTrue]);
                        if (!ret) return false;
                    }
                    catch (Exception e) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, "Property True test failed for '{0}'", _propNameTrue), Location, e);
                    }
                }

                //check for uptodate file
                if(_uptodateFile != null) {
                    FileInfo primaryFile = new FileInfo(_uptodateFile);
                    if(primaryFile == null) {
                        ret = true;
                    }
                    else {
                        string newerFile = FileSet.FindMoreRecentLastWriteTime(_compareFiles.FileNames, primaryFile.LastWriteTime);
                        bool bNeedsAnUpdate = (null == newerFile);
                        Log.WriteLineIf(Project.Verbose && bNeedsAnUpdate, "{0) is newer than {1}" , newerFile, primaryFile.Name);
                        ret = !bNeedsAnUpdate;
                    }
                    if (!ret) return false;
                }

                return ret;
            }
        }
    }

    /// <summary>
    /// The opposite of the <c>if</c> task.
    /// </summary>
    /// <example>
    ///   <para>Check existence of a property</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot propertyexists="myProp">
    ///     <echo message="myProp does not exist."/>
    ///   </if>
    ///   ]]></code>
    ///   
    ///   <para>Check that a property value is not true</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot propertytrue="myProp">
    ///     <echo message="myProp is not true."/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    ///
    /// <example>
    ///   <para>Check that a target does not exist</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot targetexists="myTarget">
    ///     <echo message="myTarget does not exist."/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    [TaskName("ifnot")]
    public class IfNotTask : IfTask{
        protected override bool ConditionsTrue {
            get {
                return !base.ConditionsTrue;
            }
        }
    }

    /*
    /// <summary>
    /// Just like if, but makes sense inside an if task.
    /// </summary>
    /// <remarks>The contents of the and/or tasks are executed before the conditionals are checked.</remarks>
    [TaskName("and")]
    public class AndTask : IfTask{
        protected override void ExecuteTask() {
            //do nothing
        }

        protected override bool ConditionsTrue {
            get {
                base.ExecuteTask();
                return base.ConditionsTrue;
            }
        }
    }

    [TaskName("or")]
    public class OrTask : AndTask{
    }
    */
}
