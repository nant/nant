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
    /// </remarks>
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
