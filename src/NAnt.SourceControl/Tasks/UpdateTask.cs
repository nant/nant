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
        #region Private Static Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTask" /> 
        /// class.
        /// </summary>
        public UpdateTask() {
        }

        #endregion Public Instance Constructors

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// Creates an instance of the update command.
        /// </summary>
        /// <returns>
        /// An instance of the update command.
        /// </returns>
        protected override ICommand CreateCommand () {
            this.PopulateFolders (this.WorkingDirectory);
            return new UpdateCommand2(this.WorkingDirectory);
        }

        #endregion Override implementation of AbstractCvsTask

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
