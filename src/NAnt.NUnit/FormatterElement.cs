// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Ian MacLean (ian_maclean@another.com)

using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.NUnit.Types {
    /// <summary>
    /// The built-in formatter types.
    /// </summary>
    public enum FormatterType {
        /// <summary>
        /// A plaintext formatter.
        /// </summary>
        Plain,

        /// <summary>
        /// An XML formatter.
        /// </summary>
        Xml
    }
    
    /// <summary>
    /// Represents the FormatterElement of the NUnit task.
    /// </summary>
    [ElementName("formatter")]
    public class FormatterElement : NAnt.Core.Element {
        #region Private Instance Fields

        private FormatterData _data = new FormatterData();

        #endregion Private Instance Fields

        #region Public Instance Properties
        
        /// <summary>
        /// Type of formatter.
        /// </summary>
        [TaskAttribute("type", Required=true)]
        public FormatterType Type {
            get { return _data.Type; }
            set { _data.Type = value; }
        }

        /// <summary>
        /// Extension to append to the output filename.
        /// </summary> 
        [TaskAttribute("extension", Required=false)]
        public string Extension {
            get { return StringUtils.ConvertNullToEmpty(_data.Extension); }
            set { _data.Extension = value; }
        }
        
        /// <summary>
        /// Determines whether output should be persisted to a file. The default 
        /// is <see langword="false" />.
        /// </summary> 
        [TaskAttribute("usefile", Required=false)]
        [BooleanValidator()]
        public bool UseFile {
            get { return _data.UseFile; }
            set { _data.UseFile = value; }
        }

        /// <summary>
        /// Specifies the directory where the output file should be written to,
        /// if <see cref="UseFile" /> is <see langword="true" />.  If not 
        /// specified, the output file will be written to the directory where
        /// the test module is located.
        /// </summary> 
        [TaskAttribute("outputdir", Required=false)]
        public DirectoryInfo OutputDirectory {
            get { return _data.OutputDirectory; }
            set { _data.OutputDirectory = value; }
        }

        /// <summary>
        /// Gets the underlying <see cref="FormatterData" /> for the element.
        /// </summary>
        public FormatterData Data {
            get { return _data; }
        }

        #endregion Public Instance Properties
    }
}
