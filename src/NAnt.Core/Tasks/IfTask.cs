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

using System;
using System.Globalization;
using System.IO;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Checks the conditional attributes and executes the children if true.
    /// </summary>
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
    /// 
    /// <para>
    ///     Note: For dates you probably want to use <ifnot/>. 
    ///     That way you can say, if these files aren't uptodate, then do this. 
    ///     Because if they are uptodate, you probably don't want to do anything.
    /// </para>
    /// <example>
    ///   <para>Checks file dates. If myfile.dll is uptodate, then do stuff.</para>
    ///   <code>
    ///   <![CDATA[
    ///   <if uptodatefile="myfile.dll" comparefile="myfile.cs">
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs"/>
    ///   </if>
    ///   ]]></code>
    ///   or
    ///   <code>
    ///   <![CDATA[
    ///   <if uptodatefile="myfile.dll">
    ///     <comparefiles>
    ///         <includes name="*.cs"/>
    ///     </comparefiles>
    ///     <echo message="myfile.dll is newer/same-date as myfile.cs"/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    [TaskName("if")]
    public class IfTask : TaskContainer {
        #region Private Instance Fields

        private string _propNameTrue = null;
        private string _propNameExists = null;
        private string _targetName = null;
        private string _uptodateFile = null;
        private FileSet _compareFiles = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to compare if uptodate
        /// </summary>
        [TaskAttribute("uptodatefile")]
        public string PrimaryFile {
            get { return _uptodateFile; }
            set {_uptodateFile = Project.GetFullPath(value);}
        }

        /// <summary>
        /// The file to check against for the uptodate file.
        /// </summary>
        [TaskAttribute("comparefile")]
        public string CompareFile {
            set { 
                if(_compareFiles == null) {
                    _compareFiles = new FileSet();                    _compareFiles.Parent = this;                    _compareFiles.Project = this.Project;                }
                _compareFiles.Includes.Add(value); 

            }
        }

        /// <summary>
        /// The FileSet that contains the comparison files for the uptodateFile.
        /// </summary>
        [FileSet("comparefiles")]
        public FileSet CompareFiles {
            get { return _compareFiles; }
            set { _compareFiles = value; }
        } 

        /// <summary>
        /// Used to test whether a property is true.
        /// </summary>
        [TaskAttribute("propertytrue")]
        public string PropertyNameTrue {
            get { return _propNameTrue; }
            set { _propNameTrue = value; }
        }

        /// <summary>
        /// Used to test whether a property exists.
        /// </summary>
        [TaskAttribute("propertyexists")]
        public string PropertyNameExists {
            get { return _propNameExists;}
            set { _propNameExists = value; }
        }

        /// <summary>
        /// Used to test whether a target exists.
        /// </summary>
       [TaskAttribute("targetexists")]
        public string TargetNameExists {
            get { return _targetName; }
            set { _targetName = value; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

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
                        ret = false;
                    }
                    else {
                        string newerFile = FileSet.FindMoreRecentLastWriteTime(_compareFiles.FileNames, primaryFile.LastWriteTime);
                        bool bNeedsAnUpdate = !(null == newerFile);
                        Log.WriteLineIf(Project.Verbose && bNeedsAnUpdate, "{2}:{0) is newer than {1}. Excuting Embedded Tasks" , newerFile, primaryFile.Name, LogPrefix);
                        ret = ret && bNeedsAnUpdate;
                    }
                    if (!ret) return false;
                }

                return ret;
            }
        }

        #endregion Protected Instance Properties

        #region Override implementation of TaskContainer

        protected override void ExecuteTask() {
            if(ConditionsTrue) {
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of TaskContainer
    }

    /// <summary>
    /// The opposite of the <c>if</c> task.
    /// </summary>
    /// <example>
    ///   <para>Checks for the existence of a property</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot propertyexists="myProp">
    ///     <echo message="myProp does not exist."/>
    ///   </if>
    ///   ]]></code>
    ///   
    ///   <para>Checks that a property value is not true</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot propertytrue="myProp">
    ///     <echo message="myProp is not true."/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    ///
    /// <example>
    ///   <para>Checks that a target does not exist</para>
    ///   <code>
    ///   <![CDATA[
    ///   <ifnot targetexists="myTarget">
    ///     <echo message="myTarget does not exist."/>
    ///   </if>
    ///   ]]></code>
    /// </example>
    [TaskName("ifnot")]
    public class IfNotTask : IfTask {
        #region Override implementation of IfTask

        protected override bool ConditionsTrue {
            get { return !base.ConditionsTrue; }
        }

        #endregion Override implementation of IfTask
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
