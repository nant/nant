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

using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Misc;
using ICSharpCode.SharpCvsLib.FileSystem;

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
    ///     module="nant">
    ///     <options>
    ///         <option name="sticky-tag" value="your_favorite_revision_here" />
    ///         <option name="override-directory" value="replacement_for_module_directory_name" />
    ///     </options>
    /// </cvs-update>
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
    ///     module="nant">
    ///     <options>
    ///         <option name="-r" value="your_favorite_revision_here" />
    ///         <option name="-d" value="replacement_for_module_directory_name" />
    ///     </options>
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
		protected const bool DEFAULT_OVERWRITE_LOCAL = false;
		/// <summary>
		/// Default value for build directory directive.
		/// </summary>
		protected const bool DEFAULT_BUILD_DIRS = true;
		/// <summary>
		/// Default value for prune empty directories directive.
		/// </summary>
		protected const bool DEFAULT_PRUNE_EMPTY = true;
		#endregion

		#region Public Instance Properties
		/// <summary>
		/// The name of the cvs command that is going to be executed.
		/// </summary>
		public override string CommandName {
			get {return COMMAND_NAME;}
		}

		/// <summary>
		/// If <code>true</code> new directories will be created on the local
		///		sandbox.
		///		
		///	Defaults to <code>true</code>.
		/// </summary>
        [TaskAttribute("builddirs", Required=false)]
		[BooleanValidator()]
		public bool BuildDirs {
			get {return ((Option)this.CommandOptions["builddirs"]).IfDefined;}
			set {this.SetCommandOption("builddirs", "d", value);}
		}

		/// <summary>
		/// If <code>true</code> empty directories copied down from the remote
		///		repository will be removed from the local sandbox.
		///		
		///	Defaults to <code>true</code>.
		/// </summary>
		[TaskAttribute("pruneempty", Required=false)]
		[BooleanValidator()]
		public bool PruneEmpty {
			get {return ((Option)this.CommandOptions["pruneempty"]).IfDefined;}
			set {this.SetCommandOption("pruneempty", "P", value);}
		}

		/// <summary>
		/// If <code>true</code> the local copy of the file will be overwritten
		///		with the copy from the remote repository.  
		///		
		///	Defaults to <code>false</code>.
		/// </summary>
		[TaskAttribute("overwritelocal", Required=false)]
		[BooleanValidator()]
		public bool OverwriteLocal {
			get {return ((Option)this.CommandOptions["overwritelocal"]).IfDefined;}
			set {this.SetCommandOption("overwritelocal", "C", value);}
		}

		/// <summary>
		/// <code>true</code> if the command should be executed recursively.
		/// </summary>
		[TaskAttribute("recursive", Required=false)]
		[BooleanValidator()]
		public bool Recursive {
			get {return ((Option)this.CommandOptions["recursive"]).IfDefined;}
			set {this.SetCommandOption("recursive", "R", value);}
		}

		#endregion

        #region Private Static Fields

        private static readonly log4net.ILog Logger = 
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private const string COMMAND_NAME = "update";

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTask" /> 
        /// class.
        /// 
        /// Sets the build directory and prune empty directory properties to
        ///		true.
        /// </summary>
        public UpdateTask() {
			this.BuildDirs = true;
			this.PruneEmpty = true;
        }

        #endregion Public Instance Constructors

        #region Private Instance Methods

        /// <summary>
        /// Creates a list of files that need to be compared against the server 
        /// and updated if necessary.
        /// </summary>
        /// <param name="workingDirectory">The directory to use in the comparison.</param>
        private void PopulateFolders (WorkingDirectory workingDirectory) {
            Logger.Debug(string.Format(CultureInfo.InvariantCulture,
                "Reading all directory entries from working directory '{0}'.",
                workingDirectory.WorkingDirectoryName));

            Manager manager = new Manager(workingDirectory.WorkingPath);
            try {
                workingDirectory.FoldersToUpdate = manager.FetchFilesToUpdate(
                    workingDirectory.WorkingPath);
            } catch (CvsFileNotFoundException e) {
                System.Console.WriteLine(workingDirectory.WorkingPath);
                System.Console.WriteLine(e.ToString());
                throw e;
            }
        }

        #endregion Private Instance Methods
    }
}
