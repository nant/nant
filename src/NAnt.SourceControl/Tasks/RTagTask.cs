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

using ICSharpCode.SharpCvsLib.Commands;

namespace NAnt.SourceControl.Tasks
{
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
    ///     password="" 
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
		private const string COMMAND_NAME = "checkout";
		/// <summary>
		/// The name of the cvs command that is going to be executed.
		/// </summary>
		public override string CommandName {
			get {return COMMAND_NAME;}
		}


        #region Private Instance Fields
        
        private String _tagName;
        private bool _remove = false;

        #endregion Private Instance Fields

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
        [TaskAttribute("tagname", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string TagName {
            get { return _tagName; }
            set { _tagName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Indicates whether the tag specified in <see cref="TagName" /> should
        /// be removed or not. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the specified tag should be removed; 
        /// otherwise, <see langword="false" />.  The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("remove", Required=false)]
        public bool Remove {
            get { return _remove; }
            set { _remove = value; }
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

    }
}
