// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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

using System.Collections;
using System.IO;
using System.Text;
using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// A set of patterns, mostly used to include or exclude certain files.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The individual patterns support <c>if</c> and <c>unless</c> attributes
    ///   to specify that the element should only be used if or unless a given
    ///   condition is met.
    ///   </para>
    ///   <para>
    ///   The <see cref="IncludesFile" /> and <see cref="ExcludesFile" />
    ///   elements load patterns from a file. When the file is a relative path,
    ///   it will be resolved relative to the project base directory in which
    ///   the patternset is defined. Each line of this file is taken to be a
    ///   pattern.
    ///   </para>
    ///   <para>
    ///   The number sign (#) as the first non-blank character in a line denotes
    ///   that all text following it is a comment:
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///        EventLog.cs
    ///        # requires Mono.Posix
    ///        SysLogEventLogImpl.cs
    ///        # uses the win32 eventlog API
    ///        Win32EventLogImpl.cs
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Patterns can be grouped to sets, and later be referenced by their
    ///   <see cref="DataTypeBase.ID" />.
    ///   </para>
    ///   <para>
    ///   When used as a standalone element (global type), any properties that
    ///   are referenced will be resolved when the definition is processed, not
    ///   when it actually used. Passing a reference to a nested build file 
    ///   will not cause the properties to be re-evaluated.
    ///   </para>
    ///   <para>
    ///   To improve reuse of globally defined patternsets, avoid referencing
    ///   any properties altogether.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Define a set of patterns that matches all .cs files that do not contain
    ///   the text <c>Test</c> in their name.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <patternset id="non.test.sources">
    ///             <include name="**/*.cs" />
    ///             <exclude name="**/*Test*" />
    ///         </patternset>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Define two sets. One holding C# sources, and one holding VB sources.
    ///   Both sets only include test sources when the <c>test</c> property is
    ///   set. A third set combines both C# and VB sources.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <patternset id="cs.sources">
    ///             <include name="src/**/*.cs" />
    ///             <include name="test/**/*.cs" if=${property::exist('test')}" />
    ///         </patternset>
    ///         
    ///         <patternset id="vb.sources">
    ///             <include name="src/**/*.vb" />
    ///             <include name="test/**/*.vb" if=${property::exist('test')}" />
    ///         </patternset>
    ///         
    ///         <patternset id="all.sources">
    ///             <patternset refid="cs.sources" />
    ///             <patternset refid="vb.sources" />
    ///         </patternset>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Define a set from patterns in a file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <patternset id="sources">
    ///             <includesfile name="test.sources" />
    ///             <includesfile name="non.test.sources" />
    ///         </patternset>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Defines a patternset with patterns that are loaded from an external
    ///   file, and shows the behavior when that patternset is passed as a
    ///   reference to a nested build script.
    ///   </para>
    ///   <para>
    ///   External file &quot;c:\foo\build\service.lst&quot; holding patterns
    ///   of source files to include for the Foo.Service assembly:
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         AssemblyInfo.cs
    ///         *Channel.cs
    ///         ServiceFactory.cs]]></code>
    ///   <para>
    ///   Main build script located in &quot;c:\foo\default.build&quot;:
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <project name="main" default="build">
    ///             <property name="build.debug" value="true" />
    ///         
    ///             <patternset id="service.sources">
    ///                 <include name="TraceListener.cs" if="${build.debug}" />
    ///                 <includesfile name="build/service.lst" />
    ///             </patternset>
    ///             
    ///             <property name="build.debug" value="false" />
    ///             
    ///             <target name="build">
    ///                 <nant buildfile="service/default.build" inheritrefs="true" />
    ///             </target>
    ///         </project>]]></code>
    ///   <para>
    ///   Nested build script located in &quot;c:\foo\services\default.build&quot;
    ///   which uses the patternset to feed sources files to the C# compiler:
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <project name="service" default="build">
    ///             <target name="build">
    ///                 <csc output="../bin/Foo.Service.dll" target="library">
    ///                     <fileset basedir="src">
    ///                         <patternset refid="service.sources" />
    ///                     </fileset>
    ///                 </csc>
    ///             </target>
    ///         </project>]]></code>
    ///   <para>
    ///   At the time when the patternset is used in the &quot;service&quot;
    ///   build script, the following source files in &quot;c:\foo\services\src&quot;
    ///   match the defined patterns:
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         AssemblyInfo.cs
    ///         MsmqChannel.cs
    ///         SmtpChannel.cs
    ///         ServiceFactory.cs
    ///         TraceListener.cs]]></code>
    ///   <para>
    ///   You should have observed that:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///         <description>
    ///         although the patternset is used from the &quot;service&quot;
    ///         build script, the path to the external file is resolved relative
    ///         to the base directory of the &quot;main&quot; build script in
    ///         which the patternset is defined.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         the &quot;TraceListener.cs&quot; file is included, even though 
    ///         the &quot;build.debug&quot; property was changed to <b>false</b>
    ///         after the patternset was defined (but before it was passed to
    ///         the nested build, and used).
    ///         </description>
    ///     </item>
    ///   </list>
    /// </example>
    /// <seealso cref="FileSet" />
    [ElementName("patternset")]
    public class PatternSet : DataTypeBase {
        #region Public Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternSet" /> class.
        /// </summary>
        public PatternSet() {
            _include = new PatternCollection();
            _exclude = new PatternCollection();
            _includesFile = new PatternCollection();
            _excludesFile = new PatternCollection();
        }

        #endregion Public Instance Constructor

        #region Public Instance Properties

        /// <summary>
        /// Defines a single pattern for files to include.
        /// </summary>
        [BuildElementArrayAttribute("include")]
        public PatternCollection Include {
            get { return _include; }
        }

        /// <summary>
        /// Loads multiple patterns of files to include from a given file, set
        /// using the <see cref="Pattern.PatternName" /> parameter.
        /// </summary>
        [BuildElementArrayAttribute("includesfile")]
        public PatternCollection IncludesFile {
            get { return _includesFile; }
        }

        /// <summary>
        /// Defines a single pattern for files to exclude.
        /// </summary>
        [BuildElementArrayAttribute("exclude")]
        public PatternCollection Exclude {
            get { return _exclude; }
        }

        /// <summary>
        /// Loads multiple patterns of files to exclude from a given file, set
        /// using the <see cref="Pattern.PatternName" /> parameter.
        /// </summary>
        [BuildElementArrayAttribute("excludesfile")]
        public PatternCollection ExcludesFile {
            get { return _excludesFile; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Adds a nested set of patterns, or references other standalone 
        /// patternset.
        /// </summary>
        /// <param name="patternSet">The <see cref="PatternSet" /> to add.</param>
        [BuildElement("patternset")]
        public void Append(PatternSet patternSet) {
            string[] includePatterns = patternSet.GetIncludePatterns();
            foreach (string includePattern in includePatterns) {
                _include.Add(new Pattern(Project, includePattern));
            }
            string[] excludePatterns = patternSet.GetExcludePatterns();
            foreach (string excludePattern in excludePatterns) {
                _exclude.Add(new Pattern(Project, excludePattern));
            }
        }

        public string[] GetIncludePatterns () {
            ArrayList includes = new ArrayList (Include.Count);
            foreach (Pattern include in Include) {
                if (!include.Enabled) {
                    continue;
                }
                includes.Add(include.PatternName);
            }
            foreach (Pattern includesfile in IncludesFile) {
                if (!includesfile.Enabled) {
                    continue;
                }
                string absoluteFile = Project.GetFullPath(includesfile.PatternName);
                if (!File.Exists (absoluteFile)) {
                    throw new BuildException ("Includesfile '" + absoluteFile
                        + "' not found.", Location);
                }
                ReadPatterns(absoluteFile, includes);
            }
            return (string[]) includes.ToArray(typeof(string));
        }

        public string[] GetExcludePatterns () {
            ArrayList excludes = new ArrayList (Exclude.Count);
            foreach (Pattern exclude in Exclude) {
                if (!exclude.Enabled) {
                    continue;
                }
                excludes.Add(exclude.PatternName);
            }
            foreach (Pattern excludesfile in ExcludesFile) {
                if (!excludesfile.Enabled) {
                    continue;
                }
                string absoluteFile = Project.GetFullPath(excludesfile.PatternName);
                if (!File.Exists (absoluteFile)) {
                    throw new BuildException ("Excludesfile '" + absoluteFile
                        + "' not found.", Location);
                }
                ReadPatterns(absoluteFile, excludes);
            }
            return (string[]) excludes.ToArray(typeof(string));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void ReadPatterns(string fileName, ArrayList patterns) {
            using (StreamReader sr = new StreamReader(fileName, Encoding.Default, true)) {
                string line = sr.ReadLine ();
                while (line != null) {
                    // remove leading and trailing whitespace
                    line = line.Trim ();
                    // only consider non-empty lines that are not comments
                    if (line.Length != 0 && line [0] != '#') {
                        // add line as pattern
                        patterns.Add(line);
                    }
                    // read next line
                    line = sr.ReadLine ();
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly PatternCollection _include;
        private readonly PatternCollection _exclude;
        private readonly PatternCollection _includesFile;
        private readonly PatternCollection _excludesFile;

        #endregion Private Instance Fields
   }
}
