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
using System.IO;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Loops over a set of Items</summary>
    /// <remarks>
    ///   <para>Loop over items in a set. Can loop over files in directory, lines in a file, etc.</para>
    ///   <para>The Property value is stored before the loop is done, and restored when the loop is finished. 
    ///   The Property is returned to its normal value once it is used.</para>
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
    ///   <para>Loops over the folders in C:\</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="Folder" in="c:\" property="foldername">
    ///     <echo message="${foldername}"/>
    /// </foreach>
    ///     ]]>
    ///   </code>
    ///   <para>Loops over a list</para>
    ///   <code>
    ///     <![CDATA[
    /// <foreach item="String" in="1 2 3" delim=" " property="count">
    ///     <echo message="${count}"/>
    /// </foreach>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("foreach")]
    public class LoopTask : TaskContainer {
        public enum ItemTypes {
            None,
            File,
            Folder,
            String,
            Line
        }
        string _prop = null;
        ItemTypes _itemType = ItemTypes.None;
        string _source = null;
        string _delim = " ";

        /// <summary>The NAnt propperty name that should be used for the current iterated item.</summary>
        [TaskAttribute("property", Required=true)]
        public string Property { 
            get { return _prop; } 
            set {
                _prop = value;
                if(Properties.IsReadOnlyProperty(_prop)) {
                    throw new BuildException("Property is readonly! :" + _prop, Location); 
                }
            }
        }

        /// <summary>
        /// The type of iteration that should be done.
        /// </summary>
        [TaskAttribute("item", Required=true)]
        public ItemTypes ItemType   { get { return _itemType;} set {_itemType = value; }}

        /// <summary>
        /// The source of the iteration.
        /// </summary>
        [TaskAttribute("in", Required=true)]
        public string Source   { get { return _source;} set {_source = value; }}

        /// <summary>
        /// The deliminator char.
        /// </summary>
        [TaskAttribute("delim")]
        public string Delimiter { get { return _delim;} set {_delim = value; }}

        protected override void ExecuteTask() {
            string oldPropVal = Properties[_prop];
            
            try {
                switch(ItemType) {
                    case ItemTypes.None:
                        throw new BuildException("Invalid itemtype", Location);
                    case ItemTypes.File: {
                        if(!Directory.Exists(Project.GetFullPath(_source)))
                            throw new BuildException("Invalid Source: " + _source, Location);
                        DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(_source));
                        FileInfo[] files = dirInfo.GetFiles();
                        foreach(FileInfo file in files) {
                            DoWork(file.FullName);
                        }
                        break;
                    }
                    case ItemTypes.Folder: {
                        if(!Directory.Exists(Project.GetFullPath(_source)))
                            throw new BuildException("Invalid Source: " + _source, Location);
                        DirectoryInfo dirInfo = new DirectoryInfo(Project.GetFullPath(_source));
                        DirectoryInfo[] dirs = dirInfo.GetDirectories();
                        foreach(DirectoryInfo dir in dirs) {
                            DoWork(dir.FullName);
                        }
                        break;
                    }
                    case ItemTypes.Line: {
                        if(!File.Exists(Project.GetFullPath(_source)))
                            throw new BuildException("Invalid Source: " + _source, Location);
                        StreamReader sr = File.OpenText(Project.GetFullPath(_source));
                        while(true) {
                            string line = sr.ReadLine();
                            if (line ==null)
                                break;
                            DoWork(line);
                        }
                        sr.Close();
                        break;
                    }
                    case ItemTypes.String: {
                        if(Delimiter != null && Delimiter.Length > 0) {
                            string[] items = _source.Split(Delimiter.ToCharArray()[0]);
                            foreach(string s in items)
                                DoWork(s);
                        }
                        else
                            throw new BuildException("Invalid delim: " + _delim, Location);
                        break;
                    }
                        
                }
            }
            finally {
                Properties[_prop] = oldPropVal;
            }

        }

        protected virtual void DoWork(string propVal) {
            Properties[_prop]= propVal;
            base.ExecuteTask();
        }
    }
}