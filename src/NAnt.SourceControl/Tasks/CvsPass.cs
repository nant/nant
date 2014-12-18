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
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Executes the cvs login command which appends or updates an entry to the
    /// specified .cvspass file.
    /// </summary>
    /// <example>
    ///   <para>Update .cvspass file to include the NAnt anonymous login.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-pass cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///      password="anonymous"
    ///      passfile="C:\.cvspass" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-pass")]
    public class CvsPass : Task {
        #region Private Instance Fields

        private string _password;
        private FileInfo _passFile;
        private string _cvsRoot;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Password to append or update to the .cvspass file.
        /// </summary>
        [TaskAttribute("password", Required=true)]
        public string Password {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// The full path to the .cvspass file.  The default is ~/.cvspass.
        /// </summary>
        /// <value></value>
        [TaskAttribute("passfile", Required=false)]
        public FileInfo PassFile {
            get { return this._passFile; }
            set { this._passFile = value; }
        }

        /// <summary>
        /// The current working directory.
        /// </summary>
        public virtual DirectoryInfo DestinationDirectory {
            get { return new DirectoryInfo(Environment.CurrentDirectory); }
        }

        /// <summary>
        /// The repository root string.
        /// </summary>
        [TaskAttribute("cvsroot", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string Root {
            get { return this._cvsRoot; }
            set { this._cvsRoot = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Ensures all information is available to execute the <see cref="Task" />.
        /// </summary>
        protected override void Initialize() {
            // ensure passfile was either set by user or could be discovered from
            // environment or located in HOME directory
            if (PassFile == null) {
                throw new BuildException("'passfile' was not explicitly specified"
                    + " and could not be determined from environment, or found in"
                    + " home directory.", Location);
            }
        }

        /// <summary>
        /// Update the .cvspass file with the given password.  If the passfile
        /// is not specified then the default search locations are used:
        /// <list type="list">
        ///     <item>CVS_PASSFILE/.cvspass</item>
        ///     <item>HOME/.cvspass</item>
        ///     <item>USERPROFILE/.cvspass  TODO: Confirm that this is valid
        ///         behavior or if it is going to give problems with the
        ///         cvsnt implementation.</item>
        /// </list>
        /// </summary>
        protected override void ExecuteTask () {
            ICSharpCode.SharpCvsLib.FileSystem.Manager manager = 
                new ICSharpCode.SharpCvsLib.FileSystem.Manager(this.DestinationDirectory);

            if (this.PassFile == null) {
                manager.UpdatePassFile(this.Password,
                    new ICSharpCode.SharpCvsLib.Misc.CvsRoot(this.Root));
            } else {
                Log(Level.Verbose, "Updating .cvspass file '{0}'.", this.PassFile.FullName);
                manager.UpdatePassFile(this.Password, 
                    new ICSharpCode.SharpCvsLib.Misc.CvsRoot(this.Root), this.PassFile);
            }
        }

        #endregion Override implementation of Task
    }
}
