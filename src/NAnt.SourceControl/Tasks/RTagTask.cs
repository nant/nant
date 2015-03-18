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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System;
using System.Globalization;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Tags all sources in the remote repository with a given tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike tag, the rtag command acts only on sources that are in the repository.  
    /// Any modified sources on the local file system will NOT be tagged with this
    /// command, so a commit should be performed before an rtag is done.
    /// </para>
    /// <para>
    /// NOTE: Although a working directory is not necessary to perform the command 
    /// one must be specified in order to remain in compliance with the cvs library.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Tag NAnt sources remotely.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-rtag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     tag="v0_8_4"
    ///      />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Remove a tag from the remote repository.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-rtag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     tag="v0_8_4"
    ///     remove="true"
    ///      />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-rtag")]
    public class RTagTask : AbstractCvsTask {
        #region Private Instance Fields

        private string _tag;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string CvsCommandName = "rtag";

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RTagTask" /> 
        /// class.
        /// </summary>
        public RTagTask() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the tag to assign or remove.
        /// </summary>
        /// <value>
        /// The name of the tag to assign or remove.
        /// </value>
        [TaskAttribute("tag", Required=true)]
        [StringValidator(AllowEmpty=false, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string Tag {
            get { return this._tag; }
            set { this._tag = value; }
        }

        /// <summary>
        /// Indicates whether the tag specified in <see cref="RTagTask.Tag" /> should
        /// be removed or not. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the specified tag should be removed; 
        /// otherwise, <see langword="false" />.  The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("remove", Required=false)]
        [BooleanValidator()]
        public bool Remove {
            get { return ((Option)CommandOptions["remove"]).IfDefined; }
            set { SetCommandOption("remove", "-d", value); }
        }

        /// <summary>
        /// Indicates whether the tag specified in <see cref="RTagTask.Tag" /> should
        /// be moved to the current file revision.  If the tag does not exist
        /// then it is created. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the specified tag should be moved; 
        /// otherwise, <see langword="false" />.  The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("move-if-exists", Required=false)]
        [BooleanValidator()]
        public bool MoveIfExists {
            get { return ((Option)CommandOptions["move-if-exists"]).IfDefined; }
            set { SetCommandOption("move-if-exists", "-F", value); }
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
        /// Indicates the repository <see cref="RTagTask.Tag" /> that is acted on
        /// for the tag command.  Note if <see cref="MoveIfExists"/> is 
        /// <see langword="true"/> then the tag specified is moved to the revision
        /// of the file on the HEAD of the branch specified.
        /// </summary>
        /// <value>
        /// The tag (or more likely) branch that should be used to apply the new tag.
        /// </value>
        [TaskAttribute("act-on-tag", Required=false)]
        [StringValidator(AllowEmpty=false, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string ActOnTag {
            get { return ((Option)CommandOptions["act-on-tag"]).Value; }
            set { SetCommandOption("act-on-tag", String.Format(CultureInfo.InvariantCulture,"-r {0}", value), true); }
        }

        /// <summary>
        /// Indicates the revision date of the file that the tag should be 
        /// applied to.
        /// </summary>
        /// <value>
        /// A valid date which specifies the revision point that the tag will
        /// be applied to.
        /// </value>
        [TaskAttribute("act-on-date", Required=false)]
        [DateTimeValidator()]
        public DateTime ActOnDate {
            get { return Convert.ToDateTime(((Option)CommandOptions["act-on-date"]).Value); }
            set { SetCommandOption("act-on-date", String.Format(CultureInfo.InvariantCulture,"-D {0}", ToCvsDateTimeString(value)), true); }
        }

        /// <summary>
        /// Indicates whether the head revision should be used if the 
        /// <see cref="ActOnTag"/> or the <see cref="ActOnDate"/> tags are not
        /// found. 
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

        #endregion Public Instance Properties

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// The name of the cvs command that is going to be executed.
        /// </summary>
        public override string CommandName {
            get {return CvsCommandName;}
        }

        /// <summary>
        /// Append the tag information to the commandline.
        /// </summary>
        protected override void AppendSubCommandArgs() {
            base.AppendSubCommandArgs ();
            if (this.Tag != null && this.Tag.Length > 0) {
                this.AddArg(this.Tag);
            }
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
