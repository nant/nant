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
using System.Text;
using System.IO;
using System.Diagnostics;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using ICSharpCode.SharpCvsLib.Commands;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Executes the cvs login command which appends or updates an entry to the
    /// specified .cvspass file.
    /// </summary>
    /// <example>
    ///   <para>Update .cvspass file to include the NAnt anonymous login.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///      password="anonymous"
    ///      passfile="C:\.cvspass" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-pass")]
    public class CvsPass : AbstractCvsTask {

        #region Private Instance Fields
        private static readonly log4net.ILog Logger = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private FileInfo _passwordfile;
        private string _password;
        #endregion

        #region Public Instance Properties

        /// <summary>
        /// The cvs command to execute.
        /// </summary>
        public override string CommandName {
            get {return "login";}
        }

        /// <summary>
        /// Password to append or update to the .cvspass file.
        /// </summary>
        [TaskAttribute("password", Required=true)]
        public new string Password {
            get { return this._password; }
            set { this._password = value; }
        }

        /// <summary>
        /// Full path to the .cvspass file.  
        /// </summary>
        /// <value>Defaults to ~/.cvspass.</value>
        [TaskAttribute("passfile", Required=false)]
        public FileInfo Passwordfile {
            get { return this._passwordfile; }
            set { this._passwordfile = value; }
        }

        /// <summary>
        /// Specify if the module is needed for this cvs command.  
        /// </summary>
        protected override bool IsModuleNeeded {
            get { return false; }
        }

        /// <summary>
        /// Update the .cvspass file with the given password.
        /// </summary>
        protected override void ExecuteTask () {
            ICSharpCode.SharpCvsLib.FileSystem.Manager manager = 
                new ICSharpCode.SharpCvsLib.FileSystem.Manager(this.DestinationDirectory);

            Log(Level.Verbose, "Updating .cvspass file '{0}'.", this.Passwordfile.FullName);
            manager.UpdatePassFile(this.Password, 
                new ICSharpCode.SharpCvsLib.Misc.CvsRoot(this.Root));
        }

        #endregion
    }
}
