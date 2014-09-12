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
//
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core.Attributes;
using NAnt.Core.Filters;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Load a text file into a single property.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Unless an encoding is specified, the encoding associated with the 
    ///   system's current ANSI code page is used.
    ///   </para>
    ///   <para>
    ///   An UTF-8, little-endian Unicode, and big-endian Unicode encoded text 
    ///   file is automatically recognized, if the file starts with the appropriate 
    ///   byte order marks.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Load file <c>message.txt</c> into property "message".
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadfile
    ///     file="message.txt"
    ///     property="message" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Load a file using the "latin-1" encoding.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadfile
    ///     file="loadfile.xml"
    ///     property="encoded-file"
    ///     encoding="iso-8859-1" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Load a file, replacing all <c>@NOW@</c> tokens with the current 
    ///   date/time. 
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <loadfile file="token.txt" property="token-file">
    ///     <filterchain>
    ///         <replacetokens>
    ///             <token key="NOW" value="${datetime::now()}" />
    ///         </replacetokens>
    ///     </filterchain>
    /// </loadfile>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("loadfile")]
    public class LoadFileTask : Task {
        #region Private Instance Fields

        private FileInfo _file;
        private Encoding _encoding;
        private string _property;
        private FilterChain _filterChain;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The file to load.
        /// </summary>
        [TaskAttribute("file", Required=true)]
        public FileInfo File {
            get { return _file; }
            set { _file = value; }
        }

        /// <summary>
        /// The name of the property to save the content to.
        /// </summary>
        [TaskAttribute("property", Required=true)]
        public string Property {
            get { return _property; }
            set { _property = value; }
        }

        /// <summary>
        /// The encoding to use when loading the file. The default is the encoding
        /// associated with the system's current ANSI code page.
        /// </summary>
        [TaskAttribute("encoding")]
        public Encoding Encoding {
            get { return _encoding; }
            set { _encoding = value; }
        }

        /// <summary>
        /// The filterchain definition to use.
        /// </summary>
        [BuildElement("filterchain")]
        public FilterChain FilterChain {
            get { return _filterChain; }
            set { _filterChain = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">If the file to load doesn't exist.
        /// </exception>
        protected override void ExecuteTask() {
            // make sure file actually exists
            if (!File.Exists) {
                throw new BuildException(string.Format(CultureInfo.InstalledUICulture,
                    "File '{0}' does not exist.", File.FullName), Location);
            }

            string content = null;

            try {
                content = FileUtils.ReadFile(File.FullName, FilterChain,
                    Encoding);
            } catch (IOException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1129"), File.FullName),
                    Location, ex);
            }

            // add/update property
            Properties[Property] = content;
        }

        #endregion Override implementation of Task
    }
}
