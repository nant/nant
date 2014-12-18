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
    /// Updates a CVS module in a local working directory.
    /// </summary>
    /// <example>
    ///   <para>Update nant.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-update 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     password="" 
    ///     module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Update your NAnt revision named <c>your_favorite_revision_here</c> in 
    ///   the folder <c>c:\src\nant\replacement_for_module_directory_name</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-update 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     module="nant"
    ///     revision="your_favorite_revision_here"
    ///     overridedir="replacement_for_module_directory_name"
    ///     usesharpcvslib="false">
    ///     <fileset>
    ///         <include name="build.number"/>
    ///     </fileset>
    /// </cvs-update>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-update")]
    public class UpdateTask : AbstractCvsTask {
        #region Internal Static Fields

        /// <summary>
        /// The command being executed.
        /// </summary>
        internal const string CvsCommandName = "update";

        #endregion Internal Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTask" /> 
        /// class.
        /// </summary>
        /// <remarks>
        /// Sets the build directory and prune empty directory properties to
        /// <see langword="true" />.
        /// </remarks>
        public UpdateTask() {
            BuildDirs = true;
            PruneEmpty = true;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// If <see langword="true" />. new directories will be created on the local
        /// sandbox. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("builddirs", Required=false)]
        [BooleanValidator()]
        public bool BuildDirs {
            get { return ((Option)CommandOptions["builddirs"]).IfDefined; }
            set { SetCommandOption("builddirs", "-d", value); }
        }

        /// <summary>
        /// If <see langword="true" /> empty directories copied down from the 
        /// remote repository will be removed from the local sandbox.
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("pruneempty", Required=false)]
        [BooleanValidator()]
        public bool PruneEmpty {
            get { return ((Option)CommandOptions["pruneempty"]).IfDefined; }
            set { SetCommandOption("pruneempty", "-P", value); }
        }

        /// <summary>
        /// If <see langword="true" /> the local copy of the file will be 
        /// overwritten with the copy from the remote repository. The default
        /// is <see langword="false" />.
        /// </summary>
        [TaskAttribute("overwritelocal", Required=false)]
        [BooleanValidator()]
        public bool OverwriteLocal {
            get { return ((Option)CommandOptions["overwritelocal"]).IfDefined; }
            set { SetCommandOption("overwritelocal", "-C", value); }
        }

        /// <summary>
        /// Specifies if the command should be executed recursively. The 
        /// default is <see langword="true" />.
        /// </summary>
        /// <remarks>
        /// The <c>-R</c> option is on by default in cvs.
        /// </remarks>
        [TaskAttribute("recursive", Required=false)]
        [BooleanValidator()]
        public bool Recursive {
            get { 
                Option option = (Option) CommandOptions["recursive"];
                if (option == null || option.Value == "-R") {
                    return true;
                }
                return false;
            }
            set {
                if (value) {
                    // update should be executed recursive
                    SetCommandOption("recursive", "-R", true);
                } else {
                    // update should be executed locally (not recursive)
                    SetCommandOption("recursive", "-l", true);
                }

            }
        }

        /// <summary>
        /// Specify the revision to update the file to.  This corresponds to the 
        /// "sticky-tag" of the file.
        /// </summary>
        [TaskAttribute("revision", Required=false)]
        [StringValidator(AllowEmpty=true, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string Revision {
            get {
                if (CommandOptions.ContainsKey("revision")) {
                    return ((Option)CommandOptions["revision"]).Value;
                }
                return null;
            }
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
        /// Sticky tag or revision to update the local file to.
        /// </summary>
        /// <value>
        /// A valid cvs tag.
        /// </value>
        [TaskAttribute("sticky-tag", Required=false)]
        public string StickyTag {
            get { return Revision; }
            set { Revision = value; }
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
            set { SetCommandOption("date", String.Format(CultureInfo.InvariantCulture,"-D \"{0}\"", ToCvsDateTimeString(value)), true); }
        }

        #endregion

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// Specify if the module is needed for this cvs command.  It is
        /// only needed if there is no module information on the local file
        /// system.
        /// </summary>
        protected override bool IsModuleNeeded {
            get {return false;}
        }

        /// <summary>
        /// The name of the cvs command that is going to be executed.
        /// </summary>
        public override string CommandName {
            get {return CvsCommandName;}
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
