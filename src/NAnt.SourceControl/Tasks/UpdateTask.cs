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
using System.IO;

using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Misc;
using ICSharpCode.SharpCvsLib.FileSystem;

using log4net;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    ///     <remarks>
    ///         <para>Updates the module in the specified working directory.</para>
    ///     </remarks>
    ///         
    ///     <example>
    ///         <para>Update nant.</para>
    ///             <code>&lt;cvs-update destination="c:\src\nant\" cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" password="" module="nant" /&gt;</code>
    ///         <para>Checkout your favorite build tool to the specified directory.</para>
    ///             <code>
    ///                 <![CDATA[
    ///                 <cvs-update destination="c:\src\nant\" cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" password="" module="nant"/>
    ///                 ]]>
    ///             </code>
    ///     </example>
    /// </summary>
    [TaskName("cvs-update")]
    public class UpdateTask : AbstractCvsTask {
        private readonly ILog LOGGER = LogManager.GetLogger (typeof (UpdateTask));
        /// <summary>
        /// 
        /// </summary>
        public UpdateTask() {
        }

        /// <summary>
        /// Creates an instance of the update command.
        /// </summary>
        /// <returns>An instance of the update command.</returns>
        protected override ICommand CreateCommand () {
            this.PopulateFolders (this.Working);
            return
                new UpdateCommand2 (this.Working);
        }

        /// <summary>
        /// Create a list of files that need to be compared
        ///     against the server and then updated.
        /// </summary>
        /// <param name="workingDirectory">The directory to
        ///     use in the comparison.
        /// </param>
        private void PopulateFolders (WorkingDirectory workingDirectory) {
            if (LOGGER.IsDebugEnabled) {
                String msg = "Reading all the working directory entries.  " +
                    "workingDirectory=[" + workingDirectory + "]";
                LOGGER.Debug (msg);
            }
            Manager manager = new Manager ();
            String updateAll = Path.Combine (this.Destination, this.Module);
            workingDirectory.FoldersToUpdate = 
                manager.FetchFilesToUpdate (updateAll);
        }
    }
}
