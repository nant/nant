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
// Arjen Poutsma (poutsma@yahoo.com)
// Sascha Andres (sa@programmers-world.com)
// John Cole     (john.cole@uai.com)

using System;
using System.Globalization;
using System.Text.RegularExpressions;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets project properties based on the evaluatuion of a regular expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Pattern" /> attribute must contain one or more 
    /// <see href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpcongroupingconstructs.asp">
    /// named grouping constructs</see>, which represents the names of the 
    /// properties to be set. These named grouping constructs can be enclosed 
    /// by angle brackets (?&lt;name&gt;) or single quotes (?'name').
    /// </para>
    /// <note>
    /// In the build file, use the XML element <![CDATA[&lt;]]> to specify &lt;, 
    /// and <![CDATA[&gt;]]> to specify &gt;.
    /// </note>
    /// <note>
    /// The named grouping construct must not contain any punctuation and it 
    /// cannot begin with a number.
    /// </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Find the last word in the given string and stores it in the property 
    ///   <c>lastword</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regex pattern="(?'lastword'\w+)$" input="This is a test sentence" />
    /// <echo message="${lastword}" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Split the full filename and extension of a filename.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regex pattern="^(?'filename'.*)\.(?'extension'\w+)$" input="d:\Temp\SomeDir\SomeDir\bla.xml" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Split the path and the filename. (This checks for <c>/</c> or <c>\</c> 
    ///   as the path separator).
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regex pattern="^(?'path'.*(\\|/)|(/|\\))(?'file'.*)$" input="d:\Temp\SomeDir\SomeDir\bla.xml" />
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Results in path=<c>d:\Temp\SomeDir\SomeDir\</c> and file=<c>bla.xml</c>.
    ///   </para>
    /// </example>
    [TaskName("regex")]
    public class RegexTask : Task {
        #region Private Instance Fields

        private string _pattern;
        private string _input;
        private RegexOptions _options = RegexOptions.None;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Represents the regular expression to be evalued.
        /// </summary>
        /// <value>
        /// The regular expression to be evalued.
        /// </value>
        /// <remarks>
        /// The pattern must contain one or more named constructs, which may 
        /// not contain any punctuation and cannot begin with a number.
        /// </remarks>
        [TaskAttribute("pattern", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Pattern {
            get { return _pattern;}
            set { _pattern = value; }
        }

        /// <summary>
        /// A comma separated list of options to pass to the regex engine. The
        /// default is <see cref="RegexOptions.None" />.
        /// </summary>
        [TaskAttribute("options", Required=false)]
        [StringValidator(AllowEmpty=true)]
        public RegexOptions Options {
            get { return _options;}
            set { _options = value; }
        }

        /// <summary>
        /// Represents the input for the regular expression.
        /// </summary>
        /// <value>
        /// The input for the regular expression.
        /// </value>
        [TaskAttribute("input", Required=true)]
        [StringValidator(AllowEmpty=true)]
        public string Input {
            get { return _input;}
            set { _input = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            Regex regex = null;

            try {
                regex = new Regex(Pattern, Options);
            } catch (ArgumentException ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                                       ResourceUtils.GetString("NA1145"), Pattern),
                    Location, ex);
            }

            Match match = regex.Match(Input);

            if (match == Match.Empty) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1144"), Pattern, 
                    Input), Location);
            }

            // we start the iteration at 1, since the collection of groups 
            // always starts with a group which matches the entire input and 
            // is named '0', this group is of no interest to us
            for (int i = 1; i < match.Groups.Count; i++) {
                string groupName = regex.GroupNameFromNumber(i);

                Log(Level.Verbose, "Setting property '{0}' to '{1}'.", 
                    groupName, match.Groups[groupName].Value);
                Properties[groupName] = match.Groups[groupName].Value;
            }
        }

        #endregion Override implementation of Task
    }
}
