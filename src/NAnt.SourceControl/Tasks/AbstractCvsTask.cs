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

using ICSharpCode.SharpCvsLib.Client;
using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Misc;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// A base class for creating tasks for executing CVS client commands on a 
    /// CVS repository.
    /// </summary>
    public abstract class AbstractCvsTask : Task {
        #region Private Instance Fields

        private string _cvsRoot;
        private string _module;
        private string _destination;
        private string _password;
        private CvsRoot _root;
        private WorkingDirectory _workingDirectory;
        private CVSServerConnection _connection;
        private ICommand _command;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCvsTask" /> 
        /// class.
        /// </summary>
        protected AbstractCvsTask () {
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Cvsroot Variable.
        /// </summary>
        [TaskAttribute("cvsroot", Required=true)]
        public string CvsRoot {
            get {return this._cvsRoot;}
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _cvsRoot = value;
                } else {
                    _cvsRoot = null;
                }
            }
        }

        /// <summary>
        /// The module to perform an operation on.
        /// </summary>
        /// <value>The module to perform an operation on.</value>
        [TaskAttribute("module", Required=true)]
        public string Module {
            get {return _module;}
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _module = value;
                } else {
                    _module = null;
                }
            }
        }

        /// <summary>
        /// Destination directory for the checked out / updated files.
        /// </summary>
        /// <value>
        /// The destination directory for the checked out or updated files.
        /// </value>
        [TaskAttribute ("destination", Required=true)]
        public string Destination {
            get { return (_destination != null) ? Project.GetFullPath(_destination) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _destination = value;
                } else {
                    _destination = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the password for logging in to the CVS repository.
        /// </summary>
        /// <value>
        /// The password for logging in to the CVS repository.
        /// </value>
        [TaskAttribute ("password")]
        public string Password {
            get { return _password;}
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _password = value;
                } else {
                    _password = null;
                }
            }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets or sets the root of the CVS repository.
        /// </summary>
        /// <value>The root of the CVS repository.</value>
        protected CvsRoot Root {
            get { return this._root; }
            set { this._root = value; }
        }

        /// <summary>
        /// Gets or sets the directory where checked out sources are placed.
        /// </summary>
        /// <value>the directory where checked out sources are placed.</value>
        protected WorkingDirectory WorkingDirectory {
            get { return this._workingDirectory; }
            set { this._workingDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the connection used to connect to the CVS repository.
        /// </summary>
        /// <value>The connection used to connect to the CVS repository.</value>
        protected CVSServerConnection Connection {
            get { return this._connection; }
            set { this._connection = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ICommand" /> to execute.
        /// </summary>
        /// <value>The <see cref="ICommand" /> to execute.</value>
        protected ICommand Command {
            get { return this._command; }
            set { this._command = value; }
        }

        #endregion Protected Instance Properties

        #region Protected Instance Methods

        /// <summary>
        /// Executes the CVS command.
        /// </summary>
        protected override void ExecuteTask () {
            if (!Directory.Exists(this.Destination)) {
                Logger.Debug("Creating directory=[" + this.Destination + "]");
                Directory.CreateDirectory(this.Destination);
            }

            this.Root = new CvsRoot(this.CvsRoot);
            this.WorkingDirectory = new WorkingDirectory(this.Root, 
                this.Destination, this.Module);
            this.Connection = new CVSServerConnection();
            this.Command = this.CreateCommand();

            this.Validate ();

            Logger.Debug("Before trying to get a connection.");
            this.Connection.Connect(this.WorkingDirectory, this.Password);
            Logger.Debug("After trying to get a connection.");

            this.Command.Execute(this.Connection);
            this.Connection.Close();
        }

        /// <summary>
        /// Creates the CVS command object to execute against the specified 
        /// CVS repository.
        /// </summary>
        /// <returns>
        /// The <see cref="ICommand" /> to execute against the CVS repository.
        /// </returns>
        protected abstract ICommand CreateCommand ();

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Validates that all required information is available.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <para><see cref="CvsRoot" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="WorkingDirectory" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="Connection" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="Command" /> is <see langword="null" /></para>
        /// </exception>
        private void Validate() {
            if (null == this.CvsRoot || null == this.WorkingDirectory || 
                null == this.Connection || null == this.Command) {
                throw new ArgumentNullException(
                    "Cvsroot, working directory, connection and command cannot be null.");
            } 
            if (Logger.IsDebugEnabled) {
                string msg = "In validate. " +
                    ";  Cvsroot=[" + this.WorkingDirectory.CvsRoot + "]" +
                    ";  Local directory=[" + this.WorkingDirectory.LocalDirectory + "]" + 
                    ";  Working directory name=[" + this.WorkingDirectory.WorkingDirectoryName + "]" +
                    "Command=[" + this.Command + "]";
                Logger.Info(msg);
            }
        }

        #endregion Private Instance Methods
    }
}