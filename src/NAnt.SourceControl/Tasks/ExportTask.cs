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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Exports a cvs module in preperation for a release (i.e. the CVS version
    /// folders are not exported).
    /// </summary>
    /// <example>
    ///   <para>Export the most recent NAnt sources from cvs.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-export 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant"  
    ///     module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Export NAnt revision named <c>your_favorite_revision_here</c> to the 
    ///   folder <c>c:\src\nant\replacement_for_module_directory_name</c>.
    ///   
    ///   <warn>**NOTE**</warn>: filesets names for the export task must be 
    ///   prefixed with the module name.  This is different than other tasks.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-export 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     module="nant"
    ///     revision="your_favorite_revision_here"
    ///     overridedir="replacement_for_module_directory_name"
    ///     recursive="false">
    ///     <fileset>
    ///         <include name="nant/bin/NAnt.exe"/>
    ///         <include name="nant/bin/NAnt.exe.config"/>
    ///     </fileset>
    /// </cvs-export>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-export")]
    public class ExportTask : AbstractCvsTask {
        #region Private Static Fields

        /// <summary>
        /// The command being executed.
        /// </summary>
        private const string CvsCommandName = "export";

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Create a new instance of the <see cref="ExportTask"/>.
        /// </summary>
        /// <value>
        /// The following values are set by default:
        ///     <ul>
        ///         <li>Recursive: <see langword="true" /></li>
        ///     </ul>
        /// </value>
        public ExportTask() {
            Recursive = true;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// No shortening.  Do not shorten module paths if -d specified.
        /// </summary>
        [TaskAttribute("no-shortening", Required=false)]
        [BooleanValidator()]
        public bool NoShortening {
            get { return ((Option)CommandOptions["no-shortening"]).IfDefined; }
            set { SetCommandOption("no-shortening", "-N", value); }
        }

        /// <summary>
        /// Indicates whether the head revision should be used if the revison specified by
        /// <see cref="Revision"/> or the <see cref="Date"/> tags are not
        /// found. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the specified tag should be moved; 
        /// otherwise, <see langword="false" />.  The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("force-head", Required=false)]
        [BooleanValidator()]
        public bool ForceHead {
            get { return ((Option)CommandOptions["force-head"]).IfDefined; }
            set { SetCommandOption("force-head", "-f", value); }
        }

        /// <summary>
        /// If a directory is specified indicates whether sub-directories should
        /// also be processed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the sub-directories should be tagged;
        /// otherwise, <see langword="false" />.  The default is <see langword="true" />.
        /// </value>
        [TaskAttribute("recursive", Required=false)]
        [BooleanValidator()]
        public bool Recursive {
            get { return ((Option)CommandOptions["recursive"]).IfDefined; }
            set {
                SetCommandOption("recursive", "-R", value);
                SetCommandOption("local-only", "-l", !value);
            }
        }

        /// <summary>
        /// Specify the revision to update the file to.  This corresponds to the "sticky-tag"
        /// of the file.
        /// </summary>
        [TaskAttribute("revision", Required=false)]
        [StringValidator(AllowEmpty=true, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string Revision {
            get {
                if (null == CommandOptions["revision"]) {
                    return null;
                }
                return ((Option)CommandOptions["revision"]).Value;}
            set { 
                if (String.IsNullOrEmpty(value)) {
                    CommandOptions.Remove("revision");
                } else {
                    SetCommandOption("revision", string.Format(CultureInfo.InvariantCulture,
                        "-r {0}", value), true);
                }
            }
        }

        /// <summary>
        /// Specify the revision date to update to.  The version of the file that
        /// existed at the date specified is retrieved.
        /// </summary>
        /// <value>
        /// A valid date time value, which is then converted to a format that
        /// cvs can parse.
        /// </value>
        [TaskAttribute("date", Required=false)]
        [DateTimeValidator()]
        public DateTime Date {
            get { return Convert.ToDateTime(((Option)CommandOptions["date"]).Value); }
            set { SetCommandOption("date", String.Format(CultureInfo.InvariantCulture,"\"-D {0}\"", ToCvsDateTimeString(value)), true); }
        }

        /// <summary>
        /// Specify a directory name to replace the module name.  Valid names
        ///     include any valid filename, excluding path information.
        /// </summary>
        [TaskAttribute("overridedir", Required=false)]
        [StringValidator(AllowEmpty=false, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string OverrideDir {
            get { return ((Option)CommandOptions["overridedir"]).Value; }
            set { SetCommandOption("overridedir", String.Format(CultureInfo.InvariantCulture,"-d{0}", value), true); }
        }

        #endregion Public Instance Properties

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// The export command name for the cvs client.
        /// </summary>
        public override string CommandName {
            get { return CvsCommandName; }
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
