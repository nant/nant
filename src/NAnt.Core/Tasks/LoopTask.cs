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

using SourceForge.NAnt.Attributes;
using SourceForge.NAnt.Types;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Loops over a set of items.
    /// </summary>
    /// <remarks>
    ///   <para>Loop over items in a set. Can loop over files in directory, lines in a file, etc.</para>
    ///   <para>The Property value is stored before the loop is done, and restored when the loop is finished. 
    ///   The Property is returned to its normal value once it is used.  Read-only parameters cannot be overridden in this loop.</para>
    /// </remarks>
    /// <example>
    ///   <para>Loops over the files in C:\</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="File" in="c:\" property="filename">
    ///     <echo message="${filename}"/>
    /// </foreach>    
    ///     ]]>
    ///   </code>
    ///   <para>Loops over all files in C:\</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="File" property="filename">
    ///     <in>
    ///         <items>
    ///             <includes name="**"/>
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${filename}"/>
    ///     </do>
    /// </foreach>    
    ///     ]]>
    ///   </code>
    ///   <para>Loops over the folders in C:\</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" in="c:\" property="foldername">
    ///     <echo message="${foldername}"/>
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over all folders in C:\</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" property="foldername">
    ///     <in>
    ///         <items>
    ///             <includes name="**"/>
    ///         </items>
    ///     </in>
    ///     <do>
    ///         <echo message="${foldername}"/>
    ///     </do>
    /// </foreach>    
    ///     ]]>
    ///   </code>
    ///   <para>Loops over a list</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="String" in="1 2,3" delim=" ," property="count">
    ///     <echo message="${count}"/>
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over lines in the file "properties.csv", where each line is of the format name,value.</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Line" in="properties.csv" delim="," property="x,y">
    ///     <echo message="Read pair ${x}=${y}"/>
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

        string _prop = null;
        string[] _props = null;
        LoopItem _loopItem = LoopItem.None;
        LoopTrim _loopTrim = LoopTrim.None;
        string _inAttribute = null;
        string _delim = null;
        InElement _inElement = null;
        TaskContainer _doStuff = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>The NAnt propperty name(s) that should be used for the current iterated item.</summary>
        /// <remarks>If specifying multiple properties, separate them with a comma.</remarks>
        [TaskAttribute("property", Required=true)]
        public string Property { 
            get { return _prop; } 
            set {
                _prop = value;
                _props = _prop.Split(',');
                foreach (string prop in _props) {
                    if(Properties.IsReadOnlyProperty(prop)) {
                        throw new BuildException("Property is readonly! :" + prop, Location); 
                    }
                }
            }
        }

        /// <summary>
        /// The type of iteration that should be done.
        /// </summary>
        [TaskAttribute("item", Required=true)]
        public LoopItem ItemType   { get { return _loopItem;} set { _loopItem = value; }}

        /// <summary>
        /// The type of whitespace trimming that should be done.
        /// </summary>
        [TaskAttribute("trim")]
        public LoopTrim TrimType   { get { return _loopTrim;} set { _loopTrim = value; }}

        /// <summary>
        /// The source of the iteration.
        /// </summary>
        [TaskAttribute("in", Required=false)]
        public string Source   { get { return _inAttribute;} set { _inAttribute = value; }}

        /// <summary>
        /// The deliminator char.
        /// </summary>
        [TaskAttribute("delim")]
        public string Delimiter { get { return _delim;} set { _delim = value; }}

        /// <summary>
        /// Stuff to operate in. Just like the in attribute, but support more complicated things like filesets and such.
        /// </summary>
        [BuildElement("in")]
        public InElement InElement { get { return _inElement; } set { _inElement = value; }}

        /// <summary>
        /// Stuff to operate in. Just like the in attribute, but support more complicated things like filesets and such.
        /// </summary>
        [BuildElement("do")]
        public TaskContainer StuffToDo { get { return _doStuff; } set { _doStuff = value; }}

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
                        if (_inAttribute == null && _inElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!","in"));
                        }

                        if(_inAttribute != null) {
                            if(!Directory.Exists(Project.GetFullPath(_inAttribute))) {
                                throw new BuildException("Invalid Source: " + _inAttribute, Location);
                            }
                        
                            if(_props.Length != 1) {
                                throw new BuildException(@"Only one property is valid for item=""File""");
                            }
                        
                            DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(_inAttribute));
                            FileInfo[] files = dirInfo.GetFiles();
                        
                            foreach (FileInfo file in files) {
                                DoWork(file.FullName);
                            }
                        } else {
                            if(_doStuff == null) {
                                throw new BuildException("Must use <do> with <in>.",Location );
                            }

                            foreach (string file in _inElement.Items.FileNames) {
                                DoWork(file);
                            }
                        }
                        break;
                    case LoopItem.Folder:
                        if (_inAttribute == null && _inElement == null) {
                            throw new BuildException("Invalid foreach", Location, new ArgumentException("Nothing to work with...!","in"));
                        }

                        if (_inAttribute != null) {
                            if(!Directory.Exists(Project.GetFullPath(_inAttribute))) {
                                throw new BuildException("Invalid Source: " + _inAttribute, Location);
                            }
                            if(_props.Length != 1) {
                                throw new BuildException(@"Only one property is valid for item=""Folder""");
                            }
                            DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(_inAttribute));
                            DirectoryInfo[] dirs = dirInfo.GetDirectories();
                            foreach (DirectoryInfo dir in dirs) {
                                DoWork(dir.FullName);
                            } 
                        } else {
                            if (_doStuff == null) {
                                throw new BuildException("Must use <do> with <in>.", Location);
                            }

                            foreach (string dir in _inElement.Items.DirectoryNames) {
                                DoWork(dir);
                            }
                        }
                        break;
                    case LoopItem.Line:
                        if(!File.Exists(Project.GetFullPath(_inAttribute))) {
                            throw new BuildException("Invalid Source: " + _inAttribute, Location);
                        }
                        if(_props.Length > 1 && ( Delimiter == null || Delimiter.Length == 0)) {
                            throw new BuildException("Delimiter(s) must be specified if multiple properties are specified");
                        }

                        StreamReader sr = File.OpenText(Project.GetFullPath(_inAttribute));
                        while(true) {
                            string line = sr.ReadLine();
                            if (line ==null)
                                break;
                            if (Delimiter == null || Delimiter.Length == 0)
                                DoWork(line);
                            else
                                DoWork(line.Split(Delimiter.ToCharArray()));
                        }
                        sr.Close();
                        break;
                    case LoopItem.String:
                        if(_props.Length > 1) {
                            throw new BuildException(@"Only one property may be specified for item=""String""");
                        }
                        if(Delimiter == null || Delimiter.Length == 0) {
                            throw new BuildException(@"Delimiter must be specified for item=""String""");
                        }
                        string[] items = _inAttribute.Split(Delimiter.ToCharArray());
                        foreach (string s in items)
                            DoWork(s);
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
            if (_doStuff == null) {
                base.ExecuteChildTasks();
            } else {
                _doStuff.Execute();
            }
        }

        #endregion Override implementation of TaskContainer

        #region Protected Instance Methods

        protected virtual void DoWork(params string[] propVals) {
            for (int nIndex = 0; nIndex < propVals.Length; nIndex++) {
                string propValue = propVals[nIndex];
                if (nIndex >= _props.Length) {
                    throw new BuildException("Too many items on line");
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
    }

    public class InElement : Element {
        #region Private Instance Fields

        FileSet _items = null;

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
