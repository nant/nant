// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Ian MacLean (ian_maclean@another.com)

using System;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks.NUnit.Formatters {
    /// <summary>
    /// The built-in formatter types.
    /// </summary>
    public enum FormatterType {
        Plain,
        Xml,
        Custom
    }
    
    /// <summary>
    /// Represents the FormatterElement of the NUnit task.
    /// </summary>
    [ElementName("formatter")]
    public class FormatterElement : Element {
        #region Private Instance Fields

        private FormatterData _data = new FormatterData();

        #endregion Private Instance Fields

        #region Public Instance Properties
        
        /// <summary>Type of formatter ( means we will load a class of the form (type)Formatter</summary>
        [TaskAttribute("type", Required=false)]
        public FormatterType Type { get { return _data.Type; } set { _data.Type = value; } }
                         
        /// <summary>Name of a custom formatter class.</summary> 
        [TaskAttribute("classname", Required=false)]
        public string ClassName { get { return _data.ClassName; } set { _data.ClassName = value ;} }

        /// <summary>Extension to append to the output filename..</summary> 
        [TaskAttribute("extension", Required=false)]
        public string Extension { get { return _data.Extension; } set { _data.Extension = value ;} }
        
        /// <summary>Boolean that determines whether output should be sent to a file.</summary> 
        [TaskAttribute("usefile")]
        [BooleanValidator()]
        public bool UseFile { get { return _data.UseFile; } set { _data.UseFile = value; } }

        public FormatterData Data {
            get { return _data; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element

        // Custom validation here
        protected override void InitializeElement(XmlNode elementNode) {
            if ((Type != FormatterType.Custom ) && ClassName != null) {
                throw new BuildException("Specify either type or classname - not both.", Location);
            }
            if (ClassName != null && Extension == null) {
                throw new BuildException("If using classname the file extension must be specified.", Location);
            }
            if (Type == FormatterType.Xml){
                Extension = ".xml";
            } else if (Type == FormatterType.Plain || Extension == null) {
                Extension = ".txt";
            }
        }

        #endregion Override implementation of Element
    }
}
