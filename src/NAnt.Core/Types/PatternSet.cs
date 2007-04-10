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

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Types {
    /// <summary>
    /// Named collection of include/exclude tags.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Patterns can be grouped to sets, and later be referenced by their
    ///   <see cref="DataTypeBase.ID" />.
    ///   </para>
    ///   <para>
    ///   The individual patterns support if and unless attributes to specify
    ///   that the element should only be used if a given condition is met
    ///   and/or that it should not be used unless a given condition is met.
    ///   </para>
    ///   <para>
    ///   The includesfile and excludesfile elements load patterns from a file.
    ///   When the file is a relative path, then it will be resolved relative
    ///   to the project base directory in which the patternset is defined.
    ///   Each line of this file is taken to be a pattern.
    ///   </para>
    ///   <para>
    ///   When used as a standalone element (global type), any properties that
    ///   are referenced will be resolved when the definition is processed, not
    ///   when it actually used. Passing a reference to a nested build file 
    ///   will not cause the properties to be re-evaluated. To improve reuse of
    ///   globally defined patternsets, avoid referencing any properties.
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
    ///             <patternset refis="cs.sources" />
    ///             <patternset refis="vb.sources" />
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
    [ElementName("patternset")]
    public class PatternSet : DataTypeBase {
        #region Public Instance Constructor

        public PatternSet() {
            _include = new PatternCollection();
            _exclude = new PatternCollection();
            _includesFile = new PatternCollection();
            _excludesFile = new PatternCollection();
        }

        #endregion Public Instance Constructor

        #region Public Instance Properties

        [BuildElementArrayAttribute("include")]
        public PatternCollection Include {
            get { return _include; }
        }

        [BuildElementArrayAttribute("includesfile")]
        public PatternCollection IncludesFile {
            get { return _includesFile; }
        }

        [BuildElementArrayAttribute("exclude")]
        public PatternCollection Exclude {
            get { return _exclude; }
        }

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
                    patterns.Add(line);
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
