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
    ///   <para>Loops over all files in the project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="File" property="filename">
    ///     <in>
    ///         <items>
    ///             <includes name="**" />
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${filename}" />
    ///     </do>
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over the folders in <c>c:\</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" in="c:\" property="foldername">
    ///     <echo message="${foldername}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over all folders in the project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" property="foldername">
    ///     <in>
    ///         <items>
    ///             <includes name="**" />
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${foldername}" />
    ///     </do>
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over a list.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="String" in="1 2,3" delim=" ," property="count">
    ///     <echo message="${count}" />
    /// </foreach>
    ///     ]]>
    ///   </code>
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
        public enum LoopItem {
            None,
            File,
            Folder,
            String,
            Line
        }

        public enum LoopTrim {
            None,
            End,
            Start,
            Both
        }

        #region Private Instance Fields

        private string _prop = null;
        private string[] _props = null;
        private LoopItem _loopItem = LoopItem.None;
        private LoopTrim _loopTrim = LoopTrim.None;
        private string _inAttribute = null;
        private string _delim = null;
        private InElement _inElement = null;
        private TaskContainer _doStuff = null;

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
        /// The type of iteration that should be done - either <see cref="LoopItem.File" />,
        /// <see cref="LoopItem.Folder" />, <see cref="LoopItem.String" /> or
        /// <see cref="LoopItem.Line" />.
        /// </summary>
        [TaskAttribute("item", Required=true)]
        public LoopItem ItemType {
            get { return _loopItem;}
            set { _loopItem = value; }
        }

        /// <summary>
        /// The type of whitespace trimming that should be done - either
        /// <see cref="LoopTrim.None" />, <see cref="LoopTrim.End" />,
        /// <see cref="LoopTrim.Start" /> or <see cref="LoopTrim.Both" />.
        /// The default is <see cref="LoopTrim.None" />.
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
        /// Stuff to operate in. Just like the in attribute, but supports more 
        /// complicated things like filesets and such.
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

        protected override void ExecuteTask() {
            string[] oldPropVals = new string[_props.Length];
            // Save all of the old property values
            for (int nIndex = 0; nIndex < oldPropVals.Length; nIndex++) {
                oldPropVals[nIndex] = Properties[_props[nIndex]];
            }
            
            try {
                switch (ItemType) {
                    case LoopItem.None:
                        throw new BuildException("Invalid itemtype", Location);
                    case LoopItem.File:
                        if (StringUtils.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (!StringUtils.IsNullOrEmpty(Source)) {
                            if (!Directory.Exists(Project.GetFullPath(Source))) {
                                throw new BuildException("Invalid Source: " + Source, Location);
                            }
                        
                            if (_props.Length != 1) {
                                throw new BuildException(@"Only one property is valid for item=""File""", Location);
                            }
                        
                            DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(_inAttribute));
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
                        if (StringUtils.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (_props.Length != 1) {
                            throw new BuildException(@"Only one property is valid for item=""Folder""", Location);
                        }

                        if (!StringUtils.IsNullOrEmpty(Source)) {
                            if (!Directory.Exists(Project.GetFullPath(Source))) {
                                throw new BuildException("Invalid Source: " + Source, Location);
                            }

                            DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(Source));
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
                        if (StringUtils.IsNullOrEmpty(Source) && InElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!", "in"));
                        }

                        if (_props.Length > 1 && Delimiter == null) {
                            throw new BuildException("Delimiter(s) must be specified if multiple properties are specified", Location);
                        }

                        if (!StringUtils.IsNullOrEmpty(Source)) {
                            if (!StringUtils.IsNullOrEmpty(Source) && !File.Exists(Project.GetFullPath(Source))) {
                                throw new BuildException("Source '" + Source + "' does not exist.", Location);
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
                        if (StringUtils.IsNullOrEmpty(Source)) {
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
                // Restore all of the old property values
                for (int nIndex = 0; nIndex < oldPropVals.Length; nIndex++) {
                    Properties[_props[nIndex]] = oldPropVals[nIndex];
                }
            }
        }

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

    public class InElement : Element {
        #region Private Instance Fields

        private FileSet _items = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        [FileSet("items")]
        public FileSet Items {
            get { return _items;}
            set { _items = value; }
        }

        #endregion Public Instance Properties
    }
}
