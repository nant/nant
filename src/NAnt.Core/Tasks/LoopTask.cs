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

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Loops over a set of items.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Can loop over files in directory, lines in a file, etc.
    ///   </para>
    ///   <para>
    ///   The property value is stored before the loop is done, and restored 
    ///   when the loop is finished.
    ///   </para>
    ///   <para>
    ///   The property is returned to its normal value once it is used. Read-only 
    ///   parameters cannot be overridden in this loop.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Loops over the files in <c>c:\</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="File" in="c:\" property="filename">
    ///     <echo message="${filename}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Loops over all files in the project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="File" property="filename">
    ///     <in>
    ///         <items>
    ///             <include name="**" />
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${filename}" />
    ///     </do>
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Loops over the folders in <c>c:\</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" in="c:\" property="foldername">
    ///     <echo message="${foldername}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Loops over all folders in the project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" property="foldername">
    ///     <in>
    ///         <items>
    ///             <include name="**" />
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${foldername}" />
    ///     </do>
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Loops over a list.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="String" in="1 2,3" delim=" ," property="count">
    ///     <echo message="${count}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Loops over lines in the file <c>properties.csv</c>, where each line 
    ///   is of the format name,value.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Line" in="properties.csv" delim="," property="x,y">
    ///     <echo message="Read pair ${x}=${y}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("foreach")]
    public class LoopTask : TaskContainer {
        /// <summary>
        /// Possible types of iteration that can be done.
        /// </summary>
        public enum LoopItem {
            /// <summary>
            /// Loop over files in a <see cref="FileSet"/>.
            /// </summary>
            File = 1,
            /// <summary>
            /// Loop over folders in a <see cref="DirSet"/>
            /// </summary>
            Folder = 2,
            /// <summary>
            /// Loop over the items of a string.
            /// </summary>
            String = 3,
            /// <summary>
            /// Loop over files of a line.
            /// </summary>
            Line = 4
        }

        /// <summary>
        /// Specifies the trimming of items.
        /// </summary>
        public enum LoopTrim {
            /// <summary>
            /// Do not remove any white space characters.
            /// </summary>
            None = 0,

            /// <summary>
            /// Remove all white space characters from the end of the current
            /// item.
            /// </summary>
            End = 1,

            /// <summary>
            /// Remove all white space characters from the beginning of the 
            /// current item.
            /// </summary>
            Start = 2,

            /// <summary>
            /// Remove all white space characters from the beginning and end of
            /// the current item.
            /// </summary>
            Both = 3
        }

        #region Private Instance Fields

        private string _prop;
        private string[] _props;
        private LoopItem _loopItem;
        private LoopTrim _loopTrim = LoopTrim.None;
        private string _inAttribute;
        private string _delim;
        private InElement _inElement;
        private TaskContainer _doStuff;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The NAnt property name(s) that should be used for the current 
        /// iterated item.
        /// </summary>
        /// <remarks>
        /// If specifying multiple properties, separate them with a comma.
        /// </remarks>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Property {
            get { return _prop; }
            set {
                _prop = value;
                _props = _prop.Split(',');
                foreach (string prop in _props) {
                    if (Properties.IsReadOnlyProperty(prop)) {
                        throw new BuildException("Property is readonly! :" + prop, Location); 
                    }
                }
            }
        }

        /// <summary>
        /// The type of iteration that should be done.
        /// </summary>
        [TaskAttribute("item", Required=true)]
        public LoopItem ItemType {
            get { return _loopItem; }
            set { _loopItem = value; }
        }

        /// <summary>
        /// The type of whitespace trimming that should be done. The default 
        /// is <see cref="LoopTrim.None" />.
        /// </summary>
        [TaskAttribute("trim")]
        public LoopTrim TrimType {
            get { return _loopTrim;}
            set { _loopTrim = value; }
        }

        /// <summary>
        /// The source of the iteration.
        /// </summary>
        [TaskAttribute("in", Required=false)]
        public string Source {
            get { return _inAttribute;}
            set { _inAttribute = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The deliminator char.
        /// </summary>
        [TaskAttribute("delim")]
        public string Delimiter {
            get { return _delim; }
            set { 
                if (value == null || value.Length == 0) {
                    _delim = null;
                } else {
                    _delim = value; 
                }
            }
        }

        /// <summary>
        /// Stuff to operate in. Just like the <see cref="LoopTask.Source" /> 
        /// attribute, but supports more complicated things like a <see cref="FileSet" /> 
        /// and such.
        /// <note>
        /// Please remove the <see cref="LoopTask.Source" /> attribute if you 
        /// are using this element.
        /// </note>
        /// </summary>
        [BuildElement("in")]
        public InElement InElement {
            get { return _inElement; }
            set { _inElement = value; }
        }

        /// <summary>
        /// Tasks to execute for each matching item.
        /// </summary>
        [BuildElement("do")]
        public TaskContainer StuffToDo {
            get { return _doStuff; }
            set { _doStuff = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of TaskContainer

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">
        /// <list type="bullet">
        /// <item>
        /// <description>If no input data is present.</description>
        /// </item>
        /// <item>
        /// <description>If <see cref="LoopItem.File"/> is used but and the directory of the input file doesn't exist.</description>
        /// </item>
        /// <item>
        /// <description>If <see cref="LoopItem.File"/> is used and more than one property item is set.</description>
        /// </item>
        /// <item>
        /// <description>If <see cref="LoopItem.File"/> is used and no do-block was found.</description>
        /// </item>
        /// </list>
        /// </exception>
        protected override void ExecuteTask() {
            string[] oldPropVals = new string[_props.Length];
            // Save all of the old property values
            for (int nIndex = 0; nIndex < oldPropVals.Length; nIndex++) {
                oldPropVals[nIndex] = Properties[_props[nIndex]];
            }
            
            try {
                switch (ItemType) {
                    case LoopItem.File:
                        if (String.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (!String.IsNullOrEmpty(Source)) {
                            // resolve to full path
                            Source = Project.GetFullPath(Source);
                            // ensure directory exists
                            if (!Directory.Exists(Source)) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA1134"), 
                                    Source), Location);
                            }
                        
                            if (_props.Length != 1) {
                                throw new BuildException(@"Only one property is valid for item=""File""", Location);
                            }
                        
                            DirectoryInfo dirInfo = new DirectoryInfo(Source);
                            FileInfo[] files = dirInfo.GetFiles();
                        
                            foreach (FileInfo file in files) {
                                DoWork(file.FullName);
                            }
                        } else {
                            if (StuffToDo == null) {
                                throw new BuildException("Must use <do> with <in>.", Location);
                            }

                            foreach (string file in InElement.Items.FileNames) {
                                DoWork(file);
                            }
                        }
                        break;
                    case LoopItem.Folder:
                        if (String.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (_props.Length != 1) {
                            throw new BuildException(@"Only one property is valid for item=""Folder""", Location);
                        }

                        if (!String.IsNullOrEmpty(Source)) {
                            // resolve to full path
                            Source = Project.GetFullPath(Source);
                            // ensure directory exists
                            if (!Directory.Exists(Source)) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA1134"), 
                                    Source), Location);
                            }

                            DirectoryInfo dirInfo = new DirectoryInfo(Source);
                            DirectoryInfo[] dirs = dirInfo.GetDirectories();
                            foreach (DirectoryInfo dir in dirs) {
                                DoWork(dir.FullName);
                            } 
                        } else {
                            if (StuffToDo == null) {
                                throw new BuildException("Must use <do> with <in>.", Location);
                            }

                            foreach (string dir in InElement.Items.DirectoryNames) {
                                DoWork(dir);
                            }
                        }
                        break;
                    case LoopItem.Line:
                        if (String.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (_props.Length > 1 && Delimiter == null) {
                            throw new BuildException("Delimiter(s) must be specified if multiple properties are specified", Location);
                        }

                        if (!String.IsNullOrEmpty(Source)) {
                            // resolve to full path
                            Source = Project.GetFullPath(Source);
                            // ensure file exists
                            if (!File.Exists(Source)) {
                                throw new BuildException(string.Format(
                                    CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA1133"), 
                                    Source), Location);
                            }

                            DoWorkOnFileLines(Source);
                        } else {
                            if (StuffToDo == null) {
                                throw new BuildException("Must use <do> with <in>.", Location);
                            }

                            foreach (string file in InElement.Items.FileNames) {
                                DoWorkOnFileLines(file);
                            }
                        }
                        break;
                    case LoopItem.String:
                        if (String.IsNullOrEmpty(Source)) {
                            return;
                        }

                        if (_props.Length > 1) {
                            throw new BuildException(@"Only one property may be specified for item=""String""", Location);
                        }

                        if (Delimiter == null) {
                            throw new BuildException(@"Delimiter must be specified for item=""String""", Location);
                        }

                        string[] items = Source.Split(Delimiter.ToCharArray());
                        foreach (string s in items) {
                            DoWork(s);
                        }
                        break;
                }
            } finally {
                // Restore all of the old property values. Make sure that the loop property
                // (or any other property) is not re-added back into the PropertyDictionary
                // with a `null` value.
                string name = null;
                string val = null;
                for (int nIndex = 0; nIndex < oldPropVals.Length; nIndex++) 
                {
                    name = _props[nIndex];
                    val = oldPropVals[nIndex];
                    if (val != null) Properties[name] = val;
                }
            }
        }

        /// <summary>
        /// Creates and executes the embedded (child XML nodes) elements.
        /// </summary>
        /// <remarks>
        /// Skips any element defined by the host <see cref="Task" /> that has
        /// a <see cref="BuildElementAttribute" /> defined.
        /// </remarks>
        protected override void ExecuteChildTasks() {
            if (StuffToDo == null) {
                base.ExecuteChildTasks();
            } else {
                StuffToDo.Execute();
            }
        }

        #endregion Override implementation of TaskContainer

        #region Protected Instance Methods

        protected virtual void DoWork(params string[] propVals) {
            for (int nIndex = 0; nIndex < propVals.Length; nIndex++) {
                string propValue = propVals[nIndex];
                if (nIndex >= _props.Length) {
                    throw new BuildException("Too many items on line", Location);
                }
                switch (TrimType) {
                    case LoopTrim.Both:
                        propValue = propValue.Trim();
                        break;
                    case LoopTrim.Start:
                        propValue = propValue.TrimStart();
                        break;
                    case LoopTrim.End:
                        propValue = propValue.TrimEnd();
                        break;
                }
                Properties[_props[nIndex]] = propValue;
            }
            base.ExecuteTask();
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private void DoWorkOnFileLines(string filename) {
            using (StreamReader sr = File.OpenText(filename)) {
                while (true) {
                    string line = sr.ReadLine();
                    if (line == null) {
                        break;
                    }
                    if (Delimiter == null) {
                        DoWork(line);
                    } else {
                        DoWork(line.Split(Delimiter.ToCharArray()));
                    }
                }
            }
        }

        #endregion Private Instance Methods
    }

    /// <summary>
    /// Class which contains nested elements which are used in the loop.
    /// </summary>
    public class InElement : Element {
        #region Private Instance Fields

        private FileSet _items;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [BuildElement("items")]
        public FileSet Items {
            get { return _items;}
            set { _items = value; }
        }

        #endregion Public Instance Properties
    }
}
