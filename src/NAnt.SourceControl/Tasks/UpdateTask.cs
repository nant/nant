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
using System.IO;


using ICSharpCode.SharpCvsLib.Util;

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
    ///     password="" 
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
        #region Protected Constant Properties
        /// <summary>
        /// Default value for the overwrite local directive.
        /// </summary>
        protected const bool DefaultOverwriteLocal = false;
        /// <summary>
        /// Default value for build directory directive.
        /// </summary>
        protected const bool DefaultBuildDirs = true;
        /// <summary>
        /// Default value for prune empty directories directive.
        /// </summary>
        protected const bool DefaultPruneEmpty = true;
        #endregion

        #region "Protected Instance Properties"
        /// <summary>
        /// Specify if the module is needed for this cvs command.  It is
        /// only needed if there is no module information on the local file
        /// system.
        /// </summary>
        protected override bool IsModuleNeeded {
            get {return false;}
        }

        /// <summary>
        /// Specify if the cvs root should be used for this cvs command.  It is
        /// only needed if there is no module information on the local file
        /// system, there fore is not needed for a cvs update.
        /// </summary>
        protected override bool IsCvsRootNeeded {
            get {return false;}
        }
        #endregion "Protected Instance Properties"

        #region Public Instance Properties
        /// <summary>
        /// The name of the cvs command that is going to be executed.
        /// </summary>
        public override string CommandName {
            get {return CvsCommandName;}
        }

        /// <summary>
        /// If <code>true</code> new directories will be created on the local
        ///     sandbox.
        ///     
        /// Defaults to <code>true</code>.
        /// </summary>
        [TaskAttribute("builddirs", Required=false)]
        [BooleanValidator()]
        public bool BuildDirs {
            get {return ((Option)CommandOptions["builddirs"]).IfDefined;}
            set {SetCommandOption("builddirs", "-d", value);}
        }

        /// <summary>
        /// If <code>true</code> empty directories copied down from the remote
        ///     repository will be removed from the local sandbox.
        ///     
        /// Defaults to <code>true</code>.
        /// </summary>
        [TaskAttribute("pruneempty", Required=false)]
        [BooleanValidator()]
        public bool PruneEmpty {
            get {return ((Option)CommandOptions["pruneempty"]).IfDefined;}
            set {SetCommandOption("pruneempty", "-P", value);}
        }

        /// <summary>
        /// If <code>true</code> the local copy of the file will be overwritten
        ///     with the copy from the remote repository.  
        ///     
        /// Defaults to <code>false</code>.
        /// </summary>
        [TaskAttribute("overwritelocal", Required=false)]
        [BooleanValidator()]
        public bool OverwriteLocal {
            get {return ((Option)CommandOptions["overwritelocal"]).IfDefined;}
            set {SetCommandOption("overwritelocal", "-C", value);}
        }

        /// <summary>
        /// <code>true</code> if the command should be executed recursively.
        /// </summary>
        [TaskAttribute("recursive", Required=false)]
        [BooleanValidator()]
        public bool Recursive {
            get {return ((Option)CommandOptions["recursive"]).IfDefined;}
            set {SetCommandOption("recursive", "-R", value);}
        }

        /// <summary>
        /// Specify the revision to update the file to.  This corresponds to the "sticky-tag"
        ///     of the file.
        /// </summary>
        [TaskAttribute("revision", Required=false)]
        [StringValidator(AllowEmpty=false, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string Revision {
            get {return ((Option)CommandOptions["revision"]).Value;}
            set {SetCommandOption("revision", String.Format(CultureInfo.InvariantCulture,"-r {0}", value), true);}
        }

        /// <summary>
        /// Sticky tag or revision to update the local file to.
        /// </summary>
        /// <value>
        /// A valid cvs tag.
        /// </value>
        [TaskAttribute("sticky-tag", Required=false)]
        public string StickyTag {
            get {return Revision;}
            set {Revision = value;}
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
            get {return Convert.ToDateTime(((Option)CommandOptions["date"]).Value);}
            set {SetCommandOption("date", String.Format(CultureInfo.InvariantCulture,"-D \"{0}\"", DateParser.GetCvsDateString(value)), true);}
        }

        #endregion

        #region Private Static Fields

        private static readonly log4net.ILog Logger = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Constants
        /// <summary>
        /// The command being executed.
        /// </summary>
        public const string CvsCommandName = "update";
        #endregion
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTask" /> 
        /// class.
        /// 
        /// Sets the build directory and prune empty directory properties to
        ///     true.
        /// </summary>
        public UpdateTask() {
            BuildDirs = true;
            PruneEmpty = true;
        }

        #endregion Public Instance Constructors

    }
}
