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

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

using ICSharpCode.SharpCvsLib;
using ICSharpCode.SharpCvsLib.Client;
using ICSharpCode.SharpCvsLib.Requests;
using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Misc;

using log4net;

namespace NAnt.SourceControl.Tasks {
    /// <summary>Performs cvs client commands on a cvs repository.</summary>
    /// <remarks>
    ///   <para>Checks out the specified module to the required directory.</para>
    ///   <para>Takes a password parameter as an attribute.</para>
    /// </remarks>
    ///         
    /// <example>
    ///   <para>Checkout nant.</para>
    ///   <code>&lt;cvs destination="c:\src\nant\" cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" password="" module="nant" /&gt;</code>
    ///   <para>Checkout your favorite build tool to the specified directory.</para>
    ///   <code>
    /// <![CDATA[
    /// <cvs destination="c:\src\nant\" cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" password="" module="nant"/>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs")]
    public abstract class AbstractCvsTask : Task {
        private readonly ILog LOGGER = 
            LogManager.GetLogger (typeof (AbstractCvsTask));

        private String _cvsroot;
        private String _module;
        private String _destination;
        private String _password;

        private CvsRoot _root;
        private WorkingDirectory _working;
        private CVSServerConnection _connection;
        private ICommand _command;

        /// <summary>
        /// Cvsroot Variable.
        /// </summary>
        [TaskAttribute("cvsroot")]
        public String Cvsroot {
            get {return this._cvsroot;}
            set {this._cvsroot = value;}
        }

        /// <summary>
        /// The module to check out.
        /// </summary>
        [TaskAttribute ("module")]
        public String Module {
            get {return this._module;}
            set {this._module = value;}
        }

        /// <summary>
        /// Destination directory for the checked out/ updated files.
        /// </summary>
        [TaskAttribute ("destination")]
        public String Destination {
            get {return this._destination;}
            set {this._destination = value;}
        }

        /// <summary>
        /// Set the password for logging in.  
        /// </summary>
        [TaskAttribute ("password")]
        public String Password {
            get {return this._password;}
            set {this._password = value;}
        }

        /// <summary>
        /// The Cvsroot of the repository.
        /// </summary>
        protected CvsRoot Root 
        {
            get {return this._root;}
            set {this._root = value;}
        }

        /// <summary>
        /// Working Directory to place checked out source in.
        /// </summary>
        protected WorkingDirectory Working {
            get {return this._working;}
            set {this._working = value;}
        }

        /// <summary>
        /// Connection object that is used to connect to the repository.
        /// </summary>
        protected CVSServerConnection Connection {
            get {return this._connection;}
            set {this._connection = value;}
        }

        /// <summary>
        /// Command to execute, command must implement
        ///     <see cref="ICSharpCode.SharpCvsLib.Commands.ICommand"/>
        /// </summary>
        protected ICommand Command {
            get {return this._command;}
            set {this._command = value;}
        }

        /// <summary>
        /// Create a new abstract cvs task.
        /// </summary>
        public AbstractCvsTask () {
        }

        /// <summary>
        /// Exectute the cvs command specified.
        /// </summary>
        protected override void ExecuteTask () {
            if (!Directory.Exists (this.Destination)) {
                if (LOGGER.IsDebugEnabled) {
                    String msg =
                        "Creating directory=[" + this.Destination + "]";
                    LOGGER.Debug (msg);
                }
                Directory.CreateDirectory (this.Destination);
            }

            this.Root = new CvsRoot (this.Cvsroot);
            this.Working = 
                new WorkingDirectory (this.Root, 
                                        this.Destination, 
                                        this.Module);

            this.Connection = new CVSServerConnection ();
            this.Command = this.CreateCommand ();

            this.validate ();

            if (LOGGER.IsDebugEnabled) {
                String msg = "Before trying to get a connection.";
                LOGGER.Debug (msg);
            }
            this.Connection.Connect (this.Working, this.Password);

            if (LOGGER.IsDebugEnabled) {
                String msg = "After trying to get a connection.";
                LOGGER.Debug (msg);
            }

            this.Command.Execute (this.Connection);
            this.Connection.Close ();
        }

        /// <summary>
        /// Validates that the required object are not null.
        /// 
        /// <throws>NullReferenceException if any of the required
        ///     fields is null.</throws>
        /// </summary>
        private void validate () {
            if (null == this.Cvsroot || 
                null == this.Working ||
                null == this.Connection ||
                null == this.Command) {
                    String msg = 
                        "Cvsroot, working directory, connection and command cannot be null.";
                    throw new NullReferenceException (msg);
            } 
            if (LOGGER.IsDebugEnabled) {
                String msg = "In validate.  " +
                    ";  Cvsroot=[" + this.Working.CvsRoot + "]" +
                    ";  Local directory=[" + this.Working.LocalDirectory + "]" + 
                    ";  Working directory name=[" + this.Working.WorkingDirectoryName + "]" +
                    "Command=[" + this.Command + "]";
                LOGGER.Warn (msg);
                System.Console.WriteLine (msg);
            }
        }

        /// <summary>
        /// Creates the cvs command object to execute against the 
        ///     specified repository.
        /// </summary>
        /// <returns>Command to execute against the repository.</returns>
        protected abstract ICommand CreateCommand ();

    }
}