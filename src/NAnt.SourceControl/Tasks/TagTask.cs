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

using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Types;

using ICSharpCode.SharpCvsLib.Util;

namespace NAnt.SourceControl.Tasks
{
    /// <summary>
    /// Tags all local sources with the specified tag.  
    /// </summary>
    /// <remarks>
    /// <para>
    /// This differs from the
    /// <see cref="RTagTask"/> in that it acts on references to the cvs files
    /// contained in your local filesystem.  As such the sticky tags and local
    /// revisions can be considered in commits.  It also allows you to verify that
    /// all local files have been checked in before a tag is performed.    
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Tag NAnt sources remotely.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-tag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     module="nant"
    ///     tagname="v0_8_4"
    ///      />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Remove a tag from the remote repository.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-tag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     module="nant"
    ///     tagname="v0_8_4"
    ///     remove="true"
    ///     fail-if-modified="true"
    ///      />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-tag")]
    public class TagTask : AbstractCvsTask {
		private const string COMMAND_NAME = "tag";
		/// <summary>
		/// The name of the cvs command that is going to be executed.
		/// </summary>
		public override string CommandName {
			get {return COMMAND_NAME;}
		}


        #region Private Static Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

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
            get {return ((Option)this.CommandOptions["tag"]).Value;}
            set {this.SetCommandOption("tag", String.Format("{0}", value), true);}
        }

        /// <summary>
        /// Indicates whether the tag specified in <see cref="Tag" /> should
        /// be removed or not. 
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the specified tag should be removed; 
        /// otherwise, <see langword="false" />.  The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("remove", Required=false)]
		[BooleanValidator()]
        public bool Remove {
            get {return ((Option)this.CommandOptions["remove"]).IfDefined;}
            set {this.SetCommandOption("remove", "-d", value);}
        }

        /// <summary>
        /// Indicates whether the tag specified in <see cref="Tag" /> should
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
            get {return ((Option)this.CommandOptions["move-if-exists"]).IfDefined;}
            set {this.SetCommandOption("move-if-exists", "-F", value);}
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
            get {return ((Option)this.CommandOptions["recursive"]).IfDefined;}
            set {
                this.SetCommandOption("recursive", "-R", value);
                this.SetCommandOption("local-only", "-l", !value);
            }
        }

        /// <summary>
        /// Indicates the repository <see cref="Tag" /> that is acted on
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
            get {return ((Option)this.CommandOptions["act-on-tag"]).Value;}
            set {this.SetCommandOption("act-on-tag", String.Format("-r {0}", value), true);}
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
            get {return Convert.ToDateTime(((Option)this.CommandOptions["act-on-date"]).Value);}
            set {this.SetCommandOption("act-on-date", String.Format("-D {0}", DateParser.GetCvsDateString(value)), true);}
        }

        /// <summary>
        /// Indicates whether the head revision should be used if the revision specified by
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
            get {return ((Option)this.CommandOptions["force-head"]).IfDefined;}
            set {this.SetCommandOption("force-head", "-f", value);}
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
        [TaskAttribute("fail-if-modified", Required=false)]
		[BooleanValidator()]
        public bool FailIfModified {
            get {return ((Option)this.CommandOptions["fail-if-modified"]).IfDefined;}
            set {this.SetCommandOption("fail-if-modified", "-c", value);}
        }

        #endregion Public Instance Properties

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TagTask" /> 
        /// class.
        /// </summary>
        public TagTask() {
        }

        #endregion Public Instance Constructors

    }
}
