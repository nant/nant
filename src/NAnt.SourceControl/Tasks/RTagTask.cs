#region "Nant Copyright notice"
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
#endregion

using System;

using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

using ICSharpCode.SharpCvsLib.Commands;

namespace NAnt.SourceControl.Tasks
{
    /// <summary>
    /// <para>
    /// Tags all sources in the remote repository with the given tag.
    /// </para>
    /// <para>
    /// Unlike tag the rtag command acts only on sources that are in the repository.  
    ///     Any modified sources on the local file system will NOT be tagged with this
    ///     command, so a commit should be performed before an rtag is done.
    /// </para>
    /// <para>
    /// NOTE: Although a working directory is not necessary to perform the command one
    ///     must be specified in order to remain in compliance with the cvs library.
    /// </para>
    /// </summary>
    /// <example>
    ///   <para>Tag NAnt sources remotely.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-rtag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     password="" 
    ///     module="nant"
    ///     tagname="v0_8_4"
    ///      />
    ///     ]]>
    ///   </code>
    ///   <para>Remove a tag from the remote repository.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-rtag 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     destination="."
    ///     password="" 
    ///     module="nant"
    ///     tagname="v0_8_4"
    ///     remove="true"
    ///      />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-rtag")]
    public class RTagTask : AbstractCvsTask {
        #region Private Instance Fields
        
        private String _tagName;
        private bool _remove = false;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Destination directory for the checked out / updated files.
        /// </summary>
        /// <value>
        /// The destination directory for the checked out or updated files.
        /// </value>
        /// <example>
        /// <para>This is the current working directory that will be modifed.
        /// </para>
        /// </example>
        [TaskAttribute ("tagname", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string TagName {
            get { return _tagName; }
            set { _tagName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Indicates whether the tag specified in the TagName property should
        ///     be removed or not.  Defaults to <code>false</code>.
        /// </summary>
        /// <value>
        /// <code>true</code> if the tag specified should be removed, <code>false</code>
        ///     otherwise.  The default value for this property is <code>false</code>.
        /// </value>
        /// <example>
        /// <para>false</para>
        /// </example>
        [TaskAttribute ("remove", Required=false)]
        public bool Remove {
            get {return _remove;}
            set {this._remove = value;}
        }

        #endregion Public Instance Properties

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RTagTask" /> 
        /// class.
        /// </summary>
        public RTagTask() {
        }

        #endregion Public Instance Constructors

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// Creates an instance of the rtag command.
        /// </summary>
        /// <returns>An instance of the update command.</returns>
        protected override ICommand CreateCommand () {
            RTagCommand command = new RTagCommand(this.WorkingDirectory);
            command.TagName = this.TagName;
            command.DeleteTag = this.Remove;

            return command;
        }

        #endregion Override implementation of AbstractCvsTask

    }
}
